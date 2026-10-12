using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FoodServiceApp.Web.Data;
using FoodServiceApp.Web.Services;

namespace FoodServiceApp.Web.Controllers.Api
{
    /// <summary>Bằng chứng giao hàng (ảnh) và vị trí tài xế cho khách — lấy từ CSDL nên không mất khi server khởi động lại.</summary>
    [ApiController]
    [Route("api/mobile")]
    public class GiaoHangMobileApiController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IFileStorageService _luuTru;
        public GiaoHangMobileApiController(AppDbContext db, IFileStorageService luuTru) { _db = db; _luuTru = luuTru; }

        private int MaTK => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        private static IActionResult Loi(string t, int s = 400) => new ObjectResult(new { thongDiep = t }) { StatusCode = s };

        /// <summary>Tài xế tải ảnh bằng chứng (đã giao / giao thất bại). Gọi TRƯỚC hoan-thanh; đơn không có mã bắt buộc phải có ảnh.</summary>
        [HttpPost("don-hang/{id:int}/bang-chung")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "TaiXe")]
        [RequestSizeLimit(6_000_000)]
        public async Task<IActionResult> TaiBangChung(int id, IFormFile file)
        {
            var tx = await _db.TaiXes.FirstOrDefaultAsync(t => t.MaTK == MaTK);
            if (tx == null) return Loi("Tài khoản tài xế không hợp lệ.", 401);
            var gh = await _db.GiaoHangs.FirstOrDefaultAsync(g => g.MaDonHang == id && g.MaTaiXe == tx.MaTaiXe);
            if (gh == null) return Loi("Không tìm thấy đơn giao của bạn.", 404);
            if (gh.TrangThai is not ("DangGiao" or "DaNhan")) return Loi("Đơn đã kết thúc, không thêm được bằng chứng.", 409);
            try { gh.AnhBangChung = await _luuTru.SaveFileAsync(file, "giaohang"); }
            catch (ArgumentException ex) { return Loi(ex.Message); }
            await _db.SaveChangesAsync();
            return Ok(new { anhBangChung = gh.AnhBangChung });
        }

        /// <summary>Khách xem vị trí tài xế của đơn mình. viTriCu = true khi quá 90 giây chưa cập nhật (app nên hiện "mất tín hiệu").</summary>
        [HttpGet("don-hang/{id:int}/vi-tri")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "KhachHang")]
        public async Task<IActionResult> ViTriTaiXe(int id)
        {
            var maKH = await _db.KhachHangs.Where(k => k.MaTK == MaTK).Select(k => (int?)k.MaKH).FirstOrDefaultAsync();
            var don = await _db.DonHangs.AsNoTracking().FirstOrDefaultAsync(d => d.MaDonHang == id && d.MaKH == maKH);
            if (don == null) return Loi("Không tìm thấy đơn hàng.", 404);
            if (don.TrangThai != "DangGiao") return Ok(new { dangGiao = false });
            var v = await _db.TaiXeViTris.AsNoTracking().FirstOrDefaultAsync(x => x.MaDonHang == id);
            if (v == null) return Ok(new { dangGiao = true, coViTri = false });
            var giay = (DateTime.Now - v.CapNhatLuc).TotalSeconds;
            return Ok(new { dangGiao = true, coViTri = true, viDo = v.ViDo, kinhDo = v.KinhDo, capNhatLuc = v.CapNhatLuc, viTriCu = giay > GiaoHangXuLy.ViTriCuGiay, giayTruoc = (int)giay });
        }
    }
}
