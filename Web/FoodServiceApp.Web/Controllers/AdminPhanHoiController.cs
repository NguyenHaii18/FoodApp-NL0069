using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FoodServiceApp.Web.Data;
using FoodServiceApp.Web.Models.Entities;

namespace FoodServiceApp.Web.Controllers
{
    // Quản trị viên xem và xử lý phản hồi của khách hàng.
    [Route("QuanTri/PhanHoi")]
    public class AdminPhanHoiController : QuanTriBaseController
    {
        private readonly AppDbContext _db;

        public AdminPhanHoiController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index(string? trangThai)
        {
            var query = _db.PhanHois.Include(p => p.KhachHang).AsQueryable();
            if (trangThai == "ChuaXuLy" || trangThai == "DaXuLy")
                query = query.Where(p => p.TrangThai == trangThai);

            ViewBag.TrangThaiDangLoc = trangThai ?? "";
            ViewBag.SoChuaXuLy = await _db.PhanHois.CountAsync(p => p.TrangThai == "ChuaXuLy");
            ViewBag.NhanVien = await _db.QuanTris.ToDictionaryAsync(q => q.MaTK, q => q.HoTen);
            var daXuLy = await _db.PhanHois.Where(p => p.NgayXuLy != null).Select(p => new { p.NgayGui, p.NgayXuLy }).ToListAsync();
            ViewBag.TrungBinhGio = daXuLy.Any() ? daXuLy.Average(x => (x.NgayXuLy!.Value - x.NgayGui).TotalHours) : (double?)null;
            var maTKHienTai = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
            ViewBag.MaTKHienTai = maTKHienTai;
            ViewBag.LaQuanLy = await _db.QuanTris.AnyAsync(q => q.MaTK == maTKHienTai && (q.CapQuyenHan == "SuperAdmin" || q.CapQuyenHan == "QuanLy"));
            return View(await query.OrderBy(p => p.TrangThai == "DaXuLy").ThenByDescending(p => p.NgayGui).ToListAsync());
        }

        private async Task<bool> LaQuanLyAsync()
        {
            var maTK = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
            return await _db.QuanTris.AnyAsync(q => q.MaTK == maTK && (q.CapQuyenHan == "SuperAdmin" || q.CapQuyenHan == "QuanLy"));
        }

        [HttpPost("{id:int}/phan-cong")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PhanCong(int id, int maTK)
        {
            if (!await LaQuanLyAsync()) return Forbid();
            var ph = await _db.PhanHois.FirstOrDefaultAsync(p => p.MaPhanHoi == id);
            if (ph == null || !await _db.QuanTris.AnyAsync(q => q.MaTK == maTK)) return NotFound();
            ph.MaTKPhuTrach = maTK;
            await _db.SaveChangesAsync();
            TempData["ThongBao"] = $"Đã phân công xử lý phản hồi #{id}.";
            return RedirectToAction("Index");
        }

        [HttpPost("{id:int}/phan-loai")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PhanLoai(int id, string phanLoai)
        {
            var ds = new[] { "DonHang", "MonAn", "TaiXe", "GianHang", "ThanhToan", "Khac" };
            var ph = await _db.PhanHois.FirstOrDefaultAsync(p => p.MaPhanHoi == id);
            if (ph == null || !ds.Contains(phanLoai)) return NotFound();
            ph.PhanLoai = phanLoai;
            await _db.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        // Yêu cầu khách bổ sung bằng chứng/thông tin: khách thấy nội dung này ở trang Phản hồi của mình
        [HttpPost("{id:int}/bo-sung")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BoSung(int id, string? yeuCau)
        {
            var ph = await _db.PhanHois.FirstOrDefaultAsync(p => p.MaPhanHoi == id);
            if (ph == null) return NotFound();
            if (string.IsNullOrWhiteSpace(yeuCau)) { TempData["LoiThongBao"] = "Nhập nội dung cần khách bổ sung."; return RedirectToAction("Index"); }
            ph.YeuCauBoSung = yeuCau.Trim();
            var kh = await _db.KhachHangs.AsNoTracking().FirstOrDefaultAsync(k => k.MaKH == ph.MaKH);
            if (kh != null)
                _db.ThongBaos.Add(new ThongBao { MaTK = kh.MaTK, Loai = "HeThong", TieuDe = "Cần bổ sung thông tin cho yêu cầu hỗ trợ", NoiDung = $"Yêu cầu \"{ph.TieuDe}\": {ph.YeuCauBoSung}" });
            await _db.SaveChangesAsync();
            TempData["ThongBao"] = "Đã gửi yêu cầu bổ sung tới khách.";
            return RedirectToAction("Index");
        }

        // Khiếu nại đủ điều kiện -> chuyển sang quy trình hoàn tiền (Thanh toán & hoàn tiền sẽ duyệt/từ chối theo phân quyền)
        [HttpPost("{id:int}/chuyen-hoan-tien")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChuyenHoanTien(int id, decimal soTien)
        {
            if (!await LaQuanLyAsync()) return Forbid();
            var ph = await _db.PhanHois.FirstOrDefaultAsync(p => p.MaPhanHoi == id);
            if (ph == null || ph.MaDonHang == null) { TempData["LoiThongBao"] = "Phản hồi này không gắn với đơn hàng."; return RedirectToAction("Index"); }
            var don = await _db.DonHangs.Include(d => d.ThanhToan).FirstOrDefaultAsync(d => d.MaDonHang == ph.MaDonHang);
            if (don == null) return NotFound();
            if (don.ThanhToan?.TrangThai != "DaThanhToan") { TempData["LoiThongBao"] = "Đơn chưa có thanh toán thành công nên chưa thể hoàn tiền."; return RedirectToAction("Index"); }
            if (soTien <= 0 || soTien > don.TongTien) soTien = don.TongTien;
            if (await _db.YeuCauHoanTiens.AnyAsync(y => y.MaDonHang == don.MaDonHang && y.TrangThai == "ChoDuyet"))
            { TempData["LoiThongBao"] = "Đơn này đã có yêu cầu hoàn tiền đang chờ duyệt."; return RedirectToAction("Index"); }
            _db.YeuCauHoanTiens.Add(new YeuCauHoanTien { MaDonHang = don.MaDonHang, MaKH = don.MaKH, SoTien = soTien, LyDo = $"Khiếu nại #{ph.MaPhanHoi}: {ph.TieuDe}", Nguon = "KhieuNai" });
            await _db.SaveChangesAsync();
            TempData["ThongBao"] = "Đã chuyển sang quy trình hoàn tiền (chờ duyệt ở mục Thanh toán & hoàn tiền).";
            return RedirectToAction("Index");
        }

        [HttpPost("{id:int}/xu-ly")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> XuLy(int id, string? traLoi)
        {
            var ph = await _db.PhanHois.FirstOrDefaultAsync(p => p.MaPhanHoi == id);
            if (ph == null) return NotFound();

            if (string.IsNullOrWhiteSpace(traLoi))
            {
                TempData["LoiThongBao"] = "Vui lòng nhập nội dung trả lời trước khi xử lý.";
                return RedirectToAction("Index");
            }

            ph.TraLoi = traLoi.Trim();
            ph.MaTKPhuTrach ??= int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
            ph.TrangThai = "DaXuLy";
            ph.NgayXuLy = DateTime.Now;
            await _db.SaveChangesAsync();

            TempData["ThongBao"] = $"Đã trả lời phản hồi #{ph.MaPhanHoi}.";
            return RedirectToAction("Index");
        }
    }
}
