using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FoodServiceApp.Web.Data;

namespace FoodServiceApp.Web.Controllers
{
    public class DonHangController : GianHangBaseController
    {
        private readonly AppDbContext _db;

        public DonHangController(AppDbContext db)
        {
            _db = db;
        }

        // GET /DonHang?trangThai=ChoXacNhan
        public async Task<IActionResult> Index(string? trangThai)
        {
            var query = _db.DonHangs
                .Include(d => d.ChiTiets).ThenInclude(c => c.MonAn)
                .Where(d => d.MaGianHang == CurrentGianHangId);

            if (!string.IsNullOrEmpty(trangThai))
                query = query.Where(d => d.TrangThai == trangThai);

            var danhSach = await query.OrderByDescending(d => d.NgayDat).ToListAsync();

            ViewBag.TrangThaiDangLoc = trangThai ?? "";
            return View(danhSach);
        }

        public async Task<IActionResult> ChiTiet(int id)
        {
            var don = await _db.DonHangs
                .Include(d => d.ChiTiets).ThenInclude(c => c.MonAn)
                .Include(d => d.ThanhToan)
                .FirstOrDefaultAsync(d => d.MaDonHang == id && d.MaGianHang == CurrentGianHangId);

            if (don == null) return NotFound();
            return View(don);
        }

        // Luồng trạng thái hợp lệ: ChoXacNhan -> DaXacNhan -> DangGiao -> DaGiao
        //                          ChoXacNhan/DaXacNhan -> DaHuy
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CapNhatTrangThai(int id, string trangThaiMoi)
        {
            var don = await _db.DonHangs.Include(d => d.ThanhToan)
                .FirstOrDefaultAsync(d => d.MaDonHang == id && d.MaGianHang == CurrentGianHangId);
            if (don == null) return NotFound();

            var chuyenHopLe = new Dictionary<string, string[]>
            {
                ["ChoXacNhan"] = new[] { "DaXacNhan", "DaHuy" },
                ["DaXacNhan"] = new[] { "DaHuy" }, // từ đây trở đi việc bàn giao do tài xế tự nhận đơn
            };

            if (chuyenHopLe.TryGetValue(don.TrangThai, out var choPhep) && choPhep.Contains(trangThaiMoi))
            {
                don.TrangThai = trangThaiMoi;

                // Huỷ đơn đã thanh toán online -> đánh dấu hoàn tiền để Quản trị đối soát
                if (trangThaiMoi == "DaHuy" && don.ThanhToan != null && don.ThanhToan.TrangThai == "DaThanhToan")
                    don.ThanhToan.TrangThai = "DaHoanTien";

                await _db.SaveChangesAsync();
                TempData["ThongBao"] = $"Đã cập nhật đơn #{don.MaDonHang} sang trạng thái mới.";
            }
            else
            {
                TempData["LoiThongBao"] = "Không thể chuyển sang trạng thái này từ trạng thái hiện tại.";
            }

            return RedirectToAction("Index");
        }
    }
}
