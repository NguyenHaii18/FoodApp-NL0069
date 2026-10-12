using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FoodServiceApp.Web.Data;
using FoodServiceApp.Web.Models.Entities;
using FoodServiceApp.Web.Models.ViewModels;
using FoodServiceApp.Web.Services;
using FoodServiceApp.Web.Services.Maps;
using FoodServiceApp.Web.Services.Payment;

namespace FoodServiceApp.Web.Controllers
{
    [Route("MuaHang/GioHang")]
    public class CustomerCartController : Controller
    {
        private readonly AppDbContext _db;
        private readonly CartService _cart;
        private readonly IVnPayService _vnPayService;
        private readonly IGoongMapsService _mapsService;
        private readonly IConfiguration _config;
        private readonly CauHinhService _cauHinh;

        // VNĐ/km — trùng công thức với Controllers/Api/MapController để 2 nơi luôn tính ra cùng 1 kết quả.
        private const decimal PhiMoiKmMacDinh = 5000m;
        private const decimal PhiGiaoMacDinhCfg = 15000m; // dùng khi không xác định được tọa độ (vd. chưa cấu hình API key Goong)

        public CustomerCartController(AppDbContext db, CartService cart, IVnPayService vnPayService, IGoongMapsService mapsService, IConfiguration config, CauHinhService cauHinh)
        {
            _cauHinh = cauHinh;
            _config = config;
            _db = db;
            _cart = cart;
            _vnPayService = vnPayService;
            _mapsService = mapsService;
        }

        private const double BanKinhPhucVuKmMacDinh = 20;

        /// <summary>Phí giao + khoảng cách (km) theo địa chỉ thật; không geocode được thì trả phí mặc định, km = null.</summary>
        private async Task<(decimal phi, double? km)> TinhPhiGiaoHangAsync(string diaChiGianHang, string diaChiGiao)
        {
            // Phí lấy từ bảng CauHinhHeThong (Admin chỉnh được); không có thì dùng mặc định
            var phiMoiKm = await _cauHinh.LaySoAsync("Giao.PhiMoiKm", PhiMoiKmMacDinh);
            var phiMacDinh = await _cauHinh.LaySoAsync("Giao.PhiMacDinh", PhiGiaoMacDinhCfg);
            try
            {
                var diemGian = await _mapsService.GeocodeAsync(diaChiGianHang);
                var diemKhach = await _mapsService.GeocodeAsync(diaChiGiao);
                if (diemGian == null || diemKhach == null) return (phiMacDinh, null);

                var ketQua = await _mapsService.GetDistanceAsync(diemGian, diemKhach);
                if (ketQua == null) return (phiMacDinh, null);

                return (Math.Ceiling((decimal)ketQua.DistanceKm) * phiMoiKm, ketQua.DistanceKm);
            }
            catch
            {
                // Goong Maps lỗi/chưa cấu hình API key -> không để việc này chặn khách đặt hàng.
                return (phiMacDinh, null);
            }
        }

        private async Task<double> LayBanKinhAsync() =>
            (double)await _cauHinh.LaySoAsync("Giao.BanKinhKm", (decimal)(_config.GetValue<double?>("Giao:BanKinhKm") ?? BanKinhPhucVuKmMacDinh));

