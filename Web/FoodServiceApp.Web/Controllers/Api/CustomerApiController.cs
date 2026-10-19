using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FoodServiceApp.Web.Data;
using FoodServiceApp.Web.Models.Dtos;
using FoodServiceApp.Web.Models.Entities;
using FoodServiceApp.Web.Services;
using FoodServiceApp.Web.Services.Maps;
using FoodServiceApp.Web.Services.Payment;

namespace FoodServiceApp.Web.Controllers.Api
{
    [ApiController]
    [Route("api/customer")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "KhachHang")]
    public class CustomerApiController : ApiBaseController
    {
        private const decimal PhiMoiKm = 5000m;
        private const decimal PhiGiaoMacDinh = 15000m;
        private const int ToiDaDiaChi = 10;

        private readonly AppDbContext _db;
        private readonly IGoongMapsService _mapsService;
        private readonly IVnPayService _vnPayService;

        public CustomerApiController(AppDbContext db, IGoongMapsService mapsService, IVnPayService vnPayService)
        {
            _db = db;
            _mapsService = mapsService;
            _vnPayService = vnPayService;
        }

        private async Task<KhachHang?> GetKhachHangAsync()
        {
            var maTK = CurrentMaTK;
            return await _db.KhachHangs.FirstOrDefaultAsync(k => k.MaTK == maTK);
        }

        private async Task<int?> GetMaKHAsync()
        {
            var kh = await GetKhachHangAsync();
            return kh?.MaKH;
        }

        private async Task<decimal> TinhPhiGiaoHangAsync(string diaChiGianHang, string diaChiGiao)
        {
            try
            {
                var diemGian = await _mapsService.GeocodeAsync(diaChiGianHang);
                var diemKhach = await _mapsService.GeocodeAsync(diaChiGiao);
                if (diemGian == null || diemKhach == null) return PhiGiaoMacDinh;

                var ketQua = await _mapsService.GetDistanceAsync(diemGian, diemKhach);
                if (ketQua == null) return PhiGiaoMacDinh;

                return Math.Ceiling((decimal)ketQua.DistanceKm) * PhiMoiKm;
            }
            catch
            {
                return PhiGiaoMacDinh;
            }
        }

        private static DonHangDto MapDonHang(DonHang d)
        {
            return new DonHangDto(
                d.MaDonHang,
                d.TrangThai,
                d.TongTien,
                d.NgayDat,
                d.DiaChiGiaoHang,
                d.GhiChu,
                d.GianHang?.TenCuaHang ?? "",
                d.GianHang?.DiaChi ?? "",
                d.ChiTiets.Select(c => new ChiTietDonHangDto(
                    c.MonAn?.TenMon ?? "Món ăn",
                    c.SoLuong,
                    c.DonGia,
                    c.ThanhTien)).ToList(),
                d.PhiGiaoHang);
        }

        [HttpGet("home")]
        public async Task<IActionResult> Home([FromQuery] string? tuKhoa)
        {
            var query = _db.GianHangs.Where(g =>
                g.TrangThaiKinhDoanh == "DangMo" && g.TrangThaiDuyet == "DaDuyet");

            if (!string.IsNullOrWhiteSpace(tuKhoa))
            {
                var kw = tuKhoa.Trim();
                query = query.Where(g => g.TenCuaHang.Contains(kw) ||
                    _db.DanhMucs.Any(d => d.MaGianHang == g.MaGianHang &&
                        d.MonAns.Any(m => m.TenMon.Contains(kw))));
            }

            var gianHangs = await query.OrderBy(g => g.MaGianHang).ToListAsync();
            var result = new List<GianHangCardDto>();

            foreach (var g in gianHangs)
            {
                var danhGias = await _db.DanhGias
                    .Where(d => !d.DaAn && d.MaGianHang == g.MaGianHang)
                    .Select(d => d.SoSao)
                    .ToListAsync();

                result.Add(new GianHangCardDto(
                    g.MaGianHang,
                    g.TenCuaHang,
                    g.DiaChi,
                    g.SDT,
                    g.MoTa,
                    g.HinhAnh,
                    g.TrangThaiKinhDoanh,
                    danhGias.Any() ? danhGias.Average() : 0,
                    danhGias.Count));
            }

            return Ok(new CustomerHomeDto(tuKhoa, result));
        }

        [HttpGet("stores/{id:int}")]
        public async Task<IActionResult> StoreDetail(int id)
        {
            var store = await _db.GianHangs.FirstOrDefaultAsync(g =>
                g.MaGianHang == id && g.TrangThaiDuyet == "DaDuyet");
            if (store == null) return NotFound(new { message = "Không tìm thấy gian hàng." });

            var categories = await _db.DanhMucs
                .Include(d => d.MonAns)
                .Where(d => d.MaGianHang == id)
                .OrderBy(d => d.MaDanhMuc)
                .Select(d => new DanhMucDto(
                    d.MaDanhMuc,
                    d.TenDanhMuc,
                    d.MonAns.Select(m => new MonAnDto(
                        m.MaMonAn,
                        m.TenMon,
                        m.Gia,
                        m.MoTa,
                        m.HinhAnh,
                        m.TinhTrang == "ConHang")).ToList()))
                .ToListAsync();

            var ratings = await _db.DanhGias
                .Where(d => d.MaGianHang == id)
                .OrderByDescending(d => d.NgayDanhGia)
                .Take(10)
                .Select(d => new StoreReviewDto(d.SoSao, d.NoiDung, d.NgayDanhGia))
                .ToListAsync();

            var score = await _db.DanhGias
                .Where(d => d.MaGianHang == id)
                .Select(d => (double?)d.SoSao)
                .AverageAsync() ?? 0;

            return Ok(new CustomerStoreDetailDto(
                store.MaGianHang,
                store.TenCuaHang,
                store.DiaChi,
                store.SDT,
                store.MoTa,
                store.HinhAnh,
                store.TrangThaiKinhDoanh,
                score,
                ratings.Count,
                categories,
                ratings));
        }

        [HttpGet("foods/{id:int}")]
        public async Task<IActionResult> FoodDetail(int id)
        {
            var mon = await _db.MonAns
                .Include(m => m.DanhMuc)
                .ThenInclude(d => d!.GianHang)
                .FirstOrDefaultAsync(m => m.MaMonAn == id);
            if (mon?.DanhMuc?.GianHang == null)
                return NotFound(new { message = "Không tìm thấy món ăn." });

            var ratings = await _db.DanhGiaMons
                .Where(d => d.MaMonAn == id)
                .OrderByDescending(d => d.NgayDanhGia)
                .Take(10)
                .Join(_db.KhachHangs,
                    d => d.MaKH,
                    k => k.MaKH,
                    (d, k) => new FoodReviewDto(k.HoTen, d.SoSao, d.NoiDung, d.NgayDanhGia))
                .ToListAsync();

            var score = await _db.DanhGiaMons
                .Where(d => d.MaMonAn == id)
                .Select(d => (double?)d.SoSao)
                .AverageAsync() ?? 0;

            return Ok(new CustomerFoodDetailDto(
                mon.MaMonAn,
                mon.TenMon,
                mon.Gia,
                mon.MoTa,
                mon.HinhAnh,
                mon.TinhTrang == "ConHang",
                mon.DanhMuc.MaGianHang,
                mon.DanhMuc.GianHang!.TenCuaHang,
                score,
                await _db.DanhGiaMons.CountAsync(d => d.MaMonAn == id),
                ratings));
        }

        [HttpGet("orders")]
        public async Task<IActionResult> Orders([FromQuery] string? loai = null)
        {
            var maKH = await GetMaKHAsync();
            if (maKH == null) return Unauthorized(new { message = "Tài khoản khách hàng không hợp lệ." });

            var query = _db.DonHangs
                .Include(d => d.GianHang)
                .Include(d => d.ChiTiets).ThenInclude(c => c.MonAn)
                .Where(d => d.MaKH == maKH.Value);

            if (string.Equals(loai, "dang-xu-ly", StringComparison.OrdinalIgnoreCase))
                query = query.Where(d => d.TrangThai != "DaGiao" && d.TrangThai != "DaHuy");
            else if (string.Equals(loai, "lich-su", StringComparison.OrdinalIgnoreCase))
                query = query.Where(d => d.TrangThai == "DaGiao" || d.TrangThai == "DaHuy");

            var orders = await query.OrderByDescending(d => d.NgayDat).ToListAsync();
            return Ok(orders.Select(MapDonHang));
        }

        [HttpGet("orders/{id:int}")]
        public async Task<IActionResult> OrderDetail(int id)
        {
            var maKH = await GetMaKHAsync();
            if (maKH == null) return Unauthorized();

            var don = await _db.DonHangs
                .Include(d => d.GianHang)
                .Include(d => d.ChiTiets).ThenInclude(c => c.MonAn)
                .Include(d => d.ThanhToan)
                .Include(d => d.GiaoHang).ThenInclude(g => g!.TaiXe)
                .FirstOrDefaultAsync(d => d.MaDonHang == id && d.MaKH == maKH.Value);

            if (don == null) return NotFound(new { message = "Không tìm thấy đơn hàng." });

            return Ok(new
            {
                donHang = MapDonHang(don),
                thanhToan = don.ThanhToan == null ? null : new
                {
                    don.ThanhToan.HinhThuc,
                    don.ThanhToan.TrangThai,
                    don.ThanhToan.MaGiaoDich,
                    don.ThanhToan.NgayThanhToan
                },
                giaoHang = don.GiaoHang == null ? null : new
                {
                    don.GiaoHang.MaTaiXe,
                    don.GiaoHang.TrangThai,
                    don.GiaoHang.ThoiGianNhan,
                    don.GiaoHang.ThoiGianGiao,
                    hoTenTaiXe = don.GiaoHang.TaiXe?.HoTen,
                    sdtTaiXe = don.GiaoHang.TaiXe?.SDT,
                    phuongTien = don.GiaoHang.TaiXe?.PhuongTien,
                    bienSo = don.GiaoHang.TaiXe?.BienSo
                }
            });
        }

        [HttpPost("orders/{id:int}/cancel")]
        public async Task<IActionResult> CancelOrder(int id)
        {
            var maKH = await GetMaKHAsync();
            if (maKH == null) return Unauthorized();

            var don = await _db.DonHangs
                .Include(d => d.ThanhToan)
                .FirstOrDefaultAsync(d => d.MaDonHang == id && d.MaKH == maKH.Value);
            if (don == null) return NotFound(new { message = "Không tìm thấy đơn hàng." });

            if (don.TrangThai != "ChoXacNhan")
                return BadRequest(new { message = "Đơn đã được gian hàng xác nhận, không thể tự huỷ." });

            don.TrangThai = "DaHuy";
            if (don.ThanhToan?.TrangThai == "DaThanhToan")
                don.ThanhToan.TrangThai = "DaHoanTien";

            await _db.SaveChangesAsync();
            return Ok(new { maDonHang = don.MaDonHang, don.TrangThai });
        }

        [HttpPost("orders")]
        public async Task<IActionResult> PlaceOrder(PlaceOrderRequest request)
        {
            var maKH = await GetMaKHAsync();
            if (maKH == null) return Unauthorized(new { message = "Tài khoản khách hàng không hợp lệ." });

            if (string.IsNullOrWhiteSpace(request.DiaChiGiaoHang))
                return BadRequest(new { message = "Vui lòng nhập địa chỉ giao hàng." });

            if (string.Equals(request.HinhThucThanhToan, "Momo", StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { message = "Ví Momo hiện chưa được hỗ trợ." });

            var allowedPayments = new[] { "TienMat", "VNPay" };
            if (!allowedPayments.Contains(request.HinhThucThanhToan))
                return BadRequest(new { message = "Hình thức thanh toán không hợp lệ." });

            if (request.Items == null || request.Items.Count == 0)
                return BadRequest(new { message = "Giỏ hàng đang trống." });

            if (request.Items.Any(x => x.SoLuong <= 0))
                return BadRequest(new { message = "Số lượng món phải lớn hơn 0." });

            var store = await _db.GianHangs.FirstOrDefaultAsync(g =>
                g.MaGianHang == request.MaGianHang && g.TrangThaiDuyet == "DaDuyet");
            if (store == null) return NotFound(new { message = "Không tìm thấy gian hàng." });

            var monIds = request.Items.Select(x => x.MaMonAn).Distinct().ToList();
            var mons = await _db.MonAns
                .Include(m => m.DanhMuc)
                .Where(m => monIds.Contains(m.MaMonAn))
                .ToListAsync();

            if (mons.Count != monIds.Count || mons.Any(m => m.DanhMuc == null || m.DanhMuc.MaGianHang != request.MaGianHang))
                return BadRequest(new { message = "Có món ăn không thuộc gian hàng đã chọn." });

            if (mons.Any(m => m.TinhTrang != "ConHang"))
                return BadRequest(new { message = "Có món ăn hiện đã hết hàng." });

            decimal tongTienMon = 0;
            foreach (var item in request.Items)
            {
                var mon = mons.First(m => m.MaMonAn == item.MaMonAn);
                tongTienMon += mon.Gia * item.SoLuong;
            }

            decimal soTienGiam = 0;
            int? maKhuyenMaiApDung = null;
            if (!string.IsNullOrWhiteSpace(request.MaKhuyenMai))
            {
                var km = await _db.KhuyenMais.FirstOrDefaultAsync(k =>
                    k.MaCode == request.MaKhuyenMai.Trim().ToUpper() &&
                    k.TrangThai &&
                    (k.MaGianHang == null || k.MaGianHang == request.MaGianHang) &&
                    DateTime.Now >= k.NgayBatDau && DateTime.Now <= k.NgayKetThuc);

                if (km != null)
                {
                    soTienGiam = km.PhanTramGiam != null
                        ? Math.Round(tongTienMon * km.PhanTramGiam.Value / 100)
                        : Math.Max(0, km.SoTienGiam ?? 0);
                    soTienGiam = Math.Min(soTienGiam, tongTienMon);
                    maKhuyenMaiApDung = km.MaKhuyenMai;
                }
            }

            var phiGiaoHang = await TinhPhiGiaoHangAsync(store.DiaChi, request.DiaChiGiaoHang);
            var tongTien = Math.Max(0, tongTienMon - soTienGiam) + phiGiaoHang;

            var donHang = new DonHang
            {
                MaKH = maKH.Value,
                MaGianHang = request.MaGianHang,
                TrangThai = "ChoXacNhan",
                TongTien = tongTien,
                PhiGiaoHang = phiGiaoHang,
                DiaChiGiaoHang = request.DiaChiGiaoHang.Trim(),
                GhiChu = string.IsNullOrWhiteSpace(request.GhiChu) ? null : request.GhiChu.Trim(),
                MaKhuyenMaiApDung = maKhuyenMaiApDung,
                MaXacNhanGiao = FoodServiceApp.Web.Services.GiaoHangXuLy.TaoMa(),
            };
            _db.DonHangs.Add(donHang);
            await _db.SaveChangesAsync();

            foreach (var item in request.Items)
            {
                var mon = mons.First(m => m.MaMonAn == item.MaMonAn);
                _db.ChiTietDonHangs.Add(new ChiTietDonHang
                {
                    MaDonHang = donHang.MaDonHang,
                    MaMonAn = mon.MaMonAn,
                    SoLuong = item.SoLuong,
                    DonGia = mon.Gia,
                });
            }

            _db.ThanhToans.Add(new ThanhToan
            {
                MaDonHang = donHang.MaDonHang,
                HinhThuc = request.HinhThucThanhToan,
                TrangThai = "ChoThanhToan",
            });

            await _db.SaveChangesAsync();

            string? paymentUrl = null;
            if (request.HinhThucThanhToan == "VNPay")
            {
                paymentUrl = _vnPayService.CreatePaymentUrl(new VnPayCreateRequest(
                    OrderId: donHang.MaDonHang,
                    AmountVnd: (long)tongTien,
                    OrderInfo: $"Thanh toan don hang #{donHang.MaDonHang} - FoodServiceApp",
                    IpAddress: HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1",
                    ReturnUrl: Url.Action("VnPayReturn", "Payment", null, Request.Scheme)!));
            }

            return Ok(new
            {
                maDonHang = donHang.MaDonHang,
                tongTienMon,
                soTienGiam,
                phiGiaoHang,
                tongTien,
                trangThai = donHang.TrangThai,
                paymentUrl
            });
        }

        [HttpGet("addresses")]
        public async Task<IActionResult> Addresses()
        {
            var maKH = await GetMaKHAsync();
            if (maKH == null) return Unauthorized();

            var result = await _db.DiaChiKhachHangs
                .Where(d => d.MaKH == maKH.Value)
                .OrderByDescending(d => d.MacDinh)
                .ThenBy(d => d.MaDiaChi)
                .Select(d => new DiaChiDto(d.MaDiaChi, d.Nhan, d.DiaChi, d.MacDinh))
                .ToListAsync();

            return Ok(result);
        }

        [HttpPost("addresses")]
        public async Task<IActionResult> AddAddress(DiaChiRequest request)
        {
            var maKH = await GetMaKHAsync();
            if (maKH == null) return Unauthorized();

            if (string.IsNullOrWhiteSpace(request.Nhan) || string.IsNullOrWhiteSpace(request.DiaChi))
                return BadRequest(new { message = "Nhãn và địa chỉ không được để trống." });

            var count = await _db.DiaChiKhachHangs.CountAsync(d => d.MaKH == maKH.Value);
            if (count >= ToiDaDiaChi)
                return BadRequest(new { message = $"Mỗi tài khoản chỉ lưu tối đa {ToiDaDiaChi} địa chỉ." });

            var macDinh = request.MacDinh || count == 0;
            if (macDinh)
            {
                var existing = await _db.DiaChiKhachHangs.Where(d => d.MaKH == maKH.Value && d.MacDinh).ToListAsync();
                foreach (var item in existing) item.MacDinh = false;
            }

            var address = new DiaChiKhachHang
            {
                MaKH = maKH.Value,
                Nhan = request.Nhan.Trim(),
                DiaChi = request.DiaChi.Trim(),
                MacDinh = macDinh,
            };
            _db.DiaChiKhachHangs.Add(address);
            await _db.SaveChangesAsync();

            return Ok(new DiaChiDto(address.MaDiaChi, address.Nhan, address.DiaChi, address.MacDinh));
        }

        [HttpPost("addresses/{id:int}/default")]
        public async Task<IActionResult> SetDefaultAddress(int id)
        {
            var maKH = await GetMaKHAsync();
            if (maKH == null) return Unauthorized();

            var ds = await _db.DiaChiKhachHangs.Where(d => d.MaKH == maKH.Value).ToListAsync();
            var selected = ds.FirstOrDefault(d => d.MaDiaChi == id);
            if (selected == null) return NotFound(new { message = "Địa chỉ không tồn tại." });

            foreach (var item in ds) item.MacDinh = item.MaDiaChi == id;
            await _db.SaveChangesAsync();

            return Ok(new { maDiaChi = id, macDinh = true });
        }

        [HttpDelete("addresses/{id:int}")]
        public async Task<IActionResult> DeleteAddress(int id)
        {
            var maKH = await GetMaKHAsync();
            if (maKH == null) return Unauthorized();

            var address = await _db.DiaChiKhachHangs.FirstOrDefaultAsync(d =>
                d.MaDiaChi == id && d.MaKH == maKH.Value);
            if (address == null) return NotFound(new { message = "Địa chỉ không tồn tại." });

            var wasDefault = address.MacDinh;
            _db.DiaChiKhachHangs.Remove(address);
            await _db.SaveChangesAsync();

            if (wasDefault)
            {
                var next = await _db.DiaChiKhachHangs
                    .Where(d => d.MaKH == maKH.Value)
                    .OrderBy(d => d.MaDiaChi)
                    .FirstOrDefaultAsync();
                if (next != null)
                {
                    next.MacDinh = true;
                    await _db.SaveChangesAsync();
                }
            }

            return Ok(new { message = "Đã xóa địa chỉ." });
        }

        [HttpGet("profile")]
        public async Task<IActionResult> Profile()
        {
            var kh = await GetKhachHangAsync();
            if (kh == null) return Unauthorized();

            var taiKhoan = await _db.TaiKhoans.FirstOrDefaultAsync(t => t.MaTK == kh.MaTK);
            return Ok(new CustomerProfileDto(
                kh.MaKH,
                kh.MaTK,
                taiKhoan?.TenDangNhap ?? "",
                kh.HoTen,
                kh.SDT,
                kh.Email,
                kh.DiaChiGiaoHang));
        }

        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile(UpdateCustomerProfileRequest request)
        {
            var kh = await GetKhachHangAsync();
            if (kh == null) return Unauthorized();

            if (string.IsNullOrWhiteSpace(request.HoTen) || string.IsNullOrWhiteSpace(request.SDT))
                return BadRequest(new { message = "Họ tên và số điện thoại không được để trống." });

            kh.HoTen = request.HoTen.Trim();
            kh.SDT = request.SDT.Trim();
            kh.Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim();
            kh.DiaChiGiaoHang = string.IsNullOrWhiteSpace(request.DiaChiGiaoHang) ? null : request.DiaChiGiaoHang.Trim();

            if (!string.IsNullOrWhiteSpace(request.MatKhauMoi))
            {
                var taiKhoan = await _db.TaiKhoans.FirstAsync(t => t.MaTK == kh.MaTK);
                if (!string.IsNullOrWhiteSpace(request.MatKhauHienTai) &&
                    !MatKhauHelper.Khop(request.MatKhauHienTai, taiKhoan.MatKhau))
                    return BadRequest(new { message = "Mật khẩu hiện tại không đúng." });

                if (string.IsNullOrWhiteSpace(request.MatKhauHienTai))
                    return BadRequest(new { message = "Phải nhập mật khẩu hiện tại khi đổi mật khẩu." });

                if (request.MatKhauMoi.Length < 6)
                    return BadRequest(new { message = "Mật khẩu mới phải có ít nhất 6 ký tự." });

                taiKhoan.MatKhau = MatKhauHelper.Hash(request.MatKhauMoi);
            }

            await _db.SaveChangesAsync();
            return Ok(new { message = "Đã cập nhật hồ sơ." });
        }

        [HttpPost("orders/{id:int}/review")]
        public async Task<IActionResult> ReviewOrder(int id, ReviewOrderRequest request)
        {
            var maKH = await GetMaKHAsync();
            if (maKH == null) return Unauthorized();

            if (request.SoSao < 1 || request.SoSao > 5)
                return BadRequest(new { message = "Số sao phải từ 1 đến 5." });

            var don = await _db.DonHangs
                .Include(d => d.ChiTiets)
                .FirstOrDefaultAsync(d => d.MaDonHang == id && d.MaKH == maKH.Value && d.TrangThai == "DaGiao");
            if (don == null) return NotFound(new { message = "Chỉ được đánh giá đơn đã giao thành công." });

            if (await _db.DanhGias.AnyAsync(d => d.MaDonHang == id && d.MaKH == maKH.Value))
                return BadRequest(new { message = "Đơn này đã được đánh giá rồi." });

            var monTrongDon = don.ChiTiets.Select(c => c.MaMonAn).ToHashSet();
            foreach (var item in request.MonAn ?? new List<ReviewFoodItemRequest>())
            {
                if (item.SoSao < 1 || item.SoSao > 5 || !monTrongDon.Contains(item.MaMonAn))
                    continue;

                _db.DanhGiaMons.Add(new DanhGiaMon
                {
                    MaKH = maKH.Value,
                    MaMonAn = item.MaMonAn,
                    MaDonHang = id,
                    SoSao = item.SoSao,
                    NoiDung = item.NoiDung,
                });
            }

            _db.DanhGias.Add(new DanhGia
            {
                MaKH = maKH.Value,
                MaGianHang = don.MaGianHang,
                MaDonHang = id,
                SoSao = request.SoSao,
                NoiDung = request.NoiDung,
            });

            await _db.SaveChangesAsync();
            return Ok(new { message = "Cảm ơn bạn đã đánh giá!" });
        }
    }
}
