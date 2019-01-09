using System;
using System.Net.Http;

namespace KrogerScrape.Client
{
    public class Response
    {
        public Response(
            string requestId,
            OperationType operationType,
            object operationParameters,
            RequestType requestType,
            DateTimeOffset timestamp,
            HttpMethod method,
            string url,
            string body)
        {
            RequestId = requestId;
            OperationType = operationType;
            OperationParameters = operationParameters;
            RequestType = requestType;
            CompletedTimestamp = timestamp;
            Method = method;
            Url = url;
            Body = body;
        }

        public string RequestId { get; }
        public OperationType OperationType { get; }
        public object OperationParameters { get; }
        public RequestType RequestType { get; }
        public DateTimeOffset CompletedTimestamp { get; }
        public HttpMethod Method { get; }
        public string Url { get; }
        public string Body { get; }
    }
}
