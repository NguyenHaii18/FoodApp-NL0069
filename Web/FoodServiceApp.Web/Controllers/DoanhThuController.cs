using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FoodServiceApp.Web.Data;
using FoodServiceApp.Web.Models.ViewModels;

namespace FoodServiceApp.Web.Controllers
{
    public class DoanhThuController : GianHangBaseController
    {
        private readonly AppDbContext _db;

        public DoanhThuController(AppDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            var tuNgay = DateTime.Today.AddDays(-29);

            var donHoanThanh = await _db.DonHangs
                .Include(d => d.ChiTiets).ThenInclude(c => c.MonAn)
                .Where(d => d.MaGianHang == CurrentGianHangId
                            && d.TrangThai == "DaGiao"
                            && d.NgayDat >= tuNgay)
                .ToListAsync();

            var doanhThuTheoNgay = donHoanThanh
                .GroupBy(d => d.NgayDat.Date)
                .OrderBy(g => g.Key)
                .Select(g => (g.Key, g.Sum(d => d.TongTien)))
                .ToList();

            var monBanChay = donHoanThanh
                .SelectMany(d => d.ChiTiets)
                .GroupBy(c => c.MonAn?.TenMon ?? "Không rõ")
                .Select(g => (g.Key, g.Sum(c => c.SoLuong)))
                .OrderByDescending(x => x.Item2)
                .Take(5)
                .ToList();

            var vm = new DoanhThuViewModel
            {
                TongDoanhThu30Ngay = donHoanThanh.Sum(d => d.TongTien),
                TongDonHoanThanh30Ngay = donHoanThanh.Count,
                DoanhThuTheoNgay = doanhThuTheoNgay,
                MonBanChay = monBanChay,
            };

            return View(vm);
        }
    }
}
