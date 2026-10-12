using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FoodServiceApp.Web.Data;
using FoodServiceApp.Web.Models.Dtos;
using FoodServiceApp.Web.Models.Entities;

namespace FoodServiceApp.Web.Controllers.Api
{
    [ApiController]
    [Route("api/driver")]
    public class DriverController : DriverApiBaseController
    {
        private readonly AppDbContext _db;

        public DriverController(AppDbContext db)
        {
            _db = db;
        }

        private async Task<int> GetMaTaiXeAsync() =>
            (await _db.TaiXes.FirstAsync(t => t.MaTK == CurrentMaTK)).MaTaiXe;

        private static DonHangDto MapDonHang(DonHang d) => new(
            d.MaDonHang, d.TrangThai, d.TongTien, d.NgayDat, d.DiaChiGiaoHang, d.GhiChu,
            d.GianHang!.TenCuaHang, d.GianHang.DiaChi,
            d.ChiTiets.Select(c => new ChiTietDonHangDto(c.MonAn!.TenMon, c.SoLuong, c.DonGia, c.ThanhTien)).ToList(),
            d.PhiGiaoHang);

        [HttpGet("home")]
        public async Task<IActionResult> Home()
        {
            var maTaiXe = await GetMaTaiXeAsync();
            var taiXe = await _db.TaiXes.FirstAsync(t => t.MaTaiXe == maTaiXe);
            var homNay = DateTime.Today;

            var donDangGiao = await _db.DonHangs
                .Include(d => d.GianHang)
                .Include(d => d.ChiTiets).ThenInclude(c => c.MonAn)
                .FirstOrDefaultAsync(d => d.MaTaiXe == maTaiXe && d.TrangThai == "DangGiao");

            var thuNhapHomNay = await _db.GiaoHangs
                .Include(g => g.DonHang)
                .Where(g => g.MaTaiXe == maTaiXe && g.TrangThai == "GiaoThanhCong" && g.ThoiGianGiao >= homNay)
                .SumAsync(g => g.DonHang!.PhiGiaoHang);

            return Ok(new DriverHomeDto(
                taiXe.HoTen, taiXe.TrangThai,
                await _db.GiaoHangs.CountAsync(g => g.MaTaiXe == maTaiXe && g.TrangThai == "GiaoThanhCong" && g.ThoiGianGiao >= homNay),
                thuNhapHomNay,
                donDangGiao == null ? null : MapDonHang(donDangGiao),
                await _db.DonHangs.CountAsync(d => d.TrangThai == "DaXacNhan" && d.MaTaiXe == null)
            ));
        }

        [HttpPost("toggle-san-sang")]
        public async Task<IActionResult> ToggleSanSang()
        {
            var maTaiXe = await GetMaTaiXeAsync();
            var taiXe = await _db.TaiXes.FirstAsync(t => t.MaTaiXe == maTaiXe);
            if (taiXe.TrangThai == "DangGiao")
                return BadRequest(new { message = "Đang trong chuyến giao, không thể đổi trạng thái." });

            taiXe.TrangThai = taiXe.TrangThai == "SanSang" ? "NgoaiTuyen" : "SanSang";
            await _db.SaveChangesAsync();
            return Ok(new { taiXe.TrangThai });
        }

        [HttpGet("available-orders")]
        public async Task<IActionResult> AvailableOrders()
        {
            var donHangs = await _db.DonHangs
                .Include(d => d.GianHang)
                .Include(d => d.ChiTiets).ThenInclude(c => c.MonAn)
                .Where(d => d.TrangThai == "DaXacNhan" && d.MaTaiXe == null)
                .OrderBy(d => d.NgayDat)
                .ToListAsync();

            return Ok(donHangs.Select(MapDonHang));
        }

