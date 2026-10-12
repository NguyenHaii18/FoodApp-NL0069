using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FoodServiceApp.Web.Data;
using FoodServiceApp.Web.Models.Entities;
using FoodServiceApp.Web.Services;

namespace FoodServiceApp.Web.Controllers
{
    /// <summary>Quản lý đơn hàng toàn hệ thống: tìm kiếm, lọc, chi tiết, lịch sử trạng thái, đơn trễ/kẹt, can thiệp hủy đơn (bắt buộc lý do).</summary>
    [Route("QuanTri/DonHang")]
    [FoodServiceApp.Web.Filters.CoQuyenGhi("DonHang")]
    public class AdminDonHangController : AdminOpsBase
    {
        private readonly CauHinhService _cauHinh;
        public AdminDonHangController(AppDbContext db, CauHinhService cauHinh) : base(db) { _cauHinh = cauHinh; }

        private IQueryable<DonHang> Loc(string? q, string? trangThai, string? thanhToan, DateTime? tu, DateTime? den)
        {
            var query = Db.DonHangs.Include(d => d.KhachHang).Include(d => d.GianHang)
                .Include(d => d.ThanhToan).Include(d => d.GiaoHang).ThenInclude(g => g!.TaiXe).AsQueryable();
            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim();
                if (int.TryParse(q.TrimStart('#'), out var ma))
                    query = query.Where(d => d.MaDonHang == ma);
                else
                    query = query.Where(d => d.KhachHang!.HoTen.Contains(q) || d.KhachHang!.SDT.Contains(q)
                        || d.GianHang!.TenCuaHang.Contains(q)
                        || (d.GiaoHang != null && d.GiaoHang.TaiXe!.HoTen.Contains(q)));
            }
            if (!string.IsNullOrEmpty(trangThai)) query = query.Where(d => d.TrangThai == trangThai);
            if (!string.IsNullOrEmpty(thanhToan)) query = query.Where(d => d.ThanhToan != null && d.ThanhToan.HinhThuc == thanhToan);
            if (tu != null) query = query.Where(d => d.NgayDat >= tu.Value.Date);
            if (den != null) query = query.Where(d => d.NgayDat < den.Value.Date.AddDays(1));
            return query;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index(string? q, string? trangThai, string? thanhToan, DateTime? tu, DateTime? den, string? bo, string? xuat)
        {
            var query = Loc(q, trangThai, thanhToan, tu, den);
            var tre = (int)await _cauHinh.LaySoAsync("Don.DuKienPhut", 45) + (int)await _cauHinh.LaySoAsync("Don.TreSauPhut", 30);
            var ket = (int)await _cauHinh.LaySoAsync("Don.KetTrangThaiPhut", 60);
            var now = DateTime.Now;

            if (bo == "tre")
            { var moc = now.AddMinutes(-tre); query = query.Where(d => d.TrangThai != "DaGiao" && d.TrangThai != "DaHuy" && d.NgayDat < moc); }
            else if (bo == "that-bai")
                query = query.Where(d => d.GiaoHang != null && d.GiaoHang.TrangThai == "GiaoThatBai");
            else if (bo == "khieu-nai")
            { var maDons = Db.PhanHois.Where(p => p.MaDonHang != null && p.Loai == "KhieuNai").Select(p => p.MaDonHang!.Value); query = query.Where(d => maDons.Contains(d.MaDonHang)); }

            var ds = await query.OrderByDescending(d => d.NgayDat).Take(xuat == "csv" ? 5000 : 200).ToListAsync();

            if (xuat == "csv")
                return Csv("DonHang", new[] { "Mã đơn", "Ngày đặt", "Khách hàng", "SĐT", "Gian hàng", "Tài xế", "Trạng thái", "Thanh toán", "TT thanh toán", "Phí giao", "Giảm giá", "Tổng tiền" },
                    ds.Select(d => new object?[] { d.MaDonHang, d.NgayDat, d.KhachHang?.HoTen, d.KhachHang?.SDT, d.GianHang?.TenCuaHang, d.GiaoHang?.TaiXe?.HoTen, d.TrangThai, d.ThanhToan?.HinhThuc, d.ThanhToan?.TrangThai, d.PhiGiaoHang, d.TienGiam, d.TongTien }));

            // thời điểm chuyển trạng thái gần nhất -> phát hiện đơn kẹt
            var ids = ds.Select(d => d.MaDonHang).ToList();
            var gan = await Db.LichSuTrangThaiDons.Where(l => ids.Contains(l.MaDonHang))
                .GroupBy(l => l.MaDonHang).Select(g => new { Ma = g.Key, T = g.Max(x => x.ThoiGian) }).ToDictionaryAsync(x => x.Ma, x => x.T);
            ViewBag.Tre = ds.Where(d => !TrangThaiCuoi.Contains(d.TrangThai) && (now - d.NgayDat).TotalMinutes > tre).Select(d => d.MaDonHang).ToHashSet();
            ViewBag.Ket = ds.Where(d => !TrangThaiCuoi.Contains(d.TrangThai) && (now - (gan.TryGetValue(d.MaDonHang, out var t) ? t : d.NgayDat)).TotalMinutes > ket).Select(d => d.MaDonHang).ToHashSet();
            ViewBag.Q = q; ViewBag.TrangThai = trangThai; ViewBag.ThanhToan = thanhToan; ViewBag.Tu = tu?.ToString("yyyy-MM-dd"); ViewBag.Den = den?.ToString("yyyy-MM-dd"); ViewBag.Bo = bo;
            ViewBag.TreSauPhut = tre; ViewBag.KetPhut = ket;
            return View(ds);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> ChiTiet(int id)
        {
            var don = await Db.DonHangs.Include(d => d.KhachHang).Include(d => d.GianHang)
                .Include(d => d.ChiTiets).ThenInclude(c => c.MonAn)
                .Include(d => d.ThanhToan).Include(d => d.GiaoHang).ThenInclude(g => g!.TaiXe)
                .FirstOrDefaultAsync(d => d.MaDonHang == id);
            if (don == null) return NotFound();
            ViewBag.LichSu = await Db.LichSuTrangThaiDons.Where(l => l.MaDonHang == id).OrderBy(l => l.ThoiGian).ToListAsync();
            try
            {
                ViewBag.GiaoDichTT = await Db.GiaoDichThanhToans.AsNoTracking().Where(l => l.MaDonHang == id).OrderBy(l => l.ThoiGian).ToListAsync();
                ViewBag.LichSuGH = await Db.LichSuGiaoHangs.AsNoTracking().Where(l => l.MaDonHang == id).OrderBy(l => l.ThoiGian).ToListAsync();
            }
            catch { /* chưa chạy NangCap_v15 */ }
            ViewBag.DieuPhoi = await Db.DieuPhoiLogs.Where(l => l.MaDonHang == id).OrderBy(l => l.ThoiGian).ToListAsync();
            ViewBag.HoanTien = await Db.YeuCauHoanTiens.Where(y => y.MaDonHang == id).OrderByDescending(y => y.NgayTao).ToListAsync();
            ViewBag.PhanHoi = await Db.PhanHois.Where(p => p.MaDonHang == id).OrderByDescending(p => p.NgayGui).ToListAsync();
            ViewBag.NguoiTao = await Db.TaiKhoans.Where(t => Db.LichSuTrangThaiDons.Any(l => l.MaDonHang == id && l.MaTK == t.MaTK)).ToDictionaryAsync(t => t.MaTK, t => t.TenDangNhap);
            return View(don);
        }

        /// <summary>Mở khóa nhập mã xác nhận (đơn bị khóa vì nhập sai quá 5 lần) và cấp mã mới để khách đọc lại.</summary>
        [HttpPost("{id:int}/mo-khoa-ma")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MoKhoaMa(int id)
        {
            var don = await Db.DonHangs.FirstOrDefaultAsync(d => d.MaDonHang == id);
            if (don == null) return NotFound();
            if (don.TrangThai != "DangGiao") { TempData["LoiThongBao"] = "Chỉ đơn đang giao mới đổi mã."; return RedirectToAction(nameof(ChiTiet), new { id }); }
            don.SoLanSaiMa = 0; don.MaXacNhanGiao = FoodServiceApp.Web.Services.GiaoHangXuLy.TaoMa();
            await Db.SaveChangesAsync();
            TempData["ThongBao"] = "Đã mở khóa và cấp mã xác nhận mới; khách xem mã mới trong đơn hàng.";
            return RedirectToAction(nameof(ChiTiet), new { id });
        }

        /// <summary>Hủy đơn do quản trị can thiệp — bắt buộc ghi lý do; đơn đã thanh toán online tự tạo yêu cầu hoàn tiền chờ duyệt.</summary>
        [HttpPost("{id:int}/huy")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Huy(int id, string? lyDo)
        {
            var don = await Db.DonHangs.Include(d => d.ThanhToan).FirstOrDefaultAsync(d => d.MaDonHang == id);
            if (don == null) return NotFound();
            lyDo = (lyDo ?? "").Trim();
            if (lyDo.Length < 5) { TempData["LoiThongBao"] = "Bắt buộc nhập lý do can thiệp (ít nhất 5 ký tự)."; return RedirectToAction(nameof(ChiTiet), new { id }); }
            if (TrangThaiCuoi.Contains(don.TrangThai)) { TempData["LoiThongBao"] = "Đơn đã kết thúc, không thể hủy."; return RedirectToAction(nameof(ChiTiet), new { id }); }

            var cu = don.TrangThai;
            don.TrangThai = "DaHuy";
            don.LyDoTuChoi = "[Quản trị] " + lyDo;
            await Db.SaveChangesAsync();

            // đánh dấu người can thiệp vào bản ghi lịch sử vừa ghi
            var ls = await Db.LichSuTrangThaiDons.Where(l => l.MaDonHang == id && l.DenTrangThai == "DaHuy").OrderByDescending(l => l.ThoiGian).FirstOrDefaultAsync();
            if (ls != null) { ls.MaTK = MaTK; ls.LyDo = "[Quản trị] " + lyDo; }

            if (don.ThanhToan?.TrangThai == "DaThanhToan" && !await Db.YeuCauHoanTiens.AnyAsync(y => y.MaDonHang == id && y.TrangThai == "ChoDuyet"))
                Db.YeuCauHoanTiens.Add(new YeuCauHoanTien { MaDonHang = id, MaKH = don.MaKH, SoTien = don.TongTien, LyDo = "Quản trị hủy đơn: " + lyDo, Nguon = "QuanTri" });
            await Db.SaveChangesAsync();
            TempData["ThongBao"] = $"Đã hủy đơn #{id} (từ trạng thái {cu}).";
            return RedirectToAction(nameof(ChiTiet), new { id });
        }
    }
}
