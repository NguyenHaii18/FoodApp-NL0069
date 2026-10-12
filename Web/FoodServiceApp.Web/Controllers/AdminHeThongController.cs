using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FoodServiceApp.Web.Data;

namespace FoodServiceApp.Web.Controllers
{
    /// <summary>Vận hành: tình trạng SQL Server/API, log lỗi, trạng thái tích hợp (bản đồ, email, SMS, thanh toán), sao lưu CSDL, quản lý phiên đăng nhập.</summary>
    [Route("QuanTri/HeThong")]
    [FoodServiceApp.Web.Filters.ChiSuperAdminGhi]
    public class AdminHeThongController : AdminOpsBase
    {
        private readonly IConfiguration _cfg;
        public AdminHeThongController(AppDbContext db, IConfiguration cfg) : base(db) { _cfg = cfg; }

        private static string Che(string? v) => string.IsNullOrWhiteSpace(v) || v.StartsWith("DIEN_") || v.StartsWith("DOI_") ? "chưa cấu hình" : "đã cấu hình (" + (v.Length > 6 ? v[..3] + "***" : "***") + ")";

        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            bool sqlOk; string? sqlLoi = null;
            try { sqlOk = await Db.Database.CanConnectAsync(); } catch (Exception ex) { sqlOk = false; sqlLoi = ex.Message; }
            sw.Stop();
            ViewBag.SqlOk = sqlOk; ViewBag.SqlMs = sw.ElapsedMilliseconds; ViewBag.SqlLoi = sqlLoi;
            ViewBag.TenDb = sqlOk ? Db.Database.GetDbConnection().Database : "";

            var h = DateTime.Now.AddHours(-1);
            if (sqlOk)
            {
                ViewBag.ReqGio = await Db.NhatKyHeThongs.CountAsync(n => n.ThoiGian >= h);
                ViewBag.Loi24h = await Db.NhatKyHeThongs.CountAsync(n => n.MucDo == "Error" && n.ThoiGian >= DateTime.Now.AddHours(-24));
                ViewBag.MsTB = await Db.NhatKyHeThongs.Where(n => n.ThoiGianMs != null && n.ThoiGian >= h).Select(n => (double?)n.ThoiGianMs).AverageAsync() ?? 0;
                ViewBag.Loi = await Db.NhatKyHeThongs.Where(n => n.MucDo == "Error").OrderByDescending(n => n.ThoiGian).Take(30).ToListAsync();
                ViewBag.Phien = await Db.PhienDangNhaps.CountAsync(p => p.ThuHoiLuc == null);
            }
            ViewBag.TichHop = new (string Ten, string TrangThai)[]
            {
                ("Bản đồ Goong", Che(_cfg["GoongMaps:ApiKey"])),
                ("VNPay (TmnCode)", Che(_cfg["VnPay:TmnCode"])),
                ("VNPay (HashSecret)", Che(_cfg["VnPay:HashSecret"])),
                ("Email SMTP", string.IsNullOrWhiteSpace(_cfg["Smtp:Host"]) ? "chưa cấu hình (OTP hiển thị chế độ demo)" : "đã cấu hình (" + _cfg["Smtp:Host"] + ")"),
                ("SMS", string.IsNullOrWhiteSpace(_cfg["Sms:Url"]) ? "chưa tích hợp (OTP SMS dùng chế độ demo)" : "đã cấu hình"),
                ("JWT khóa bí mật", Che(_cfg["Jwt:Key"])),
            };
            ViewBag.ThuMucBackup = _cfg["Backup:ThuMuc"] ?? @"C:\FoodServiceBackup";
            return View();
        }

        /// <summary>Sao lưu: BACKUP DATABASE ... TO DISK (thư mục cấu hình Backup:ThuMuc trên máy chạy SQL Server).</summary>
        [HttpPost("sao-luu")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaoLuu()
        {
            var db = Db.Database.GetDbConnection().Database;
            if (!System.Text.RegularExpressions.Regex.IsMatch(db, @"^[A-Za-z0-9_]+$")) { TempData["LoiThongBao"] = "Tên CSDL không hợp lệ."; return RedirectToAction(nameof(Index)); }
            var thuMuc = (_cfg["Backup:ThuMuc"] ?? @"C:\FoodServiceBackup").TrimEnd('\\', '/');
            var tep = $"{thuMuc}\\{db}_{DateTime.Now:yyyyMMdd_HHmmss}.bak";
            try
            {
                await Db.Database.SetCommandTimeout(300).ExecuteSqlRawAsync("BACKUP DATABASE [" + db + "] TO DISK = {0} WITH INIT", tep);
                TempData["ThongBao"] = "Đã sao lưu: " + tep;
            }
            catch (Exception ex)
            {
                TempData["LoiThongBao"] = "Sao lưu thất bại (thư mục phải tồn tại và SQL Server có quyền ghi): " + ex.Message;
            }
            return RedirectToAction(nameof(Index));
        }

        // ===== Quản lý phiên đăng nhập =====
        [HttpGet("phien")]
        public async Task<IActionResult> Phien()
        {
            var ds = await (from p in Db.PhienDangNhaps join t in Db.TaiKhoans on p.MaTK equals t.MaTK
                            where p.ThuHoiLuc == null orderby p.LanCuoi descending
                            select new PhienRow(p.MaPhien, t.TenDangNhap, t.VaiTro, p.Kenh, p.ThietBi, p.DiaChiIP, p.NgayTao, p.LanCuoi)).Take(200).ToListAsync();
            return View(ds);
        }
        public record PhienRow(Guid MaPhien, string TenDangNhap, string VaiTro, string Kenh, string? ThietBi, string? DiaChiIP, DateTime NgayTao, DateTime LanCuoi);

        [HttpPost("phien/thu-hoi/{id:guid}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ThuHoi(Guid id)
        {
            var p = await Db.PhienDangNhaps.FirstOrDefaultAsync(x => x.MaPhien == id && x.ThuHoiLuc == null);
            if (p == null) return NotFound();
            p.ThuHoiLuc = DateTime.Now;
            await Db.SaveChangesAsync();
            TempData["ThongBao"] = "Đã thu hồi phiên (người dùng bị đăng xuất ở lần truy cập kế tiếp).";
            return RedirectToAction(nameof(Phien));
        }

        [HttpPost("phien/thu-hoi-tai-khoan")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ThuHoiTaiKhoan(string tenDangNhap)
        {
            var tk = await Db.TaiKhoans.FirstOrDefaultAsync(t => t.TenDangNhap == (tenDangNhap ?? "").Trim());
            if (tk == null) { TempData["LoiThongBao"] = "Không tìm thấy tài khoản."; return RedirectToAction(nameof(Phien)); }
            var ds = await Db.PhienDangNhaps.Where(p => p.MaTK == tk.MaTK && p.ThuHoiLuc == null).ToListAsync();
            foreach (var p in ds) p.ThuHoiLuc = DateTime.Now;
            await Db.SaveChangesAsync();
            TempData["ThongBao"] = $"Đã thu hồi {ds.Count} phiên của {tk.TenDangNhap}.";
            return RedirectToAction(nameof(Phien));
        }
    }
}
