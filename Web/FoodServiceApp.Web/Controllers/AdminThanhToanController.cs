using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FoodServiceApp.Web.Data;
using FoodServiceApp.Web.Models.Entities;
using FoodServiceApp.Web.Services;

namespace FoodServiceApp.Web.Controllers
{
    /// <summary>Giao dịch thanh toán, đối soát (kể cả tiền COD) và quy trình hoàn tiền có phân quyền.</summary>
    [Route("QuanTri/ThanhToan")]
    public class AdminThanhToanController : AdminOpsBase
    {
        private readonly CauHinhService _cauHinh;
        private readonly FoodServiceApp.Web.Services.Payment.IVnPayService _vnPay;
        public AdminThanhToanController(AppDbContext db, CauHinhService cauHinh, FoodServiceApp.Web.Services.Payment.IVnPayService vnPay) : base(db) { _cauHinh = cauHinh; _vnPay = vnPay; }

        [HttpGet("")]
        public async Task<IActionResult> Index(string? q, string? hinhThuc, string? trangThai, DateTime? tu, DateTime? den, bool chuaDoiSoat = false, string? xuat = null)
        {
            var query = Db.DonHangs.AsNoTracking().Include(d => d.KhachHang).Include(d => d.GianHang).Include(d => d.ThanhToan)
                .Where(d => d.ThanhToan != null);
            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim();
                var qs = q.TrimStart('#');
                query = query.Where(d => (d.ThanhToan!.MaGiaoDich != null && d.ThanhToan.MaGiaoDich.Contains(q)) || d.MaDonHang.ToString() == qs || d.KhachHang!.HoTen.Contains(q));
            }
            if (!string.IsNullOrEmpty(hinhThuc)) query = query.Where(d => d.ThanhToan!.HinhThuc == hinhThuc);
            if (!string.IsNullOrEmpty(trangThai)) query = query.Where(d => d.ThanhToan!.TrangThai == trangThai);
            if (tu != null) query = query.Where(d => d.NgayDat >= tu.Value.Date);
            if (den != null) query = query.Where(d => d.NgayDat < den.Value.Date.AddDays(1));
            // chưa đối soát: online đã thanh toán / COD đã giao nhưng chưa đánh dấu đối soát
            if (chuaDoiSoat) query = query.Where(d => !d.ThanhToan!.DaDoiSoat && (d.ThanhToan.TrangThai == "DaThanhToan" || (d.ThanhToan.HinhThuc == "TienMat" && d.TrangThai == "DaGiao")));

            var ds = await query.OrderByDescending(d => d.NgayDat).Take(xuat == "csv" ? 5000 : 200).ToListAsync();
            if (xuat == "csv")
                return Csv("GiaoDich", new[] { "Mã đơn", "Ngày đặt", "Khách hàng", "Gian hàng", "Hình thức", "Trạng thái", "Mã giao dịch", "Số tiền", "Trạng thái đơn", "Đã đối soát", "Mã GD hoàn", "Ngày hoàn" },
                    ds.Select(d => new object?[] { d.MaDonHang, d.NgayDat, d.KhachHang?.HoTen, d.GianHang?.TenCuaHang, d.ThanhToan!.HinhThuc, d.ThanhToan.TrangThai, d.ThanhToan.MaGiaoDich, d.TongTien, d.TrangThai, d.ThanhToan.DaDoiSoat ? "Có" : "Chưa", d.ThanhToan.MaGiaoDichHoan, d.ThanhToan.NgayHoan }));

            ViewBag.Ds = ds.Select(d => (d.ThanhToan!, d)).ToList();
            ViewBag.Q = q; ViewBag.HinhThuc = hinhThuc; ViewBag.TrangThai = trangThai; ViewBag.Tu = tu?.ToString("yyyy-MM-dd"); ViewBag.Den = den?.ToString("yyyy-MM-dd"); ViewBag.ChuaDoiSoat = chuaDoiSoat;