        [HttpPost("orders/{id:int}/accept")]
        public async Task<IActionResult> AcceptOrder(int id)
        {
            var maTaiXe = await GetMaTaiXeAsync();
            var taiXe = await _db.TaiXes.FirstAsync(t => t.MaTaiXe == maTaiXe);
            var kq = await FoodServiceApp.Web.Services.GiaoHangXuLy.NhanDonAsync(_db, taiXe, id);
            if (!kq.Ok) return kq.ThongDiep.Contains("tài xế khác") ? Conflict(new { message = kq.ThongDiep }) : BadRequest(new { message = kq.ThongDiep });
            return Ok(new { message = kq.ThongDiep });
        }

        /// <summary>Đã lấy hàng tại quán -> đơn chuyển sang "DangGiao" (cùng quy tắc với Website và app mobile).</summary>
        [HttpPost("orders/{id:int}/start")]
        public async Task<IActionResult> StartDelivery(int id)
        {
            var maTaiXe = await GetMaTaiXeAsync();
            var don = await _db.DonHangs.FirstOrDefaultAsync(d => d.MaDonHang == id && d.MaTaiXe == maTaiXe);
            var giaoHang = await _db.GiaoHangs.FirstOrDefaultAsync(g => g.MaDonHang == id && g.MaTaiXe == maTaiXe);
            if (don == null || giaoHang == null) return NotFound();
            var kq = await FoodServiceApp.Web.Services.GiaoHangXuLy.DaLayHangAsync(_db, don, giaoHang);
            return kq.Ok ? Ok(new { giaoHang.TrangThai, message = kq.ThongDiep }) : BadRequest(new { message = kq.ThongDiep });
        }

        public record DriverCompleteRequest(string? MaXacNhan, double? ViDo = null, double? KinhDo = null);
        public record DriverFailRequest(string LyDo, double? ViDo = null, double? KinhDo = null);

        [HttpPost("orders/{id:int}/complete")]
        public async Task<IActionResult> CompleteDelivery(int id, [FromBody] DriverCompleteRequest request)
        {
            var maTaiXe = await GetMaTaiXeAsync();
            var don = await _db.DonHangs.Include(d => d.ThanhToan).FirstOrDefaultAsync(d => d.MaDonHang == id && d.MaTaiXe == maTaiXe);
            var giaoHang = await _db.GiaoHangs.FirstOrDefaultAsync(g => g.MaDonHang == id && g.MaTaiXe == maTaiXe);
            if (don == null || giaoHang == null) return NotFound();
            var taiXe = await _db.TaiXes.FirstAsync(t => t.MaTaiXe == maTaiXe);
            var kq = await FoodServiceApp.Web.Services.GiaoHangXuLy.HoanThanhAsync(_db, don, giaoHang, taiXe, request?.MaXacNhan, request?.ViDo, request?.KinhDo);
            return kq.Ok ? Ok(new { message = kq.ThongDiep }) : BadRequest(new { message = kq.ThongDiep });
        }

        [HttpPost("orders/{id:int}/fail")]
        public async Task<IActionResult> FailDelivery(int id, [FromBody] DriverFailRequest request)
        {
            var maTaiXe = await GetMaTaiXeAsync();
            var don = await _db.DonHangs.Include(d => d.ThanhToan).FirstOrDefaultAsync(d => d.MaDonHang == id && d.MaTaiXe == maTaiXe);
            var giaoHang = await _db.GiaoHangs.FirstOrDefaultAsync(g => g.MaDonHang == id && g.MaTaiXe == maTaiXe);
            if (don == null || giaoHang == null) return NotFound();
            var taiXe = await _db.TaiXes.FirstAsync(t => t.MaTaiXe == maTaiXe);
            var kq = await FoodServiceApp.Web.Services.GiaoHangXuLy.ThatBaiAsync(_db, don, giaoHang, taiXe, request?.LyDo, request?.ViDo, request?.KinhDo);
            return kq.Ok ? Ok(new { message = kq.ThongDiep, tinhPhi = giaoHang.TinhPhiThatBai }) : BadRequest(new { message = kq.ThongDiep });
        }
    }
}
