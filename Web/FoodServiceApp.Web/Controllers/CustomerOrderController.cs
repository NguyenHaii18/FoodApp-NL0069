using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FoodServiceApp.Web.Data;
using FoodServiceApp.Web.Models.Entities;
using FoodServiceApp.Web.Models.ViewModels;

namespace FoodServiceApp.Web.Controllers
{
    [Route("MuaHang/DonHang")]
    public class CustomerOrderController : CustomerBaseController
    {
        // Được sửa/xóa đánh giá trong vòng bao nhiêu ngày kể từ lúc gửi
        private const int SoNgaySuaDanhGia = 7;
        private readonly AppDbContext _db;
        private readonly FoodServiceApp.Web.Services.CartService _cart;
        private readonly FoodServiceApp.Web.Services.CauHinhService _cauHinh;

        public CustomerOrderController(AppDbContext db, FoodServiceApp.Web.Services.CartService cart, FoodServiceApp.Web.Services.CauHinhService cauHinh)
        {
            _cauHinh = cauHinh;
            _db = db;
            _cart = cart;
        }

        // Đơn đang xử lý (chưa hoàn tất/huỷ) — "theo dõi trạng thái đơn hàng và giao hàng"
        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var danhSach = await _db.DonHangs
                .Include(d => d.GianHang)
                .Include(d => d.ChiTiets).ThenInclude(c => c.MonAn)
                .Where(d => d.MaKH == CurrentMaKH && d.TrangThai != "DaGiao" && d.TrangThai != "DaHuy")
                .OrderByDescending(d => d.NgayDat)
                .ToListAsync();

            return View(danhSach);
        }

