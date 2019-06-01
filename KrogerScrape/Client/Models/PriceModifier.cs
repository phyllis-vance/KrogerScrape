namespace KrogerScrape.Client
{
    public class PriceModifier
    {
        public string Type { get; set; }
        public decimal? Amount { get; set; }
        public string Action { get; set; }
        public string PromotionId { get; set; }
        public string CouponType { get; set; }
        public string ReportingCode { get; set; }
        public bool? Cancelled { get; set; }
    }
}
