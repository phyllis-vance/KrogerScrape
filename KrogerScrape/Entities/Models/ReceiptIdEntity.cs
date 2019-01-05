using System.Collections.Generic;

namespace KrogerScrape.Entities
{
    public class ReceiptIdEntity
    {
        public long Id { get; set; }
        public long UserEntityId { get; set; }
        public string DivisionNumber { get; set; }
        public string StoreNumber { get; set; }
        public string TransactionDate { get; set; }
        public string TerminalNumber { get; set; }
        public string TransactionId { get; set; }

        public UserEntity UserEntity { get; set; }
        public List<GetReceiptEntity> GetReceiptOperationEntities { get; set; }
    }
}
