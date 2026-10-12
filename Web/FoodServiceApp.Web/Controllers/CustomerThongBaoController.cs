using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FoodServiceApp.Web.Data;

namespace FoodServiceApp.Web.Controllers
{
    /// <summary>Thông báo đơn hàng / thanh toán / khuyến mãi và cài đặt bật–tắt theo loại.</summary>
    [Route("MuaHang/ThongBao")]
    public class CustomerThongBaoController : CustomerBaseController
    {
        private readonly AppDbContext _db;
        public CustomerThongBaoController(AppDbContext db) { _db = db; }

        private async Task<int> MaTKAsync() => await _db.KhachHangs.Where(k => k.MaKH == CurrentMaKH).Select(k => k.MaTK).FirstAsync();

        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var maTK = await MaTKAsync();
            ViewBag.KhachHang = await _db.KhachHangs.FirstAsync(k => k.MaKH == CurrentMaKH);
            var ds = await _db.ThongBaos.Where(t => t.MaTK == maTK).OrderByDescending(t => t.NgayTao).Take(100).ToListAsync();
            return View(ds);
        }

        [HttpPost("doc-tat-ca")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DocTatCa()
        {
            var maTK = await MaTKAsync();
            foreach (var t in await _db.ThongBaos.Where(t => t.MaTK == maTK && !t.DaDoc).ToListAsync()) t.DaDoc = true;
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpGet("mo/{id:long}")]
        public async Task<IActionResult> Mo(long id)
        {
            var maTK = await MaTKAsync();
            var t = await _db.ThongBaos.FirstOrDefaultAsync(x => x.MaThongBao == id && x.MaTK == maTK);
            if (t == null) return NotFound();
            t.DaDoc = true;
            await _db.SaveChangesAsync();
            if (int.TryParse(t.MaThamChieu, out var maDon) && t.Loai is "DonHang" or "HeThong")
                return RedirectToAction("ChiTiet", "CustomerOrder", new { id = maDon });
            return RedirectToAction(nameof(Index));
        }

        [HttpPost("cai-dat")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CaiDat(bool donHang, bool thanhToan, bool khuyenMai)
        {
            var k = await _db.KhachHangs.FirstAsync(x => x.MaKH == CurrentMaKH);
            k.NhanTbDonHang = donHang; k.NhanTbThanhToan = thanhToan; k.NhanTbKhuyenMai = khuyenMai;
            await _db.SaveChangesAsync();
            TempData["ThongBao"] = "Đã lưu cài đặt thông báo.";
            return RedirectToAction(nameof(Index));
        }
    }
}
