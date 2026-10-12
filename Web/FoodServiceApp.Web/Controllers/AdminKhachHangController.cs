using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FoodServiceApp.Web.Data;

namespace FoodServiceApp.Web.Controllers
{
    [Route("QuanTri/KhachHang")]
    [FoodServiceApp.Web.Filters.ChiSuperAdminGhi]
    public class AdminKhachHangController : QuanTriBaseController
    {
        private readonly AppDbContext _db;

        public AdminKhachHangController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index(string? timKiem)
        {
            var query = _db.KhachHangs
                .Join(_db.TaiKhoans, kh => kh.MaTK, tk => tk.MaTK, (kh, tk) => new { KhachHang = kh, TaiKhoan = tk });

            if (!string.IsNullOrWhiteSpace(timKiem))
                query = query.Where(x => x.KhachHang.HoTen.Contains(timKiem) || x.KhachHang.SDT.Contains(timKiem));

            var danhSach = await query.OrderByDescending(x => x.KhachHang.MaKH).ToListAsync();

            ViewBag.TimKiem = timKiem ?? "";
            return View(danhSach.Select(x => (x.KhachHang, x.TaiKhoan)).ToList());
        }

        [HttpGet("ChiTiet/{id:int}")]
        public async Task<IActionResult> ChiTiet(int id)
        {
            var kh = await _db.KhachHangs.FirstOrDefaultAsync(k => k.MaKH == id);
            if (kh == null) return NotFound();
            ViewBag.TaiKhoan = await _db.TaiKhoans.FirstAsync(t => t.MaTK == kh.MaTK);
            ViewBag.DonHangs = await _db.DonHangs.Include(d => d.GianHang).Where(d => d.MaKH == id).OrderByDescending(d => d.NgayDat).Take(50).ToListAsync();
            ViewBag.DiaChis = await _db.DiaChiKhachHangs.Where(d => d.MaKH == id).ToListAsync();
            ViewBag.PhanHois = await _db.PhanHois.Where(p => p.MaKH == id).OrderByDescending(p => p.NgayGui).Take(20).ToListAsync();
            ViewBag.BaoCao = await _db.BaoCaoDanhGias.CountAsync(b => b.MaKH == id);
            return View(kh);
        }

        // Xử lý vi phạm: khóa tài khoản kèm lý do (ghi nhật ký tự động + thông báo)
        [HttpPost("XuLyViPham/{maTK:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> XuLyViPham(int maTK, string? lyDo)
        {
            var tk = await _db.TaiKhoans.FirstOrDefaultAsync(t => t.MaTK == maTK && t.VaiTro == "KhachHang");
            if (tk == null) return NotFound();
            if (string.IsNullOrWhiteSpace(lyDo)) { TempData["LoiThongBao"] = "Nhập lý do xử lý vi phạm."; return RedirectToAction("Index"); }
            tk.TrangThai = false;
            _db.ThongBaos.Add(new FoodServiceApp.Web.Models.Entities.ThongBao { MaTK = tk.MaTK, Loai = "HeThong", TieuDe = "Tài khoản bị khóa do vi phạm", NoiDung = "Lý do: " + lyDo.Trim() });
            await _db.SaveChangesAsync();
            TempData["ThongBao"] = "Đã khóa tài khoản vi phạm và thông báo cho khách.";
            return RedirectToAction("Index");
        }

        [HttpPost("ToggleKhoa/{maTK:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleKhoa(int maTK)
        {
            var taiKhoan = await _db.TaiKhoans.FirstOrDefaultAsync(t => t.MaTK == maTK && t.VaiTro == "KhachHang");
            if (taiKhoan == null) return NotFound();

            taiKhoan.TrangThai = !taiKhoan.TrangThai;
            await _db.SaveChangesAsync();

            TempData["ThongBao"] = taiKhoan.TrangThai ? "Đã mở khoá tài khoản." : "Đã khoá tài khoản.";
            return RedirectToAction("Index");
        }
    }
}
