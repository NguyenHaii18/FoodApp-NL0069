using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FoodServiceApp.Web.Data;

namespace FoodServiceApp.Web.Controllers
{
    [Route("QuanTri/ThongKe")]
    public class AdminThongKeController : QuanTriBaseController
    {
        private readonly AppDbContext _db;

        public AdminThongKeController(AppDbContext db)
        {
            _db = db;
        }

        // ky = Ngay (30 ngày gần nhất, nhóm theo ngày) / Thang (12 tháng gần nhất, nhóm theo tháng)
        //    / Nam (5 năm gần nhất, nhóm theo năm) — đúng yêu cầu đề cương "thống kê doanh thu
        // theo ngày, tháng hoặc năm".
        [HttpGet("")]
        public async Task<IActionResult> Index(string ky = "Ngay")
        {
            if (ky != "Thang" && ky != "Nam") ky = "Ngay";

            var tuNgay = ky switch
            {
                "Thang" => DateTime.Today.AddMonths(-11).AddDays(1 - DateTime.Today.Day),
                "Nam" => new DateTime(DateTime.Today.Year - 4, 1, 1),
                _ => DateTime.Today.AddDays(-29),
            };

            var donHoanThanh = await _db.DonHangs
                .Include(d => d.GianHang)
                .Include(d => d.ChiTiets).ThenInclude(c => c.MonAn)
                .Where(d => d.TrangThai == "DaGiao" && d.NgayDat >= tuNgay)
                .ToListAsync();

            ViewBag.KyBaoCao = ky;
            ViewBag.TongDoanhThu = donHoanThanh.Sum(d => d.TongTien);
            ViewBag.TongDonHoanThanh = donHoanThanh.Count;
            ViewBag.TongGianHangHoatDong = donHoanThanh.Select(d => d.MaGianHang).Distinct().Count();

            var doanhThuTheoKy = ky switch
            {
                "Thang" => donHoanThanh
                    .GroupBy(d => new DateTime(d.NgayDat.Year, d.NgayDat.Month, 1))
                    .OrderBy(g => g.Key)
                    .Select(g => new { Nhan = g.Key.ToString("MM/yyyy"), DoanhThu = g.Sum(d => d.TongTien) })
                    .ToList(),
                "Nam" => donHoanThanh
                    .GroupBy(d => d.NgayDat.Year)
                    .OrderBy(g => g.Key)
                    .Select(g => new { Nhan = g.Key.ToString(), DoanhThu = g.Sum(d => d.TongTien) })
                    .ToList(),
                _ => donHoanThanh
                    .GroupBy(d => d.NgayDat.Date)
                    .OrderBy(g => g.Key)
                    .Select(g => new { Nhan = g.Key.ToString("dd/MM"), DoanhThu = g.Sum(d => d.TongTien) })
                    .ToList(),
            };

            var monBanChayToanHeThong = donHoanThanh
                .SelectMany(d => d.ChiTiets)
                .GroupBy(c => c.MonAn!.TenMon)
                .Select(g => new { TenMon = g.Key, SoLuong = g.Sum(c => c.SoLuong) })
                .OrderByDescending(x => x.SoLuong)
                .Take(10)
                .ToList();

            var doanhThuTheoGianHang = donHoanThanh
                .GroupBy(d => d.GianHang!.TenCuaHang)
                .Select(g => new { TenGianHang = g.Key, DoanhThu = g.Sum(d => d.TongTien), SoDon = g.Count() })
                .OrderByDescending(x => x.DoanhThu)
                .ToList();

            // Hiệu quả khuyến mãi: gộp TOÀN BỘ đơn có dùng mã (không giới hạn theo kỳ báo cáo ở
            // trên, để phản ánh đúng hiệu quả từ lúc mã bắt đầu chạy) — số lần dùng, tổng doanh
            // thu các đơn có áp mã, ước tính tổng tiền đã giảm.
            var donCoKhuyenMai = await _db.DonHangs
                .Include(d => d.KhuyenMaiApDung)
                .Where(d => d.MaKhuyenMaiApDung != null && d.TrangThai != "DaHuy")
                .ToListAsync();

            var hieuQuaKhuyenMai = donCoKhuyenMai
                .GroupBy(d => d.KhuyenMaiApDung!.MaCode)
                .Select(g => new
                {
                    MaCode = g.Key,
                    SoLanDung = g.Count(),
                    DoanhThuTuMa = g.Sum(d => d.TongTien),
                    TongTienDaGiam = g.Sum(d =>
                        d.KhuyenMaiApDung!.PhanTramGiam != null
                            ? Math.Round((d.TongTien / (1 - d.KhuyenMaiApDung.PhanTramGiam.Value / 100m)) * (d.KhuyenMaiApDung.PhanTramGiam.Value / 100m))
                            : (d.KhuyenMaiApDung.SoTienGiam ?? 0)),
                })
                .OrderByDescending(x => x.SoLanDung)
                .ToList();

            ViewBag.NhanKy = doanhThuTheoKy.Select(x => x.Nhan).ToList();
            ViewBag.GiaTriKy = doanhThuTheoKy.Select(x => x.DoanhThu).ToList();
            ViewBag.MonBanChay = monBanChayToanHeThong;
            ViewBag.DoanhThuTheoGianHang = doanhThuTheoGianHang;
            ViewBag.HieuQuaKhuyenMai = hieuQuaKhuyenMai;

            return View();
        }
    }
}
