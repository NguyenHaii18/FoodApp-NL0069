using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FoodServiceApp.Web.Data;
using FoodServiceApp.Web.Models.Entities;

namespace FoodServiceApp.Web.Controllers
{
    [Route("QuanTri/GianHang")]
    [FoodServiceApp.Web.Filters.ChiSuperAdminGhi]
    public class AdminGianHangController : QuanTriBaseController
    {
        private readonly AppDbContext _db;

        public AdminGianHangController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index(string? timKiem)
        {
            var query = _db.GianHangs
                .Where(g => g.TrangThaiDuyet == "DaDuyet")
                .Join(_db.TaiKhoans, g => g.MaTK, t => t.MaTK, (g, t) => new { GianHang = g, TaiKhoan = t });

            if (!string.IsNullOrWhiteSpace(timKiem))
                query = query.Where(x => x.GianHang.TenCuaHang.Contains(timKiem));

            var danhSach = await query.OrderByDescending(x => x.GianHang.NgayDangKy).ToListAsync();

            ViewBag.TimKiem = timKiem ?? "";
            ViewBag.SoChoDuyet = await _db.GianHangs.CountAsync(g => g.TrangThaiDuyet == "ChoDuyet");
            return View(danhSach.Select(x => (x.GianHang, x.TaiKhoan)).ToList());
        }

        // Danh sách gian hàng mới đăng ký, đang chờ Quản trị duyệt
        [HttpGet("ChoDuyet")]
        public async Task<IActionResult> ChoDuyet()
        {
            var danhSach = await _db.GianHangs
                .Where(g => g.TrangThaiDuyet == "ChoDuyet")
                .OrderBy(g => g.NgayDangKy)
                .ToListAsync();
            return View(danhSach);
        }

