using System;
using System.Collections.Generic;
using KrogerScrape.Client;

namespace KrogerScrape.Entities
{
    public class OperationEntity
    {
        public OperationEntity()
        {
            Type = OperationType.Uncategorized;
        }

        public long Id { get; set; }
        public OperationType Type { get; set; }
        public DateTimeOffset StartedTimestamp { get; set; }
        public DateTimeOffset? CompletedTimestamp { get; set; }
        public long? ParentId { get; set; }

        public OperationEntity Parent { get; set; }
        public List<OperationEntity> Children { get; set; }
        public List<ResponseEntity> ResponseEntities { get; set; }
    }
}
