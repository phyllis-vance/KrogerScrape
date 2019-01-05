namespace KrogerScrape.Client
{
    public class Tender
    {
        public string TenderType { get; set; }
        public decimal? TenderAmount { get; set; }
        public int? ReasonCode { get; set; }
        public string ReferenceCode { get; set; }
    }
}
