using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FoodServiceApp.Web.Services;

namespace FoodServiceApp.Web.Controllers
{
    /// <summary>Quên mật khẩu bằng OTP — dùng chung cho Khách hàng, Gian hàng, Tài xế, Quản trị.</summary>
    [AllowAnonymous]
    [Route("QuenMatKhau")]
    public class QuenMatKhauController : Controller
    {
        private readonly TaiKhoanService _tk;
        public QuenMatKhauController(TaiKhoanService tk) { _tk = tk; }

        [HttpGet("")]
        public IActionResult Index() => View();

        [HttpPost("")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(string? taiKhoan)
        {
            var kq = await _tk.YeuCauQuenMatKhauAsync(taiKhoan);
            if (!kq.Ok) { ViewBag.Loi = kq.ThongDiep; return View(); }
            TempData["ThongBao"] = kq.ThongDiep;
            if (kq.MaDev != null) TempData["MaDev"] = kq.MaDev;   // chỉ khi chưa cấu hình SMTP và Otp:HienMaDev = true
            return RedirectToAction(nameof(DatLai), new { taiKhoan });
        }

        [HttpGet("DatLai")]
        public IActionResult DatLai(string? taiKhoan) { ViewBag.TaiKhoan = taiKhoan; return View(); }

        [HttpPost("DatLai")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DatLai(string? taiKhoan, string? otp, string? matKhauMoi, string? xacNhan)
        {
            ViewBag.TaiKhoan = taiKhoan;
            if (matKhauMoi != xacNhan) { ViewBag.Loi = "Xác nhận mật khẩu không khớp."; return View(); }
            var kq = await _tk.DatLaiMatKhauAsync(taiKhoan, otp, matKhauMoi);
            if (!kq.Ok) { ViewBag.Loi = kq.ThongDiep; return View(); }
            TempData["ThongBao"] = kq.ThongDiep;
            return Redirect("/");
        }
    }
}
