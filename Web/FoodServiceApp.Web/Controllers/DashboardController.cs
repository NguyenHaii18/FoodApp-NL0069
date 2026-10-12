using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FoodServiceApp.Web.Data;
using FoodServiceApp.Web.Models.ViewModels;

namespace FoodServiceApp.Web.Controllers
{
    public class DashboardController : GianHangBaseController
    {
        private readonly AppDbContext _db;

        public DashboardController(AppDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            var gianHang = await _db.GianHangs.FirstAsync(g => g.MaGianHang == CurrentGianHangId);
            var homNay = DateTime.Today;

            var donHomNay = await _db.DonHangs
                .Where(d => d.MaGianHang == CurrentGianHangId && d.NgayDat >= homNay)
                .ToListAsync();

            var soMonAn = await _db.MonAns
                .CountAsync(m => m.DanhMuc!.MaGianHang == CurrentGianHangId);

            var donMoiNhat = await _db.DonHangs
                .Where(d => d.MaGianHang == CurrentGianHangId)
                .OrderByDescending(d => d.NgayDat)
                .Take(5)
                .ToListAsync();

            var vm = new DashboardViewModel
            {
                GianHang = gianHang,
                DoanhThuHomNay = donHomNay.Where(d => d.TrangThai == "DaGiao").Sum(d => d.TongTien),
                SoDonHomNay = donHomNay.Count(d => d.TrangThai == "DaGiao"),
                SoDonChoXacNhan = await _db.DonHangs.CountAsync(d => d.MaGianHang == CurrentGianHangId && d.TrangThai == "ChoXacNhan"),
                SoMonAn = soMonAn,
                DonMoiNhat = donMoiNhat,
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleMoCua()
        {
            var gianHang = await _db.GianHangs.FirstAsync(g => g.MaGianHang == CurrentGianHangId);
            gianHang.TrangThaiKinhDoanh = gianHang.TrangThaiKinhDoanh == "DangMo" ? "DongCua" : "DangMo";
            await _db.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        // Trang chờ duyệt — hiển thị khi gian hàng vừa đăng ký, chưa được Quản trị duyệt.
        // GianHangBaseController tự động điều hướng mọi trang khác về đây cho tới khi duyệt xong.
        public async Task<IActionResult> ChoDuyet()
        {
            var gianHang = await _db.GianHangs.FirstAsync(g => g.MaGianHang == CurrentGianHangId);
            return View(gianHang);
        }
    }
}
