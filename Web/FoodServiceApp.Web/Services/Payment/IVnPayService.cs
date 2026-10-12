using Microsoft.AspNetCore.Http;

namespace FoodServiceApp.Web.Services.Payment
{
    public record VnPayCreateRequest(int OrderId, long AmountVnd, string OrderInfo, string IpAddress, string? ReturnUrl = null);

    public record VnPayCallbackResult(bool IsValidSignature, bool IsSuccess, int OrderId, string TransactionId, long Amount, string PayDate = "");

    /// <summary>Kết quả gọi API hoàn tiền: ChuaCauHinh = chưa điền TmnCode/HashSecret thật nên không gọi cổng.</summary>
    public record VnPayRefundResult(bool Ok, bool ChuaCauHinh, string ThongDiep, string? MaGiaoDichHoan = null);

    public interface IVnPayService
    {
        /// <summary>Tạo URL thanh toán để redirect người dùng sang cổng VNPay.</summary>
        string CreatePaymentUrl(VnPayCreateRequest request);

        /// <summary>Xác thực dữ liệu trả về từ VNPay (ReturnUrl hoặc IPN) bằng chữ ký HMAC-SHA512.</summary>
        VnPayCallbackResult ValidateCallback(IQueryCollection query);

        /// <summary>Gọi API hoàn tiền VNPay (refund). Toàn phần = 02, một phần = 03.</summary>
        Task<VnPayRefundResult> RefundAsync(int orderId, long amountVnd, long tongTienGoc, string maGiaoDichCong, string ngayGiaoDichCong, string nguoiThucHien, string lyDo, string ip);
    }
}
