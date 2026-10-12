using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FoodServiceApp.Web.Data;
using FoodServiceApp.Web.Filters;
using FoodServiceApp.Web.Models.Entities;
using FoodServiceApp.Web.Services;

namespace FoodServiceApp.Web.Controllers.Api
{
    /// <summary>
    /// Tài khoản & bảo mật dùng chung cho 4 vai trò (khách, gian hàng, tài xế, quản trị) của app mobile:
    /// quên/đặt lại mật khẩu bằng OTP, xác thực email/SĐT, lịch sử đăng nhập, xóa tài khoản, ảnh đại diện,
    /// giấy tờ gian hàng, trạng thái tài xế, nhật ký thao tác, giỏ hàng trên server.
    /// </summary>
    [ApiController]
    [Route("api/mobile")]
    public class TaiKhoanApiController : ControllerBase
    {
        private const string Jwt = JwtBearerDefaults.AuthenticationScheme;
        private readonly AppDbContext _db;
        private readonly TaiKhoanService _tk;

        public TaiKhoanApiController(AppDbContext db, TaiKhoanService tk) { _db = db; _tk = tk; }

        private int MaTK => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        private static IActionResult Loi(string m, int status = 400) => new ObjectResult(new { thongDiep = m }) { StatusCode = status };
        private IActionResult Tra(TaiKhoanService.KetQua k) =>
            k.Ok ? Ok(new { thongDiep = k.ThongDiep, maDev = k.MaDev }) : Loi(k.ThongDiep);

        // ---------------------------------------------------------------- quên mật khẩu (công khai)
        public record QuenMkRequest(string? TaiKhoan);
        public record DatLaiMkRequest(string? TaiKhoan, string? Otp, string? MatKhauMoi);

        /// <summary>Nhập tên đăng nhập / email / SĐT -> gửi OTP. Luôn trả thông điệp chung.</summary>
        [HttpPost("tai-khoan/quen-mat-khau")]
        [AllowAnonymous]
        public async Task<IActionResult> QuenMatKhau(QuenMkRequest r) => Tra(await _tk.YeuCauQuenMatKhauAsync(r.TaiKhoan));

        [HttpPost("tai-khoan/dat-lai-mat-khau")]
        [AllowAnonymous]
        public async Task<IActionResult> DatLaiMatKhau(DatLaiMkRequest r) => Tra(await _tk.DatLaiMatKhauAsync(r.TaiKhoan, r.Otp, r.MatKhauMoi));

        // ---------------------------------------------------------------- xác thực email / SĐT
        public record KenhRequest(string? Kenh);
        public record XacNhanOtpRequest(string? Kenh, string? Otp);

        [HttpPost("tai-khoan/xac-thuc/gui-otp")]
        [Authorize(AuthenticationSchemes = Jwt)]
        public async Task<IActionResult> GuiOtp(KenhRequest r) => Tra(await _tk.GuiXacThucAsync(MaTK, r.Kenh == "sms" ? "sms" : "email"));

        [HttpPost("tai-khoan/xac-thuc/xac-nhan")]
        [Authorize(AuthenticationSchemes = Jwt)]
        public async Task<IActionResult> XacNhanOtp(XacNhanOtpRequest r) => Tra(await _tk.XacNhanXacThucAsync(MaTK, r.Kenh == "sms" ? "sms" : "email", r.Otp));

        [HttpGet("tai-khoan/trang-thai-xac-thuc")]
        [Authorize(AuthenticationSchemes = Jwt)]
        public async Task<IActionResult> TrangThaiXacThuc()
        {
            var t = await _db.TaiKhoans.FindAsync(MaTK);
            return t == null ? Loi("Không tìm thấy tài khoản.", 404) : Ok(new { emailDaXacThuc = t.EmailDaXacThuc, sdtDaXacThuc = t.SdtDaXacThuc, lanDangNhapCuoi = t.LanDangNhapCuoi });
        }

        // ---------------------------------------------------------------- lịch sử đăng nhập / xóa tài khoản
        [HttpGet("tai-khoan/lich-su-dang-nhap")]
        [Authorize(AuthenticationSchemes = Jwt)]
        public async Task<IActionResult> LichSuDangNhap()
        {
            var ds = await _tk.LichSuDangNhapAsync(MaTK);
            return Ok(ds.Select(n => new { thoiGian = n.ThoiGian, noiDung = n.ThongDiep, diaChiIp = n.DiaChiIP, thanhCong = n.MucDo == "Info" }));
        }

        public record XoaTaiKhoanRequest(string? MatKhau);

        [HttpPost("tai-khoan/xoa")]
        [Authorize(AuthenticationSchemes = Jwt)]
        public async Task<IActionResult> XoaTaiKhoan(XoaTaiKhoanRequest r) => Tra(await _tk.XoaTaiKhoanAsync(MaTK, r.MatKhau));

