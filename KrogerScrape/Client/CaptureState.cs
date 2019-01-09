using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using KrogerScrape.Support;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PuppeteerSharp;

namespace KrogerScrape.Client
{
    public class CaptureState : IDisposable
    {
        private static readonly JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings
        {
            DateTimeZoneHandling = DateTimeZoneHandling.Unspecified,
            DateParseHandling = DateParseHandling.DateTime,
            MissingMemberHandling = MissingMemberHandling.Error,
        };

        private readonly ConcurrentBag<Task> _tasks = new ConcurrentBag<Task>();
        private readonly ConcurrentBag<Action> _detachActions = new ConcurrentBag<Action>();
        private readonly object _valuesLock = new object();
        private readonly Dictionary<Type, List<object>> _values = new Dictionary<Type, List<object>>();
        private readonly OperationType _operationType;
        private readonly object _operationParameters;
        private readonly AsyncBlockingQueue<Response> _queue;
        private readonly ILogger _logger;

        public CaptureState(
            OperationType operationType,
            object operationParameters,
            AsyncBlockingQueue<Response> queue,
            ILogger logger)
        {
            _operationType = operationType;
            _operationParameters = operationParameters;
            _queue = queue;
            _logger = logger;
        }

        public List<DeserializedResponse<T>> GetValues<T>()
        {
            var type = typeof(DeserializedResponse<T>);
            lock (_valuesLock)
            {
                if (_values.TryGetValue(type, out var values))
                {
                    return values.Cast<DeserializedResponse<T>>().ToList();
                }
                else
                {
                    return new List<DeserializedResponse<T>>();
                }
            }
        }

        public void CaptureAuthenticationState(Page page)
        {
            Capture<AuthenticationState>(
                page,
                RequestType.AuthenticationState,
                HttpMethod.Get,
                "https://www.kroger.com/auth/api/authentication-state");
        }

        public void CaptureReceiptSummaryByUserId(Page page)
        {
            Capture<List<Receipt>>(
                page,
                RequestType.ReceiptSummaryByUserId,
                HttpMethod.Get,
                "https://www.kroger.com/mypurchases/api/v1/receipt/summary/by-user-id");
        }

        public void CaptureReceiptDetail(Page page)
        {
            Capture<Receipt>(
                page,
                RequestType.ReceiptDetail,
                HttpMethod.Post,
                "https://www.kroger.com/mypurchases/api/v1/receipt/detail");
        }

        public void CaptureSignIn(Page page)
        {
            Capture<SignInResponse>(
                page,
                RequestType.SignIn,
                HttpMethod.Post,
                "https://www.kroger.com/auth/api/sign-in");
        }

        private void InitializeDeserializedResponseList<T>()
        {
            var type = typeof(DeserializedResponse<T>);
            lock (_valuesLock)
            {
                if (!_values.ContainsKey(type))
                {
                    _values[type] = new List<object>();
                }
            }
        }

        private void AddDeserializedResponse<T>(string requestId, T value)
        {
            lock (_values)
            {
                _values[typeof(DeserializedResponse<T>)].Add(new DeserializedResponse<T>
                {
                    RequestId = requestId,
                    Response = value,
                });
            }
        }

        private void Capture<T>(Page page, RequestType requestType, HttpMethod method, string url)
        {
            InitializeDeserializedResponseList<T>();
            InitializeDeserializedResponseList<ErrorResponse>();

            EventHandler<ResponseCreatedEventArgs> eventHandler = (sender, args) =>
            {
                if (args.Response.Request.Method == method
                    && args.Response.Request.Url == url)
                {
                    _logger.LogDebug("Received a response for {Method} {Url}", method, url);
                    _tasks.Add(CaptureJsonResponseAsync<T>(requestType, args));
                }
            };

            _detachActions.Add(() => page.Response -= eventHandler);
            page.Response += eventHandler;
        }

        public async Task WaitForCompletionAsync()
        {
            await Task.WhenAll(_tasks);
        }

        public void Dispose()
        {
            foreach (var action in _detachActions)
            {
                action();
            }
        }

        private async Task CaptureJsonResponseAsync<T>(RequestType requestType, ResponseCreatedEventArgs args)
        {
            await Task.Yield();

            var body = await args.Response.TextAsync();

            _queue.Enqueue(new Response(
                args.Response.Request.RequestId,
                _operationType,
                _operationParameters,
                requestType,
                DateTimeOffset.UtcNow,
                args.Response.Request.Method,
                args.Response.Request.Url,
                body));

            try
            {
                AddDeserializedResponse(
                    args.Response.Request.RequestId,
                    JsonConvert.DeserializeObject<T>(body, JsonSerializerSettings));
            }
            catch (JsonSerializationException)
            {
                try
                {
                    AddDeserializedResponse(
                        args.Response.Request.RequestId,
                        JsonConvert.DeserializeObject<ErrorResponse>(body, JsonSerializerSettings));

                    var prettyJson = JObject.Parse(body).ToString(Formatting.Indented);
                    _logger.LogError(
                        $"An error was returned by Kroger.com:{Environment.NewLine}{{ErrorJson}}",
                        prettyJson);
                }
                catch
                {
                    // Ignore these failures.
                }

                throw;
            }
        }
    }
}