        [HttpPost("Duyet/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Duyet(int id)
        {
            var gianHang = await _db.GianHangs
                .FirstOrDefaultAsync(g => g.MaGianHang == id);

            if (gianHang == null)
                return NotFound();

            if (gianHang.TrangThaiDuyet != "ChoDuyet")
            {
                TempData["LoiThongBao"] =
                    "Chỉ có thể duyệt hồ sơ đang chờ xét duyệt.";

                return RedirectToAction("ChoDuyet");
            }

            gianHang.TrangThaiDuyet = "DaDuyet";

            // Được duyệt không có nghĩa là tự động mở bán
            gianHang.TrangThaiKinhDoanh = "DongCua";
            gianHang.DangNhanDon = false;
            gianHang.YeuCauBoSung = null;
            gianHang.LyDoTuChoi = null;

            await _db.SaveChangesAsync();

            TempData["ThongBao"] =
                $"Đã duyệt gian hàng \"{gianHang.TenCuaHang}\".";

            return RedirectToAction("ChoDuyet");
        }

        [HttpPost("BoSung/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BoSung(int id, string? yeuCau)
        {
            var gianHang = await _db.GianHangs
                .FirstOrDefaultAsync(g => g.MaGianHang == id);

            if (gianHang == null)
                return NotFound();

            if (gianHang.TrangThaiDuyet != "ChoDuyet")
            {
                TempData["LoiThongBao"] =
                    "Chỉ có thể yêu cầu bổ sung hồ sơ đang chờ duyệt.";

                return RedirectToAction("ChoDuyet");
            }

            if (string.IsNullOrWhiteSpace(yeuCau))
            {
                TempData["LoiThongBao"] =
                    "Vui lòng nhập nội dung cần bổ sung.";

                return RedirectToAction("ChoDuyet");
            }

            gianHang.TrangThaiDuyet = "CanBoSung";
            gianHang.TrangThaiKinhDoanh = "DongCua";
            gianHang.DangNhanDon = false;
            gianHang.YeuCauBoSung = yeuCau.Trim();

            _db.ThongBaos.Add(new ThongBao
            {
                MaTK = gianHang.MaTK,
                Loai = "HeThong",
                TieuDe = "Yêu cầu bổ sung hồ sơ gian hàng",
                NoiDung = gianHang.YeuCauBoSung
            });

            await _db.SaveChangesAsync();

            TempData["ThongBao"] =
                $"Đã yêu cầu gian hàng \"{gianHang.TenCuaHang}\" bổ sung hồ sơ.";

            return RedirectToAction("ChoDuyet");
        }


        [HttpPost("TuChoi/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TuChoi(int id, string? lyDo)
        {
            var gianHang = await _db.GianHangs.FirstOrDefaultAsync(g => g.MaGianHang == id);
            if (gianHang == null) return NotFound();

            if (string.IsNullOrWhiteSpace(lyDo))
            {
                TempData["LoiThongBao"] = "Vui lòng nhập lý do từ chối.";
                return RedirectToAction("ChoDuyet");
            }
            gianHang.TrangThaiDuyet = "TuChoi";
            gianHang.LyDoTuChoi = lyDo.Trim();
            _db.ThongBaos.Add(new ThongBao { MaTK = gianHang.MaTK, Loai = "HeThong", TieuDe = "Hồ sơ gian hàng bị từ chối", NoiDung = "Lý do: " + gianHang.LyDoTuChoi });
            await _db.SaveChangesAsync();

            TempData["ThongBao"] = $"Đã từ chối hồ sơ \"{gianHang.TenCuaHang}\".";
            return RedirectToAction("ChoDuyet");
        }

        [HttpGet("ChiTiet/{id:int}")]
        public async Task<IActionResult> ChiTiet(int id)
        {
            var gianHang = await _db.GianHangs.FirstOrDefaultAsync(g => g.MaGianHang == id);
            if (gianHang == null) return NotFound();

            var taiKhoan = await _db.TaiKhoans.FirstAsync(t => t.MaTK == gianHang.MaTK);

            var donHoanThanh = await _db.DonHangs
                .Where(d => d.MaGianHang == id && d.TrangThai == "DaGiao")
                .ToListAsync();

            ViewBag.TaiKhoan = taiKhoan;
            ViewBag.TongDoanhThu = donHoanThanh.Sum(d => d.TongTien);
            ViewBag.TongDonHoanThanh = donHoanThanh.Count;
            ViewBag.SoMonAn = await _db.MonAns.CountAsync(m => m.DanhMuc!.MaGianHang == id);
            ViewBag.SoDanhGia = await _db.DanhGias.CountAsync(dg => dg.MaGianHang == id);
            ViewBag.DiemTrungBinh = await _db.DanhGias
                .Where(dg => dg.MaGianHang == id)
                .Select(dg => (double?)dg.SoSao)
                .AverageAsync() ?? 0;

            return View(gianHang);
        }

        /// <summary>Đặt hoa hồng/phí nền tảng riêng cho một gian hàng (0–100%). Admin tổng mới ghi được.</summary>
        [HttpPost("HoaHong/{maGianHang:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DatHoaHong(int maGianHang, decimal phanTram)
        {
            var gh = await _db.GianHangs.FirstOrDefaultAsync(g => g.MaGianHang == maGianHang);
            if (gh == null) return NotFound();
            if (phanTram < 0 || phanTram > 100) { TempData["LoiThongBao"] = "Hoa hồng phải từ 0 đến 100%."; return RedirectToAction("ChiTiet", new { id = maGianHang }); }
            gh.TyLeChietKhau = Math.Round(phanTram / 100m, 4);
            await _db.SaveChangesAsync();
            TempData["ThongBao"] = $"Đã đặt hoa hồng của \"{gh.TenCuaHang}\" = {phanTram:0.##}% (áp dụng cho báo cáo từ nay).";
            return RedirectToAction("ChiTiet", new { id = maGianHang });
        }

        [HttpPost("ToggleKhoa/{maTK:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleKhoa(int maTK)
        {
            var taiKhoan = await _db.TaiKhoans.FirstOrDefaultAsync(t => t.MaTK == maTK && t.VaiTro == "GianHang");
            if (taiKhoan == null) return NotFound();

            taiKhoan.TrangThai = !taiKhoan.TrangThai;
            await _db.SaveChangesAsync();

            TempData["ThongBao"] = taiKhoan.TrangThai
                ? "Đã mở khoá tài khoản gian hàng."
                : "Đã khoá tài khoản gian hàng (do vi phạm/khiếu nại).";
            return RedirectToAction("Index");
        }
    }
}
