using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FoodServiceApp.Web.Data;
using FoodServiceApp.Web.Models.Entities;
using FoodServiceApp.Web.Services;

namespace FoodServiceApp.Web.Controllers
{
    /// <summary>Bảo mật tài khoản cho người đang đăng nhập (mọi vai trò): xác thực email/SĐT, lịch sử đăng nhập, ảnh đại diện, xóa tài khoản.</summary>
    [Authorize]
    [Route("BaoMat")]
    public class BaoMatController : Controller
    {
        private readonly AppDbContext _db;
        private readonly TaiKhoanService _tk;
        private readonly IFileStorageService _storage;

        public BaoMatController(AppDbContext db, TaiKhoanService tk, IFileStorageService storage)
        { _db = db; _tk = tk; _storage = storage; }

        private int MaTK => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        private Guid? MaPhienHienTai => Guid.TryParse(User.FindFirstValue("MaPhien"), out var g) ? g : null;

        [HttpPost("ThuHoiPhien")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ThuHoiPhien(Guid id)
        {
            var p = await _db.PhienDangNhaps.FirstOrDefaultAsync(x => x.MaPhien == id && x.MaTK == MaTK && x.ThuHoiLuc == null);
            if (p == null) { TempData["Loi"] = "Không tìm thấy phiên."; return RedirectToAction(nameof(Index)); }
            p.ThuHoiLuc = DateTime.Now;
            await _db.SaveChangesAsync();
            if (p.MaPhien == MaPhienHienTai)
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return Redirect("/");
            }
            TempData["ThongBao"] = "Đã đăng xuất thiết bị đó.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost("DangXuatThietBiKhac")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DangXuatThietBiKhac()
        {
            var hienTai = MaPhienHienTai;
            var ds = await _db.PhienDangNhaps.Where(x => x.MaTK == MaTK && x.ThuHoiLuc == null && x.MaPhien != hienTai).ToListAsync();
            foreach (var p in ds) p.ThuHoiLuc = DateTime.Now;
            await _db.SaveChangesAsync();
            TempData["ThongBao"] = $"Đã đăng xuất {ds.Count} thiết bị khác.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var tk = await _db.TaiKhoans.FindAsync(MaTK);
            if (tk == null) return RedirectToAction("Logout");
            var (email, sdt) = await _tk.LienHeAsync(tk);
            ViewBag.TaiKhoan = tk; ViewBag.Email = email; ViewBag.Sdt = sdt;
            ViewBag.LichSu = await _tk.LichSuDangNhapAsync(MaTK);
            ViewBag.Phien = await _db.PhienDangNhaps.Where(p => p.MaTK == MaTK && p.ThuHoiLuc == null)
                .OrderByDescending(p => p.LanCuoi).Take(20).ToListAsync();
            ViewBag.PhienHienTai = MaPhienHienTai;
            ViewBag.CoAnh = tk.VaiTro is "KhachHang" or "TaiXe";
            ViewBag.Anh = tk.VaiTro == "KhachHang"
                ? (await _db.KhachHangs.Where(k => k.MaTK == MaTK).Select(k => k.AnhDaiDien).FirstOrDefaultAsync())
                : tk.VaiTro == "TaiXe" ? (await _db.TaiXes.Where(t => t.MaTK == MaTK).Select(t => t.AnhDaiDien).FirstOrDefaultAsync()) : null;
            return View();
        }

        [HttpPost("GuiOtp")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GuiOtp(string kenh)
        {
            var kq = await _tk.GuiXacThucAsync(MaTK, kenh == "sms" ? "sms" : "email");
            TempData[kq.Ok ? "ThongBao" : "Loi"] = kq.ThongDiep;
            if (kq.MaDev != null) TempData["MaDev"] = kq.MaDev;
            return RedirectToAction(nameof(Index));
        }

        [HttpPost("XacNhan")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> XacNhan(string kenh, string? otp)
        {
            var kq = await _tk.XacNhanXacThucAsync(MaTK, kenh == "sms" ? "sms" : "email", otp);
            TempData[kq.Ok ? "ThongBao" : "Loi"] = kq.ThongDiep;
            return RedirectToAction(nameof(Index));
        }

        [HttpPost("AnhDaiDien")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AnhDaiDien(IFormFile? anh)
        {
            if (anh == null || anh.Length == 0) { TempData["Loi"] = "Chưa chọn ảnh."; return RedirectToAction(nameof(Index)); }
            if (anh.Length > 3 * 1024 * 1024 || !anh.ContentType.StartsWith("image/")) { TempData["Loi"] = "Chỉ nhận ảnh tối đa 3MB."; return RedirectToAction(nameof(Index)); }
            var url = await _storage.SaveFileAsync(anh, "avatar");
            var k = await _db.KhachHangs.FirstOrDefaultAsync(x => x.MaTK == MaTK);
            if (k != null) k.AnhDaiDien = url;
            else
            {
                var t = await _db.TaiXes.FirstOrDefaultAsync(x => x.MaTK == MaTK);
                if (t != null) t.AnhDaiDien = url; else { TempData["Loi"] = "Vai trò này không có ảnh đại diện."; return RedirectToAction(nameof(Index)); }
            }
            await _db.SaveChangesAsync();
            TempData["ThongBao"] = "Đã cập nhật ảnh đại diện.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost("Xoa")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Xoa(string? matKhau)
        {
            var kq = await _tk.XoaTaiKhoanAsync(MaTK, matKhau);
            if (!kq.Ok) { TempData["Loi"] = kq.ThongDiep; return RedirectToAction(nameof(Index)); }
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Redirect("/");
        }
    }
}