            // COD cần đối chiếu: đơn tiền mặt đã giao nhưng chưa đối soát
            var cod = await Db.ThanhToans.Where(t => t.HinhThuc == "TienMat" && !t.DaDoiSoat)
                .Join(Db.DonHangs.Where(d => d.TrangThai == "DaGiao"), t => t.MaDonHang, d => d.MaDonHang, (t, d) => d.TongTien).ToListAsync();
            ViewBag.CodSo = cod.Count; ViewBag.CodTien = cod.Sum();
            ViewBag.ChoThanhToanLau = await Db.ThanhToans.CountAsync(t => t.TrangThai == "ChoThanhToan" && t.HinhThuc != "TienMat"
                && Db.DonHangs.Any(d => d.MaDonHang == t.MaDonHang && d.NgayDat < DateTime.Now.AddHours(-1) && d.TrangThai != "DaHuy"));
            return View();
        }

        /// <summary>Đánh dấu đã đối soát (đối chiếu giao dịch/tiền COD với đơn hàng).</summary>
        [HttpPost("doi-soat/{maThanhToan:int}")]
        [ValidateAntiForgeryToken]
        [FoodServiceApp.Web.Filters.CoQuyenGhi("ThanhToan")]
        public async Task<IActionResult> DoiSoat(int maThanhToan)
        {
            var t = await Db.ThanhToans.FirstOrDefaultAsync(x => x.MaThanhToan == maThanhToan);
            if (t == null) return NotFound();
            t.DaDoiSoat = true; t.NgayDoiSoat = DateTime.Now;
            await Db.SaveChangesAsync();
            TempData["ThongBao"] = $"Đã đối soát giao dịch của đơn #{t.MaDonHang}.";
            return RedirectToAction(nameof(Index), new { chuaDoiSoat = true });
        }

        [HttpPost("doi-soat-tat-ca")]
        [ValidateAntiForgeryToken]
        [FoodServiceApp.Web.Filters.CoQuyenGhi("ThanhToan")]
        public async Task<IActionResult> DoiSoatTatCa()
        {
            var ds = await Db.ThanhToans.Where(t => !t.DaDoiSoat && (t.TrangThai == "DaThanhToan" ||
                (t.HinhThuc == "TienMat" && Db.DonHangs.Any(d => d.MaDonHang == t.MaDonHang && d.TrangThai == "DaGiao")))).ToListAsync();
            foreach (var t in ds) { t.DaDoiSoat = true; t.NgayDoiSoat = DateTime.Now; }
            await Db.SaveChangesAsync();
            TempData["ThongBao"] = $"Đã đối soát {ds.Count} giao dịch.";
            return RedirectToAction(nameof(Index));
        }

        // ===================== HOÀN TIỀN =====================
        [HttpGet("hoan-tien")]
        public async Task<IActionResult> HoanTien(string? trangThai)
        {
            var q = Db.YeuCauHoanTiens.AsQueryable();
            if (!string.IsNullOrEmpty(trangThai)) q = q.Where(y => y.TrangThai == trangThai);
            var ds = await q.OrderBy(y => y.TrangThai == "ChoDuyet" ? 0 : 1).ThenByDescending(y => y.NgayTao).Take(200).ToListAsync();
            var ids = ds.Select(y => y.MaDonHang).ToList();
            ViewBag.Don = await Db.DonHangs.Include(d => d.ThanhToan).Where(d => ids.Contains(d.MaDonHang)).ToDictionaryAsync(d => d.MaDonHang);
            ViewBag.Nguong = await _cauHinh.LaySoAsync("HoanTien.NguongQuanLy", 200000);
            ViewBag.ChinhSach = await _cauHinh.LayChuoiAsync("HoanTien.ChinhSach", "");
            ViewBag.CapQuyen = await CapQuyenAsync();
            ViewBag.CoQuyenHoan = await QuyenAdmin.CoAsync(Db, MaTK, "HoanTien");
            ViewBag.TrangThai = trangThai;
            return View(ds);
        }

