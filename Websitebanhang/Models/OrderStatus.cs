namespace Websitebanhang.Models
{
    public static class OrderStatus
    {
        public const string Pending = "Pending";
        public const string Confirmed = "Confirmed";
        public const string Preparing = "Preparing";
        public const string Shipping = "Shipping";
        public const string Delivered = "Delivered";
        public const string ReturnRequested = "ReturnRequested"; // user requested return
        public const string ReturnApproved = "ReturnApproved"; // admin approved return but not refunded (COD)
        public const string Returned = "Returned"; // final returned state
        public const string Cancelled = "Cancelled";
        public const string Refunded = "Refunded"; // completed refund
    }
}