        /// <summary>
        /// Đối chiếu giỏ (Session) với CSDL: món đã hết/ngừng bán -> loại khỏi giỏ; giá hoặc giá tùy chọn đổi -> cập nhật giá mới.
        /// Trả về danh sách cảnh báo để báo khách (rỗng = giỏ khớp CSDL).
        /// </summary>
        private async Task<List<string>> DoiChieuGioAsync(Cart cart)
        {
            var canhBao = new List<string>();
            if (!cart.Items.Any()) return canhBao;

            var ids = cart.Items.Select(i => i.MaMonAn).Distinct().ToList();
            var mons = await _db.MonAns.Include(m => m.DanhMuc).Where(m => ids.Contains(m.MaMonAn)).ToDictionaryAsync(m => m.MaMonAn);
            var tuyChons = await _db.TuyChonMons.Where(t => ids.Contains(t.MaMonAn)).ToListAsync();
            var daiThay = false;

            foreach (var it in cart.Items.ToList())
            {
                if (!mons.TryGetValue(it.MaMonAn, out var m) || m.DanhMuc == null || m.DanhMuc.MaGianHang != cart.MaGianHang)
                { cart.Items.Remove(it); canhBao.Add($"\"{it.TenMon}\" không còn tồn tại nên đã bị xóa khỏi giỏ."); daiThay = true; continue; }
                if (m.TinhTrang != "ConHang")
                { cart.Items.Remove(it); canhBao.Add($"\"{it.TenMon}\" đã {(m.TinhTrang == "TamNgung" ? "ngừng bán" : "hết hàng")} nên đã bị xóa khỏi giỏ."); daiThay = true; continue; }

                var chon = tuyChons.Where(t => t.MaMonAn == it.MaMonAn && it.TuyChonIds.Contains(t.MaTuyChon)).ToList();
                if (chon.Count != it.TuyChonIds.Count)
                {
                    it.TuyChonIds = chon.Select(t => t.MaTuyChon).ToList();
                    it.TuyChonText = chon.Count == 0 ? null : string.Join("; ", chon.Select(t => t.TenTuyChon));
                    canhBao.Add($"Một số tùy chọn của \"{it.TenMon}\" không còn, đã được bỏ.");
                    daiThay = true;
                }
                var giaMoi = m.Gia + chon.Sum(t => t.GiaThem);
                if (giaMoi != it.Gia)
                {
                    canhBao.Add($"Giá \"{it.TenMon}\" đã thay đổi từ {it.Gia:N0}đ thành {giaMoi:N0}đ.");
                    it.Gia = giaMoi; daiThay = true;
                }
                it.TenMon = m.TenMon;
            }
            if (daiThay) _cart.Thay(cart);
            return canhBao;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var cart = _cart.LayGioHang();
            var canhBao = await DoiChieuGioAsync(cart);
            if (canhBao.Any()) ViewBag.CanhBao = canhBao;
            if (cart.Items.Any())
            {
                var gh = await _db.GianHangs.FirstOrDefaultAsync(g => g.MaGianHang == cart.MaGianHang);
                ViewBag.GianHangDong = gh != null && !GianHangHelper.NhanDon(gh);
            }
            return View(cart);
        }

        [HttpPost("cap-nhat")]
        [ValidateAntiForgeryToken]
        public IActionResult CapNhat(string khoa, int soLuong)
        {
            _cart.CapNhatTheoKhoa(khoa, soLuong);
            return RedirectToAction("Index");
        }

        [HttpPost("xoa-mon")]
        [ValidateAntiForgeryToken]
        public IActionResult XoaMon(string khoa)
        {
            _cart.CapNhatTheoKhoa(khoa, 0);
            return RedirectToAction("Index");
        }

        [HttpPost("xoa-tat-ca")]
        [ValidateAntiForgeryToken]
        public IActionResult XoaTatCa()
        {
            _cart.XoaGioHang();
            return RedirectToAction("Index", "CustomerHome");
        }

        // Nạp sổ địa chỉ của khách để trang Checkout cho chọn nhanh
        private async Task NapDiaChiAsync()
        {
            var maKH = int.Parse(User.FindFirst("MaKH")!.Value);
            ViewBag.DiaChis = await _db.DiaChiKhachHangs
                .Where(d => d.MaKH == maKH)
                .OrderByDescending(d => d.MacDinh).ThenBy(d => d.MaDiaChi)
                .ToListAsync();
        }

        private static string DiaChiDayDu(DiaChiKhachHang d) =>
            string.Join(", ", new[] { d.DiaChi, d.Phuong, d.Quan, d.Tinh }.Where(x => !string.IsNullOrWhiteSpace(x)));

