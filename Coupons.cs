namespace Pizza
{
    public class Coupons
    {
        public List<Coupon>? CouponList { get; set; }
    }

    public class Coupon
    {
        public int? CouponID { get; set; }

        public string? CouponText { get; set; }
    }
}