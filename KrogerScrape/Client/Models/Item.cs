using System.Collections.Generic;

namespace KrogerScrape.Client
{
    public class Item
    {
        public string ItemIdentifier { get; set; }
        public string BaseUpc { get; set; }
        public string Department { get; set; }
        public string UnitOfMeasure { get; set; }
        public ItemDetail Detail { get; set; }
        public decimal? Quantity { get; set; }
        public decimal? TotalSavings { get; set; }
        public decimal? ExtendedPrice { get; set; }
        public decimal? PricePaid { get; set; }
        public List<PriceModifier> PriceModifiers { get; set; }
        public decimal? UnitPrice { get; set; }
        public string RicDescription { get; set; }
        public string RicShortDescription { get; set; }
        public string ItemType { get; set; }
        public string EntryMethod { get; set; }
        public string ImageUrl { get; set; }
        public string ImageFallbackType { get; set; }
        public string DetailUrl { get; set; }
        public string SellableUpc { get; set; }
        public bool? ContainerDeposit { get; set; }
        public bool? Pharmacy { get; set; }
        public bool? Fuel { get; set; }
        public bool? Weighted { get; set; }
        public bool? GiftCard { get; set; }
        public bool? ManagerOverrideIgnored { get; set; }
        public bool? ManagerOverride { get; set; }
    }
}
