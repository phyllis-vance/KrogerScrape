using System;
using System.ComponentModel.DataAnnotations;
using KrogerScrape.Client;

namespace KrogerScrape.Entities
{
    public class ResponseEntity
    {
        public long Id { get; set; }
        public long OperationEntityId { get; set; }
        [Required]
        public string RequestId { get; set; }
        public RequestType RequestType { get; set; }
        public DateTimeOffset CompletedTimestamp { get; set; }
        [Required]
        public string Method { get; set; }
        [Required]
        public string Url { get; set; }
        public CompressionType CompressionType { get; set; }
        [Required]
        public byte[] Body { get; set; }

        public OperationEntity OperationEntity { get; set; }
    }
}