        // Lịch sử mua hàng — đơn đã hoàn tất hoặc đã huỷ
        [HttpGet("LichSu")]
        public async Task<IActionResult> LichSu()
        {
            var danhSach = await _db.DonHangs
                .Include(d => d.GianHang)
                .Include(d => d.ChiTiets).ThenInclude(c => c.MonAn)
                .Where(d => d.MaKH == CurrentMaKH && (d.TrangThai == "DaGiao" || d.TrangThai == "DaHuy"))
                .OrderByDescending(d => d.NgayDat)
                .ToListAsync();

            var maDonDaDanhGia = await _db.DanhGias
                .Where(dg => dg.MaKH == CurrentMaKH)
                .Select(dg => dg.MaDonHang)
                .ToListAsync();

            ViewBag.MaDonDaDanhGia = maDonDaDanhGia;
            var hanSua = DateTime.Now.AddDays(-SoNgaySuaDanhGia);
            ViewBag.MaDonSuaDuoc = await _db.DanhGias
                .Where(dg => dg.MaKH == CurrentMaKH && dg.NgayDanhGia >= hanSua)
                .Select(dg => dg.MaDonHang).ToListAsync();
            return View(danhSach);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> ChiTiet(int id)
        {
            var don = await _db.DonHangs
                .Include(d => d.GianHang)
                .Include(d => d.ChiTiets).ThenInclude(c => c.MonAn)
                .Include(d => d.ThanhToan)
                .Include(d => d.GiaoHang).ThenInclude(g => g!.TaiXe)
                .FirstOrDefaultAsync(d => d.MaDonHang == id && d.MaKH == CurrentMaKH);

            if (don == null) return NotFound();
            ViewBag.HoanTien = await _db.YeuCauHoanTiens.Where(y => y.MaDonHang == id).OrderByDescending(y => y.NgayTao).ToListAsync();
            return View(don);
        }

        [HttpPost("{id:int}/huy")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> HuyDon(int id)
        {
            var don = await _db.DonHangs.Include(d => d.ThanhToan)
                .FirstOrDefaultAsync(d => d.MaDonHang == id && d.MaKH == CurrentMaKH);
            if (don == null) return NotFound();

            if (don.TrangThai != "ChoXacNhan")
            {
                TempData["LoiThongBao"] = "Đơn đã được gian hàng xác nhận, không thể tự huỷ. Vui lòng liên hệ gian hàng.";
                return RedirectToAction("ChiTiet", new { id });
            }

            var gioiHan = await _cauHinh.LaySoAsync("Don.TuHuyPhut", 0);
            if (gioiHan > 0 && (DateTime.Now - don.NgayDat).TotalMinutes > (double)gioiHan)
            {
                TempData["LoiThongBao"] = $"Đơn đã quá {gioiHan:0} phút kể từ lúc đặt nên không thể tự hủy. Vui lòng liên hệ hỗ trợ.";
                return RedirectToAction("ChiTiet", new { id });
            }
            don.TrangThai = "DaHuy";
            don.LyDoTuChoi ??= "Khách hàng tự hủy đơn";

            // Đơn đã thanh toán online mà bị huỷ -> đánh dấu hoàn tiền (xử lý hoàn tiền thật
            // qua cổng thanh toán cần gọi thêm API của VNPay/Momo, ở đây cập nhật trạng thái
            // trong CSDL để Quản trị/Gian hàng theo dõi và đối soát).
            if (don.ThanhToan != null && don.ThanhToan.TrangThai == "DaThanhToan")
                don.ThanhToan.TrangThai = "DaHoanTien";

            await _db.SaveChangesAsync();
            TempData["ThongBao"] = $"Đã huỷ đơn #{don.MaDonHang}.";
            return RedirectToAction("Index");
        }

        [HttpGet("{id:int}/danh-gia")]
        public async Task<IActionResult> DanhGia(int id)
        {
            var don = await _db.DonHangs.Include(d => d.GianHang)
                .Include(d => d.ChiTiets).ThenInclude(c => c.MonAn)
                .FirstOrDefaultAsync(d => d.MaDonHang == id && d.MaKH == CurrentMaKH && d.TrangThai == "DaGiao");
            if (don == null) return NotFound();

            if (await _db.DanhGias.AnyAsync(dg => dg.MaDonHang == id))
            {
                TempData["LoiThongBao"] = "Đơn này đã được đánh giá rồi.";
                return RedirectToAction("LichSu");
            }

            return View(new DanhGiaFormViewModel
            {
                MaDonHang = don.MaDonHang,
                MaGianHang = don.MaGianHang,
                TenGianHang = don.GianHang!.TenCuaHang,
                Mons = don.ChiTiets
                    .GroupBy(c => c.MaMonAn)
                    .Select(g => new MonDanhGiaItem { MaMonAn = g.Key, TenMon = g.First().MonAn?.TenMon ?? "Món ăn" })
                    .ToList(),
            });
        }

        [HttpPost("{id:int}/danh-gia")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DanhGia(int id, DanhGiaFormViewModel model)
        {
            var don = await _db.DonHangs.Include(d => d.ChiTiets)
                .FirstOrDefaultAsync(d => d.MaDonHang == id && d.MaKH == CurrentMaKH && d.TrangThai == "DaGiao");
            if (don == null) return NotFound();

            if (await _db.DanhGias.AnyAsync(dg => dg.MaDonHang == id))
                return RedirectToAction("LichSu");

            if (!ModelState.IsValid) return View(model);

            // Chỉ nhận đánh giá cho các món thật sự nằm trong đơn này (chống sửa form)
            var monTrongDon = don.ChiTiets.Select(c => c.MaMonAn).ToHashSet();
            foreach (var m in model.Mons.Where(m => m.SoSao >= 1 && m.SoSao <= 5 && monTrongDon.Contains(m.MaMonAn)))
            {
                _db.DanhGiaMons.Add(new DanhGiaMon
                {
                    MaKH = CurrentMaKH,
                    MaMonAn = m.MaMonAn,
                    MaDonHang = id,
                    SoSao = m.SoSao,
                    NoiDung = m.NoiDung,
                });
            }

            _db.DanhGias.Add(new DanhGia
            {
                MaKH = CurrentMaKH,
                MaGianHang = don.MaGianHang,
                MaDonHang = id,
                SoSao = model.SoSao,
                NoiDung = model.NoiDung,
                SoSaoGiaoHang = model.SoSaoGiaoHang is >= 1 and <= 5 ? model.SoSaoGiaoHang : null,
            });
            await _db.SaveChangesAsync();

            TempData["ThongBao"] = "Cảm ơn bạn đã đánh giá!";
            return RedirectToAction("LichSu");
        }
        // ====== Mua lại ======
        [HttpPost("{id:int}/mua-lai")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MuaLai(int id)
        {
            var don = await _db.DonHangs.Include(d => d.GianHang)
                .Include(d => d.ChiTiets).ThenInclude(c => c.MonAn)
                .FirstOrDefaultAsync(d => d.MaDonHang == id && d.MaKH == CurrentMaKH);
            if (don == null) return NotFound();
            if (don.TrangThai != "DaGiao" && don.TrangThai != "DaHuy")
            {
                TempData["LoiThongBao"] = "Chỉ mua lại được đơn đã hoàn tất hoặc đã hủy.";
                return RedirectToAction("ChiTiet", new { id });
            }
            if (don.GianHang == null || don.GianHang.TrangThaiDuyet != "DaDuyet" || don.GianHang.TrangThaiKinhDoanh != "DangMo" || !don.GianHang.DangNhanDon)
            {
                TempData["LoiThongBao"] = "Gian hàng hiện không nhận đơn.";
                return RedirectToAction("ChiTiet", new { id });
            }

            var maMons = don.ChiTiets.Select(c => c.MaMonAn).Distinct().ToList();
            var tuyChons = await _db.TuyChonMons.Where(t => maMons.Contains(t.MaMonAn)).ToListAsync();

            var gio = new Models.ViewModels.Cart { MaGianHang = don.MaGianHang, TenGianHang = don.GianHang.TenCuaHang };
            var boQua = new List<string>();
            foreach (var ct in don.ChiTiets)
            {
                var mon = ct.MonAn;
                if (mon == null || mon.TinhTrang != "ConHang") { boQua.Add(mon?.TenMon ?? "Món đã xóa"); continue; }

                // Dựng lại tùy chọn theo tên, lấy GIÁ HIỆN TẠI
                var tenChon = (ct.TuyChon ?? "").Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                var chon = tuyChons.Where(t => t.MaMonAn == mon.MaMonAn && tenChon.Contains(t.TenTuyChon)).ToList();
                var item = new Models.ViewModels.CartItem
                {
                    MaMonAn = mon.MaMonAn,
                    TenMon = mon.TenMon,
                    Gia = mon.Gia + chon.Sum(t => t.GiaThem),
                    SoLuong = ct.SoLuong,
                    HinhAnh = mon.HinhAnh,
                    GhiChu = ct.GhiChu,
                    TuyChonIds = chon.Select(t => t.MaTuyChon).ToList(),
                    TuyChonText = chon.Any() ? string.Join("; ", chon.Select(t => t.TenTuyChon)) : null,
                };
                var trung = gio.Items.FirstOrDefault(i => i.Khoa == item.Khoa);
                if (trung != null) trung.SoLuong += item.SoLuong; else gio.Items.Add(item);
            }

            if (!gio.Items.Any())
            {
                TempData["LoiThongBao"] = "Các món trong đơn này hiện đều không còn bán.";
                return RedirectToAction("ChiTiet", new { id });
            }

            _cart.Thay(gio);
            TempData["ThongBao"] = boQua.Any()
                ? $"Đã đưa các món vào giỏ (giá hiện tại). Không còn bán: {string.Join(", ", boQua)}."
                : "Đã đưa các món vào giỏ hàng (giá hiện tại).";
            return RedirectToAction("Index", "CustomerCart");
        }

        // ====== Báo cáo đánh giá không phù hợp ======
        [HttpPost("bao-cao-danh-gia")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BaoCaoDanhGia(int maDanhGia, string? lyDo)
        {
            var dg = await _db.DanhGias.FirstOrDefaultAsync(d => d.MaDanhGia == maDanhGia);
            var quayVe = dg != null ? Url.Action("ChiTiet", "CustomerHome", new { id = dg.MaGianHang }) : Url.Action("Index", "CustomerHome");
            if (dg == null) return NotFound();
            lyDo = (lyDo ?? "").Trim();
            if (lyDo.Length < 3) { TempData["LoiThongBao"] = "Vui lòng nhập lý do báo cáo."; return Redirect(quayVe!); }
            if (dg.MaKH == CurrentMaKH) { TempData["LoiThongBao"] = "Bạn không thể báo cáo đánh giá của chính mình."; return Redirect(quayVe!); }
            if (await _db.BaoCaoDanhGias.AnyAsync(b => b.MaKH == CurrentMaKH && b.MaDanhGia == maDanhGia))
            { TempData["ThongBao"] = "Bạn đã báo cáo đánh giá này rồi."; return Redirect(quayVe!); }

            _db.BaoCaoDanhGias.Add(new BaoCaoDanhGia { MaKH = CurrentMaKH, MaDanhGia = maDanhGia, LyDo = lyDo.Length > 255 ? lyDo[..255] : lyDo });
            await _db.SaveChangesAsync();
            TempData["ThongBao"] = "Đã gửi báo cáo, quản trị viên sẽ xem xét.";
            return Redirect(quayVe!);
        }

        // ====== Sửa / xóa đánh giá trong thời hạn ======
        [HttpGet("{id:int}/sua-danh-gia")]
        public async Task<IActionResult> SuaDanhGia(int id)
        {
            var dg = await _db.DanhGias.FirstOrDefaultAsync(d => d.MaDonHang == id && d.MaKH == CurrentMaKH);
            if (dg == null) return NotFound();
            if (dg.NgayDanhGia < DateTime.Now.AddDays(-SoNgaySuaDanhGia))
            { TempData["LoiThongBao"] = $"Chỉ được sửa/xóa đánh giá trong {SoNgaySuaDanhGia} ngày."; return RedirectToAction("LichSu"); }

            var don = await _db.DonHangs.Include(d => d.GianHang).Include(d => d.ChiTiets).ThenInclude(c => c.MonAn)
                .FirstAsync(d => d.MaDonHang == id);
            var dgMons = await _db.DanhGiaMons.Where(m => m.MaDonHang == id && m.MaKH == CurrentMaKH).ToListAsync();
            ViewBag.DangSua = true;
            return View("DanhGia", new DanhGiaFormViewModel
            {
                MaDonHang = id,
                MaGianHang = don.MaGianHang,
                TenGianHang = don.GianHang?.TenCuaHang ?? "",
                SoSao = dg.SoSao,
                NoiDung = dg.NoiDung,
                SoSaoGiaoHang = dg.SoSaoGiaoHang ?? 0,
                Mons = don.ChiTiets.GroupBy(c => c.MaMonAn).Select(g =>
                {
                    var cu = dgMons.FirstOrDefault(m => m.MaMonAn == g.Key);
                    return new MonDanhGiaItem { MaMonAn = g.Key, TenMon = g.First().MonAn?.TenMon ?? "Món ăn", SoSao = cu?.SoSao ?? 0, NoiDung = cu?.NoiDung };
                }).ToList(),
            });
        }

        [HttpPost("{id:int}/sua-danh-gia")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SuaDanhGia(int id, DanhGiaFormViewModel model)
        {
            var dg = await _db.DanhGias.FirstOrDefaultAsync(d => d.MaDonHang == id && d.MaKH == CurrentMaKH);
            if (dg == null) return NotFound();
            if (dg.NgayDanhGia < DateTime.Now.AddDays(-SoNgaySuaDanhGia))
            { TempData["LoiThongBao"] = $"Chỉ được sửa/xóa đánh giá trong {SoNgaySuaDanhGia} ngày."; return RedirectToAction("LichSu"); }
            if (!ModelState.IsValid) { ViewBag.DangSua = true; return View("DanhGia", model); }

            dg.SoSao = model.SoSao;
            dg.NoiDung = model.NoiDung;
            dg.SoSaoGiaoHang = model.SoSaoGiaoHang is >= 1 and <= 5 ? model.SoSaoGiaoHang : null;

            var don = await _db.DonHangs.Include(d => d.ChiTiets).FirstAsync(d => d.MaDonHang == id);
            var monTrongDon = don.ChiTiets.Select(c => c.MaMonAn).ToHashSet();
            var cu = await _db.DanhGiaMons.Where(m => m.MaDonHang == id && m.MaKH == CurrentMaKH).ToListAsync();
            _db.DanhGiaMons.RemoveRange(cu);
            foreach (var m in model.Mons.Where(m => m.SoSao >= 1 && m.SoSao <= 5 && monTrongDon.Contains(m.MaMonAn)))
                _db.DanhGiaMons.Add(new DanhGiaMon { MaKH = CurrentMaKH, MaMonAn = m.MaMonAn, MaDonHang = id, SoSao = m.SoSao, NoiDung = m.NoiDung });

            await _db.SaveChangesAsync();
            TempData["ThongBao"] = "Đã cập nhật đánh giá.";
            return RedirectToAction("LichSu");
        }

        [HttpPost("{id:int}/xoa-danh-gia")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> XoaDanhGia(int id)
        {
            var dg = await _db.DanhGias.FirstOrDefaultAsync(d => d.MaDonHang == id && d.MaKH == CurrentMaKH);
            if (dg == null) return NotFound();
            if (dg.NgayDanhGia < DateTime.Now.AddDays(-SoNgaySuaDanhGia))
            { TempData["LoiThongBao"] = $"Chỉ được sửa/xóa đánh giá trong {SoNgaySuaDanhGia} ngày."; return RedirectToAction("LichSu"); }
            _db.DanhGiaMons.RemoveRange(await _db.DanhGiaMons.Where(m => m.MaDonHang == id && m.MaKH == CurrentMaKH).ToListAsync());
            _db.DanhGias.Remove(dg);
            await _db.SaveChangesAsync();
            TempData["ThongBao"] = "Đã xóa đánh giá.";
            return RedirectToAction("LichSu");
        }

        // ====== Lịch sử giao dịch + biên nhận ======
        [HttpGet("giao-dich")]
        public async Task<IActionResult> GiaoDich()
        {
            var ds = await _db.DonHangs.Include(d => d.GianHang).Include(d => d.ThanhToan)
                .Where(d => d.MaKH == CurrentMaKH && d.ThanhToan != null)
                .OrderByDescending(d => d.NgayDat).Take(100).ToListAsync();
            return View(ds);
        }

        /// <summary>Khách thăm dò vị trí tài xế (chỉ khi đơn đang giao và là đơn của chính khách).</summary>
        [HttpGet("{id:int}/vi-tri-tai-xe")]
        public async Task<IActionResult> ViTriTaiXe(int id)
        {
            var don = await _db.DonHangs.AsNoTracking().FirstOrDefaultAsync(d => d.MaDonHang == id && d.MaKH == CurrentMaKH);
            if (don == null) return NotFound();
            if (don.TrangThai != "DangGiao" && don.TrangThai != "DaXacNhan") return Json(new { coViTri = false });
            var vt = await _db.TaiXeViTris.AsNoTracking().FirstOrDefaultAsync(v => v.MaDonHang == id);
            if (vt == null || don.MaTaiXe == null || vt.MaTaiXe != don.MaTaiXe) return Json(new { coViTri = false });
            var giay = (int)(DateTime.Now - vt.CapNhatLuc).TotalSeconds;
            return Json(new { coViTri = true, lat = vt.ViDo, lng = vt.KinhDo, giayTruoc = giay, cu = giay > FoodServiceApp.Web.Services.GiaoHangXuLy.ViTriCuGiay });
        }

        [HttpGet("{id:int}/bien-nhan")]
        public async Task<IActionResult> BienNhan(int id)
        {
            var don = await _db.DonHangs.Include(d => d.GianHang).Include(d => d.KhachHang)
                .Include(d => d.ChiTiets).ThenInclude(c => c.MonAn).Include(d => d.ThanhToan)
                .FirstOrDefaultAsync(d => d.MaDonHang == id && d.MaKH == CurrentMaKH);
            if (don == null) return NotFound();
            return View(don);
        }
        // ====== Yêu cầu hoàn tiền (khách) ======
        [HttpPost("{id:int}/yeu-cau-hoan-tien")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> YeuCauHoanTien(int id, string? lyDo)
        {
            var don = await _db.DonHangs.Include(d => d.ThanhToan)
                .FirstOrDefaultAsync(d => d.MaDonHang == id && d.MaKH == CurrentMaKH);
            if (don == null) return NotFound();
            lyDo = (lyDo ?? "").Trim();
            if (lyDo.Length < 5) { TempData["LoiThongBao"] = "Vui lòng nhập lý do hoàn tiền (ít nhất 5 ký tự)."; return RedirectToAction("ChiTiet", new { id }); }
            if (don.ThanhToan == null || don.ThanhToan.TrangThai != "DaThanhToan")
            { TempData["LoiThongBao"] = "Chỉ đơn đã thanh toán trực tuyến mới yêu cầu hoàn tiền được."; return RedirectToAction("ChiTiet", new { id }); }
            if (await _db.YeuCauHoanTiens.AnyAsync(y => y.MaDonHang == id && y.TrangThai == "ChoDuyet"))
            { TempData["LoiThongBao"] = "Đơn này đang có yêu cầu hoàn tiền chờ duyệt."; return RedirectToAction("ChiTiet", new { id }); }

            _db.YeuCauHoanTiens.Add(new YeuCauHoanTien { MaDonHang = id, MaKH = CurrentMaKH, SoTien = don.TongTien, LyDo = lyDo.Length > 400 ? lyDo[..400] : lyDo, Nguon = "KhachHang" });
            await _db.SaveChangesAsync();
            TempData["ThongBao"] = "Đã gửi yêu cầu hoàn tiền, quản trị viên sẽ xem xét.";
            return RedirectToAction("ChiTiet", new { id });
        }
    }
}
