using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FoodServiceApp.Web.Data;
using FoodServiceApp.Web.Models.Entities;
using FoodServiceApp.Web.Models.ViewModels;
using FoodServiceApp.Web.Services;

namespace FoodServiceApp.Web.Controllers
{
    public class AuthController : Controller
    {
        private readonly AppDbContext _db;

        public AuthController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Dashboard");

            return View("Login", new LoginViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View("Login", model);

            // Mật khẩu được hash bằng BCrypt (xem MatKhauHelper); vẫn tương thích ngược với
            // dữ liệu mẫu cũ chưa hash để không phá vỡ tài khoản demo có sẵn.
            var taiKhoan = await _db.TaiKhoans.FirstOrDefaultAsync(t =>
                t.TenDangNhap == model.TenDangNhap && t.VaiTro == "GianHang");

            if (taiKhoan == null || !await BaoMat.KhopAsync(_db, HttpContext, model.MatKhau, taiKhoan))
            {
                model.LoiThongBao = BaoMat.ThongBaoKhoa(taiKhoan) ?? "Tên đăng nhập hoặc mật khẩu không đúng.";
                return View("Login", model);
            }

            if (!taiKhoan.TrangThai)
            {
                model.LoiThongBao = "Tài khoản của bạn đã bị khóa. Vui lòng liên hệ quản trị viên.";
                return View("Login", model);
            }

            var gianHang = await _db.GianHangs.FirstOrDefaultAsync(g => g.MaTK == taiKhoan.MaTK);
            if (gianHang == null)
            {
                model.LoiThongBao = "Không tìm thấy thông tin gian hàng ứng với tài khoản này.";
                return View("Login", model);
            }

            if (gianHang.TrangThaiDuyet == "TuChoi")
            {
                model.LoiThongBao = "Đơn đăng ký gian hàng của bạn đã bị từ chối. Vui lòng liên hệ quản trị viên để biết thêm chi tiết.";
                return View("Login", model);
            }

            await DangNhapAsync(taiKhoan, gianHang);

            if (gianHang.TrangThaiDuyet != "DaDuyet")
                return RedirectToAction("ChoDuyet", "Dashboard");

            return RedirectToAction("Index", "Dashboard");
        }

        [HttpGet]
        public IActionResult DangKy() => View(new VendorRegisterViewModel());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DangKy(VendorRegisterViewModel model)
        {
            if (model.MatKhau != model.XacNhanMatKhau)
                ModelState.AddModelError(nameof(model.XacNhanMatKhau), "Xác nhận mật khẩu không khớp.");

            if (await _db.TaiKhoans.AnyAsync(t => t.TenDangNhap == model.TenDangNhap))
                ModelState.AddModelError(nameof(model.TenDangNhap), "Tên đăng nhập đã tồn tại.");

            if (!ModelState.IsValid) return View(model);

            var taiKhoan = new TaiKhoan
            {
                TenDangNhap = model.TenDangNhap,
                MatKhau = MatKhauHelper.Hash(model.MatKhau),
                VaiTro = "GianHang",
            };
            _db.TaiKhoans.Add(taiKhoan);
            await _db.SaveChangesAsync();

            var gianHang = new GianHang
            {
                MaTK = taiKhoan.MaTK,
                TenCuaHang = model.TenCuaHang,
                DiaChi = model.DiaChi,
                SDT = model.SDT,
                MoTa = model.MoTa,
                TrangThaiKinhDoanh = "TamNgung", // chưa mở bán cho tới khi được duyệt
                TrangThaiDuyet = "ChoDuyet",
                TyLeChietKhau = await FoodServiceApp.Web.Services.CauHinhService.HoaHongMacDinhAsync(_db),
            };
            _db.GianHangs.Add(gianHang);
            await _db.SaveChangesAsync();

            await DangNhapAsync(taiKhoan, gianHang);
            return RedirectToAction("ChoDuyet", "Dashboard");
        }

        private async Task DangNhapAsync(TaiKhoan taiKhoan, GianHang gianHang)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, taiKhoan.MaTK.ToString()),
                new Claim(ClaimTypes.Name, taiKhoan.TenDangNhap),
                new Claim(ClaimTypes.Role, "GianHang"),
                new Claim("MaGianHang", gianHang.MaGianHang.ToString()),
                new Claim("TenCuaHang", gianHang.TenCuaHang),
                new Claim("DiaChi", gianHang.DiaChi),
                new Claim("TrangThaiDuyet", gianHang.TrangThaiDuyet),
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }
    }
}
