namespace KrogerScrape.Client
{
    public class PriceModifier
    {
        public string Type { get; set; }
        public decimal? Amount { get; set; }
        public string Action { get; set; }
        public string PromotionId { get; set; }
    }
}
