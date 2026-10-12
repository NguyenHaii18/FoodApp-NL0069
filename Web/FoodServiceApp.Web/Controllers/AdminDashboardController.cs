using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FoodServiceApp.Web.Data;

namespace FoodServiceApp.Web.Controllers
{
    [Route("QuanTri")]
    public class AdminDashboardController : QuanTriBaseController
    {
        private readonly AppDbContext _db;
        public AdminDashboardController(AppDbContext db) { _db = db; }

        // ky: hom-nay | 7-ngay | 30-ngay | nam | tu-chon (tuNgay, denNgay)
        [HttpGet("")]
        [HttpGet("Dashboard")]
        public async Task<IActionResult> Index(string ky = "hom-nay", DateTime? tuNgay = null, DateTime? denNgay = null)
        {
            var homNay = DateTime.Today;
            DateTime tu, den = DateTime.Now.AddMinutes(1);
            switch (ky)
            {
                case "7-ngay": tu = homNay.AddDays(-6); break;
                case "30-ngay": tu = homNay.AddDays(-29); break;
                case "nam": tu = new DateTime(homNay.Year, 1, 1); break;
                case "tu-chon" when tuNgay != null:
                    tu = tuNgay.Value.Date; den = (denNgay ?? tuNgay.Value).Date.AddDays(1); break;
                default: ky = "hom-nay"; tu = homNay; break;
            }
            ViewBag.Ky = ky; ViewBag.Tu = tu; ViewBag.Den = den;

            ViewBag.TongKhachHang = await _db.KhachHangs.CountAsync();
            ViewBag.TongGianHang = await _db.GianHangs.CountAsync();
            ViewBag.GianHangDangMo = await _db.GianHangs.CountAsync(g => g.TrangThaiKinhDoanh == "DangMo");
            ViewBag.TongTaiXe = await _db.TaiXes.CountAsync();
            ViewBag.TaiXeDaDuyet = await _db.TaiXes.CountAsync(t => t.TrangThaiDuyet == "DaDuyet");

            var donKy = _db.DonHangs.Where(d => d.NgayDat >= tu && d.NgayDat < den);
            ViewBag.TongDon = await donKy.CountAsync();
            ViewBag.DonTheoTrangThai = await donKy.GroupBy(d => d.TrangThai).Select(g => new { g.Key, SoLuong = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.SoLuong);
            ViewBag.DoanhThu = await donKy.Where(d => d.TrangThai == "DaGiao").SumAsync(d => (decimal?)d.TongTien) ?? 0;
            ViewBag.PhiNenTang = await donKy.Where(d => d.TrangThai == "DaGiao")
                .SumAsync(d => (decimal?)((d.TongTien - d.PhiGiaoHang) * d.GianHang!.TyLeChietKhau)) ?? 0;

            ViewBag.ThanhToan = await _db.ThanhToans
                .Where(t => donKy.Any(d => d.MaDonHang == t.MaDonHang))
                .GroupBy(t => t.TrangThai).Select(g => new { g.Key, SoLuong = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.SoLuong);

            ViewBag.KhieuNaiCho = await _db.PhanHois.CountAsync(p => p.Loai == "KhieuNai" && p.TrangThai == "ChuaXuLy");
            ViewBag.PhanHoiCho = await _db.PhanHois.CountAsync(p => p.TrangThai == "ChuaXuLy");
            ViewBag.HoanTienCho = await _db.YeuCauHoanTiens.CountAsync(y => y.TrangThai == "ChoDuyet");
            ViewBag.BaoCaoDanhGiaCho = await _db.BaoCaoDanhGias.CountAsync(b => b.TrangThai == "ChoXuLy");
            ViewBag.SoChoDuyet = await _db.GianHangs.CountAsync(g => g.TrangThaiDuyet == "ChoDuyet");
            ViewBag.TaiXeChoDuyet = await _db.TaiXes.CountAsync(t => t.TrangThaiDuyet == "ChoDuyet");
            ViewBag.SoSuCo = await _db.DonHangs.CountAsync(d =>
                d.TrangThai == "DangGiao" && d.GiaoHang != null && d.GiaoHang.TrangThai == "GiaoThatBai");

            ViewBag.TopGianHang = await donKy.Where(d => d.TrangThai == "DaGiao")
                .GroupBy(d => d.GianHang!.TenCuaHang)
                .Select(g => new { TenGianHang = g.Key, DoanhThu = g.Sum(x => x.TongTien), SoDon = g.Count() })
                .OrderByDescending(x => x.DoanhThu).Take(5).ToListAsync();

            ViewBag.TopMon = await _db.ChiTietDonHangs
                .Where(c => donKy.Any(d => d.MaDonHang == c.MaDonHang && d.TrangThai == "DaGiao"))
                .GroupBy(c => c.MonAn!.TenMon)
                .Select(g => new { TenMon = g.Key, SoLuong = g.Sum(c => c.SoLuong) })
                .OrderByDescending(x => x.SoLuong).Take(5).ToListAsync();

            // Khu vực nhiều đơn: lấy quận/huyện từ địa chỉ giao (phần áp chót khi địa chỉ có >= 3 phần cách nhau dấu phẩy)
            var diaChis = await donKy.OrderByDescending(d => d.NgayDat).Select(d => d.DiaChiGiaoHang).Take(5000).ToListAsync();
            ViewBag.KhuVuc = diaChis
                .Select(a => a.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
                .Where(p => p.Length > 0)
                .Select(p => p.Length >= 3 ? p[^2] : p[^1])
                .GroupBy(x => x).Select(g => new KeyValuePair<string, int>(g.Key, g.Count()))
                .OrderByDescending(x => x.Value).Take(6).ToList();

            return View();
        }
    }
}
