using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FoodServiceApp.Web.Data;
using FoodServiceApp.Web.Models.Entities;

namespace FoodServiceApp.Web.Controllers
{
    [Route("TaiXe/DonHang")]
    public class DriverDonHangController : TaiXeBaseController
    {
        private readonly AppDbContext _db;
        private readonly FoodServiceApp.Web.Services.IFileStorageService _luuTru;

        public DriverDonHangController(AppDbContext db, FoodServiceApp.Web.Services.IFileStorageService luuTru)
        {
            _db = db;
            _luuTru = luuTru;
        }

        // Danh sách đơn đã được gian hàng xác nhận, đang chờ tài xế nhận
        [HttpGet("ChoNhan")]
        public async Task<IActionResult> ChoNhan()
        {
            if (await FoodServiceApp.Web.Services.GiaoHangXuLy.DangCoChuyenAsync(_db, CurrentTaiXeId))
            {
                TempData["LoiThongBao"] = "Bạn đang có một chuyến giao chưa hoàn thành, hãy xử lý xong trước khi nhận đơn mới.";
                return RedirectToAction("DonHienTai");
            }

            var danhSach = await _db.DonHangs
                .Include(d => d.GianHang)
                .Include(d => d.ChiTiets)
                .Where(d => d.TrangThai == "DaXacNhan" && d.MaTaiXe == null)
                .OrderBy(d => d.NgayDat)
                .ToListAsync();

            return View(danhSach);
        }

