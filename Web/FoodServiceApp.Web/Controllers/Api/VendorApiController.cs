using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FoodServiceApp.Web.Data;
using FoodServiceApp.Web.Models.Dtos;

namespace FoodServiceApp.Web.Controllers.Api
{
    [ApiController]
    [Route("api/vendor")]
    public class VendorController : VendorApiBaseController
    {
        private readonly AppDbContext _db;

        public VendorController(AppDbContext db)
        {
            _db = db;
        }

        private async Task<int> GetMaGianHangAsync() =>
            (await _db.GianHangs.FirstAsync(g => g.MaTK == CurrentMaTK)).MaGianHang;

        [HttpGet("home")]
        public async Task<IActionResult> Home()
        {
            var maGianHang = await GetMaGianHangAsync();
            var gianHang = await _db.GianHangs.FirstAsync(g => g.MaGianHang == maGianHang);
            var homNay = DateTime.Today;

            var donHomNay = await _db.DonHangs
                .Where(d => d.MaGianHang == maGianHang && d.NgayDat >= homNay)
                .ToListAsync();

            var soMonAn = await _db.MonAns.CountAsync(m => m.DanhMuc!.MaGianHang == maGianHang);
            var choXacNhan = await _db.DonHangs.CountAsync(d => d.MaGianHang == maGianHang && d.TrangThai == "ChoXacNhan");

            return Ok(new VendorHomeDto(
                gianHang.TenCuaHang, gianHang.TrangThaiKinhDoanh,
                donHomNay.Where(d => d.TrangThai == "DaGiao").Sum(d => d.TongTien),
                donHomNay.Count(d => d.TrangThai == "DaGiao"),
                choXacNhan, soMonAn));
        }

        [HttpPost("toggle-mo-cua")]
        public async Task<IActionResult> ToggleMoCua()
        {
            var maGianHang = await GetMaGianHangAsync();
            var gianHang = await _db.GianHangs.FirstAsync(g => g.MaGianHang == maGianHang);
            gianHang.TrangThaiKinhDoanh = gianHang.TrangThaiKinhDoanh == "DangMo" ? "DongCua" : "DangMo";
            await _db.SaveChangesAsync();
            return Ok(new { gianHang.TrangThaiKinhDoanh });
        }

        [HttpGet("orders")]
        public async Task<IActionResult> Orders([FromQuery] string? trangThai)
        {
            var maGianHang = await GetMaGianHangAsync();
            var query = _db.DonHangs
                .Include(d => d.GianHang)
                .Include(d => d.ChiTiets).ThenInclude(c => c.MonAn)
                .Where(d => d.MaGianHang == maGianHang);

            if (!string.IsNullOrEmpty(trangThai))
                query = query.Where(d => d.TrangThai == trangThai);

            var donHangs = await query.OrderByDescending(d => d.NgayDat).ToListAsync();

            var result = donHangs.Select(d => new DonHangDto(
                d.MaDonHang, d.TrangThai, d.TongTien, d.NgayDat, d.DiaChiGiaoHang, d.GhiChu,
                d.GianHang!.TenCuaHang, d.GianHang.DiaChi,
                d.ChiTiets.Select(c => new ChiTietDonHangDto(c.MonAn!.TenMon, c.SoLuong, c.DonGia, c.ThanhTien)).ToList()
            ));

            return Ok(result);
        }

        [HttpPost("orders/{id:int}/status")]
        public async Task<IActionResult> UpdateOrderStatus(int id, UpdateOrderStatusRequest request)
        {
            var maGianHang = await GetMaGianHangAsync();
            var don = await _db.DonHangs.FirstOrDefaultAsync(d => d.MaDonHang == id && d.MaGianHang == maGianHang);
            if (don == null) return NotFound();

            // Gian hàng chỉ được: ChoXacNhan -> DaXacNhan/DaHuy, DaXacNhan -> DaHuy.
            // Việc chuyển sang DangGiao do tài xế tự nhận đơn (xem DriverController).
            var chuyenHopLe = new Dictionary<string, string[]>
            {
                ["ChoXacNhan"] = new[] { "DaXacNhan", "DaHuy" },
                ["DaXacNhan"] = new[] { "DaHuy" },
            };

            if (!chuyenHopLe.TryGetValue(don.TrangThai, out var choPhep) || !choPhep.Contains(request.TrangThaiMoi))
                return BadRequest(new { message = "Không thể chuyển sang trạng thái này." });

            don.TrangThai = request.TrangThaiMoi;
            await _db.SaveChangesAsync();
            return Ok(new { don.TrangThai });
        }

        [HttpGet("menu")]
        public async Task<IActionResult> Menu()
        {
            var maGianHang = await GetMaGianHangAsync();
            var monAns = await _db.MonAns
                .Where(m => m.DanhMuc!.MaGianHang == maGianHang)
                .Select(m => new MonAnDto(m.MaMonAn, m.TenMon, m.Gia, m.MoTa, m.HinhAnh, m.TinhTrang == "ConHang"))
                .ToListAsync();

            return Ok(monAns);
        }
    }
}
