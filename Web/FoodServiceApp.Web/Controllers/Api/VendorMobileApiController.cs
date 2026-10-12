using System.Collections.Concurrent;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FoodServiceApp.Web.Data;
using FoodServiceApp.Web.Models.Entities;
using FoodServiceApp.Web.Services;

namespace FoodServiceApp.Web.Controllers.Api
{
    /// <summary>
    /// API /api/mobile bổ sung cho phần Đối tác / Gian hàng của FoodApp.Maui:
    /// giờ hoạt động & nhận đơn, đánh giá + phản hồi, thông báo, tài chính (phí nền tảng), quên mật khẩu.
    /// Cần chạy Database/04_BoSung_DoiTac.sql (đã gộp sẵn trong 01_TaoMoi_Full.sql).
    /// </summary>
    [ApiController]
    [Route("api/mobile")]
    public class VendorMobileApiController : ControllerBase
    {
        private readonly AppDbContext _db;
        public VendorMobileApiController(AppDbContext db) => _db = db;

        private int CurrentMaTK => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        private static IActionResult Loi(string thongDiep, int status = 400) =>
            new ObjectResult(new { thongDiep }) { StatusCode = status };

        private Task<GianHang?> OwnStoreAsync(int id) =>
            _db.GianHangs.FirstOrDefaultAsync(g => g.MaGianHang == id && g.MaTK == CurrentMaTK);

        // ------------------------------------------------------------------ giờ hoạt động / nhận đơn
        public record GioHoatDongRequest(string? GioMoCua, string? GioDongCua, bool DangNhanDon, bool DangMoCua);

        [HttpPut("gian-hang/{id:int}/gio-hoat-dong")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "GianHang")]
        public async Task<IActionResult> GioHoatDong(int id, GioHoatDongRequest request)
        {
            var store = await OwnStoreAsync(id);
            if (store == null) return Loi("Không tìm thấy gian hàng.", 404);
            if (!TimeSpan.TryParse(request.GioMoCua, out var mo) || !TimeSpan.TryParse(request.GioDongCua, out var dong))
                return Loi("Giờ mở/đóng cửa không hợp lệ (định dạng HH:mm).");
            store.GioMoCua = mo;
            store.GioDongCua = dong;
            store.DangNhanDon = request.DangNhanDon;
            if (store.TrangThaiDuyet == "DaDuyet") store.TrangThaiKinhDoanh = request.DangMoCua ? "DangMo" : "DongCua";
            await _db.SaveChangesAsync();
            return Ok(new { gioMoCua = $"{mo:hh\\:mm}", gioDongCua = $"{dong:hh\\:mm}", dangNhanDon = store.DangNhanDon, dangMoCua = store.TrangThaiKinhDoanh == "DangMo" });
        }

