using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FoodServiceApp.Web.Data;
using FoodServiceApp.Web.Models.Dtos;

namespace FoodServiceApp.Web.Controllers.Api
{
    [ApiController]
    [Route("api/admin")]
    public class AdminController : AdminApiBaseController
    {
        private readonly AppDbContext _db;

        public AdminController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet("home")]
        public async Task<IActionResult> Home()
        {
            var homNay = DateTime.Today;
            return Ok(new AdminHomeDto(
                await _db.KhachHangs.CountAsync(),
                await _db.GianHangs.CountAsync(),
                await _db.GianHangs.CountAsync(g => g.TrangThaiKinhDoanh == "DangMo"),
                await _db.DonHangs.CountAsync(d => d.NgayDat >= homNay),
                await _db.DonHangs.Where(d => d.NgayDat >= homNay && d.TrangThai == "DaGiao")
                    .SumAsync(d => (decimal?)d.TongTien) ?? 0
            ));
        }

        [HttpGet("vendors")]
        public async Task<IActionResult> Vendors([FromQuery] string? timKiem)
        {
            var query = _db.GianHangs.Join(_db.TaiKhoans, g => g.MaTK, t => t.MaTK,
                (g, t) => new VendorListItemDto(g.MaGianHang, g.TenCuaHang, g.DiaChi, g.NgayDangKy, g.TrangThaiKinhDoanh, t.TrangThai));

            if (!string.IsNullOrWhiteSpace(timKiem))
                query = query.Where(x => x.TenCuaHang.Contains(timKiem));

            return Ok(await query.OrderByDescending(x => x.NgayDangKy).ToListAsync());
        }

        [HttpGet("customers")]
        public async Task<IActionResult> Customers([FromQuery] string? timKiem)
        {
            var query = _db.KhachHangs.Join(_db.TaiKhoans, k => k.MaTK, t => t.MaTK,
                (k, t) => new CustomerListItemDto(k.MaKH, k.HoTen, k.SDT, k.Email, t.TrangThai));

            if (!string.IsNullOrWhiteSpace(timKiem))
                query = query.Where(x => x.HoTen.Contains(timKiem) || x.SDT.Contains(timKiem));

            return Ok(await query.OrderByDescending(x => x.MaKH).ToListAsync());
        }

        [HttpPost("accounts/{maTK:int}/toggle-lock")]
        public async Task<IActionResult> ToggleLock(int maTK)
        {
            var taiKhoan = await _db.TaiKhoans.FirstOrDefaultAsync(t => t.MaTK == maTK);
            if (taiKhoan == null) return NotFound();

            taiKhoan.TrangThai = !taiKhoan.TrangThai;
            await _db.SaveChangesAsync();
            return Ok(new { taiKhoan.TrangThai });
        }
    }
}
