using System;
using KrogerScrape.Client;

namespace KrogerScrape.Entities
{
    public class ResponseEntity
    {
        public long Id { get; set; }
        public long OperationEntityId { get; set; }
        public RequestType RequestType { get; set; }
        public DateTimeOffset CompletedTimestamp { get; set; }
        public string Method { get; set; }
        public string Url { get; set; }
        public string Body { get; set; }

        public OperationEntity OperationEntity { get; set; }
    }
}
