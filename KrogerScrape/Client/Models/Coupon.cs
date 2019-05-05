using System.Collections.Generic;

namespace KrogerScrape.Client
{
    public class Coupon
    {
        public AutomaticCouponTransaction AutomaticCouponTransaction { get; set; }
        public List<CouponValue> CouponValues { get; set; }
        public List<NonAssociatedCoupon> NonAssociatedCoupons { get; set; }
    }
}