        [HttpGet("thanh-toan")]
        [Authorize(Roles = "KhachHang")]
        public async Task<IActionResult> Checkout()
        {
            var gioHang = _cart.LayGioHang();
            if (!gioHang.Items.Any())
                return RedirectToAction("Index", "CustomerHome");

            var canhBao = await DoiChieuGioAsync(gioHang);
            if (canhBao.Any())
            {
                TempData["LoiThongBao"] = "Giỏ hàng vừa được cập nhật: " + string.Join(" ", canhBao);
                return RedirectToAction("Index");
            }

            var maKH = int.Parse(User.FindFirst("MaKH")!.Value);
            var khachHang = await _db.KhachHangs.FirstAsync(k => k.MaKH == maKH);
            await NapDiaChiAsync();

            // Ưu tiên địa chỉ mặc định trong sổ địa chỉ; nếu chưa có thì dùng địa chỉ trong hồ sơ
            var macDinh = await _db.DiaChiKhachHangs.FirstOrDefaultAsync(d => d.MaKH == maKH && d.MacDinh);

            return View(new CheckoutViewModel
            {
                GioHang = gioHang,
                DiaChiGiaoHang = macDinh != null ? DiaChiDayDu(macDinh) : (khachHang.DiaChiGiaoHang ?? ""),
                GhiChu = macDinh?.HuongDan,
                MaYeuCauDat = Guid.NewGuid().ToString("N"),
            });
        }

        // AJAX: khách gõ/đổi địa chỉ giao ở trang Checkout -> gọi endpoint này để hiện phí ước tính ngay,
        // trước khi bấm Đặt hàng. Giá trị cuối cùng vẫn được TÍNH LẠI Ở SERVER trong DatHang() bên dưới,
        // không tin tưởng số phí do client gửi lên, tránh khách sửa phí qua DevTools.
        [HttpPost("uoc-tinh-phi")]
        [Authorize(Roles = "KhachHang")]
        public async Task<IActionResult> UocTinhPhi([FromBody] string diaChiGiao)
        {
            var gioHang = _cart.LayGioHang();
            if (!gioHang.Items.Any() || string.IsNullOrWhiteSpace(diaChiGiao))
                return Ok(new { phiGiaoHang = 0m });

            var gianHang = await _db.GianHangs.FirstOrDefaultAsync(g => g.MaGianHang == gioHang.MaGianHang);
            if (gianHang == null) return Ok(new { phiGiaoHang = await _cauHinh.LaySoAsync("Giao.PhiMacDinh", PhiGiaoMacDinhCfg) });

            var (phi, km) = await TinhPhiGiaoHangAsync(gianHang.DiaChi, diaChiGiao);
            var banKinh = await LayBanKinhAsync();
            return Ok(new { phiGiaoHang = phi, khoangCachKm = km, ngoaiKhuVuc = km != null && km > banKinh, banKinhKm = banKinh });
        }

