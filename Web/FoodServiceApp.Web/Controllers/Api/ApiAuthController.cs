using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using FoodServiceApp.Web.Data;
using FoodServiceApp.Web.Models.Dtos;
using FoodServiceApp.Web.Services;

namespace FoodServiceApp.Web.Controllers.Api
{
    [ApiController]
    [Route("api/auth")]
    public class ApiAuthController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IConfiguration _config;

        public ApiAuthController(AppDbContext db, IConfiguration config)
        {
            _db = db;
            _config = config;
        }

        // Dùng chung 1 endpoint cho cả KhachHang / GianHang / QuanTri / TaiXe —
        // MAUI truyền lên đúng vai trò đang đăng nhập ở màn hình tương ứng.
        [HttpPost("login/{vaiTro}")]
        public async Task<IActionResult> Login(string vaiTro, LoginRequest request)
        {
            var vaiTroHopLe = new[] { "KhachHang", "GianHang", "QuanTri", "TaiXe" };
            if (!vaiTroHopLe.Contains(vaiTro))
                return BadRequest(new { message = "Vai trò không hợp lệ." });

            var taiKhoan = await _db.TaiKhoans.FirstOrDefaultAsync(t =>
                t.TenDangNhap == request.TenDangNhap && t.VaiTro == vaiTro);

            // DEMO ONLY: so sánh mật khẩu plaintext — đổi sang BCrypt.Verify khi triển khai thật.
            if (taiKhoan == null || !await BaoMat.KhopAsync(_db, HttpContext, request.MatKhau, taiKhoan))
                return Unauthorized(new { message = "Tên đăng nhập hoặc mật khẩu không đúng." });

            if (!taiKhoan.TrangThai)
                return Unauthorized(new { message = "Tài khoản đã bị khoá." });

            string hoTenHienThi = vaiTro switch
            {
                "GianHang" => (await _db.GianHangs.FirstAsync(g => g.MaTK == taiKhoan.MaTK)).TenCuaHang,
                "QuanTri" => (await _db.QuanTris.FirstAsync(q => q.MaTK == taiKhoan.MaTK)).HoTen,
                "TaiXe" => (await _db.TaiXes.FirstAsync(t => t.MaTK == taiKhoan.MaTK)).HoTen,
                "KhachHang" => (await _db.KhachHangs.FirstAsync(k => k.MaTK == taiKhoan.MaTK)).HoTen,
                _ => taiKhoan.TenDangNhap,
            };

            var token = TaoJwtToken(taiKhoan.MaTK, taiKhoan.TenDangNhap, vaiTro);
            return Ok(new LoginResponse(token, vaiTro, taiKhoan.MaTK, hoTenHienThi));
        }

        private string TaoJwtToken(int maTK, string tenDangNhap, string vaiTro)
        {
            var jwtSection = _config.GetSection("Jwt");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSection["Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, maTK.ToString()),
                new Claim(ClaimTypes.Name, tenDangNhap),
                new Claim(ClaimTypes.Role, vaiTro),
            };

            var token = new JwtSecurityToken(
                issuer: jwtSection["Issuer"],
                audience: jwtSection["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(double.Parse(jwtSection["ExpireHours"]!)),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