        // ---------------------------------------------------------------- ảnh đại diện (khách / tài xế)
        public record AnhRequest(string? Url);

        [HttpPut("tai-khoan/anh-dai-dien")]
        [Authorize(AuthenticationSchemes = Jwt, Roles = "KhachHang,TaiXe")]
        public async Task<IActionResult> AnhDaiDien(AnhRequest r)
        {
            var url = string.IsNullOrWhiteSpace(r.Url) ? null : r.Url.Trim();
            if (url != null && url.Length > 500) return Loi("Đường dẫn ảnh quá dài.");
            if (User.IsInRole("KhachHang"))
            {
                var k = await _db.KhachHangs.FirstOrDefaultAsync(x => x.MaTK == MaTK);
                if (k == null) return Loi("Không tìm thấy khách hàng.", 404);
                k.AnhDaiDien = url;
            }
            else
            {
                var t = await _db.TaiXes.FirstOrDefaultAsync(x => x.MaTK == MaTK);
                if (t == null) return Loi("Không tìm thấy tài xế.", 404);
                t.AnhDaiDien = url;
            }
            await _db.SaveChangesAsync();
            return Ok(new { anhDaiDien = url });
        }

        // ---------------------------------------------------------------- gian hàng: giấy tờ
        public record GiayToRequest(string? GiayPhepKinhDoanh, string? GiayChungNhanAttp);

        [HttpPut("gian-hang/giay-to")]
        [Authorize(AuthenticationSchemes = Jwt, Roles = "GianHang")]
        public async Task<IActionResult> GiayTo(GiayToRequest r)
        {
            var g = await _db.GianHangs.FirstOrDefaultAsync(x => x.MaTK == MaTK);
            if (g == null) return Loi("Không tìm thấy gian hàng.", 404);
            if (r.GiayPhepKinhDoanh != null) g.GiayPhepKinhDoanh = r.GiayPhepKinhDoanh.Trim();
            if (r.GiayChungNhanAttp != null) g.GiayChungNhanATTP = r.GiayChungNhanAttp.Trim();
            await _db.SaveChangesAsync();
            return Ok(new { giayPhepKinhDoanh = g.GiayPhepKinhDoanh ?? "", giayChungNhanAttp = g.GiayChungNhanATTP ?? "" });
        }

        // ---------------------------------------------------------------- tài xế: online/offline, sẵn sàng
        public record TrangThaiTaiXeRequest(string? TrangThai);

        /// <summary>SanSang = online & nhận đơn; NgoaiTuyen = offline. (DangGiao do hệ thống đặt khi nhận đơn.)</summary>
        [HttpPut("tai-xe/trang-thai")]
        [Authorize(AuthenticationSchemes = Jwt, Roles = "TaiXe")]
        public async Task<IActionResult> TrangThaiTaiXe(TrangThaiTaiXeRequest r)
        {
            if (r.TrangThai != "SanSang" && r.TrangThai != "NgoaiTuyen") return Loi("Trạng thái chỉ nhận SanSang hoặc NgoaiTuyen.");
            var t = await _db.TaiXes.FirstOrDefaultAsync(x => x.MaTK == MaTK);
            if (t == null) return Loi("Không tìm thấy tài xế.", 404);
            if (t.TrangThai == "DangGiao") return Loi("Bạn đang giao đơn, hoàn tất đơn trước khi đổi trạng thái.");
            if (t.TrangThaiDuyet != "DaDuyet") return Loi("Hồ sơ tài xế chưa được duyệt.", 403);
            t.TrangThai = r.TrangThai;
            await _db.SaveChangesAsync();
            return Ok(new { trangThai = t.TrangThai });
        }

        // ---------------------------------------------------------------- quản trị: nhật ký thao tác
        [HttpGet("admin/nhat-ky")]
        [Authorize(AuthenticationSchemes = Jwt, Roles = "QuanTri")]
        public async Task<IActionResult> NhatKy(string? nguon, int? maTK, int trang = 1, int kichThuoc = 50)
        {
            if (!await ChiSuperAdminGhiAttribute.LaSuperAdminAsync(HttpContext)) return Loi("Chỉ Admin tổng được xem nhật ký.", 403);
            kichThuoc = Math.Clamp(kichThuoc, 1, 200); trang = Math.Max(1, trang);
            var q = _db.NhatKyHeThongs.AsQueryable();
            if (!string.IsNullOrWhiteSpace(nguon)) q = q.Where(n => n.Nguon == nguon);
            if (maTK != null) q = q.Where(n => n.MaTK == maTK);
            var ds = await q.OrderByDescending(n => n.ThoiGian).Skip((trang - 1) * kichThuoc).Take(kichThuoc)
                .Select(n => new { n.MaLog, n.ThoiGian, n.MucDo, n.Nguon, n.ThongDiep, n.MaTK, n.DiaChiIP, n.PhuongThuc, n.DuongDan, n.MaTrangThai })
                .ToListAsync();
            return Ok(ds);
        }