        [HttpPost("Nhan/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Nhan(int id)
        {
            var taiXe = await _db.TaiXes.FirstAsync(t => t.MaTaiXe == CurrentTaiXeId);
            var kq = await FoodServiceApp.Web.Services.GiaoHangXuLy.NhanDonAsync(_db, taiXe, id);
            if (!kq.Ok)
            {
                TempData["LoiThongBao"] = kq.ThongDiep;
                return RedirectToAction(kq.ThongDiep.Contains("chuyến giao") ? "DonHienTai" : "ChoNhan");
            }
            TempData["ThongBao"] = $"Đã nhận đơn #{id}. Hãy đến quán lấy món rồi bấm \"Đã lấy hàng\".";
            return RedirectToAction("DonHienTai");
        }

        // Chuyến giao hiện tại: đã nhận (đang đến quán) hoặc đang giao
        [HttpGet("HienTai")]
        public async Task<IActionResult> DonHienTai()
        {
            var don = await _db.DonHangs
                .Include(d => d.GianHang)
                .Include(d => d.ChiTiets).ThenInclude(c => c.MonAn)
                .Include(d => d.GiaoHang)
                .FirstOrDefaultAsync(d => d.MaTaiXe == CurrentTaiXeId && (d.TrangThai == "DaXacNhan" || d.TrangThai == "DangGiao"));

            return View(don);
        }

        // Đã lấy hàng tại quán -> bắt đầu giao
        [HttpPost("BatDauGiao/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BatDauGiao(int id)
        {
            var don = await _db.DonHangs.FirstOrDefaultAsync(d => d.MaDonHang == id && d.MaTaiXe == CurrentTaiXeId);
            var giaoHang = await _db.GiaoHangs.FirstOrDefaultAsync(g => g.MaDonHang == id && g.MaTaiXe == CurrentTaiXeId);
            if (don == null || giaoHang == null) return NotFound();

            var kq = await FoodServiceApp.Web.Services.GiaoHangXuLy.DaLayHangAsync(_db, don, giaoHang);
            if (!kq.Ok) TempData["LoiThongBao"] = kq.ThongDiep;
            return RedirectToAction("DonHienTai");
        }

        // Trình duyệt tài xế gửi vị trí định kỳ (chống dồn dập / nhảy bất thường trong GiaoHangXuLy.LuuViTriAsync)
        [HttpPost("ViTri/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ViTri(int id, double lat, double lng)
        {
            if (lat is < -90 or > 90 || lng is < -180 or > 180) return BadRequest(new { message = "Tọa độ không hợp lệ." });
            var coChuyen = await _db.DonHangs.AnyAsync(d => d.MaDonHang == id && d.MaTaiXe == CurrentTaiXeId && (d.TrangThai == "DaXacNhan" || d.TrangThai == "DangGiao"));
            if (!coChuyen) return NotFound();
            var kq = await FoodServiceApp.Web.Services.GiaoHangXuLy.LuuViTriAsync(_db, id, CurrentTaiXeId, lat, lng);
            return kq.Ok ? Ok(new { message = kq.ThongDiep }) : StatusCode(422, new { message = kq.ThongDiep });
        }

        [HttpPost("HoanThanh/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> HoanThanh(int id, string? maXacNhan, IFormFile? anh, double? lat, double? lng)
        {
            var don = await _db.DonHangs.Include(d => d.ThanhToan).FirstOrDefaultAsync(d => d.MaDonHang == id && d.MaTaiXe == CurrentTaiXeId);
            var giaoHang = await _db.GiaoHangs.FirstOrDefaultAsync(g => g.MaDonHang == id && g.MaTaiXe == CurrentTaiXeId);
            if (don == null || giaoHang == null) return NotFound();

            if (anh != null && anh.Length > 0)
            {
                try { giaoHang.AnhBangChung = await _luuTru.SaveFileAsync(anh, "giaohang"); }
                catch (ArgumentException ex) { TempData["LoiThongBao"] = ex.Message; return RedirectToAction("DonHienTai"); }
            }
            var taiXe = await _db.TaiXes.FirstAsync(t => t.MaTaiXe == CurrentTaiXeId);
            var kq = await FoodServiceApp.Web.Services.GiaoHangXuLy.HoanThanhAsync(_db, don, giaoHang, taiXe, maXacNhan, lat, lng);
            if (!kq.Ok) { await _db.SaveChangesAsync(); TempData["LoiThongBao"] = kq.ThongDiep; return RedirectToAction("DonHienTai"); }
            TempData["ThongBao"] = $"Đã hoàn thành giao đơn #{don.MaDonHang}.";
            return RedirectToAction("Index", "DriverDashboard");
        }

        [HttpPost("GiaoThatBai/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GiaoThatBai(int id, string lyDo, IFormFile? anh, double? lat, double? lng)
        {
            var don = await _db.DonHangs.Include(d => d.ThanhToan).FirstOrDefaultAsync(d => d.MaDonHang == id && d.MaTaiXe == CurrentTaiXeId);
            var giaoHang = await _db.GiaoHangs.FirstOrDefaultAsync(g => g.MaDonHang == id && g.MaTaiXe == CurrentTaiXeId);
            if (don == null || giaoHang == null) return NotFound();

            if (anh != null && anh.Length > 0)
            {
                try { giaoHang.AnhBangChung = await _luuTru.SaveFileAsync(anh, "giaohang"); }
                catch (ArgumentException ex) { TempData["LoiThongBao"] = ex.Message; return RedirectToAction("DonHienTai"); }
            }
            var taiXe = await _db.TaiXes.FirstAsync(t => t.MaTaiXe == CurrentTaiXeId);
            var kq = await FoodServiceApp.Web.Services.GiaoHangXuLy.ThatBaiAsync(_db, don, giaoHang, taiXe, lyDo, lat, lng);
            if (!kq.Ok) { TempData["LoiThongBao"] = kq.ThongDiep; return RedirectToAction("DonHienTai"); }
            TempData["ThongBao"] = $"Đã ghi nhận giao thất bại đơn #{don.MaDonHang}. {kq.ThongDiep}";
            return RedirectToAction("Index", "DriverDashboard");
        }

        [HttpGet("LichSu")]
        public async Task<IActionResult> LichSu()
        {
            var danhSach = await _db.GiaoHangs
                .Include(g => g.DonHang).ThenInclude(d => d!.GianHang)
                .Where(g => g.MaTaiXe == CurrentTaiXeId && (g.TrangThai == "GiaoThanhCong" || g.TrangThai == "GiaoThatBai"))
                .OrderByDescending(g => g.ThoiGianGiao)
                .Take(50)
                .ToListAsync();

            return View(danhSach);
        }
    }
}
