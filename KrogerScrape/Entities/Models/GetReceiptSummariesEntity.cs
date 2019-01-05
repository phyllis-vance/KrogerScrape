using KrogerScrape.Client;

namespace KrogerScrape.Entities
{
    public class GetReceiptSummariesEntity : OperationEntity
    {
        public GetReceiptSummariesEntity()
        {
            Type = OperationType.GetReceiptSummaries;
        }
    }
}
