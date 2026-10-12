using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FoodServiceApp.Web.Data;
using FoodServiceApp.Web.Models.Entities;

namespace FoodServiceApp.Web.Controllers
{
    /// <summary>Kiểm duyệt đánh giá: xem đánh giá gian hàng/món/tài xế, xử lý báo cáo vi phạm (ẩn hoặc giữ lại), hiện lại đánh giá đã ẩn.</summary>
    [Route("QuanTri/DanhGia")]
    [FoodServiceApp.Web.Filters.CoQuyenGhi("DanhGia")]
    public class AdminDanhGiaController : AdminOpsBase
    {
        public AdminDanhGiaController(AppDbContext db) : base(db) { }

        [HttpGet("")]
        public async Task<IActionResult> Index(string tab = "bao-cao", int? sao = null, int? maGianHang = null)
        {
            ViewBag.Tab = tab;
            if (tab == "bao-cao")
            {
                var bc = await Db.BaoCaoDanhGias.Where(b => b.TrangThai == "ChoXuLy").OrderBy(b => b.NgayTao).ToListAsync();
                var ids = bc.Select(b => b.MaDanhGia).Distinct().ToList();
                ViewBag.DanhGias = await Db.DanhGias.Where(d => ids.Contains(d.MaDanhGia)).ToDictionaryAsync(d => d.MaDanhGia);
                var gh = ((Dictionary<int, DanhGia>)ViewBag.DanhGias).Values.Select(d => d.MaGianHang).Distinct().ToList();
                ViewBag.Quan = await Db.GianHangs.Where(g => gh.Contains(g.MaGianHang)).ToDictionaryAsync(g => g.MaGianHang, g => g.TenCuaHang);
                ViewBag.Dem = await Db.BaoCaoDanhGias.Where(b => ids.Contains(b.MaDanhGia)).GroupBy(b => b.MaDanhGia).Select(g => new { g.Key, N = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.N);
                return View("BaoCao", bc);
            }
            var q = Db.DanhGias.AsQueryable();
            if (sao != null) q = q.Where(d => d.SoSao == sao);
            if (maGianHang != null) q = q.Where(d => d.MaGianHang == maGianHang);
            var ds = await q.OrderByDescending(d => d.NgayDanhGia).Take(200).ToListAsync();
            var ghs = ds.Select(d => d.MaGianHang).Distinct().ToList();
            ViewBag.Quan = await Db.GianHangs.Where(g => ghs.Contains(g.MaGianHang)).ToDictionaryAsync(g => g.MaGianHang, g => g.TenCuaHang);
            var khs = ds.Select(d => d.MaKH).Distinct().ToList();
            ViewBag.Khach = await Db.KhachHangs.Where(k => khs.Contains(k.MaKH)).ToDictionaryAsync(k => k.MaKH, k => k.HoTen);
            ViewBag.Sao = sao; ViewBag.MaGianHang = maGianHang;
            return View("TatCa", ds);
        }

        [HttpPost("an/{maDanhGia:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> An(int maDanhGia, string? lyDo)
        {
            var dg = await Db.DanhGias.FirstOrDefaultAsync(d => d.MaDanhGia == maDanhGia);
            if (dg == null) return NotFound();
            if (string.IsNullOrWhiteSpace(lyDo)) { TempData["LoiThongBao"] = "Nhập lý do ẩn đánh giá."; return RedirectToAction(nameof(Index)); }
            dg.DaAn = true; dg.LyDoAn = lyDo.Trim();
            foreach (var b in await Db.BaoCaoDanhGias.Where(x => x.MaDanhGia == maDanhGia && x.TrangThai == "ChoXuLy").ToListAsync())
            { b.TrangThai = "DaXuLy"; b.KetQua = "Đã ẩn đánh giá: " + dg.LyDoAn; b.NgayXuLy = DateTime.Now; }
            await Db.SaveChangesAsync();
            TempData["ThongBao"] = "Đã ẩn đánh giá và đóng các báo cáo liên quan.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost("giu/{maBaoCao:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Giu(int maBaoCao, string? lyDo)
        {
            var b = await Db.BaoCaoDanhGias.FirstOrDefaultAsync(x => x.MaBaoCao == maBaoCao);
            if (b == null) return NotFound();
            b.TrangThai = "DaXuLy"; b.NgayXuLy = DateTime.Now; b.KetQua = string.IsNullOrWhiteSpace(lyDo) ? "Đánh giá phù hợp, giữ nguyên" : lyDo.Trim();
            await Db.SaveChangesAsync();
            TempData["ThongBao"] = "Đã bác báo cáo, giữ nguyên đánh giá.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost("hien/{maDanhGia:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Hien(int maDanhGia)
        {
            var dg = await Db.DanhGias.FirstOrDefaultAsync(d => d.MaDanhGia == maDanhGia);
            if (dg == null) return NotFound();
            dg.DaAn = false; dg.LyDoAn = null;
            await Db.SaveChangesAsync();
            TempData["ThongBao"] = "Đã hiện lại đánh giá.";
            return RedirectToAction(nameof(Index), new { tab = "tat-ca" });
        }
    }
}
