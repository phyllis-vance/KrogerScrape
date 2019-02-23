using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace KrogerScrape.Client
{
    public class Receipt
    {
        private List<JToken> _tenderChanges;
        private List<JToken> _priceModifiers;
        private List<JToken> _tags;

        public ReceiptAddress Address { get; set; }
        public List<Item> Items { get; set; }
        public decimal? TotalSavings { get; set; }
        public int? TotalLineItems { get; set; }
        public string LoyaltyId { get; set; }
        public ReceiptId ReceiptId { get; set; }
        public List<Tax> Tax { get; set; }
        public List<Tender> Tenders { get; set; }

        public List<JToken> TenderChanges
        {
            get => _tenderChanges;
            set => ModelHelper.SetIfNullOrEmpty(ref _tenderChanges, value);
        }

        public decimal? Total { get; set; }
        public decimal? Subtotal { get; set; }
        public decimal? TotalTax { get; set; }
        public string FulfillmentType { get; set; }

        public List<JToken> PriceModifiers
        {
            get => _priceModifiers;
            set => ModelHelper.SetIfNullOrEmpty(ref _priceModifiers, value);
        }

        public List<JToken> Tags
        {
            get => _tags;
            set => ModelHelper.SetIfNullOrEmpty(ref _tags, value);
        }

        public GrossAmount GrossAmount { get; set; }
        public Coupon Coupon { get; set; }
        public string Source { get; set; }
        public string Version { get; set; }
        public DateTime? TransactionTime { get; set; }
        public DateTimeOffset? TransactionTimeWithTimezone { get; set; }
        public decimal? TotalTender { get; set; }
        public decimal? TotalTenderChange { get; set; }
    }
}