        /// <summary>Tiếp nhận yêu cầu hoàn tiền (mọi quản trị viên kể cả nhân viên hỗ trợ đều được tạo; chỉ duyệt mới bị giới hạn quyền).</summary>
        [HttpPost("hoan-tien/tao")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TaoHoanTien(int maDonHang, decimal? soTien, string? lyDo)
        {
            lyDo = (lyDo ?? "").Trim();
            var don = await Db.DonHangs.Include(d => d.ThanhToan).FirstOrDefaultAsync(d => d.MaDonHang == maDonHang);
            if (don == null) { TempData["LoiThongBao"] = "Không tìm thấy đơn hàng."; return RedirectToAction(nameof(HoanTien)); }
            if (lyDo.Length < 5) { TempData["LoiThongBao"] = "Nhập lý do hoàn tiền (ít nhất 5 ký tự)."; return RedirectToAction(nameof(HoanTien)); }
            var loi = await KiemTraDieuKienAsync(don, soTien ?? don.TongTien);
            if (loi != null) { TempData["LoiThongBao"] = loi; return RedirectToAction(nameof(HoanTien)); }
            Db.YeuCauHoanTiens.Add(new YeuCauHoanTien { MaDonHang = maDonHang, MaKH = don.MaKH, SoTien = soTien ?? don.TongTien, LyDo = lyDo, Nguon = "QuanTri" });
            await Db.SaveChangesAsync();
            TempData["ThongBao"] = "Đã tạo yêu cầu hoàn tiền, chờ duyệt.";
            return RedirectToAction(nameof(HoanTien));
        }

        /// <summary>Kiểm tra điều kiện hoàn tiền: đã thanh toán online, chưa hoàn, số tiền hợp lệ.</summary>
        private async Task<string?> KiemTraDieuKienAsync(DonHang don, decimal soTien)
        {
            if (don.ThanhToan == null || don.ThanhToan.TrangThai != "DaThanhToan") return "Đơn chưa có thanh toán thành công nên không đủ điều kiện hoàn tiền.";
            if (don.ThanhToan.HinhThuc == "TienMat") return "Đơn thanh toán tiền mặt không hoàn qua hệ thống.";
            if (soTien <= 0 || soTien > don.TongTien) return "Số tiền hoàn phải lớn hơn 0 và không vượt tổng đơn.";
            if (await Db.YeuCauHoanTiens.AnyAsync(y => y.MaDonHang == don.MaDonHang && (y.TrangThai == "ChoDuyet" || y.TrangThai == "DaDuyet")))
                return "Đơn này đã có yêu cầu hoàn tiền đang chờ hoặc đã được duyệt.";
            return null;
        }

        [HttpPost("hoan-tien/{id:int}/duyet")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Duyet(int id, string? ketQua, string? maHoanThuCong)
        {
            var y = await Db.YeuCauHoanTiens.FirstOrDefaultAsync(x => x.MaYeuCau == id);
            if (y == null) return NotFound();
            if (y.TrangThai != "ChoDuyet") { TempData["LoiThongBao"] = "Yêu cầu đã được xử lý."; return RedirectToAction(nameof(HoanTien)); }

            // Phân quyền: cần quyền "HoanTien"; Admin tổng duyệt mọi mức, người khác chỉ tới ngưỡng cấu hình
            var cap = await CapQuyenAsync();
            var nguong = await _cauHinh.LaySoAsync("HoanTien.NguongQuanLy", 200000);
            var coQuyen = cap == "SuperAdmin" || await QuyenAdmin.CoAsync(Db, MaTK, "HoanTien");
            if (!coQuyen) { TempData["LoiThongBao"] = "Bạn không có quyền duyệt hoàn tiền."; return RedirectToAction(nameof(HoanTien)); }
            if (cap != "SuperAdmin" && y.SoTien > nguong) { TempData["LoiThongBao"] = $"Hoàn tiền trên {nguong:N0}đ cần Admin tổng duyệt."; return RedirectToAction(nameof(HoanTien)); }

            var don = await Db.DonHangs.Include(d => d.ThanhToan).FirstAsync(d => d.MaDonHang == y.MaDonHang);
            var tt = don.ThanhToan;
            if (tt == null || tt.TrangThai != "DaThanhToan") { TempData["LoiThongBao"] = "Giao dịch không còn ở trạng thái Đã thanh toán."; return RedirectToAction(nameof(HoanTien)); }

            string ghiChuCong;
            if (tt.HinhThuc == "VNPay" && string.IsNullOrWhiteSpace(maHoanThuCong))
            {
                // Hoàn tiền thật qua API VNPay
                var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
                var kq = await _vnPay.RefundAsync(don.MaDonHang, (long)y.SoTien, (long)don.TongTien, tt.MaGiaoDich ?? "", tt.NgayGiaoDichCong ?? "", User.Identity?.Name ?? ("TK" + MaTK), y.LyDo, ip);
                tt.KetQuaHoan = kq.ThongDiep;
                if (!kq.Ok)
                {
                    await Db.SaveChangesAsync();
                    TempData["LoiThongBao"] = "Chưa hoàn được qua cổng: " + kq.ThongDiep + " Nếu đã hoàn thủ công trên cổng merchant, nhập mã giao dịch hoàn vào ô \"Mã hoàn thủ công\" rồi duyệt lại.";
                    return RedirectToAction(nameof(HoanTien));
                }
                tt.MaGiaoDichHoan = kq.MaGiaoDichHoan; ghiChuCong = kq.ThongDiep;
            }
            else
            {
                // Cổng khác (Momo...) hoặc đã hoàn thủ công: ghi mã giao dịch hoàn do người duyệt nhập
                if (tt.HinhThuc != "TienMat" && string.IsNullOrWhiteSpace(maHoanThuCong))
                { TempData["LoiThongBao"] = $"Cổng {tt.HinhThuc} chưa tích hợp hoàn tiền tự động: hoàn trên cổng rồi nhập mã giao dịch hoàn vào ô \"Mã hoàn thủ công\"."; return RedirectToAction(nameof(HoanTien)); }
                tt.MaGiaoDichHoan = maHoanThuCong?.Trim(); ghiChuCong = "Hoàn thủ công, mã: " + tt.MaGiaoDichHoan;
                tt.KetQuaHoan = ghiChuCong;
            }

            y.TrangThai = "DaDuyet"; y.MaTKXuLy = MaTK; y.NgayXuLy = DateTime.Now;
            y.KetQua = (string.IsNullOrWhiteSpace(ketQua) ? "Đã duyệt hoàn tiền" : ketQua.Trim()) + " — " + ghiChuCong;
            tt.NgayHoan = DateTime.Now;
            tt.TrangThai = "DaHoanTien";   // SaveChanges tự gửi thông báo "Đã hoàn tiền" cho khách
            await Db.SaveChangesAsync();
            TempData["ThongBao"] = $"Đã duyệt hoàn {y.SoTien:N0}đ cho đơn #{y.MaDonHang}. {ghiChuCong}";
            return RedirectToAction(nameof(HoanTien));
        }

        [HttpPost("hoan-tien/{id:int}/tu-choi")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TuChoi(int id, string? ketQua)
        {
            var y = await Db.YeuCauHoanTiens.FirstOrDefaultAsync(x => x.MaYeuCau == id);
            if (y == null) return NotFound();
            if (!await QuyenAdmin.CoAsync(Db, MaTK, "HoanTien")) { TempData["LoiThongBao"] = "Bạn không có quyền từ chối hoàn tiền."; return RedirectToAction(nameof(HoanTien)); }
            if (string.IsNullOrWhiteSpace(ketQua)) { TempData["LoiThongBao"] = "Nhập lý do từ chối."; return RedirectToAction(nameof(HoanTien)); }
            if (y.TrangThai != "ChoDuyet") { TempData["LoiThongBao"] = "Yêu cầu đã được xử lý."; return RedirectToAction(nameof(HoanTien)); }
            y.TrangThai = "TuChoi"; y.MaTKXuLy = MaTK; y.NgayXuLy = DateTime.Now; y.KetQua = ketQua.Trim();
            if (y.MaKH != null)
            {
                var maTK = await Db.KhachHangs.Where(k => k.MaKH == y.MaKH).Select(k => (int?)k.MaTK).FirstOrDefaultAsync();
                if (maTK != null) Db.ThongBaos.Add(new ThongBao { MaTK = maTK.Value, Loai = "HeThong", TieuDe = "Yêu cầu hoàn tiền bị từ chối", NoiDung = $"Đơn #{y.MaDonHang}: {y.KetQua}", MaThamChieu = y.MaDonHang.ToString() });
            }
            await Db.SaveChangesAsync();
            TempData["ThongBao"] = "Đã từ chối yêu cầu hoàn tiền.";
            return RedirectToAction(nameof(HoanTien));
        }
    }
}