        // ---------------------------------------------------------------- giỏ hàng lưu trên server (khách)
        public record GioHangDong(int MaMonAn, int SoLuong, string? GhiChu, List<string>? TuyChon);
        public record GioHangRequest(int MaGianHang, List<GioHangDong>? ChiTiet);

        private async Task<int?> MaKhAsync() =>
            await _db.KhachHangs.Where(k => k.MaTK == MaTK).Select(k => (int?)k.MaKH).FirstOrDefaultAsync();

        [HttpGet("khach-hang/gio-hang")]
        [Authorize(AuthenticationSchemes = Jwt, Roles = "KhachHang")]
        public async Task<IActionResult> LayGioHang()
        {
            var maKh = await MaKhAsync();
            if (maKh == null) return Loi("Không tìm thấy khách hàng.", 404);
            var g = await _db.GioHangs.Include(x => x.ChiTiet).FirstOrDefaultAsync(x => x.MaKH == maKh);
            if (g == null) return Ok(new { maGianHang = (int?)null, chiTiet = Array.Empty<object>(), tamTinh = 0m });

            var ids = g.ChiTiet.Select(c => c.MaMonAn).ToList();
            var mon = await _db.MonAns.Where(m => ids.Contains(m.MaMonAn)).ToDictionaryAsync(m => m.MaMonAn);
            // Giá luôn lấy theo MonAn.Gia hiện tại, món đã ngừng bán vẫn trả về kèm cờ để app báo cho khách
            var dong = g.ChiTiet.Where(c => mon.ContainsKey(c.MaMonAn)).Select(c => new
            {
                maMonAn = c.MaMonAn,
                tenMon = mon[c.MaMonAn].TenMon,
                gia = mon[c.MaMonAn].Gia,
                soLuong = c.SoLuong,
                ghiChu = c.GhiChu ?? "",
                tuyChon = string.IsNullOrEmpty(c.TuyChon) ? new List<string>() : (JsonSerializer.Deserialize<List<string>>(c.TuyChon) ?? new()),
                conBan = mon[c.MaMonAn].TinhTrang == "ConHang"
            }).ToList();
            return Ok(new { maGianHang = (int?)g.MaGianHang, chiTiet = dong, tamTinh = dong.Where(d => d.conBan).Sum(d => d.gia * d.soLuong) });
        }

        /// <summary>Ghi đè toàn bộ giỏ. ChiTiet rỗng = xóa giỏ. Mọi món phải thuộc đúng gian hàng.</summary>
        [HttpPut("khach-hang/gio-hang")]
        [Authorize(AuthenticationSchemes = Jwt, Roles = "KhachHang")]
        public async Task<IActionResult> LuuGioHang(GioHangRequest r)
        {
            var maKh = await MaKhAsync();
            if (maKh == null) return Loi("Không tìm thấy khách hàng.", 404);
            var g = await _db.GioHangs.Include(x => x.ChiTiet).FirstOrDefaultAsync(x => x.MaKH == maKh);

            if (r.ChiTiet == null || r.ChiTiet.Count == 0)
            {
                if (g != null) { _db.GioHangs.Remove(g); await _db.SaveChangesAsync(); }
                return Ok(new { thongDiep = "Đã xóa giỏ hàng." });
            }
            if (r.ChiTiet.Any(c => c.SoLuong < 1 || c.SoLuong > 99)) return Loi("Số lượng mỗi món từ 1 đến 99.");

            var ids = r.ChiTiet.Select(c => c.MaMonAn).Distinct().ToList();
            var mon = await _db.MonAns.Include(m => m.DanhMuc).Where(m => ids.Contains(m.MaMonAn)).ToListAsync();
            if (mon.Count != ids.Count) return Loi("Có món không tồn tại.");
            if (mon.Any(m => m.DanhMuc == null || m.DanhMuc.MaGianHang != r.MaGianHang)) return Loi("Giỏ hàng chỉ gồm món của một gian hàng.");

            if (g == null) { g = new GioHang { MaKH = maKh.Value }; _db.GioHangs.Add(g); }
            else { _db.GioHangChiTiets.RemoveRange(g.ChiTiet); g.ChiTiet.Clear(); }
            g.MaGianHang = r.MaGianHang;
            g.NgayCapNhat = DateTime.Now;
            foreach (var c in r.ChiTiet)
                g.ChiTiet.Add(new GioHangChiTiet
                {
                    MaMonAn = c.MaMonAn, SoLuong = c.SoLuong,
                    GhiChu = string.IsNullOrWhiteSpace(c.GhiChu) ? null : c.GhiChu.Trim(),
                    TuyChon = c.TuyChon is { Count: > 0 } ? JsonSerializer.Serialize(c.TuyChon) : null,
                });
            await _db.SaveChangesAsync();
            return Ok(new { thongDiep = "Đã lưu giỏ hàng." });
        }
    }
}