        /// <summary>
        /// Tính tiền giảm của mã khuyến mãi; trả null + lý do nếu mã không dùng được. Kiểm tra: thời hạn, trạng thái, gian hàng/món/danh mục áp dụng,
        /// đơn tối thiểu, tổng lượt dùng, mỗi khách một lần, giảm tối đa; loại GiamPhiVanChuyen chỉ giảm vào phí giao.
        /// </summary>
        private async Task<(KhuyenMai? km, decimal giam, string? loi)> TinhKhuyenMaiAsync(string? ma, Cart cart, decimal phiGiao)
        {
            if (string.IsNullOrWhiteSpace(ma)) return (null, 0, null);
            var maCode = ma.Trim().ToUpper();
            var now = DateTime.Now;
            var km = await _db.KhuyenMais.FirstOrDefaultAsync(k =>
                k.MaCode == maCode && k.TrangThai &&
                (k.MaGianHang == null || k.MaGianHang == cart.MaGianHang) &&
                now >= k.NgayBatDau && now <= k.NgayKetThuc);
            if (km == null) return (null, 0, "Mã khuyến mãi không hợp lệ, đã hết hạn, hoặc không áp dụng cho gian hàng này.");

            var dsQuan = await _db.KhuyenMaiGianHangs.Where(x => x.MaKhuyenMai == km.MaKhuyenMai).Select(x => x.MaGianHang).ToListAsync();
            if (dsQuan.Any() && !dsQuan.Contains(cart.MaGianHang))
                return (null, 0, "Mã này không áp dụng cho gian hàng trong giỏ.");

            if (cart.TongTien < km.GiaTriDonToiThieu)
                return (null, 0, $"Đơn tối thiểu {km.GiaTriDonToiThieu:N0}đ mới dùng được mã này.");
            if (km.SoLuongToiDa != null && km.SoLuongDaDung >= km.SoLuongToiDa)
                return (null, 0, "Mã khuyến mãi đã hết lượt sử dụng.");
            var maKH = int.Parse(User.FindFirst("MaKH")!.Value);
            if (km.MoiKhachMotLan && await _db.KhuyenMaiKhachDungs.AnyAsync(x => x.MaKhuyenMai == km.MaKhuyenMai && x.MaKH == maKH))
                return (null, 0, "Bạn đã dùng mã này rồi (mỗi khách chỉ dùng một lần).");

            // Phần giá trị đơn được áp dụng: nếu mã giới hạn món/danh mục thì chỉ tính các món đó
            var dsMon = await _db.KhuyenMaiMonAns.Where(x => x.MaKhuyenMai == km.MaKhuyenMai).Select(x => x.MaMonAn).ToListAsync();
            var dsDm = await _db.KhuyenMaiDanhMucs.Where(x => x.MaKhuyenMai == km.MaKhuyenMai).Select(x => x.MaDanhMuc).ToListAsync();
            var coGioiHan = dsMon.Any() || dsDm.Any();
            var coSan = cart.TongTien;
            if (coGioiHan)
            {
                var maMons = cart.Items.Select(i => i.MaMonAn).ToList();
                var dmCua = await _db.MonAns.Where(m => maMons.Contains(m.MaMonAn)).ToDictionaryAsync(m => m.MaMonAn, m => m.MaDanhMuc);
                coSan = cart.Items.Where(i => dsMon.Contains(i.MaMonAn) || (dmCua.TryGetValue(i.MaMonAn, out var dm) && dsDm.Contains(dm)))
                                  .Sum(i => i.Gia * i.SoLuong);
                if (coSan <= 0) return (null, 0, "Giỏ hàng không có món nào thuộc diện áp dụng của mã này.");
            }

            decimal giam;
            if (km.LoaiGiam == "GiamPhiVanChuyen")
                giam = Math.Min(km.SoTienGiam ?? phiGiao, phiGiao);
            else
            {
                giam = km.PhanTramGiam != null ? Math.Round(coSan * km.PhanTramGiam.Value / 100) : (km.SoTienGiam ?? 0);
                if (km.GiamToiDa != null) giam = Math.Min(giam, km.GiamToiDa.Value);
                giam = Math.Min(giam, coSan);
            }
            return (km, giam, null);
        }

