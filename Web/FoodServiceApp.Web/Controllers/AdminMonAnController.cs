using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FoodServiceApp.Web.Data;
using FoodServiceApp.Web.Models.Entities;

namespace FoodServiceApp.Web.Controllers
{
    /// <summary>Kiểm duyệt nội dung món ăn: xem theo gian hàng, ẩn/hiện món vi phạm (có lý do + thông báo cho quán). Không sửa giá.</summary>
    [Route("QuanTri/MonAn")]
    [FoodServiceApp.Web.Filters.CoQuyenGhi("ThongBao")]
    public class AdminMonAnController : QuanTriBaseController
    {
        private readonly AppDbContext _db;
        public AdminMonAnController(AppDbContext db) { _db = db; }

        [HttpGet("")]
        public async Task<IActionResult> Index(int? maGianHang, string? tuKhoa, bool? daAn)
        {
            var q = _db.MonAns.Include(m => m.DanhMuc).ThenInclude(d => d!.GianHang).AsQueryable();
            if (maGianHang != null) q = q.Where(m => m.DanhMuc!.MaGianHang == maGianHang);
            if (!string.IsNullOrWhiteSpace(tuKhoa)) q = q.Where(m => m.TenMon.Contains(tuKhoa) || (m.MoTa != null && m.MoTa.Contains(tuKhoa)));
            if (daAn == true) q = q.Where(m => m.LyDoAn != null);
            ViewBag.MaGianHang = maGianHang; ViewBag.TuKhoa = tuKhoa ?? ""; ViewBag.DaAn = daAn == true;
            ViewBag.GianHangs = await _db.GianHangs.OrderBy(g => g.TenCuaHang).Select(g => new { g.MaGianHang, g.TenCuaHang }).ToListAsync();
            return View(await q.OrderByDescending(m => m.MaMonAn).Take(200).ToListAsync());
        }

        [HttpPost("{id:int}/an")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> An(int id, string? lyDo)
        {
            var m = await _db.MonAns.Include(x => x.DanhMuc).ThenInclude(d => d!.GianHang).FirstOrDefaultAsync(x => x.MaMonAn == id);
            if (m == null) return NotFound();
            if (string.IsNullOrWhiteSpace(lyDo)) { TempData["LoiThongBao"] = "Bắt buộc nhập lý do ẩn món."; return RedirectToAction("Index"); }
            m.TinhTrang = "TamNgung";
            m.LyDoAn = lyDo.Trim();
            if (m.DanhMuc?.GianHang != null)
                _db.ThongBaos.Add(new ThongBao { MaTK = m.DanhMuc.GianHang.MaTK, Loai = "HeThong", TieuDe = $"Món \"{m.TenMon}\" bị ẩn", NoiDung = "Lý do: " + m.LyDoAn });
            await _db.SaveChangesAsync();
            TempData["ThongBao"] = $"Đã ẩn món \"{m.TenMon}\".";
            return RedirectToAction("Index", new { maGianHang = m.DanhMuc?.MaGianHang });
        }

        [HttpPost("{id:int}/hien")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Hien(int id)
        {
            var m = await _db.MonAns.Include(x => x.DanhMuc).FirstOrDefaultAsync(x => x.MaMonAn == id);
            if (m == null) return NotFound();
            m.TinhTrang = "ConHang"; m.LyDoAn = null;
            await _db.SaveChangesAsync();
            TempData["ThongBao"] = $"Đã hiện lại món \"{m.TenMon}\".";
            return RedirectToAction("Index", new { maGianHang = m.DanhMuc?.MaGianHang });
        }
    }
}
