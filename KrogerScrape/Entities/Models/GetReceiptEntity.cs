using KrogerScrape.Client;

namespace KrogerScrape.Entities
{
    public class GetReceiptEntity : OperationEntity
    {
        public GetReceiptEntity()
        {
            Type = OperationType.GetReceipt;
        }

        public long ReceiptEntityId { get; set; }

        public ReceiptIdEntity ReceiptEntity { get; set; }
    }
}
