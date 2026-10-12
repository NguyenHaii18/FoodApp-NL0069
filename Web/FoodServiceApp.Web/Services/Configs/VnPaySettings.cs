namespace FoodServiceApp.Web.Services.Configs
{
    public class VnPaySettings
    {
        public string TmnCode { get; set; } = string.Empty;
        public string HashSecret { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";
        public string ReturnUrl { get; set; } = string.Empty;
        public string Version { get; set; } = "2.1.0";
        public string CurrCode { get; set; } = "VND";
        public string Locale { get; set; } = "vn";
        public string RefundUrl { get; set; } = "https://sandbox.vnpayment.vn/merchant_webapi/api/transaction";
    }
}
