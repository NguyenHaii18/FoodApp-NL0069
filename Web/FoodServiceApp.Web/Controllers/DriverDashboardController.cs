using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FoodServiceApp.Web.Data;

namespace FoodServiceApp.Web.Controllers
{
    [Route("TaiXe")]
    public class DriverDashboardController : TaiXeBaseController
    {
        private readonly AppDbContext _db;

        public DriverDashboardController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet("")]
        [HttpGet("Dashboard")]
        public async Task<IActionResult> Index()
        {
            var taiXe = await _db.TaiXes.FirstAsync(t => t.MaTaiXe == CurrentTaiXeId);
            var homNay = DateTime.Today;

            ViewBag.TaiXe = taiXe;
            ViewBag.SoDonHomNay = await _db.GiaoHangs.CountAsync(g =>
                g.MaTaiXe == CurrentTaiXeId && g.TrangThai == "GiaoThanhCong" && g.ThoiGianGiao >= homNay);
            ViewBag.ThuNhapHomNay = await _db.GiaoHangs
                .Include(g => g.DonHang)
                .Where(g => g.MaTaiXe == CurrentTaiXeId && g.TrangThai == "GiaoThanhCong" && g.ThoiGianGiao >= homNay)
                .SumAsync(g => g.DonHang!.PhiGiaoHang);

            ViewBag.DonDangGiao = await _db.DonHangs
                .Include(d => d.GianHang)
                .Include(d => d.ChiTiets)
                .Where(d => d.MaTaiXe == CurrentTaiXeId && (d.TrangThai == "DaXacNhan" || d.TrangThai == "DangGiao"))
                .Include(d => d.GiaoHang)
                .FirstOrDefaultAsync();

            ViewBag.SoDonChoNhan = await _db.DonHangs.CountAsync(d =>
                d.TrangThai == "DaXacNhan" && d.MaTaiXe == null);

            return View();
        }

        [HttpPost("ToggleSanSang")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleSanSang()
        {
            var taiXe = await _db.TaiXes.FirstAsync(t => t.MaTaiXe == CurrentTaiXeId);

            if (taiXe.TrangThai == "DangGiao")
            {
                TempData["LoiThongBao"] = "Bạn đang trong chuyến giao, không thể chuyển trạng thái.";
                return RedirectToAction("Index");
            }

            taiXe.TrangThai = taiXe.TrangThai == "SanSang" ? "NgoaiTuyen" : "SanSang";
            await _db.SaveChangesAsync();
            return RedirectToAction("Index");
        }
    }
}
