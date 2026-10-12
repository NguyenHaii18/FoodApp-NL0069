using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FoodServiceApp.Web.Data;

namespace FoodServiceApp.Web.Controllers
{
    [Route("QuanTri/SuCo")]
    public class AdminSuCoController : QuanTriBaseController
    {
        private readonly AppDbContext _db;

        public AdminSuCoController(AppDbContext db)
        {
            _db = db;
        }

        // Đơn có sự cố: tài xế đã báo giao thất bại nhưng đơn chưa được xử lý tiếp
        // (vẫn đang ở trạng thái DangGiao) — cần Quản trị can thiệp.
        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var danhSach = await _db.DonHangs
                .Include(d => d.GianHang)
                .Include(d => d.GiaoHang).ThenInclude(g => g!.TaiXe)
                .Include(d => d.ThanhToan)
                .Where(d => d.TrangThai == "DangGiao" && d.GiaoHang != null && d.GiaoHang.TrangThai == "GiaoThatBai")
                .OrderByDescending(d => d.NgayDat)
                .ToListAsync();

            return View(danhSach);
        }

        // Gán lại đơn cho tài xế khác: đưa đơn về "DaXacNhan", bỏ tài xế cũ để đơn
        // xuất hiện lại trong danh sách "chờ nhận" cho tài xế khác tự nhận.
        [HttpPost("gan-lai/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GanLai(int id)
        {
            var don = await _db.DonHangs.Include(d => d.GiaoHang)
                .FirstOrDefaultAsync(d => d.MaDonHang == id && d.TrangThai == "DangGiao");
            if (don == null || don.GiaoHang == null) return NotFound();

            don.TrangThai = "DaXacNhan";
            don.MaTaiXe = null;
            _db.GiaoHangs.Remove(don.GiaoHang);

            await _db.SaveChangesAsync();
            TempData["ThongBao"] = $"Đã đưa đơn #{don.MaDonHang} về danh sách chờ tài xế khác nhận.";
            return RedirectToAction("Index");
        }

        [HttpPost("huy-hoan-tien/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> HuyVaHoanTien(int id)
        {
            var don = await _db.DonHangs.Include(d => d.ThanhToan)
                .FirstOrDefaultAsync(d => d.MaDonHang == id && d.TrangThai == "DangGiao");
            if (don == null) return NotFound();

            don.TrangThai = "DaHuy";
            if (don.ThanhToan != null && don.ThanhToan.TrangThai == "DaThanhToan")
                don.ThanhToan.TrangThai = "DaHoanTien";

            await _db.SaveChangesAsync();
            TempData["ThongBao"] = $"Đã huỷ đơn #{don.MaDonHang} và đánh dấu hoàn tiền (nếu đã thanh toán).";
            return RedirectToAction("Index");
        }
    }
}
