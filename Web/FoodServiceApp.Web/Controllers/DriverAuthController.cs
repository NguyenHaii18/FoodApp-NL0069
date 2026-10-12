using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FoodServiceApp.Web.Data;
using FoodServiceApp.Web.Models.ViewModels;
using FoodServiceApp.Web.Models.Entities;
using FoodServiceApp.Web.Services;

namespace FoodServiceApp.Web.Controllers
{
    [Route("DriverAuth")]
    public class DriverAuthController : Controller
    {
        private readonly AppDbContext _db;

        public DriverAuthController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet("Login")]
        public IActionResult Login()
        {
            if (User.Identity?.IsAuthenticated == true && User.IsInRole("TaiXe"))
                return RedirectToAction("Index", "DriverDashboard");

            return View("Login", new LoginViewModel());
        }

        [HttpPost("Login")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View("Login", model);

            var taiKhoan = await _db.TaiKhoans.FirstOrDefaultAsync(t =>
                t.TenDangNhap == model.TenDangNhap && t.VaiTro == "TaiXe");

            if (taiKhoan == null || !await BaoMat.KhopAsync(_db, HttpContext, model.MatKhau, taiKhoan))
            {
                model.LoiThongBao = BaoMat.ThongBaoKhoa(taiKhoan) ?? "Tên đăng nhập hoặc mật khẩu không đúng.";
                return View("Login", model);
            }

            if (!taiKhoan.TrangThai)
            {
                model.LoiThongBao = "Tài khoản của bạn đã bị khoá. Vui lòng liên hệ quản trị viên.";
                return View("Login", model);
            }

            var taiXe = await _db.TaiXes.FirstOrDefaultAsync(t => t.MaTK == taiKhoan.MaTK);
            if (taiXe == null)
            {
                model.LoiThongBao = "Không tìm thấy hồ sơ tài xế ứng với tài khoản này.";
                return View("Login", model);
            }

            if (taiXe.TrangThaiDuyet == "ChoDuyet")
            {
                model.LoiThongBao = "Hồ sơ tài xế của bạn đang chờ Quản trị viên duyệt. Vui lòng quay lại sau.";
                return View("Login", model);
            }

            if (taiXe.TrangThaiDuyet == "TuChoi")
            {
                model.LoiThongBao = "Hồ sơ tài xế của bạn đã bị từ chối. Vui lòng liên hệ quản trị viên.";
                return View("Login", model);
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, taiKhoan.MaTK.ToString()),
                new Claim(ClaimTypes.Name, taiKhoan.TenDangNhap),
                new Claim(ClaimTypes.Role, "TaiXe"),
                new Claim("MaTaiXe", taiXe.MaTaiXe.ToString()),
                new Claim("HoTenTaiXe", taiXe.HoTen),
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

            return RedirectToAction("Index", "DriverDashboard");
        }

        [HttpPost("Logout")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        [HttpGet("DangKy")]
        public IActionResult DangKy() => View(new DriverRegisterViewModel());

        [HttpPost("DangKy")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DangKy(DriverRegisterViewModel model)
        {
            if (model.MatKhau != model.XacNhanMatKhau)
                ModelState.AddModelError(nameof(model.XacNhanMatKhau), "Xác nhận mật khẩu không khớp.");

            if (await _db.TaiKhoans.AnyAsync(t => t.TenDangNhap == model.TenDangNhap))
                ModelState.AddModelError(nameof(model.TenDangNhap), "Tên đăng nhập đã tồn tại.");

            if (!ModelState.IsValid) return View(model);

            var taiKhoan = new Models.Entities.TaiKhoan
            {
                TenDangNhap = model.TenDangNhap,
                MatKhau = MatKhauHelper.Hash(model.MatKhau),
                VaiTro = "TaiXe",
            };
            _db.TaiKhoans.Add(taiKhoan);
            await _db.SaveChangesAsync();

            var taiXeMoi = new Models.Entities.TaiXe
            {
                MaTK = taiKhoan.MaTK,
                HoTen = model.HoTen,
                SDT = model.SDT,
                PhuongTien = model.PhuongTien,
                BienSo = model.BienSo,
                KhuVucHoatDong = model.KhuVucHoatDong,
                TrangThai = "NgoaiTuyen",
                TrangThaiDuyet = "ChoDuyet",
            };
            _db.TaiXes.Add(taiXeMoi);
            await _db.SaveChangesAsync();

            TempData["ThongBao"] = "Đăng ký thành công! Hồ sơ của bạn đang chờ Quản trị viên duyệt, vui lòng đăng nhập lại sau.";
            return RedirectToAction("Login");
        }
    }
}
