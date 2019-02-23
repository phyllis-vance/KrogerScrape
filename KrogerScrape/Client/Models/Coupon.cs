using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace KrogerScrape.Client
{
    public class Coupon
    {
        private List<JToken> _nonAssociatedCoupons;

        public AutomaticCouponTransaction AutomaticCouponTransaction { get; set; }
        public List<CouponValue> CouponValues { get; set; }

        public List<JToken> NonAssociatedCoupons
        {
            get => _nonAssociatedCoupons;
            set => ModelHelper.SetIfNullOrEmpty(ref _nonAssociatedCoupons, value);
        }
    }
}