using System;

namespace KrogerScrape.Client
{
    public class ShoppingContext
    {
        public ShoppingContextLocation Location { get; set; }
        public string ShipmentType { get; set; }
        public DateTime? LastModifiedDate { get; set; }
    }
}
