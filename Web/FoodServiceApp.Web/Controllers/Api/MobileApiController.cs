using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using FoodServiceApp.Web.Data;
using FoodServiceApp.Web.Models.Entities;
using FoodServiceApp.Web.Services;
using FoodServiceApp.Web.Services.Maps;
using FoodServiceApp.Web.Services.Payment;

namespace FoodServiceApp.Web.Controllers.Api
{
    /// <summary>
    /// Compatibility API dành riêng cho FoodApp.Maui.
    /// Giữ nguyên contract /api/mobile/* mà app đang gọi.
    /// </summary>
    [ApiController]
    [Route("api/mobile")]
    public class MobileApiController : ControllerBase
    {
        private const decimal PhiCoBan = 15000m;
        private const double KmMienPhi = 2.0;
        private const decimal PhiMoiKmThem = 5000m;
        private const decimal PhuPhiNgoaiThanh = 10000m;
        private const decimal PhiToiDa = 70000m;

        // App cần vị trí tài xế cập nhật theo chu kỳ. DB hiện chưa có bảng vị trí,
        // nên lưu phiên hiện tại trong memory; dữ liệu vẫn được trả về cho app trong
        // cùng thời gian server đang chạy. Khi cần lưu bền vững, thêm bảng TaiXeViTri.
        private static readonly ConcurrentDictionary<int, DriverPosition> DriverPositions = new();
        private static readonly ConcurrentDictionary<int, decimal> ActualDeliveryFee = new();
        private static readonly ConcurrentDictionary<int, double> ActualDeliveryKm = new();
        private static readonly ConcurrentDictionary<int, byte> AdjustedFeeOrders = new();
        private static readonly ConcurrentDictionary<int, DeliveryRoute> DeliveryRoutes = new();
        private static readonly ConcurrentDictionary<int, byte> IssuesInProgress = new();

        private readonly AppDbContext _db;
        private readonly IConfiguration _config;
        private readonly IGoongMapsService _maps;
        private readonly IVnPayService _vnpay;

        public MobileApiController(AppDbContext db, IConfiguration config, IGoongMapsService maps, IVnPayService vnpay)
        {
            _db = db;
            _config = config;
            _maps = maps;
            _vnpay = vnpay;
        }

        private int CurrentMaTK => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        private string CurrentRole => User.FindFirstValue(ClaimTypes.Role) ?? "";

        private static IActionResult Loi(string thongDiep, int status = 400) =>
            new ObjectResult(new { thongDiep }) { StatusCode = status };

        private string CreateToken(int maTK, string tenDangNhap, string vaiTro)
        {
            var jwt = _config.GetSection("Jwt");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, maTK.ToString()),
                new Claim(ClaimTypes.Name, tenDangNhap),
                new Claim(ClaimTypes.Role, vaiTro),
            };
            var token = new JwtSecurityToken(
                issuer: jwt["Issuer"],
                audience: jwt["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(double.Parse(jwt["ExpireHours"] ?? "8")),
                signingCredentials: creds);
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        /// <summary>Trong giờ hoạt động? Hỗ trợ ca qua đêm (vd 18:00 → 02:00). 00:00–23:59 = cả ngày.</summary>
        private static bool StoreOpenNow(GianHang g)
        {
            var now = DateTime.Now.TimeOfDay;
            if (g.GioMoCua == g.GioDongCua) return true;
            return g.GioMoCua < g.GioDongCua ? now >= g.GioMoCua && now <= g.GioDongCua : now >= g.GioMoCua || now <= g.GioDongCua;
        }

        private static bool IsPaymentCash(string hinhThuc) => string.Equals(hinhThuc, "TienMat", StringComparison.OrdinalIgnoreCase);

        private static bool IsValidPayment(string hinhThuc) =>
            string.Equals(hinhThuc, "TienMat", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(hinhThuc, "VNPay", StringComparison.OrdinalIgnoreCase);

        // ============================== Catalog ==============================

        [HttpGet("gian-hang")]
        [AllowAnonymous]
        public async Task<IActionResult> GianHang()
        {
            var stores = await _db.GianHangs
                .Include(g => g.TaiKhoan)
                .Where(g => g.TrangThaiDuyet == "DaDuyet" && g.TrangThaiKinhDoanh == "DangMo")
                .OrderBy(g => g.MaGianHang)
                .ToListAsync();

            var results = new List<object>();
            foreach (var g in stores)
            {
                var ratings = await _db.DanhGias.Where(x => !x.DaAn && x.MaGianHang == g.MaGianHang).Select(x => x.SoSao).ToListAsync();
                results.Add(new
                {
                    id = g.MaGianHang,
                    tenGianHang = g.TenCuaHang,
                    diaChi = g.DiaChi,
                    hinhAnh = g.HinhAnh ?? "🍽️",
                    phanLoai = "",
                    danhGia = ratings.Count == 0 ? 0d : ratings.Average(),
                    thoiGianGiaoPhut = 30,
                    soDienThoai = g.SDT,
                    email = g.TaiKhoan == null ? "" : (g.TaiKhoan.TenDangNhap.Contains("@") ? g.TaiKhoan.TenDangNhap : $"{g.TaiKhoan.TenDangNhap}@gianhang.vn"),
                    moTa = g.MoTa ?? "",
                    dangMoCua = g.TrangThaiKinhDoanh == "DangMo",
                    taiKhoanBiKhoa = g.TaiKhoan != null && !g.TaiKhoan.TrangThai,
                    ngayDangKy = g.NgayDangKy,
                    trangThaiDangKy = g.TrangThaiDuyet
                });
            }
            return Ok(results);
        }

        [HttpGet("mon-an")]
        [AllowAnonymous]
        public async Task<IActionResult> MonAn()
        {
            var foods = await _db.MonAns
                .Include(m => m.DanhMuc)
                .Where(m => m.DanhMuc != null &&
                            m.DanhMuc.GianHang != null &&
                            m.DanhMuc.GianHang.TrangThaiDuyet == "DaDuyet" &&
                            m.DanhMuc.GianHang.TrangThaiKinhDoanh == "DangMo")
                .OrderBy(m => m.MaMonAn)
                .Select(m => new
                {
                    id = m.MaMonAn,
                    tenMon = m.TenMon,
                    gia = m.Gia,
                    hinhAnh = m.HinhAnh ?? "🍲",
                    danhMuc = m.DanhMuc!.TenDanhMuc,
                    gianHangId = m.DanhMuc.MaGianHang,
                    conHang = m.TinhTrang == "ConHang",
                    tamNgungBan = m.TinhTrang == "TamNgung",
                    choGhiChu = m.ChoGhiChu,
                    moTa = m.MoTa ?? ""
                })
                .ToListAsync();
            return Ok(foods);
        }

        [HttpGet("khuyen-mai")]
        [AllowAnonymous]
        public async Task<IActionResult> KhuyenMai()
        {
            var now = DateTime.Now;
            var promotions = await _db.KhuyenMais
                .Where(k => k.TrangThai && k.NgayBatDau <= now && k.NgayKetThuc >= now)
                .OrderByDescending(k => k.NgayKetThuc)
                .ToListAsync();

            return Ok(promotions.Select(k => new
            {
                id = k.MaKhuyenMai,
                maCode = k.MaCode,
                tenChuongTrinh = k.MoTa ?? k.MaCode,
                moTa = k.MoTa ?? "",
                loaiGiam = k.PhanTramGiam != null ? "PhanTram" : "SoTien",
                giaTriGiam = k.PhanTramGiam ?? k.SoTienGiam ?? 0,
                giaTriDonToiThieu = 0m,
                giamToiDa = (decimal?)null,
                ngayBatDau = k.NgayBatDau,
                ngayKetThuc = k.NgayKetThuc,
                gianHangId = k.MaGianHang,
                soLuongToiDa = 999999,
                soLuongDaDung = 0,
                dangHoatDong = k.TrangThai
            }));
        }

        // ============================== Auth ==============================

        public record CustomerLoginRequest(string Email, string MatKhau);
        public record CustomerRegisterRequest(string HoTen, string Sdt, string Email, string MatKhau, string? DiaChi);
        public record VendorLoginRequest(string Email, string MatKhau);
        public record DriverLoginRequest(string Sdt, string MatKhau);
        public record AdminLoginRequest(string Email, string MatKhau);

        [HttpPost("khach-hang/dang-nhap")]
        [AllowAnonymous]
        public async Task<IActionResult> CustomerLogin(CustomerLoginRequest request)
        {
            var email = request.Email?.Trim() ?? "";
            var kh = await _db.KhachHangs.FirstOrDefaultAsync(k => k.Email == email);
            if (kh == null) return Loi("Email hoặc mật khẩu không đúng.", 401);

            var tk = await _db.TaiKhoans.FirstOrDefaultAsync(t => t.MaTK == kh.MaTK && t.VaiTro == "KhachHang");
            if (tk == null || !await BaoMat.KhopAsync(_db, HttpContext, request.MatKhau, tk)) return Loi("Email hoặc mật khẩu không đúng.", 401);
            if (!tk.TrangThai) return Loi("Tài khoản đã bị khoá.", 401);

            return Ok(new
            {
                token = CreateToken(tk.MaTK, tk.TenDangNhap, "KhachHang"),
                khachHang = MapKhachHang(kh, tk)
            });
        }

        [HttpPost("khach-hang/dang-ky")]
        [AllowAnonymous]
        public async Task<IActionResult> CustomerRegister(CustomerRegisterRequest request)
        {
            var email = request.Email?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(email) || !email.Contains('@')) return Loi("Email không hợp lệ.");
            if (string.IsNullOrWhiteSpace(request.MatKhau) || request.MatKhau.Length < 6) return Loi("Mật khẩu tối thiểu 6 ký tự.");
            if (await _db.KhachHangs.AnyAsync(k => k.Email == email)) return Loi("Email này đã được đăng ký.", 409);
            if (await _db.TaiKhoans.AnyAsync(t => t.TenDangNhap == email)) return Loi("Tài khoản đã tồn tại.", 409);

            var tk = new TaiKhoan
            {
                TenDangNhap = email,
                MatKhau = MatKhauHelper.Hash(request.MatKhau),
                VaiTro = "KhachHang",
                TrangThai = true,
            };
            _db.TaiKhoans.Add(tk);
            await _db.SaveChangesAsync();

            var kh = new KhachHang
            {
                MaTK = tk.MaTK,
                HoTen = request.HoTen.Trim(),
                SDT = request.Sdt?.Trim() ?? "",
                Email = email,
                DiaChiGiaoHang = string.IsNullOrWhiteSpace(request.DiaChi) ? null : request.DiaChi.Trim()
            };
            _db.KhachHangs.Add(kh);
            await _db.SaveChangesAsync();

            if (!string.IsNullOrWhiteSpace(kh.DiaChiGiaoHang))
            {
                _db.DiaChiKhachHangs.Add(new DiaChiKhachHang
                {
                    MaKH = kh.MaKH,
                    Nhan = "Nhà",
                    DiaChi = kh.DiaChiGiaoHang,
                    MacDinh = true
                });
                await _db.SaveChangesAsync();
            }

            return Ok(new
            {
                token = CreateToken(tk.MaTK, tk.TenDangNhap, "KhachHang"),
                khachHang = MapKhachHang(kh, tk)
            });
        }

        [HttpPost("gian-hang/dang-nhap")]
        [AllowAnonymous]
        public async Task<IActionResult> VendorLogin(VendorLoginRequest request)
        {
            var email = request.Email?.Trim() ?? "";
            var loginName = email.EndsWith("@gianhang.vn", StringComparison.OrdinalIgnoreCase)
                ? email[..email.IndexOf('@')]
                : email;
            var tk = await _db.TaiKhoans.FirstOrDefaultAsync(t =>
                (t.TenDangNhap == email || t.TenDangNhap == loginName) && t.VaiTro == "GianHang");
            if (tk == null) return Loi("Email hoặc mật khẩu không đúng.", 401);
            var store = await _db.GianHangs.FirstOrDefaultAsync(g => g.MaTK == tk.MaTK);
            if (store == null) return Loi("Email hoặc mật khẩu không đúng.", 401);
            if (tk == null || !await BaoMat.KhopAsync(_db, HttpContext, request.MatKhau, tk)) return Loi("Email hoặc mật khẩu không đúng.", 401);
            if (!tk.TrangThai) return Loi("Tài khoản gian hàng đang bị khoá.", 401);
            if (store.TrangThaiDuyet == "ChoDuyet") return Loi("Hồ sơ gian hàng đang chờ quản trị viên duyệt.", 403);
            if (store.TrangThaiDuyet == "TuChoi") return Loi("Hồ sơ gian hàng đã bị từ chối.", 403);

            return Ok(new
            {
                token = CreateToken(tk.MaTK, tk.TenDangNhap, "GianHang"),
                gianHang = MapGianHang(store, tk)
            });
        }

        [HttpPost("gian-hang/dang-ky")]
        [AllowAnonymous]
        public async Task<IActionResult> VendorRegister(MobileVendorRegisterRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.TenGianHang) || string.IsNullOrWhiteSpace(request.DiaChi)) return Loi("Tên và địa chỉ gian hàng không được để trống.");
            if (string.IsNullOrWhiteSpace(request.Email) || !request.Email.Contains('@')) return Loi("Email không hợp lệ.");
            if (request.MatKhau?.Length < 6) return Loi("Mật khẩu tối thiểu 6 ký tự.");
            if (await _db.TaiKhoans.AnyAsync(t => t.TenDangNhap == request.Email.Trim())) return Loi("Email này đã được đăng ký.", 409);
            if (string.IsNullOrWhiteSpace(request.MatKhau)) return Loi("Vui lòng nhập mật khẩu.");

