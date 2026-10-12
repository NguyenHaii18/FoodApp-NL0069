using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FoodServiceApp.Web.Data;
using FoodServiceApp.Web.Filters;
using FoodServiceApp.Web.Models.Entities;

namespace FoodServiceApp.Web.Controllers
{
    /// <summary>Nhật ký thao tác & đăng nhập — chỉ Admin tổng xem được.</summary>
    [Route("QuanTri/NhatKy")]
    public class AdminNhatKyController : QuanTriBaseController
    {
        private readonly AppDbContext _db;
        public AdminNhatKyController(AppDbContext db) { _db = db; }

        [HttpGet("")]
        public async Task<IActionResult> Index(string? nguon, string? mucDo, string? tuKhoa, int trang = 1)
        {
            if (!await ChiSuperAdminGhiAttribute.LaSuperAdminAsync(HttpContext)) return Forbid();
            const int kichThuoc = 50;
            var q = _db.NhatKyHeThongs.AsQueryable();
            if (!string.IsNullOrWhiteSpace(nguon)) q = q.Where(n => n.Nguon == nguon);
            if (!string.IsNullOrWhiteSpace(mucDo)) q = q.Where(n => n.MucDo == mucDo);
            if (!string.IsNullOrWhiteSpace(tuKhoa)) q = q.Where(n => n.ThongDiep.Contains(tuKhoa) || (n.DiaChiIP != null && n.DiaChiIP.Contains(tuKhoa)));
            trang = Math.Max(1, trang);
            var tong = await q.CountAsync();
            var ds = await q.OrderByDescending(n => n.ThoiGian).Skip((trang - 1) * kichThuoc).Take(kichThuoc).ToListAsync();
            var maTKs = ds.Where(n => n.MaTK != null).Select(n => n.MaTK!.Value).Distinct().ToList();
            ViewBag.Ten = await _db.TaiKhoans.Where(t => maTKs.Contains(t.MaTK)).ToDictionaryAsync(t => t.MaTK, t => t.TenDangNhap);
            ViewBag.Nguon = nguon; ViewBag.MucDo = mucDo; ViewBag.TuKhoa = tuKhoa;
            ViewBag.Trang = trang; ViewBag.TongTrang = (int)Math.Ceiling(tong / (double)kichThuoc);
            return View(ds);
        }
    }
}