        // ------------------------------------------------------------------ đánh giá + phản hồi
        [HttpGet("gian-hang/{id:int}/danh-gia")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "GianHang")]
        public async Task<IActionResult> DanhGia(int id)
        {
            if (await OwnStoreAsync(id) == null) return Loi("Bạn không có quyền xem gian hàng này.", 403);
            var ds = await (from d in _db.DanhGias
                            join k in _db.KhachHangs on d.MaKH equals k.MaKH
                            where d.MaGianHang == id
                            orderby d.NgayDanhGia descending
                            select new
                            {
                                id = d.MaDanhGia,
                                donHangId = d.MaDonHang,
                                khachTen = k.HoTen,
                                soSao = d.SoSao,
                                soSaoGiaoHang = d.SoSaoGiaoHang,
                                theNhanXet = d.TheNhanXet ?? "",
                                noiDung = d.NoiDung ?? "",
                                ngay = d.NgayDanhGia,
                                phanHoi = d.PhanHoi ?? "",
                                ngayPhanHoi = d.NgayPhanHoi
                            }).ToListAsync();
            return Ok(ds);
        }

        public record PhanHoiRequest(string? NoiDung);

        [HttpPut("gian-hang/{id:int}/danh-gia/{maDanhGia:int}/phan-hoi")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "GianHang")]
        public async Task<IActionResult> PhanHoiDanhGia(int id, int maDanhGia, PhanHoiRequest request)
        {
            if (await OwnStoreAsync(id) == null) return Loi("Bạn không có quyền với gian hàng này.", 403);
            var dg = await _db.DanhGias.FirstOrDefaultAsync(d => d.MaDanhGia == maDanhGia && d.MaGianHang == id);
            if (dg == null) return Loi("Không tìm thấy đánh giá.", 404);
            var nd = request.NoiDung?.Trim();
            if (string.IsNullOrWhiteSpace(nd)) return Loi("Nội dung phản hồi không được để trống.");
            if (nd.Length > 500) return Loi("Phản hồi tối đa 500 ký tự.");
            dg.PhanHoi = nd;
            dg.NgayPhanHoi = DateTime.Now;
            await _db.SaveChangesAsync();
            return Ok(new { phanHoi = dg.PhanHoi, ngayPhanHoi = dg.NgayPhanHoi });
        }

        // ------------------------------------------------------------------ thông báo (suy ra từ đơn hàng & đánh giá gần đây)
        [HttpGet("gian-hang/{id:int}/thong-bao")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "GianHang")]
        public async Task<IActionResult> ThongBao(int id)
        {
            if (await OwnStoreAsync(id) == null) return Loi("Bạn không có quyền xem gian hàng này.", 403);
            var tu = DateTime.Now.AddDays(-7);
            var don = await _db.DonHangs.Where(d => d.MaGianHang == id && d.NgayDat >= tu).OrderByDescending(d => d.NgayDat).Take(50).ToListAsync();
            var dg = await _db.DanhGias.Where(d => d.MaGianHang == id && d.NgayDanhGia >= tu).OrderByDescending(d => d.NgayDanhGia).Take(20).ToListAsync();

            var ds = new List<(DateTime Luc, object Item)>();
            foreach (var d in don)
            {
                if (d.TrangThai == "ChoXacNhan")
                    ds.Add((d.NgayDat, new { loai = "DonMoi", donHangId = d.MaDonHang, tieuDe = $"Đơn mới #{d.MaDonHang}", noiDung = $"Tổng {d.TongTien:N0}đ — hãy phản hồi trong 5 phút.", ngay = d.NgayDat }));
                else if (d.TrangThai == "DaHuy")
                    ds.Add((d.NgayDat, new { loai = "HuyDon", donHangId = d.MaDonHang, tieuDe = $"Đơn #{d.MaDonHang} đã hủy", noiDung = d.LyDoTuChoi ?? "", ngay = d.NgayDat }));
            }
            foreach (var d in dg)
                ds.Add((d.NgayDanhGia, new { loai = "DanhGiaMoi", donHangId = d.MaDonHang, tieuDe = $"Đánh giá mới {d.SoSao}★", noiDung = d.NoiDung ?? "", ngay = d.NgayDanhGia }));
            return Ok(ds.OrderByDescending(x => x.Luc).Select(x => x.Item));
        }

        // ------------------------------------------------------------------ tài chính: doanh thu, phí nền tảng, thực nhận
        [HttpGet("gian-hang/{id:int}/tai-chinh")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "GianHang")]
        public async Task<IActionResult> TaiChinh(int id, DateTime? tu, DateTime? den)
        {
            var store = await OwnStoreAsync(id);
            if (store == null) return Loi("Bạn không có quyền xem gian hàng này.", 403);
            var from = (tu ?? DateTime.Today.AddDays(-29)).Date;
            var to = (den ?? DateTime.Today).Date.AddDays(1);

            var don = await _db.DonHangs.Include(d => d.ChiTiets)
                .Where(d => d.MaGianHang == id && d.TrangThai == "DaGiao" && d.NgayDat >= from && d.NgayDat < to).ToListAsync();
            // Doanh thu món = tiền hàng (không tính phí giao: phí giao thuộc về tài xế)
            var doanhThu = don.Sum(d => d.ChiTiets.Sum(c => c.DonGia * c.SoLuong));
            var phiNenTang = Math.Round(doanhThu * store.TyLeChietKhau, 0);
            var giaVon = (await (from c in _db.ChiTietDonHangs
                                 join d in _db.DonHangs on c.MaDonHang equals d.MaDonHang
                                 join m in _db.MonAns on c.MaMonAn equals m.MaMonAn
                                 where d.MaGianHang == id && d.TrangThai == "DaGiao" && d.NgayDat >= from && d.NgayDat < to
                                 select (decimal?)(m.GiaVon * c.SoLuong)).SumAsync()) ?? 0m;
            return Ok(new
            {
                tu = from, den = to.AddDays(-1),
                soDon = don.Count,
                doanhThuMon = doanhThu,
                tyLeChietKhau = store.TyLeChietKhau,
                phiNenTang,
                thucNhan = doanhThu - phiNenTang,
                giaVon,
                loiNhuan = doanhThu - phiNenTang - giaVon
            });
        }

        // ------------------------------------------------------------------ quên mật khẩu (không có OTP/email → hạn chế số lần thử)
        public record QuenMatKhauRequest(string? Email, string? SoDienThoai, string? MatKhauMoi);
        private static readonly ConcurrentDictionary<string, (int Lan, DateTime Tu)> LanThu = new();

        [HttpPost("gian-hang/quen-mat-khau")]
        [AllowAnonymous]
        public async Task<IActionResult> QuenMatKhau(QuenMatKhauRequest request)
        {
            var email = request.Email?.Trim() ?? "";
            var sdt = (request.SoDienThoai ?? "").Replace(" ", "");
            if (email.Length == 0 || sdt.Length == 0) return Loi("Vui lòng nhập email và số điện thoại đã đăng ký.");
            if (string.IsNullOrEmpty(request.MatKhauMoi) || request.MatKhauMoi.Length < 6) return Loi("Mật khẩu mới tối thiểu 6 ký tự.");

            var khoa = $"{HttpContext.Connection.RemoteIpAddress}|{email.ToLowerInvariant()}";
            var tt = LanThu.GetValueOrDefault(khoa);
            if (tt.Lan >= 5 && DateTime.Now - tt.Tu < TimeSpan.FromMinutes(15)) return Loi("Thử quá nhiều lần. Vui lòng thử lại sau 15 phút.", 429);
            if (DateTime.Now - tt.Tu >= TimeSpan.FromMinutes(15)) tt = (0, DateTime.Now);

            var local = email.Contains('@') ? email[..email.IndexOf('@')] : email;
            var tk = await _db.TaiKhoans.FirstOrDefaultAsync(t => (t.TenDangNhap == email || t.TenDangNhap == local) && t.VaiTro == "GianHang");
            var gh = tk == null ? null : await _db.GianHangs.FirstOrDefaultAsync(g => g.MaTK == tk.MaTK);
            if (tk == null || gh == null || gh.SDT.Replace(" ", "") != sdt)
            {
                LanThu[khoa] = (tt.Lan + 1, tt.Tu == default ? DateTime.Now : tt.Tu);
                return Loi("Email và số điện thoại không khớp với gian hàng nào.");
            }
            tk.MatKhau = MatKhauHelper.Hash(request.MatKhauMoi);
            LanThu.TryRemove(khoa, out _);
            await _db.SaveChangesAsync();
            return Ok(new { message = "Đã đặt lại mật khẩu." });
        }
    }
}