            var tk = new TaiKhoan
            {
                TenDangNhap = request.Email.Trim(),
                MatKhau = MatKhauHelper.Hash(request.MatKhau),
                VaiTro = "GianHang",
                TrangThai = true,
            };
            _db.TaiKhoans.Add(tk);
            await _db.SaveChangesAsync();

            var gh = new GianHang
            {
                MaTK = tk.MaTK,
                TenCuaHang = request.TenGianHang.Trim(),
                DiaChi = request.DiaChi.Trim(),
                SDT = request.Sdt?.Trim() ?? "",
                MoTa = request.MoTa?.Trim(),
                HinhAnh = request.HinhAnh,
                TrangThaiKinhDoanh = "DongCua",
                TrangThaiDuyet = "ChoDuyet",
                NgayDangKy = DateTime.Now,
                TyLeChietKhau = await FoodServiceApp.Web.Services.CauHinhService.HoaHongMacDinhAsync(_db),
            };
            _db.GianHangs.Add(gh);
            await _db.SaveChangesAsync();

            return Ok(new { id = gh.MaGianHang, trangThaiDangKy = gh.TrangThaiDuyet });
        }

        public record MobileDriverRegisterRequest(string HoTen, string SoDienThoai, string? SoCccd, string? LoaiXe, string? BienSoXe, string? SoGplx, string MatKhau);
        public record MobileLyDoRequest(string? LyDo);

        /// <summary>Tài xế tự đăng ký -> hồ sơ ở trạng thái ChoDuyet, quản trị viên duyệt rồi mới đăng nhập được.</summary>
        [HttpPost("tai-xe/dang-ky")]
        [AllowAnonymous]
        public async Task<IActionResult> DriverRegister(MobileDriverRegisterRequest request)
        {
            var sdt = NormalizePhone(request.SoDienThoai ?? "");
            if (string.IsNullOrWhiteSpace(request.HoTen)) return Loi("Họ tên không được để trống.");
            if (string.IsNullOrWhiteSpace(sdt) || sdt.Length < 9) return Loi("Số điện thoại không hợp lệ.");
            if (request.MatKhau == null || request.MatKhau.Length < 6) return Loi("Mật khẩu tối thiểu 6 ký tự.");
            if (await _db.TaiXes.AnyAsync(t => t.SDT == sdt) || await _db.TaiKhoans.AnyAsync(t => t.TenDangNhap == sdt))
                return Loi("Số điện thoại này đã được đăng ký.", 409);

            var tk = new TaiKhoan { TenDangNhap = sdt, MatKhau = MatKhauHelper.Hash(request.MatKhau), VaiTro = "TaiXe", TrangThai = true };
            _db.TaiKhoans.Add(tk);
            await _db.SaveChangesAsync();

            var tx = new TaiXe
            {
                MaTK = tk.MaTK,
                HoTen = request.HoTen.Trim(),
                SDT = sdt,
                PhuongTien = request.LoaiXe?.Trim(),
                BienSo = request.BienSoXe?.Trim(),
                SoCCCD = request.SoCccd?.Trim(),
                SoGPLX = request.SoGplx?.Trim(),
                TrangThai = "NgoaiTuyen",
                TrangThaiDuyet = "ChoDuyet",
                NgayDangKy = DateTime.Now,
            };
            _db.TaiXes.Add(tx);
            await _db.SaveChangesAsync();
            return Ok(new { id = tx.MaTaiXe, trangThaiDangKy = tx.TrangThaiDuyet });
        }

        [HttpPost("tai-xe/dang-nhap")]
        [AllowAnonymous]
        public async Task<IActionResult> DriverLogin(DriverLoginRequest request)
        {
            var sdt = NormalizePhone(request.Sdt);
            var tx = await _db.TaiXes.FirstOrDefaultAsync(t => t.SDT == sdt || t.SDT == request.Sdt);
            if (tx == null) return Loi("Sai số điện thoại hoặc mật khẩu.", 401);
            var tk = await _db.TaiKhoans.FirstOrDefaultAsync(t => t.MaTK == tx.MaTK && t.VaiTro == "TaiXe");
            if (tk == null || !await BaoMat.KhopAsync(_db, HttpContext, request.MatKhau, tk)) return Loi("Sai số điện thoại hoặc mật khẩu.", 401);
            if (!tk.TrangThai || tx.TrangThaiDuyet != "DaDuyet") return Loi("Tài khoản tài xế chưa được kích hoạt.", 403);
            return Ok(new { token = CreateToken(tk.MaTK, tk.TenDangNhap, "TaiXe") });
        }

        [HttpPost("admin/dang-nhap")]
        [AllowAnonymous]
        public async Task<IActionResult> AdminLogin(AdminLoginRequest request)
        {
            var email = request.Email?.Trim() ?? "";
            var admin = await _db.QuanTris.FirstOrDefaultAsync(q => q.Email == email);
            if (admin == null) return Loi("Email hoặc mật khẩu không đúng.", 401);
            var tk = await _db.TaiKhoans.FirstOrDefaultAsync(t => t.MaTK == admin.MaTK && t.VaiTro == "QuanTri");
            if (tk == null || !await BaoMat.KhopAsync(_db, HttpContext, request.MatKhau, tk)) return Loi("Email hoặc mật khẩu không đúng.", 401);
            if (!tk.TrangThai) return Loi("Tài khoản đã bị khoá.", 401);
            // vaiTro: SuperAdmin | QuanLy | NhanVien — app MAUI dùng để ẩn/khóa chức năng theo quyền (server vẫn kiểm tra lại)
            return Ok(new { token = CreateToken(tk.MaTK, tk.TenDangNhap, "QuanTri"), vaiTro = admin.CapQuyenHan, hoTen = admin.HoTen });
        }

        // ============================== Customer ==============================

        [HttpGet("khach-hang/don-hang")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "KhachHang")]
        public async Task<IActionResult> CustomerOrders()
        {
            var kh = await CurrentCustomerAsync();
            if (kh == null) return Loi("Tài khoản khách hàng không hợp lệ.", 401);
            var orders = await _db.DonHangs
                .Include(d => d.GianHang)
                .Include(d => d.KhachHang)
                .Include(d => d.ChiTiets).ThenInclude(c => c.MonAn)
                .Include(d => d.GiaoHang).ThenInclude(g => g!.TaiXe)
                .Include(d => d.ThanhToan)
                .Include(d => d.KhuyenMaiApDung)
                .Where(d => d.MaKH == kh.MaKH)
                .OrderByDescending(d => d.NgayDat)
                .ToListAsync();
            await HydratePositionsAsync(orders.Where(o => o.TrangThai == "DangGiao").Select(o => o.MaDonHang).ToList());
            return Ok(orders.Select(o => MapMobileOrder(o, true)));
        }

        [HttpPost("don-hang")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "KhachHang")]
        public async Task<IActionResult> PlaceOrder(MobilePlaceOrderRequest request)
        {
            var kh = await CurrentCustomerAsync();
            if (kh == null) return Loi("Tài khoản khách hàng không hợp lệ.", 401);
            if (request.ChiTiet is null || request.ChiTiet.Count == 0) return Loi("Giỏ hàng đang trống.");
            if (string.IsNullOrWhiteSpace(request.DiaChiGiao)) return Loi("Vui lòng nhập địa chỉ giao hàng.");
            if (!IsValidPayment(request.HinhThucThanhToan)) return Loi("Hình thức thanh toán không hợp lệ.");

            var store = await _db.GianHangs.FirstOrDefaultAsync(g =>
                g.MaGianHang == request.GianHangId && g.TrangThaiDuyet == "DaDuyet" && g.TrangThaiKinhDoanh == "DangMo");
            if (store == null) return Loi("Gian hàng không còn hoạt động.", 404);
            if (!store.DangNhanDon) return Loi("Gian hàng đang tạm dừng nhận đơn.");
            if (!StoreOpenNow(store)) return Loi($"Gian hàng ngoài giờ hoạt động ({store.GioMoCua:hh\\:mm} – {store.GioDongCua:hh\\:mm}).");

            if (request.ChiTiet.Any(x => x.SoLuong <= 0)) return Loi("Số lượng món phải lớn hơn 0.");
            var ids = request.ChiTiet.Select(x => x.MonAnId).Distinct().ToList();
            var foods = await _db.MonAns.Include(m => m.DanhMuc).Where(m => ids.Contains(m.MaMonAn)).ToListAsync();
            if (foods.Count != ids.Count || foods.Any(m => m.DanhMuc?.MaGianHang != store.MaGianHang)) return Loi("Có món ăn không thuộc gian hàng đã chọn.");
            if (foods.Any(m => m.TinhTrang != "ConHang")) return Loi("Có món ăn hiện đã hết hàng.");

            var tongTienMon = request.ChiTiet.Sum(i => foods.First(f => f.MaMonAn == i.MonAnId).Gia * i.SoLuong);
            var (giamGia, maKm) = await CalculatePromotionAsync(request.MaKhuyenMai, store.MaGianHang, tongTienMon);
            var route = await CalculateDeliveryAsync(store.DiaChi, request.DiaChiGiao);
            var tongThanhToan = Math.Max(0, tongTienMon - giamGia) + route.Phi;

            var maXacNhan = FoodServiceApp.Web.Services.GiaoHangXuLy.TaoMa();   // lưu cột riêng, KHÔNG để trong ghi chú (tài xế đọc được ghi chú)
            var ghiChu = string.IsNullOrWhiteSpace(request.GhiChu) ? null : request.GhiChu.Trim();

            var don = new DonHang
            {
                MaKH = kh.MaKH,
                MaGianHang = store.MaGianHang,
                TrangThai = "ChoXacNhan",
                TongTien = tongThanhToan,
                PhiGiaoHang = route.Phi,
                DiaChiGiaoHang = request.DiaChiGiao.Trim(),
                GhiChu = ghiChu,
                MaXacNhanGiao = maXacNhan,
                MaKhuyenMaiApDung = maKm,
            };
            _db.DonHangs.Add(don);
            await _db.SaveChangesAsync();

            foreach (var item in request.ChiTiet)
            {
                var mon = foods.First(f => f.MaMonAn == item.MonAnId);
                _db.ChiTietDonHangs.Add(new ChiTietDonHang
                {
                    MaDonHang = don.MaDonHang,
                    MaMonAn = mon.MaMonAn,
                    SoLuong = item.SoLuong,
                    DonGia = mon.Gia,
                });
            }

            _db.ThanhToans.Add(new ThanhToan
            {
                MaDonHang = don.MaDonHang,
                HinhThuc = request.HinhThucThanhToan,
                TrangThai = IsPaymentCash(request.HinhThucThanhToan) ? "ChoThanhToan" : "ChoThanhToan"
            });
            await _db.SaveChangesAsync();
            DeliveryRoutes[don.MaDonHang] = route;

            string? paymentUrl = null;
            if (string.Equals(request.HinhThucThanhToan, "VNPay", StringComparison.OrdinalIgnoreCase))
            {
                paymentUrl = _vnpay.CreatePaymentUrl(new VnPayCreateRequest(
                    don.MaDonHang,
                    (long)tongThanhToan,
                    $"Thanh toan don hang #{don.MaDonHang} - FoodServiceApp",
                    HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1",
                    Url.Action("VnPayReturn", "Payment", null, Request.Scheme)!));
            }

            return Ok(new
            {
                id = don.MaDonHang,
                maDonHang = don.MaDonHang,
                tongTienMon,
                giamGia,
                phiGiaoHang = route.Phi,
                khoangCachKm = route.Km,
                thoiGianDuKienPhut = route.Phut,
                tongTien = tongThanhToan,
                maXacNhanGiao = maXacNhan,
                paymentUrl
            });
        }

        [HttpPut("khach-hang/ho-so")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "KhachHang")]
        public async Task<IActionResult> UpdateCustomerProfile(MobileCustomerProfileRequest request)
        {
            var kh = await CurrentCustomerAsync();
            if (kh == null) return Loi("Tài khoản khách hàng không hợp lệ.", 401);
            if (string.IsNullOrWhiteSpace(request.HoTen)) return Loi("Vui lòng nhập họ tên.");
            kh.HoTen = request.HoTen.Trim();
            if (request.Sdt != null) kh.SDT = NormalizePhone(request.Sdt);
            if (request.DiaChi != null) kh.DiaChiGiaoHang = request.DiaChi.Trim();
            await _db.SaveChangesAsync();
            return Ok(new { message = "Đã cập nhật hồ sơ." });
        }

        [HttpPost("van-de")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "KhachHang")]
        public async Task<IActionResult> CreateIssue(MobileIssueRequest request)
        {
            var kh = await CurrentCustomerAsync();
            if (kh == null) return Loi("Tài khoản khách hàng không hợp lệ.", 401);
            if (string.IsNullOrWhiteSpace(request.TieuDe) || string.IsNullOrWhiteSpace(request.MoTa)) return Loi("Tiêu đề và nội dung không được để trống.");

            int? orderId = request.DonHangId;
            int? storeId = null;
            if (orderId is int id)
            {
                var order = await _db.DonHangs.FirstOrDefaultAsync(d => d.MaDonHang == id && d.MaKH == kh.MaKH);
                if (order == null) return Loi("Đơn hàng không hợp lệ.", 404);
                storeId = order.MaGianHang;
            }

            var ph = new PhanHoi
            {
                MaKH = kh.MaKH,
                MaDonHang = orderId,
                MaGianHang = storeId,
                Loai = "KhieuNai",
                TieuDe = request.TieuDe.Trim(),
                NoiDung = request.MoTa.Trim(),
                TrangThai = "ChuaXuLy"
            };
            _db.PhanHois.Add(ph);
            await _db.SaveChangesAsync();
            return Ok(new { id = ph.MaPhanHoi });
        }

        [HttpPost("danh-gia")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "KhachHang")]
        public async Task<IActionResult> Review(MobileReviewRequest request)
        {
            var kh = await CurrentCustomerAsync();
            if (kh == null) return Loi("Tài khoản khách hàng không hợp lệ.", 401);
            if (request.SoSaoMonAn < 1 || request.SoSaoMonAn > 5) return Loi("Số sao món ăn phải từ 1 đến 5.");
            if (request.SoSaoGiaoHang < 0 || request.SoSaoGiaoHang > 5) return Loi("Số sao giao hàng phải từ 0 đến 5.");

            var order = await _db.DonHangs.FirstOrDefaultAsync(d => d.MaDonHang == request.DonHangId && d.MaKH == kh.MaKH);
            if (order == null) return Loi("Không tìm thấy đơn hàng.", 404);
            if (order.TrangThai != "DaGiao") return Loi("Chỉ được đánh giá sau khi đơn đã giao.");
            if (await _db.DanhGias.AnyAsync(x => x.MaKH == kh.MaKH && x.MaDonHang == order.MaDonHang)) return Loi("Đơn hàng này đã được đánh giá.");

            _db.DanhGias.Add(new DanhGia
            {
                MaKH = kh.MaKH,
                MaGianHang = order.MaGianHang,
                MaDonHang = order.MaDonHang,
                SoSao = request.SoSaoMonAn,
                NoiDung = request.NoiDung?.Trim()
            });

            if (request.SoSaoGiaoHang > 0 && order.MaTaiXe.HasValue)
            {
                var note = string.IsNullOrWhiteSpace(request.NoiDung) ? "" : $"Đánh giá giao hàng: {request.SoSaoGiaoHang}/5 — {request.NoiDung.Trim()}";
                order.GhiChu = string.IsNullOrWhiteSpace(order.GhiChu) ? note : $"{order.GhiChu} | {note}";
            }

            await _db.SaveChangesAsync();
            return Ok(new { message = "Cảm ơn bạn đã đánh giá." });
        }

        // ============================== Vendor ==============================

        [HttpGet("gian-hang/{id:int}/don-hang")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "GianHang")]
        public async Task<IActionResult> VendorOrders(int id)
        {
            var owner = await _db.GianHangs.FirstOrDefaultAsync(g => g.MaGianHang == id && g.MaTK == CurrentMaTK);
            if (owner == null) return Loi("Bạn không có quyền xem gian hàng này.", 403);
            var orders = await _db.DonHangs
                .Include(d => d.GianHang)
                .Include(d => d.KhachHang)
                .Include(d => d.ChiTiets).ThenInclude(c => c.MonAn)
                .Include(d => d.GiaoHang).ThenInclude(g => g!.TaiXe)
                .Include(d => d.KhuyenMaiApDung)
                .Where(d => d.MaGianHang == id)
                .OrderByDescending(d => d.NgayDat)
                .ToListAsync();
            return Ok(orders.Select(MapMobileOrder));
        }

        [HttpPut("gian-hang/{id:int}/trang-thai-kinh-doanh")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "GianHang")]
        public async Task<IActionResult> VendorBusinessStatus(int id, MobileVendorBusinessStatusRequest request)
        {
            var store = await _db.GianHangs.FirstOrDefaultAsync(g => g.MaGianHang == id && g.MaTK == CurrentMaTK);
            if (store == null) return Loi("Không tìm thấy gian hàng.", 404);
            if (store.TrangThaiDuyet != "DaDuyet") return Loi("Gian hàng chưa được duyệt.", 403);
            store.TrangThaiKinhDoanh = request.DangMoCua ? "DangMo" : "DongCua";
            await _db.SaveChangesAsync();
            return Ok(new { dangMoCua = request.DangMoCua, trangThaiKinhDoanh = store.TrangThaiKinhDoanh });
        }

        [HttpPut("don-hang/{id:int}/xac-nhan")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "GianHang")]
        public async Task<IActionResult> VendorConfirm(int id)
        {
            return await UpdateVendorOrderStatus(id, "DaXacNhan");
        }

        [HttpPut("don-hang/{id:int}/tu-choi")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "GianHang")]
        public async Task<IActionResult> VendorReject(int id, [FromBody] MobileRejectOrderRequest? request)
        {
            var lyDo = request?.LyDo?.Trim();
            if (string.IsNullOrWhiteSpace(lyDo)) return Loi("Từ chối đơn phải kèm lý do.");   // NV04
            return await UpdateVendorOrderStatus(id, "DaHuy", lyDo);
        }

        [HttpPut("don-hang/{id:int}/san-sang-giao")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "GianHang")]
        public async Task<IActionResult> VendorReadyToDeliver(int id)
        {
            var store = await GetOrderForVendorAsync(id);
            if (store == null) return Loi("Không tìm thấy đơn hàng hoặc bạn không có quyền.", 404);
            if (store.TrangThai != "DaXacNhan") return Loi("Đơn chưa ở trạng thái sẵn sàng giao.");
            return Ok(new { trangThai = store.TrangThai });
        }

        // ============================== Admin ==============================

        [HttpGet("admin/khach-hang")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "QuanTri")]
        public async Task<IActionResult> AdminCustomers()
        {
            var data = await _db.KhachHangs
                .Include(k => k.TaiKhoan)
                .Select(k => new
                {
                    id = k.MaKH,
                    hoTen = k.HoTen,
                    email = k.Email ?? "",
                    matKhau = "",
                    soDienThoai = k.SDT,
                    diaChi = k.DiaChiGiaoHang ?? "",
                    ngayDangKy = k.TaiKhoan != null ? k.TaiKhoan.NgayTao : DateTime.Now,
                    tongSoDon = _db.DonHangs.Count(d => d.MaKH == k.MaKH),
                    taiKhoanBiKhoa = k.TaiKhoan != null && !k.TaiKhoan.TrangThai
                })
                .OrderByDescending(k => k.id)
                .ToListAsync();
            return Ok(data);
        }

        [HttpGet("admin/van-de")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "QuanTri")]
        public async Task<IActionResult> AdminIssues()
        {
            var issues = await _db.PhanHois.Include(p => p.KhachHang).OrderByDescending(p => p.NgayGui).ToListAsync();
            return Ok(issues.Select(MapIssue));
        }

        [FoodServiceApp.Web.Filters.ChiSuperAdminGhi]

        [HttpPut("admin/gian-hang/{id:int}/duyet")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "QuanTri")]
        public async Task<IActionResult> AdminApproveVendor(int id) => await SetVendorApproval(id, "DaDuyet");

        [FoodServiceApp.Web.Filters.ChiSuperAdminGhi]

        [HttpPut("admin/gian-hang/{id:int}/tu-choi")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "QuanTri")]
        public async Task<IActionResult> AdminRejectVendor(int id) => await SetVendorApproval(id, "TuChoi");

        [FoodServiceApp.Web.Filters.ChiSuperAdminGhi]

        [HttpPut("admin/gian-hang/{id:int}/khoa")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "QuanTri")]
        public async Task<IActionResult> AdminLockVendor(int id) => await ToggleVendorLock(id, false);

        [FoodServiceApp.Web.Filters.ChiSuperAdminGhi]

        [HttpPut("admin/gian-hang/{id:int}/mo-khoa")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "QuanTri")]
        public async Task<IActionResult> AdminUnlockVendor(int id) => await ToggleVendorLock(id, true);

        [FoodServiceApp.Web.Filters.ChiSuperAdminGhi]

        [HttpPut("admin/khach-hang/{id:int}/khoa")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "QuanTri")]
        public async Task<IActionResult> AdminLockCustomer(int id) => await ToggleCustomerLock(id, false);

        [FoodServiceApp.Web.Filters.ChiSuperAdminGhi]

        [HttpPut("admin/khach-hang/{id:int}/mo-khoa")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "QuanTri")]
        public async Task<IActionResult> AdminUnlockCustomer(int id) => await ToggleCustomerLock(id, true);



        [HttpPut("admin/van-de/{id:int}/dang-xu-ly")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "QuanTri")]
        public async Task<IActionResult> AdminIssueInProgress(int id)
        {
            var exists = await _db.PhanHois.AnyAsync(p => p.MaPhanHoi == id);
            if (!exists) return Loi("Không tìm thấy vấn đề.", 404);
            IssuesInProgress[id] = 1;
            return Ok(new { trangThai = "DangXuLy" });
        }



        [HttpPut("admin/van-de/{id:int}/giai-quyet")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "QuanTri")]
        public async Task<IActionResult> AdminResolveIssue(int id)
        {
            var ph = await _db.PhanHois.FirstOrDefaultAsync(p => p.MaPhanHoi == id);
            if (ph == null) return Loi("Không tìm thấy vấn đề.", 404);
            ph.TrangThai = "DaXuLy";
            ph.NgayXuLy = DateTime.Now;
            IssuesInProgress.TryRemove(id, out _);
            await _db.SaveChangesAsync();
            return Ok(new { trangThai = "DaGiaiQuyet" });
        }

        [HttpGet("admin/tai-xe")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "QuanTri")]
        public async Task<IActionResult> AdminDrivers()
        {
            var data = await (from t in _db.TaiXes
                              join k in _db.TaiKhoans on t.MaTK equals k.MaTK
                              orderby t.MaTaiXe descending
                              select new
                              {
                                  id = t.MaTaiXe.ToString(),
                                  hoTen = t.HoTen,
                                  loaiXe = t.PhuongTien ?? "Xe máy",
                                  soDienThoai = t.SDT,
                                  bienSoXe = t.BienSo ?? "",
                                  soCccd = t.SoCCCD ?? "",
                                  soGplx = t.SoGPLX ?? "",
                                  trangThaiDangKy = t.TrangThaiDuyet,
                                  lyDoTuChoi = t.LyDoTuChoi ?? "",
                                  ngayDangKy = t.NgayDangKy,
                                  taiKhoanBiKhoa = !k.TrangThai,
                                  dangHoatDong = t.TrangThai != "NgoaiTuyen"
                              }).ToListAsync();
            return Ok(data);
        }

        [FoodServiceApp.Web.Filters.ChiSuperAdminGhi]

        [HttpPut("admin/tai-xe/{id:int}/duyet")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "QuanTri")]
        public async Task<IActionResult> AdminApproveDriver(int id)
        {
            var tx = await _db.TaiXes.FirstOrDefaultAsync(t => t.MaTaiXe == id);
            if (tx == null) return Loi("Không tìm thấy tài xế.", 404);
            tx.TrangThaiDuyet = "DaDuyet";
            tx.LyDoTuChoi = null;
            if (tx.TrangThai == "NgoaiTuyen") tx.TrangThai = "SanSang";
            await _db.SaveChangesAsync();
            return Ok(new { trangThaiDangKy = tx.TrangThaiDuyet });
        }

        [FoodServiceApp.Web.Filters.ChiSuperAdminGhi]

        [HttpPut("admin/tai-xe/{id:int}/tu-choi")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "QuanTri")]
        public async Task<IActionResult> AdminRejectDriver(int id, [FromBody] MobileLyDoRequest? body)
        {
            var tx = await _db.TaiXes.FirstOrDefaultAsync(t => t.MaTaiXe == id);
            if (tx == null) return Loi("Không tìm thấy tài xế.", 404);
            tx.TrangThaiDuyet = "TuChoi";
            tx.LyDoTuChoi = string.IsNullOrWhiteSpace(body?.LyDo) ? "Hồ sơ không đạt yêu cầu" : body!.LyDo!.Trim();
            await _db.SaveChangesAsync();
            return Ok(new { trangThaiDangKy = tx.TrangThaiDuyet });
        }

        [FoodServiceApp.Web.Filters.ChiSuperAdminGhi]

        [HttpPut("admin/tai-xe/{id:int}/khoa")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "QuanTri")]
        public async Task<IActionResult> AdminLockDriver(int id) => await ToggleDriverLock(id, false);

        [FoodServiceApp.Web.Filters.ChiSuperAdminGhi]

        [HttpPut("admin/tai-xe/{id:int}/mo-khoa")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "QuanTri")]
        public async Task<IActionResult> AdminUnlockDriver(int id) => await ToggleDriverLock(id, true);

        private async Task<IActionResult> ToggleDriverLock(int id, bool active)
        {
            var tx = await _db.TaiXes.FirstOrDefaultAsync(t => t.MaTaiXe == id);
            if (tx == null) return Loi("Không tìm thấy tài xế.", 404);
            var tk = await _db.TaiKhoans.FirstOrDefaultAsync(t => t.MaTK == tx.MaTK);
            if (tk == null) return Loi("Không tìm thấy tài khoản tài xế.", 404);
            tk.TrangThai = active;
            if (!active) tx.TrangThai = "NgoaiTuyen";
            await _db.SaveChangesAsync();
            return Ok(new { taiKhoanBiKhoa = !tk.TrangThai });
        }

        // ============================== Driver ==============================

        [HttpPut("tai-xe/ho-so")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "TaiXe")]
        public async Task<IActionResult> UpdateDriverProfile(MobileDriverProfileRequest request)
        {
            var tx = await CurrentDriverAsync();
            if (tx == null) return Loi("Tài khoản tài xế không hợp lệ.", 401);
            if (!string.IsNullOrWhiteSpace(request.HoTen)) tx.HoTen = request.HoTen.Trim();
            if (!string.IsNullOrWhiteSpace(request.SoDienThoai)) tx.SDT = NormalizePhone(request.SoDienThoai);
            if (!string.IsNullOrWhiteSpace(request.BienSoXe)) tx.BienSo = request.BienSoXe.Trim();
            if (!string.IsNullOrWhiteSpace(request.LoaiXe)) tx.PhuongTien = request.LoaiXe.Trim();
            await _db.SaveChangesAsync();
            return Ok(new { message = "Đã cập nhật hồ sơ tài xế." });
        }

        [HttpGet("tai-xe/don-cho-nhan")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "TaiXe")]
        public async Task<IActionResult> DriverAvailableOrders()
        {
            var tx = await CurrentDriverAsync();
            if (tx == null) return Loi("Tài khoản tài xế không hợp lệ.", 401);
            var orders = await _db.DonHangs
                .Include(d => d.GianHang)
                .Include(d => d.KhachHang)
                .Include(d => d.ChiTiets).ThenInclude(c => c.MonAn)
                .Include(d => d.GiaoHang).ThenInclude(g => g!.TaiXe)
                .Include(d => d.KhuyenMaiApDung)
                .Where(d => d.TrangThai == "DaXacNhan" && d.MaTaiXe == null)
                .OrderBy(d => d.NgayDat)
                .ToListAsync();
            return Ok(orders.Select(MapMobileOrder));
        }

        [HttpPut("tai-xe/don-hang/{id:int}/nhan")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "TaiXe")]
        public async Task<IActionResult> DriverAccept(int id)
        {
            var tx = await CurrentDriverAsync();
            if (tx == null) return Loi("Tài khoản tài xế không hợp lệ.", 401);
            var kq = await FoodServiceApp.Web.Services.GiaoHangXuLy.NhanDonAsync(_db, tx, id);
            if (!kq.Ok) return Loi(kq.ThongDiep, kq.ThongDiep.Contains("tài xế khác") ? 409 : 400);
            return Ok(new { id, taiXeId = tx.MaTaiXe });
        }

        [HttpGet("tai-xe/dang-giao")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "TaiXe")]
        public async Task<IActionResult> DriverCurrent()
        {
            var tx = await CurrentDriverAsync();
            if (tx == null) return Loi("Tài khoản tài xế không hợp lệ.", 401);
            var order = await _db.DonHangs
                .Include(d => d.KhachHang)
                .Include(d => d.GianHang)
                .Include(d => d.ChiTiets).ThenInclude(c => c.MonAn)
                .Include(d => d.GiaoHang).ThenInclude(g => g!.TaiXe)
                .Include(d => d.ThanhToan)
                .Include(d => d.KhuyenMaiApDung)
                .FirstOrDefaultAsync(d => d.MaTaiXe == tx.MaTaiXe && (d.TrangThai == "DaXacNhan" || d.TrangThai == "DangGiao"));
            return Ok(order == null ? null : MapMobileOrder(order));
        }

        [HttpPut("tai-xe/vi-tri")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "TaiXe")]
        public async Task<IActionResult> UpdateDriverPosition(MobileDriverPositionRequest request)
        {
            var tx = await CurrentDriverAsync();
            if (tx == null) return Loi("Tài khoản tài xế không hợp lệ.", 401);
            var donHangId = request.DonHangId;
            var order = await _db.DonHangs.FirstOrDefaultAsync(d => d.MaDonHang == donHangId && d.MaTaiXe == tx.MaTaiXe);
            if (order == null) return Loi("Đơn hàng không thuộc tài xế hiện tại.", 404);
            if (request.ViDo is < -90 or > 90 || request.KinhDo is < -180 or > 180) return Loi("Tọa độ không hợp lệ.");
            if (order.TrangThai is not ("DaXacNhan" or "DangGiao")) return Loi("Đơn không còn ở trạng thái giao, không nhận vị trí.", 409);
            var kq = await FoodServiceApp.Web.Services.GiaoHangXuLy.LuuViTriAsync(_db, donHangId, tx.MaTaiXe, request.ViDo, request.KinhDo);
            if (!kq.Ok) return Loi(kq.ThongDiep, 422);
            DriverPositions[donHangId] = new DriverPosition(request.ViDo, request.KinhDo, DateTime.Now);
            return Ok(new { viDo = request.ViDo, kinhDo = request.KinhDo, viTriCapNhatLuc = DateTime.Now });
        }

        [HttpPut("don-hang/{id:int}/da-lay-hang")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "TaiXe")]
        public async Task<IActionResult> DriverPickedUp(int id)
        {
            var tx = await CurrentDriverAsync();
            if (tx == null) return Loi("Tài khoản tài xế không hợp lệ.", 401);
            var order = await _db.DonHangs.Include(d => d.GiaoHang).FirstOrDefaultAsync(d => d.MaDonHang == id && d.MaTaiXe == tx.MaTaiXe);
            if (order == null) return Loi("Không tìm thấy đơn giao.", 404);
            if (order.GiaoHang == null) return Loi("Đơn chưa được nhận hợp lệ.", 409);
            var kq = await FoodServiceApp.Web.Services.GiaoHangXuLy.DaLayHangAsync(_db, order, order.GiaoHang);
            if (!kq.Ok) return Loi(kq.ThongDiep, 409);
            return Ok(new { trangThai = order.TrangThai, daLayHang = true });
        }

        [HttpPut("don-hang/{id:int}/hoan-thanh")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "TaiXe")]
        public async Task<IActionResult> DriverComplete(int id, MobileCompleteOrderRequest request)
        {
            var tx = await CurrentDriverAsync();
            if (tx == null) return Loi("Tài khoản tài xế không hợp lệ.", 401);
            var order = await _db.DonHangs.Include(d => d.GiaoHang).Include(d => d.ThanhToan)
                .FirstOrDefaultAsync(d => d.MaDonHang == id && d.MaTaiXe == tx.MaTaiXe);
            if (order == null || order.GiaoHang == null) return Loi("Không tìm thấy đơn giao.", 404);

            var kq = await FoodServiceApp.Web.Services.GiaoHangXuLy.HoanThanhAsync(_db, order, order.GiaoHang, tx, request.MaXacNhan, request.ViDo, request.KinhDo);
            if (!kq.Ok) return Loi(kq.ThongDiep, 400);
            return Ok(new { trangThai = order.TrangThai, giaoLuc = order.GiaoHang.ThoiGianGiao });
        }

        [HttpPut("don-hang/{id:int}/giao-that-bai")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "TaiXe")]
        public async Task<IActionResult> DriverFailed(int id, MobileFailedDeliveryRequest request)
        {
            var tx = await CurrentDriverAsync();
            if (tx == null) return Loi("Tài khoản tài xế không hợp lệ.", 401);
            var order = await _db.DonHangs.Include(d => d.GiaoHang).Include(d => d.ThanhToan)
                .FirstOrDefaultAsync(d => d.MaDonHang == id && d.MaTaiXe == tx.MaTaiXe);
            if (order == null || order.GiaoHang == null) return Loi("Không tìm thấy đơn giao.", 404);

            var kq = await FoodServiceApp.Web.Services.GiaoHangXuLy.ThatBaiAsync(_db, order, order.GiaoHang, tx, request.LyDo, request.ViDo, request.KinhDo);
            if (!kq.Ok) return Loi(kq.ThongDiep, 400);
            return Ok(new { trangThai = order.TrangThai, tinhPhi = order.GiaoHang.TinhPhiThatBai, thongDiep = kq.ThongDiep });
        }

        [HttpPut("don-hang/{id:int}/phi-giao-hang")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "TaiXe")]
        public async Task<IActionResult> AdjustDeliveryFee(int id, MobileAdjustedFeeRequest request)
        {
            var tx = await CurrentDriverAsync();
            if (tx == null) return Loi("Tài khoản tài xế không hợp lệ.", 401);
            var order = await _db.DonHangs
                .Include(d => d.ThanhToan)
                .Include(d => d.ChiTiets)
                .FirstOrDefaultAsync(d => d.MaDonHang == id && d.MaTaiXe == tx.MaTaiXe);
            if (order == null) return Loi("Không tìm thấy đơn giao.", 404);
            if (request.KhoangCachKm <= 0 || request.PhiGiaoHang < 0 || request.PhiGiaoHang > PhiToiDa) return Loi("Khoảng cách hoặc phí giao hàng không hợp lệ.");
            if (order.ThanhToan?.HinhThuc == "VNPay" && request.PhiGiaoHang > order.PhiGiaoHang)
                return Loi("Đơn đã thanh toán VNPay, không thể tăng phí giao hàng sau khi thanh toán.");
            var oldFee = order.PhiGiaoHang;
            order.PhiGiaoHang = request.PhiGiaoHang;
            var foodTotal = order.ChiTiets.Count > 0 ? order.ChiTiets.Sum(c => c.DonGia * c.SoLuong) : 0m;
            var discount = Math.Max(0m, foodTotal + oldFee - order.TongTien);
            order.TongTien = Math.Max(0m, foodTotal - discount) + request.PhiGiaoHang;
            ActualDeliveryFee[id] = request.PhiGiaoHang;
            ActualDeliveryKm[id] = request.KhoangCachKm;
            AdjustedFeeOrders[id] = 1;
            await _db.SaveChangesAsync();
            return Ok(new { phiGiaoHang = order.PhiGiaoHang, khoangCachKm = request.KhoangCachKm, phiDaDieuChinh = true });
        }

        // ============================== Common order ==============================

        [HttpPut("don-hang/{id:int}/huy")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "KhachHang")]
        public async Task<IActionResult> CustomerCancel(int id)
        {
            var kh = await CurrentCustomerAsync();
            if (kh == null) return Loi("Tài khoản khách hàng không hợp lệ.", 401);
            var order = await _db.DonHangs.Include(d => d.ThanhToan).FirstOrDefaultAsync(d => d.MaDonHang == id && d.MaKH == kh.MaKH);
            if (order == null) return Loi("Không tìm thấy đơn hàng.", 404);
            if (order.TrangThai != "ChoXacNhan") return Loi("Đơn đã được gian hàng xác nhận, không thể tự hủy.");
            order.TrangThai = "DaHuy";
            if (order.ThanhToan?.TrangThai == "DaThanhToan") order.ThanhToan.TrangThai = "DaHoanTien";
            await _db.SaveChangesAsync();
            return Ok(new { trangThai = order.TrangThai, daHoanTien = order.ThanhToan?.TrangThai == "DaHoanTien" });
        }

        [HttpPut("doi-mat-khau")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> ChangePassword(MobileChangePasswordRequest request)
        {
            if (request.VaiTro != CurrentRole) return Loi("Vai trò không khớp phiên đăng nhập.", 403);
            var tk = await _db.TaiKhoans.FirstOrDefaultAsync(t => t.MaTK == CurrentMaTK && t.VaiTro == CurrentRole);
            if (tk == null) return Loi("Tài khoản không hợp lệ.", 401);
            if (!MatKhauHelper.Khop(request.MatKhauCu, tk.MatKhau)) return Loi("Mật khẩu hiện tại không đúng.");
            if (string.IsNullOrWhiteSpace(request.MatKhauMoi) || request.MatKhauMoi.Length < 6) return Loi("Mật khẩu mới tối thiểu 6 ký tự.");
            tk.MatKhau = MatKhauHelper.Hash(request.MatKhauMoi);
            await _db.SaveChangesAsync();
            return Ok(new { message = "Đã đổi mật khẩu." });
        }

        // ============================== Map/payment ==============================

        [HttpPost("map/phi-giao-hang")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> DeliveryFeePreview(MobileDeliveryPreviewRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.DiaChiLay) || string.IsNullOrWhiteSpace(request.DiaChiGiao)) return Loi("Thiếu địa chỉ lấy/giao.");
            var route = await CalculateDeliveryAsync(request.DiaChiLay, request.DiaChiGiao);
            return Ok(new { phiGiaoHang = route.Phi, khoangCachKm = route.Km, thoiGianDuKienPhut = route.Phut });
        }

        [HttpPost("payment/vnpay/tao-url")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "KhachHang")]
        public async Task<IActionResult> VnPayUrl(MobileVnPayRequest request)
        {
            var kh = await CurrentCustomerAsync();
            if (kh == null) return Loi("Tài khoản khách hàng không hợp lệ.", 401);
            var order = await _db.DonHangs.Include(d => d.ThanhToan).FirstOrDefaultAsync(d => d.MaDonHang == request.MaDonHang && d.MaKH == kh.MaKH);
            if (order == null) return Loi("Không tìm thấy đơn hàng.", 404);
            if (request.SoTien <= 0) return Loi("Số tiền không hợp lệ.");
            if (order.ThanhToan?.HinhThuc != "VNPay") return Loi("Đơn hàng không dùng VNPay.");
            var url = _vnpay.CreatePaymentUrl(new VnPayCreateRequest(
                order.MaDonHang,
                request.SoTien,
                request.NoiDung ?? $"Thanh toan don hang #{order.MaDonHang} - FoodServiceApp",
                HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1"));
            return Ok(new { paymentUrl = url });
        }

        // ============================== Local helpers ==============================

        public record MobileVendorRegisterRequest(string TenGianHang, string DiaChi, string Sdt, string Email, string MatKhau, string? MoTa, string? HinhAnh);
        public record MobilePlaceOrderItem(int MonAnId, int SoLuong);
        public record MobilePlaceOrderRequest(int GianHangId, string DiaChiGiao, string? GhiChu, string HinhThucThanhToan, string? MaKhuyenMai, List<MobilePlaceOrderItem> ChiTiet);
        public record MobileCustomerProfileRequest(string HoTen, string? Sdt, string? DiaChi);
        public record MobileIssueRequest(int? DonHangId, string TieuDe, string MoTa);
        public record MobileReviewRequest(int DonHangId, int SoSaoMonAn, int SoSaoGiaoHang, string? NoiDung);
        public record MobileVendorBusinessStatusRequest(bool DangMoCua);
        public record MobileRejectOrderRequest(string? LyDo);
        public record MobileDriverProfileRequest(string? HoTen, string? SoDienThoai, string? BienSoXe, string? LoaiXe);
        public record MobileDriverPositionRequest(int DonHangId, double ViDo, double KinhDo);
        public record MobileCompleteOrderRequest(string? MaXacNhan, double? ViDo = null, double? KinhDo = null);
        public record MobileFailedDeliveryRequest(string LyDo, bool TinhPhi = false, double? ViDo = null, double? KinhDo = null);   // TinhPhi do server quyết định theo lý do
        public record MobileAdjustedFeeRequest(double KhoangCachKm, decimal PhiGiaoHang);
        public record MobileChangePasswordRequest(string VaiTro, string MatKhauCu, string MatKhauMoi);
        public record MobileDeliveryPreviewRequest(string DiaChiLay, string DiaChiGiao);
        public record MobileVnPayRequest(int MaDonHang, long SoTien, string? NoiDung);
        private sealed record DriverPosition(double Lat, double Lng, DateTime UpdatedAt);
        private sealed record DeliveryRoute(double Km, int Phut, decimal Phi, bool NgoaiThanh);

        private async Task<KhachHang?> CurrentCustomerAsync() => await _db.KhachHangs.FirstOrDefaultAsync(k => k.MaTK == CurrentMaTK);
        private async Task<TaiXe?> CurrentDriverAsync() => await _db.TaiXes.FirstOrDefaultAsync(t => t.MaTK == CurrentMaTK);

        private static string NormalizePhone(string? phone) => new string((phone ?? "").Where(char.IsDigit).ToArray());

        private static object MapKhachHang(KhachHang kh, TaiKhoan tk) => new
        {
            id = kh.MaKH,
            hoTen = kh.HoTen,
            email = kh.Email ?? "",
            matKhau = "",
            soDienThoai = kh.SDT,
            diaChi = kh.DiaChiGiaoHang ?? "",
            ngayDangKy = tk.NgayTao,
            tongSoDon = 0,
            taiKhoanBiKhoa = !tk.TrangThai
        };

        private static object MapGianHang(GianHang gh, TaiKhoan tk) => new
        {
            id = gh.MaGianHang,
            tenGianHang = gh.TenCuaHang,
            diaChi = gh.DiaChi,
            hinhAnh = gh.HinhAnh ?? "🍽️",
            phanLoai = "",
            danhGia = 0d,
            thoiGianGiaoPhut = 30,
            soDienThoai = gh.SDT,
            email = tk.TenDangNhap.Contains("@") ? tk.TenDangNhap : $"{tk.TenDangNhap}@gianhang.vn",
            matKhau = "",
            moTa = gh.MoTa ?? "",
            dangMoCua = gh.TrangThaiKinhDoanh == "DangMo",
            gioMoCua = gh.GioMoCua.ToString(@"hh\:mm"),
            gioDongCua = gh.GioDongCua.ToString(@"hh\:mm"),
            dangNhanDon = gh.DangNhanDon,
            tyLeChietKhau = gh.TyLeChietKhau,
            taiKhoanBiKhoa = !tk.TrangThai,
            ngayDangKy = gh.NgayDangKy,
            trangThaiDangKy = gh.TrangThaiDuyet
        };

        private object MapIssue(PhanHoi p)
        {
            var trangThai = IssuesInProgress.ContainsKey(p.MaPhanHoi)
                ? "DangXuLy"
                : p.TrangThai == "DaXuLy" ? "DaGiaiQuyet" : "MoiPhatSinh";
            return new
            {
                id = p.MaPhanHoi,
                tieuDe = p.TieuDe,
                moTa = p.NoiDung,
                nguonGoc = p.KhachHang?.HoTen ?? "Khách hàng",
                ngayBaoCao = p.NgayGui,
                trangThai,
                donHangId = p.MaDonHang,
                traLoi = p.TraLoi
            };
        }

        /// <summary>Khôi phục vị trí từ CSDL khi bộ nhớ trống (server vừa khởi động lại).</summary>
        private async Task HydratePositionsAsync(List<int> ids)
        {
            var thieu = ids.Where(i => !DriverPositions.ContainsKey(i)).ToList();
            if (thieu.Count == 0) return;
            foreach (var v in await _db.TaiXeViTris.AsNoTracking().Where(x => thieu.Contains(x.MaDonHang)).ToListAsync())
                DriverPositions[v.MaDonHang] = new DriverPosition(v.ViDo, v.KinhDo, v.CapNhatLuc);
        }

        private object MapMobileOrder(DonHang d) => MapMobileOrder(d, false);

        /// <summary>choKhach = true chỉ khi người gọi là chính khách hàng của đơn: mã xác nhận giao hàng không được lộ cho tài xế/gian hàng.</summary>
        private object MapMobileOrder(DonHang d, bool choKhach)
        {
            var position = DriverPositions.TryGetValue(d.MaDonHang, out var p) ? p : null;
            var route = DeliveryRoutes.TryGetValue(d.MaDonHang, out var r) ? r : null;
            var adjustedKm = ActualDeliveryKm.TryGetValue(d.MaDonHang, out var akm) ? akm : 0d;
            var adjustedFee = ActualDeliveryFee.TryGetValue(d.MaDonHang, out var fee) ? fee : d.PhiGiaoHang;
            var hasAdjusted = AdjustedFeeOrders.ContainsKey(d.MaDonHang);
            var taiXe = d.GiaoHang?.TaiXe;

            return new
            {
                id = d.MaDonHang,
                gianHangId = d.MaGianHang,
                tenGianHang = d.GianHang?.TenCuaHang ?? "",
                chiTiet = d.ChiTiets.Select(c => new
                {
                    monAn = c.MonAn == null ? null : new
                    {
                        id = c.MonAn.MaMonAn,
                        tenMon = c.MonAn.TenMon,
                        gia = c.MonAn.Gia,
                        hinhAnh = c.MonAn.HinhAnh ?? "🍲",
                        danhMuc = c.MonAn.DanhMuc?.TenDanhMuc ?? "",
                        gianHangId = d.MaGianHang,
                        conHang = c.MonAn.TinhTrang == "ConHang"
                    },
                    soLuong = c.SoLuong,
                    thanhTien = c.ThanhTien
                }).ToList(),
                trangThai = ToMobileOrderStatus(d.TrangThai),
                diaChiGiao = d.DiaChiGiaoHang,
                ngayDat = d.NgayDat,
                khachTen = d.KhachHang?.HoTen ?? "",
                khachSdt = d.KhachHang?.SDT ?? "",
                khachEmail = d.KhachHang?.Email ?? "",
                diaChiLayHang = d.GianHang?.DiaChi ?? "",
                taiXeId = d.MaTaiXe?.ToString(),
                phiGiaoHang = hasAdjusted ? adjustedFee : d.PhiGiaoHang,
                hinhThucThanhToan = d.ThanhToan?.HinhThuc ?? "TienMat",
                maKhuyenMai = d.KhuyenMaiApDung?.MaCode ?? "",
                giamGia = Math.Max(0, d.ChiTiets.Sum(c => c.DonGia * c.SoLuong) - (d.TongTien - d.PhiGiaoHang)),
                khoangCachKm = adjustedKm > 0 ? adjustedKm : (route?.Km ?? 0d),
                thoiGianDuKienPhut = route?.Phut ?? 0,
                khuVucLay = "",
                khuVucGiao = "",
                ngoaiThanh = route?.NgoaiThanh ?? false,
                phiKhoangCach = hasAdjusted ? adjustedFee : d.PhiGiaoHang,
                phuPhiKhuVuc = route?.NgoaiThanh == true ? PhuPhiNgoaiThanh : 0m,
                phiDaDieuChinh = hasAdjusted,
                khoangCachThucTeKm = adjustedKm,
                taiXeTen = taiXe?.HoTen ?? "",
                taiXeSdt = taiXe?.SDT ?? "",
                taiXeBienSo = string.IsNullOrWhiteSpace(taiXe?.BienSo) ? "" : $"{taiXe!.PhuongTien} · {taiXe.BienSo}",
                daLayHang = d.GiaoHang?.TrangThai is "DangGiao" or "GiaoThanhCong" or "GiaoThatBai",
                layHangLuc = d.GiaoHang?.ThoiGianNhan,
                taiXeLat = position?.Lat,
                taiXeLng = position?.Lng,
                viTriCapNhatLuc = position?.UpdatedAt,
                viTriCu = position != null && (DateTime.Now - position.UpdatedAt).TotalSeconds > FoodServiceApp.Web.Services.GiaoHangXuLy.ViTriCuGiay,
                maXacNhanGiao = choKhach ? GetDeliveryCode(d) : null,
                soLanSaiMa = choKhach ? d.SoLanSaiMa : 0,
                bangChungGiao = d.GiaoHang?.AnhBangChung,
                giaoLuc = d.GiaoHang?.ThoiGianGiao,
                giaoThatBai = d.GiaoHang?.TrangThai == "GiaoThatBai",
                lyDoGiaoThatBai = d.GiaoHang?.TrangThai == "GiaoThatBai" ? ExtractFailedReason(d.GhiChu) : "",
                lyDoTuChoi = d.LyDoTuChoi ?? "",
                ghiChu = CleanUserNote(d.GhiChu)
            };
        }

        private static string ToMobileOrderStatus(string status) => status switch
        {
            "ChoXacNhan" => "ChoXacNhan",
            "DaXacNhan" => "DangLam",
            "DangGiao" => "DangGiao",
            "DaGiao" => "HoanThanh",
            "DaHuy" => "DaHuy",
            _ => status
        };

        private static string CleanUserNote(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return "";
            var marker = "[MA_GIAO:";
            var start = text.IndexOf(marker, StringComparison.Ordinal);
            if (start < 0) return text;
            var end = text.IndexOf(']', start);
            if (end < 0) return text;
            return text[(end + 1)..].Trim();
        }


        private static string? GetDeliveryCode(DonHang d)
        {
            if (!string.IsNullOrWhiteSpace(d.MaXacNhanGiao))
                return d.MaXacNhanGiao;

            if (string.IsNullOrEmpty(d.GhiChu))
                return null;

            const string prefix = "[MA_GIAO:";

            int marker = d.GhiChu.IndexOf(
                prefix,
                StringComparison.Ordinal);

            if (marker < 0)
                return null;

            int start = marker + prefix.Length;

            int end = d.GhiChu.IndexOf(']', start);

            if (end < 0)
                return null;

            return d.GhiChu.Substring(start, end - start);
        }

        private static string? ExtractDeliveryCode(string text)
        {
            var marker = text.IndexOf("[MA_GIAO:", StringComparison.Ordinal);
            if (marker < 0) return null;
            var start = marker + "[MA_GIAO:".Length;
            var end = text.IndexOf(']', start);
            return end > start ? text[start..end] : null;
        }

        private static string ExtractFailedReason(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return "";
            var marker = "Giao thất bại:";
            var i = text.LastIndexOf(marker, StringComparison.OrdinalIgnoreCase);
            return i >= 0 ? text[(i + marker.Length)..].Trim() : "";
        }

        private async Task<IActionResult> SetVendorApproval(int id, string state)
        {
            var store = await _db.GianHangs.FirstOrDefaultAsync(g => g.MaGianHang == id);
            if (store == null) return Loi("Không tìm thấy gian hàng.", 404);
            store.TrangThaiDuyet = state;
            if (state == "DaDuyet") store.TrangThaiKinhDoanh = "DangMo";
            if (state == "TuChoi") store.TrangThaiKinhDoanh = "DongCua";
            await _db.SaveChangesAsync();
            return Ok(new { trangThaiDangKy = store.TrangThaiDuyet });
        }

        private async Task<IActionResult> ToggleVendorLock(int id, bool active)
        {
            var store = await _db.GianHangs.FirstOrDefaultAsync(g => g.MaGianHang == id);
            if (store == null) return Loi("Không tìm thấy gian hàng.", 404);
            var tk = await _db.TaiKhoans.FirstOrDefaultAsync(t => t.MaTK == store.MaTK);
            if (tk == null) return Loi("Không tìm thấy tài khoản gian hàng.", 404);
            tk.TrangThai = active;
            if (!active) store.TrangThaiKinhDoanh = "DongCua";
            await _db.SaveChangesAsync();
            return Ok(new { taiKhoanBiKhoa = !tk.TrangThai });
        }

        private async Task<IActionResult> ToggleCustomerLock(int id, bool active)
        {
            var kh = await _db.KhachHangs.FirstOrDefaultAsync(k => k.MaKH == id);
            if (kh == null) return Loi("Không tìm thấy khách hàng.", 404);
            var tk = await _db.TaiKhoans.FirstOrDefaultAsync(t => t.MaTK == kh.MaTK);
            if (tk == null) return Loi("Không tìm thấy tài khoản.", 404);
            tk.TrangThai = active;
            await _db.SaveChangesAsync();
            return Ok(new { taiKhoanBiKhoa = !tk.TrangThai });
        }

        private async Task<DonHang?> GetOrderForVendorAsync(int id) =>
            await _db.DonHangs.FirstOrDefaultAsync(d => d.MaDonHang == id && d.MaGianHang == _db.GianHangs.Where(g => g.MaTK == CurrentMaTK).Select(g => g.MaGianHang).FirstOrDefault());

        private async Task<IActionResult> UpdateVendorOrderStatus(int id, string next, string? lyDo = null)
        {
            var order = await GetOrderForVendorAsync(id);
            if (order == null) return Loi("Không tìm thấy đơn hàng hoặc bạn không có quyền.", 404);
            var legal = next switch
            {
                "DaXacNhan" => order.TrangThai == "ChoXacNhan",
                "DaHuy" => order.TrangThai is "ChoXacNhan" or "DaXacNhan",
                _ => false
            };
            if (!legal) return Loi("Không thể chuyển trạng thái đơn hàng.");
            order.TrangThai = next;
            if (next == "DaHuy")
            {
                order.LyDoTuChoi = lyDo;
                var tt = await _db.ThanhToans.FirstOrDefaultAsync(t => t.MaDonHang == order.MaDonHang);
                if (tt?.TrangThai == "DaThanhToan") tt.TrangThai = "DaHoanTien";   // hoàn tiền khi quán từ chối
            }
            await _db.SaveChangesAsync();
            return Ok(new { trangThai = order.TrangThai });
        }

        private async Task<(decimal GiamGia, int? MaKm)> CalculatePromotionAsync(string? code, int storeId, decimal amount)
        {
            if (string.IsNullOrWhiteSpace(code)) return (0, null);
            var km = await _db.KhuyenMais.FirstOrDefaultAsync(k =>
                k.MaCode == code.Trim().ToUpper() && k.TrangThai &&
                (k.MaGianHang == null || k.MaGianHang == storeId) &&
                DateTime.Now >= k.NgayBatDau && DateTime.Now <= k.NgayKetThuc);
            if (km == null) return (0, null);
            var discount = km.PhanTramGiam != null
                ? Math.Round(amount * km.PhanTramGiam.Value / 100)
                : Math.Max(0, km.SoTienGiam ?? 0);
            return (Math.Min(discount, amount), km.MaKhuyenMai);
        }

        private async Task<DeliveryRoute> CalculateDeliveryAsync(string from, string to)
        {
            try
            {
                var origin = await _maps.GeocodeAsync(from);
                var destination = await _maps.GeocodeAsync(to);
                if (origin == null || destination == null) return new DeliveryRoute(0, 0, PhiCoBan, false);
                var result = await _maps.GetDistanceAsync(origin, destination);
                if (result == null) return new DeliveryRoute(0, 0, PhiCoBan, false);
                var km = result.DistanceKm;
                var phi = km <= KmMienPhi ? PhiCoBan : PhiCoBan + (decimal)Math.Ceiling(km - KmMienPhi) * PhiMoiKmThem;
                var ngoaiThanh = IsOutskirts(to);
                if (ngoaiThanh) phi += PhuPhiNgoaiThanh;
                phi = Math.Min(PhiToiDa, phi);
                return new DeliveryRoute(km, result.DurationMinutes, phi, ngoaiThanh);
            }
            catch
            {
                return new DeliveryRoute(0, 0, PhiCoBan, false);
            }
        }

        private static bool IsOutskirts(string address)
        {
            var s = RemoveDiacritics(address).ToLowerInvariant();
            return s.Contains("cu chi") || s.Contains("hoc mon") || s.Contains("binh chanh") || s.Contains("nha be") || s.Contains("can gio");
        }

        private static string RemoveDiacritics(string text)
        {
            var normalized = text.Normalize(System.Text.NormalizationForm.FormD);
            var sb = new StringBuilder(normalized.Length);
            foreach (var c in normalized)
                if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            return sb.ToString().Normalize(System.Text.NormalizationForm.FormC).Replace('đ', 'd').Replace('Đ', 'D');
        }
    }
}