        [HttpPost("ap-dung-khuyen-mai")]
        [Authorize(Roles = "KhachHang")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApDungKhuyenMai(CheckoutViewModel model)
        {
            model.GioHang = _cart.LayGioHang();
            if (!model.GioHang.Items.Any()) return RedirectToAction("Index", "CustomerHome");
            if (string.IsNullOrEmpty(model.MaYeuCauDat)) model.MaYeuCauDat = Guid.NewGuid().ToString("N");

            if (!string.IsNullOrWhiteSpace(model.DiaChiGiaoHang))
            {
                var gianHangChoPhi = await _db.GianHangs.FirstOrDefaultAsync(g => g.MaGianHang == model.GioHang.MaGianHang);
                if (gianHangChoPhi != null)
                    model.PhiGiaoHangUocTinh = (await TinhPhiGiaoHangAsync(gianHangChoPhi.DiaChi, model.DiaChiGiaoHang)).phi;
            }

            var (_, giam, loi) = await TinhKhuyenMaiAsync(model.MaKhuyenMai, model.GioHang, model.PhiGiaoHangUocTinh);
            model.SoTienGiam = giam;
            model.LoiThongBao = loi;

            await NapDiaChiAsync();
            return View("Checkout", model);
        }

        [HttpPost("dat-hang")]
        [Authorize(Roles = "KhachHang")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DatHang(CheckoutViewModel model)
        {
            var maKH = int.Parse(User.FindFirst("MaKH")!.Value);

            // Chống tạo đơn trùng: cùng một token chỉ tạo đúng một đơn (bấm đúp / gửi lại form / F5)
            if (!string.IsNullOrEmpty(model.MaYeuCauDat))
            {
                var daCo = await _db.DonHangs.Where(d => d.MaKH == maKH && d.MaYeuCauDat == model.MaYeuCauDat)
                    .Select(d => (int?)d.MaDonHang).FirstOrDefaultAsync();
                if (daCo != null) return RedirectToAction("ChiTiet", "CustomerOrder", new { id = daCo });
            }

            var gioHang = _cart.LayGioHang();
            if (!gioHang.Items.Any())
                return RedirectToAction("Index", "CustomerHome");

            async Task<IActionResult> VeCheckout(string loi)
            {
                model.GioHang = gioHang;
                model.LoiThongBao = loi;
                await NapDiaChiAsync();
                return View("Checkout", model);
            }

            if (string.IsNullOrWhiteSpace(model.DiaChiGiaoHang))
                return await VeCheckout("Vui lòng nhập địa chỉ giao hàng.");

            if (model.HinhThucThanhToan != "TienMat" && model.HinhThucThanhToan != "VNPay")
                return await VeCheckout(model.HinhThucThanhToan == "Momo"
                    ? "Ví Momo hiện chưa được hỗ trợ. Vui lòng chọn VNPay hoặc Tiền mặt khi nhận hàng."
                    : "Phương thức thanh toán không hợp lệ.");

            // Đối chiếu lại giá + tình trạng món ngay trước khi tạo đơn
            var canhBao = await DoiChieuGioAsync(gioHang);
            if (canhBao.Any())
            {
                TempData["LoiThongBao"] = "Giỏ hàng vừa thay đổi: " + string.Join(" ", canhBao) + " Vui lòng kiểm tra lại trước khi đặt.";
                return RedirectToAction("Index");
            }

            var gianHang = await _db.GianHangs.FirstAsync(g => g.MaGianHang == gioHang.MaGianHang);
            if (!GianHangHelper.NhanDon(gianHang))
                return await VeCheckout("Gian hàng hiện đang đóng cửa hoặc tạm dừng nhận đơn.");

            var (phiGiaoHang, khoangCach) = await TinhPhiGiaoHangAsync(gianHang.DiaChi, model.DiaChiGiaoHang);
            var (km, soTienGiam, loiKm) = await TinhKhuyenMaiAsync(model.MaKhuyenMai, gioHang, phiGiaoHang);
            if (loiKm != null) return await VeCheckout(loiKm);
            var banKinh = await LayBanKinhAsync();
            if (khoangCach != null && khoangCach > banKinh)
                return await VeCheckout($"Địa chỉ giao hàng nằm ngoài khu vực phục vụ ({khoangCach:0.#} km, tối đa {banKinh:0.#} km).");

            var tongTien = Math.Max(0, gioHang.TongTien + phiGiaoHang - soTienGiam);

            var donHang = new DonHang
            {
                MaKH = maKH,
                MaGianHang = gioHang.MaGianHang,
                TrangThai = "ChoXacNhan",
                TongTien = tongTien,
                PhiGiaoHang = phiGiaoHang,
                DiaChiGiaoHang = model.DiaChiGiaoHang.Trim(),
                GhiChu = string.IsNullOrWhiteSpace(model.GhiChu) ? null : model.GhiChu.Trim(),
                MaKhuyenMaiApDung = km?.MaKhuyenMai,
                MaXacNhanGiao = FoodServiceApp.Web.Services.GiaoHangXuLy.TaoMa(),
                TienGiam = soTienGiam,
                MaYeuCauDat = string.IsNullOrEmpty(model.MaYeuCauDat) ? null : model.MaYeuCauDat,
            };
            _db.DonHangs.Add(donHang);
            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // Hai request cùng token chạy song song: request đến sau vi phạm UX_DonHang_MaYeuCauDat -> chuyển về đơn đã tạo
                _db.Entry(donHang).State = EntityState.Detached;
                var daCo = await _db.DonHangs.Where(d => d.MaKH == maKH && d.MaYeuCauDat == model.MaYeuCauDat)
                    .Select(d => (int?)d.MaDonHang).FirstOrDefaultAsync();
                if (daCo != null) return RedirectToAction("ChiTiet", "CustomerOrder", new { id = daCo });
                throw;
            }

            foreach (var item in gioHang.Items)
            {
                _db.ChiTietDonHangs.Add(new ChiTietDonHang
                {
                    MaDonHang = donHang.MaDonHang,
                    MaMonAn = item.MaMonAn,
                    SoLuong = item.SoLuong,
                    DonGia = item.Gia,
                    GhiChu = item.GhiChu,
                    TuyChon = item.TuyChonText,
                });
            }

            _db.ThanhToans.Add(new ThanhToan
            {
                MaDonHang = donHang.MaDonHang,
                HinhThuc = model.HinhThucThanhToan,
                TrangThai = "ChoThanhToan",
            });

            if (km != null)
            {
                km.SoLuongDaDung += 1;
                _db.KhuyenMaiKhachDungs.Add(new KhuyenMaiKhachDung { MaKhuyenMai = km.MaKhuyenMai, MaKH = maKH, MaDonHang = donHang.MaDonHang });
            }
            await _db.SaveChangesAsync();
            _cart.XoaGioHang();

            if (model.HinhThucThanhToan == "VNPay")
            {
                var returnUrl = Url.Action("VnPayReturn", "CustomerCart", null, Request.Scheme)!;
                var paymentUrl = _vnPayService.CreatePaymentUrl(new VnPayCreateRequest(
                    OrderId: donHang.MaDonHang,
                    AmountVnd: (long)tongTien,
                    OrderInfo: $"Thanh toan don hang #{donHang.MaDonHang} - FoodServiceApp",
                    IpAddress: HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1",
                    ReturnUrl: returnUrl));

                return Redirect(paymentUrl);
            }

            // Thanh toán tiền mặt khi nhận hàng -> không cần cổng thanh toán, vào thẳng trang theo dõi đơn.
            TempData["ThongBao"] = $"Đặt hàng thành công! Mã đơn #{donHang.MaDonHang}.";
            return RedirectToAction("ChiTiet", "CustomerOrder", new { id = donHang.MaDonHang });
        }

        // VNPay chuyển trình duyệt người dùng về đây sau khi thanh toán (thành công hoặc thất bại).
        // Cập nhật CSDL luôn tại đây (không chỉ chờ IPN) để demo vẫn hoạt động đúng ngay cả khi máy
        // chạy local không có địa chỉ public cho VNPay gọi IPN tới được. Có kiểm tra idempotent
        // (bỏ qua nếu đã "DaThanhToan") nên nếu IPN đã cập nhật trước đó thì không bị ghi đè sai.
        [HttpGet("vnpay-return")]
        [Authorize(Roles = "KhachHang")]
        public async Task<IActionResult> VnPayReturn()
        {
            var result = _vnPayService.ValidateCallback(Request.Query);

            if (!result.IsValidSignature)
            {
                TempData["LoiThongBao"] = "Không xác thực được kết quả thanh toán từ VNPay (chữ ký không hợp lệ).";
                return RedirectToAction("ChiTiet", "CustomerOrder", new { id = result.OrderId });
            }

            var thanhToan = await _db.ThanhToans.FirstOrDefaultAsync(t => t.MaDonHang == result.OrderId);
            if (thanhToan != null && thanhToan.TrangThai != "DaThanhToan")
            {
                if (result.IsSuccess)
                {
                    thanhToan.TrangThai = "DaThanhToan";
                    thanhToan.MaGiaoDich = result.TransactionId;
                    thanhToan.NgayThanhToan = DateTime.Now;
                    thanhToan.NgayGiaoDichCong = string.IsNullOrEmpty(result.PayDate) ? null : result.PayDate;

                    var donHang = await _db.DonHangs.FirstOrDefaultAsync(d => d.MaDonHang == result.OrderId);
                    if (donHang != null && donHang.TrangThai == "ChoXacNhan")
                        donHang.TrangThai = "DaXacNhan";
                }
                else
                {
                    thanhToan.TrangThai = "ThatBai";
                }
                await _db.SaveChangesAsync();
            }

            if (!result.IsSuccess && thanhToan?.TrangThai != "DaThanhToan")
            {
                TempData["LoiThongBao"] = $"Thanh toán VNPay không thành công. Bạn có thể thử lại hoặc đổi sang tiền mặt cho đơn #{result.OrderId}.";
            }
            else
            {
                TempData["ThongBao"] = $"Thanh toán VNPay thành công! Mã đơn #{result.OrderId}.";
            }
            return RedirectToAction("ChiTiet", "CustomerOrder", new { id = result.OrderId });
        }
    }
}
