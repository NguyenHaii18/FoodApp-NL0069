using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FoodServiceApp.Web.Data;
using FoodServiceApp.Web.Models.ViewModels;
using FoodServiceApp.Web.Services;

namespace FoodServiceApp.Web.Controllers
{
    [Route("AdminAuth")]
    public class AdminAuthController : Controller
    {
        private readonly AppDbContext _db;

        public AdminAuthController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet("Login")]
        public IActionResult Login()
        {
            if (User.Identity?.IsAuthenticated == true && User.IsInRole("QuanTri"))
                return RedirectToAction("Index", "AdminDashboard");

            return View("Login", new LoginViewModel());
        }

        [HttpPost("Login")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View("Login", model);

            // DEMO ONLY: so sánh mật khẩu plaintext — xem ghi chú bảo mật trong README.
            var taiKhoan = await _db.TaiKhoans.FirstOrDefaultAsync(t =>
                t.TenDangNhap == model.TenDangNhap && t.VaiTro == "QuanTri");

            if (taiKhoan == null || !await BaoMat.KhopAsync(_db, HttpContext, model.MatKhau, taiKhoan))
            {
                model.LoiThongBao = BaoMat.ThongBaoKhoa(taiKhoan) ?? "Tên đăng nhập hoặc mật khẩu không đúng.";
                return View("Login", model);
            }

            if (!taiKhoan.TrangThai)
            {
                model.LoiThongBao = "Tài khoản quản trị này đã bị vô hiệu hoá.";
                return View("Login", model);
            }

            var quanTri = await _db.QuanTris.FirstOrDefaultAsync(q => q.MaTK == taiKhoan.MaTK);
            if (quanTri == null)
            {
                model.LoiThongBao = "Không tìm thấy hồ sơ quản trị viên ứng với tài khoản này.";
                return View("Login", model);
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, taiKhoan.MaTK.ToString()),
                new Claim(ClaimTypes.Name, taiKhoan.TenDangNhap),
                new Claim(ClaimTypes.Role, "QuanTri"),
                new Claim("MaQuanTri", quanTri.MaQuanTri.ToString()),
                new Claim("HoTenQuanTri", quanTri.HoTen),
                new Claim("CapQuyenHan", quanTri.CapQuyenHan),
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

            return RedirectToAction("Index", "AdminDashboard");
        }

        [HttpPost("Logout")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }
    }
}
