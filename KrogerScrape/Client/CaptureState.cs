﻿using System;
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
        private readonly ConcurrentBag<Task> _tasks = new ConcurrentBag<Task>();
        private readonly ConcurrentBag<Action> _detachActions = new ConcurrentBag<Action>();
        private readonly object _valuesLock = new object();
        private readonly Dictionary<Type, List<object>> _values = new Dictionary<Type, List<object>>();
        private readonly Deserializer _deserializer;
        private readonly OperationType _operationType;
        private readonly object _operationParameters;
        private readonly AsyncBlockingQueue<Response> _queue;
        private readonly Task _dequeueTask;
        private readonly IReadOnlyList<Func<Response, Task>> _listeners;
        private readonly ILogger _logger;

        public CaptureState(
            Deserializer deserializer,
            OperationType operationType,
            object operationParameters,
            IReadOnlyList<Func<Response, Task>> listeners,
            ILogger logger)
        {
            _deserializer = deserializer;
            _operationType = operationType;
            _operationParameters = operationParameters;
            _queue = new AsyncBlockingQueue<Response>();
            _dequeueTask = DequeueAsync();
            _listeners = listeners;
            _logger = logger;
        }

        private async Task DequeueAsync()
        {
            await Task.Yield();

            bool hasItem;
            do
            {
                var result = await _queue.TryDequeueAsync();
                hasItem = result.HasItem;

                if (hasItem)
                {
                    foreach (var listener in _listeners)
                    {
                        await listener(result.Item);
                    }
                }
            }
            while (hasItem);
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
            Capture(
                page,
                RequestType.AuthenticationState,
                HttpMethod.Get,
                "https://www.kroger.com/auth/api/authentication-state",
                x => _deserializer.AuthenticationState(x));
        }

        public void CaptureReceiptSummaryByUserId(Page page)
        {
            Capture(
                page,
                RequestType.ReceiptSummaryByUserId,
                HttpMethod.Get,
                "https://www.kroger.com/mypurchases/api/v1/receipt/summary/by-user-id",
                x => _deserializer.ReceiptSummaries(x));
        }

        public void CaptureReceiptDetail(Page page)
        {
            Capture(
                page,
                RequestType.ReceiptDetail,
                HttpMethod.Post,
                "https://www.kroger.com/mypurchases/api/v1/receipt/details",
                x => _deserializer.Receipt(x));
        }

        public void CaptureSignIn(Page page)
        {
            Capture(
                page,
                RequestType.SignIn,
                HttpMethod.Post,
                "https://www.kroger.com/auth/api/sign-in",
                x => _deserializer.SignInResponse(x));
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

        private void Capture<T>(
            Page page,
            RequestType requestType,
            HttpMethod method,
            string url,
            Func<string, T> deserialize)
        {
            InitializeDeserializedResponseList<T>();
            InitializeDeserializedResponseList<ErrorResponse>();

            EventHandler<ResponseCreatedEventArgs> eventHandler = async (sender, args) =>
            {
                if (args.Response.Request.Method == method
                    && args.Response.Request.Url == url)
                {
                    _logger.LogDebug("Received a response for: {Method} {Url}", method, url);
                    var captureTask = CaptureJsonResponseAsync(requestType, args, deserialize);
                    _tasks.Add(captureTask);
                    await captureTask;
                }
            };

            _detachActions.Add(() => page.Response -= eventHandler);
            page.Response += eventHandler;
        }

        public async Task WaitForCompletionAsync()
        {
            try
            {
                await Task.WhenAll(_tasks);
            }
            finally
            {
                _queue.MarkAsComplete();
                await _dequeueTask;
            }
        }

        public void Dispose()
        {
            foreach (var action in _detachActions)
            {
                action();
            }
        }

        private async Task CaptureJsonResponseAsync<T>(
            RequestType requestType,
            ResponseCreatedEventArgs args,
            Func<string, T> deserialize)
        {
            await Task.Yield();

            var body = await args.Response.TextAsync();

            var requestId = $"{DateTimeOffset.UtcNow.Ticks:D20}-{Guid.NewGuid():N}";

            _queue.Enqueue(new Response(
                requestId,
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
                    requestId,
                    deserialize(body));
            }
            catch (JsonException)
            {
                try
                {
                    AddDeserializedResponse(
                        requestId,
                        _deserializer.ErrorResponse(body));

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
