using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FoodServiceApp.Web.Data;
using FoodServiceApp.Web.Services.Payment;

namespace FoodServiceApp.Web.Controllers.Api
{
    public record TaoThanhToanRequest(int MaDonHang, long SoTien, string NoiDung);

    [ApiController]
    [Route("api/payment")]
    public class PaymentController : ControllerBase
    {
        private readonly IVnPayService _vnPayService;
        private readonly AppDbContext _db;

        public PaymentController(IVnPayService vnPayService, AppDbContext db)
        {
            _vnPayService = vnPayService;
            _db = db;
        }

        // POST /api/payment/vnpay/tao-url
        // Mobile Khách hàng gọi API này trước, nhận lại paymentUrl rồi mở WebView/trình duyệt sang đó
        [HttpPost("vnpay/tao-url")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "KhachHang")]
        public IActionResult TaoUrlThanhToan(TaoThanhToanRequest request)
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

            var url = _vnPayService.CreatePaymentUrl(new VnPayCreateRequest(
                OrderId: request.MaDonHang,
                AmountVnd: request.SoTien,
                OrderInfo: request.NoiDung,
                IpAddress: ipAddress
            ));

            return Ok(new { paymentUrl = url });
        }

        // GET /api/payment/vnpay/return — VNPay redirect trình duyệt/WebView người dùng về đây
        [HttpGet("vnpay/return")]
        public async Task<IActionResult> VnPayReturn()
        {
            var result = _vnPayService.ValidateCallback(Request.Query);

            if (!result.IsValidSignature)
                return BadRequest(new { message = "Chữ ký không hợp lệ, có thể dữ liệu đã bị thay đổi." });

            return Ok(new
            {
                thanhCong = result.IsSuccess,
                maDonHang = result.OrderId,
                maGiaoDich = result.TransactionId,
                soTien = result.Amount
            });
        }

        // GET /api/payment/vnpay/ipn — VNPay server-to-server gọi ngầm để xác nhận giao dịch
        // (nguồn xác nhận chính thức — dùng cái này để cập nhật CSDL, KHÔNG dùng return ở trên,
        // vì return có thể bị người dùng đóng trình duyệt giữa chừng).
        [HttpGet("vnpay/ipn")]
        public async Task<IActionResult> VnPayIpn()
        {
            var result = _vnPayService.ValidateCallback(Request.Query);

            if (!result.IsValidSignature)
                return Ok(new { RspCode = "97", Message = "Invalid signature" });

            var thanhToan = await _db.ThanhToans.FirstOrDefaultAsync(t => t.MaDonHang == result.OrderId);
            if (thanhToan == null)
                return Ok(new { RspCode = "01", Message = "Order not found" });

            if (thanhToan.TrangThai == "DaThanhToan")
                return Ok(new { RspCode = "02", Message = "Order already confirmed" });

            if (result.IsSuccess)
            {
                thanhToan.TrangThai = "DaThanhToan";
                thanhToan.MaGiaoDich = result.TransactionId;
                thanhToan.NgayThanhToan = DateTime.Now;
                thanhToan.NgayGiaoDichCong = string.IsNullOrEmpty(result.PayDate) ? null : result.PayDate;

                var donHang = await _db.DonHangs.FirstOrDefaultAsync(d => d.MaDonHang == result.OrderId);
                if (donHang != null && donHang.TrangThai == "ChoXacNhan")
                    donHang.TrangThai = "DaXacNhan"; // thanh toán online thành công -> tự động xác nhận đơn
            }
            else
            {
                thanhToan.TrangThai = "ThatBai";
            }

            await _db.SaveChangesAsync();
            return Ok(new { RspCode = "00", Message = "Confirm Success" });
        }
    }
}
