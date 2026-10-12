using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FoodServiceApp.Web.Data;
using FoodServiceApp.Web.Models.Entities;
using FoodServiceApp.Web.Models.ViewModels;
using FoodServiceApp.Web.Services;

namespace FoodServiceApp.Web.Controllers
{
    [Route("MuaHang")]
    public class CustomerHomeController : Controller
    {
        private const string CookieLichSu = "ls_tk";
        private readonly AppDbContext _db;
        private readonly CartService _cart;

        public CustomerHomeController(AppDbContext db, CartService cart)
        {
            _db = db;
            _cart = cart;
        }

        private int? MaKhDangNhap => int.TryParse(User.FindFirst("MaKH")?.Value, out var id) ? id : null;

        private List<string> DocLichSu()
        {
            try
            {
                if (Request.Cookies.TryGetValue(CookieLichSu, out var raw) && !string.IsNullOrEmpty(raw))
                    return JsonSerializer.Deserialize<List<string>>(Uri.UnescapeDataString(raw)) ?? new();
            }
            catch { }
            return new();
        }

        private void GhiLichSu(List<string> ds) =>
            Response.Cookies.Append(CookieLichSu, Uri.EscapeDataString(JsonSerializer.Serialize(ds)),
                new CookieOptions { Expires = DateTimeOffset.Now.AddDays(90), HttpOnly = true, SameSite = SameSiteMode.Lax });

        /// <summary>Trang chủ: tìm kiếm + lọc (mở cửa, giá, sao, danh mục, khu vực) + sắp xếp + lịch sử/gợi ý.</summary>
        [HttpGet("")]
        [HttpGet("/")]
        public async Task<IActionResult> Index(string? tuKhoa, string? danhMuc, string? khuVuc, bool chiDangMo = true,
            decimal? giaTu = null, decimal? giaDen = null, int? saoTu = null, string? sapXep = null, bool xoaLichSu = false)
        {
            var lichSu = DocLichSu();
            if (xoaLichSu) { lichSu.Clear(); GhiLichSu(lichSu); }
            if (!string.IsNullOrWhiteSpace(tuKhoa))
            {
                var kw0 = tuKhoa.Trim();
                lichSu.RemoveAll(x => x.Equals(kw0, StringComparison.OrdinalIgnoreCase));
                lichSu.Insert(0, kw0.Length > 60 ? kw0[..60] : kw0);
                if (lichSu.Count > 8) lichSu = lichSu.Take(8).ToList();
                GhiLichSu(lichSu);
            }

            var vm = new CustomerHomeViewModel
            {
                TuKhoa = tuKhoa, DanhMuc = danhMuc, KhuVuc = khuVuc, ChiDangMo = chiDangMo,
                GiaTu = giaTu, GiaDen = giaDen, SaoTu = saoTu, SapXep = sapXep ?? "", LichSuTimKiem = lichSu,
            };

            // Chỉ gian hàng đã được duyệt (đang mở hoặc tạm đóng)
            var query = _db.GianHangs.Where(g => g.TrangThaiDuyet == "DaDuyet");
            if (!string.IsNullOrWhiteSpace(tuKhoa))
            {
                var kw = tuKhoa.Trim();
                query = query.Where(g => g.TenCuaHang.Contains(kw) ||
                    _db.DanhMucs.Any(d => d.MaGianHang == g.MaGianHang && (d.TenDanhMuc.Contains(kw) || d.MonAns.Any(m => m.TenMon.Contains(kw)))));
            }
            if (!string.IsNullOrWhiteSpace(danhMuc))
            {
                var dm = danhMuc.Trim();
                query = query.Where(g => _db.DanhMucs.Any(d => d.MaGianHang == g.MaGianHang && d.TenDanhMuc == dm));
            }
            if (!string.IsNullOrWhiteSpace(khuVuc))
            {
                var kv = khuVuc.Trim();
                query = query.Where(g => g.DiaChi.Contains(kv));
            }
            if (giaTu != null || giaDen != null)
            {
                var tu = giaTu ?? 0; var den = giaDen ?? decimal.MaxValue;
                query = query.Where(g => _db.DanhMucs.Any(d => d.MaGianHang == g.MaGianHang &&
                    d.MonAns.Any(m => m.TinhTrang == "ConHang" && m.Gia >= tu && m.Gia <= den)));
            }

            var gianHangs = (await query.ToListAsync());
            if (chiDangMo) gianHangs = gianHangs.Where(GianHangHelper.DangMo).ToList();
            var ids = gianHangs.Select(g => g.MaGianHang).ToList();

            // Đánh giá, số món đã bán, giá thấp nhất — gom 3 truy vấn thay vì N+1
            var dg = await _db.DanhGias.Where(d => !d.DaAn && ids.Contains(d.MaGianHang))
                .GroupBy(d => d.MaGianHang).Select(g => new { g.Key, Avg = g.Average(x => (double)x.SoSao), Cnt = g.Count() })
                .ToDictionaryAsync(x => x.Key);
            var daBan = await (from c in _db.ChiTietDonHangs
                               join d in _db.DonHangs on c.MaDonHang equals d.MaDonHang
                               where ids.Contains(d.MaGianHang) && d.TrangThai != "DaHuy"
                               group c by d.MaGianHang into g
                               select new { g.Key, Tong = g.Sum(x => x.SoLuong) }).ToDictionaryAsync(x => x.Key, x => x.Tong);
            var tuGia = await (from m in _db.MonAns
                               join d in _db.DanhMucs on m.MaDanhMuc equals d.MaDanhMuc
                               where ids.Contains(d.MaGianHang) && m.TinhTrang == "ConHang"
                               group m by d.MaGianHang into g
                               select new { g.Key, Min = g.Min(x => x.Gia) }).ToDictionaryAsync(x => x.Key, x => x.Min);
            var yeuThich = MaKhDangNhap is int maKh
                ? (await _db.YeuThichs.Where(y => y.MaKH == maKh && y.Loai == "GianHang").Select(y => y.MaThamChieu).ToListAsync()).ToHashSet()
                : new HashSet<int>();

            var cards = gianHangs.Select(g => new GianHangCardViewModel
            {
                MaGianHang = g.MaGianHang, TenCuaHang = g.TenCuaHang, DiaChi = g.DiaChi, HinhAnh = g.HinhAnh,
                TrangThaiKinhDoanh = g.TrangThaiKinhDoanh,
                DangMo = GianHangHelper.DangMo(g),
                DiemTrungBinh = dg.TryGetValue(g.MaGianHang, out var r) ? r.Avg : 0,
                SoDanhGia = dg.TryGetValue(g.MaGianHang, out var r2) ? r2.Cnt : 0,
                DaBan = daBan.GetValueOrDefault(g.MaGianHang),
                TuGia = tuGia.TryGetValue(g.MaGianHang, out var p) ? p : null,
                YeuThich = yeuThich.Contains(g.MaGianHang),
            }).ToList();

            if (saoTu != null) cards = cards.Where(c => c.SoDanhGia > 0 && c.DiemTrungBinh >= saoTu.Value).ToList();

            cards = (sapXep switch
            {
                "danhGia" => cards.OrderByDescending(c => c.DiemTrungBinh).ThenByDescending(c => c.SoDanhGia),
                "giaThap" => cards.OrderBy(c => c.TuGia ?? decimal.MaxValue),
                "giaCao" => cards.OrderByDescending(c => c.TuGia ?? 0),
                "banChay" => cards.OrderByDescending(c => c.DaBan),
                _ => cards.OrderByDescending(c => c.DangMo).ThenByDescending(c => c.DaBan),
            }).ToList();
            vm.DanhSachGianHang = cards;

            // Gợi ý phổ biến: món bán chạy + danh mục thường gặp
            vm.MonPhoBien = await (from c in _db.ChiTietDonHangs
                                   join m in _db.MonAns on c.MaMonAn equals m.MaMonAn
                                   group c by m.TenMon into g
                                   orderby g.Sum(x => x.SoLuong) descending
                                   select g.Key).Take(8).ToListAsync();
            vm.DanhMucPho = await _db.DanhMucs.GroupBy(d => d.TenDanhMuc).OrderByDescending(g => g.Count())
                .Select(g => g.Key).Take(10).ToListAsync();

            return View(vm);
        }

        [HttpGet("GianHang/{id:int}")]
        public async Task<IActionResult> ChiTiet(int id)
        {
            var gianHang = await _db.GianHangs.FirstOrDefaultAsync(g => g.MaGianHang == id && g.TrangThaiDuyet == "DaDuyet");
            if (gianHang == null) return NotFound();

            var danhMucs = await _db.DanhMucs.Include(d => d.MonAns).Where(d => d.MaGianHang == id).ToListAsync();
            var danhGias = await _db.DanhGias.Where(d => !d.DaAn && d.MaGianHang == id).OrderByDescending(d => d.NgayDanhGia).Take(10).ToListAsync();
            var maKhachDanhGia = danhGias.Select(x => x.MaKH).Distinct().ToList();
            var tenKhach = await _db.KhachHangs.Where(k => maKhachDanhGia.Contains(k.MaKH)).ToDictionaryAsync(k => k.MaKH, k => k.HoTen);
            var idMonCuaQuan = danhMucs.SelectMany(d => d.MonAns).Select(m => m.MaMonAn).ToList();

            // Món bán chạy của quán
            var banChay = await (from c in _db.ChiTietDonHangs
                                 join d in _db.DonHangs on c.MaDonHang equals d.MaDonHang
                                 where d.MaGianHang == id && d.TrangThai != "DaHuy"
                                 group c by c.MaMonAn into g
                                 orderby g.Sum(x => x.SoLuong) descending
                                 select new { g.Key, Tong = g.Sum(x => x.SoLuong) }).Take(4).ToListAsync();
            var idMonBanChay = banChay.Select(b => b.Key).ToList();
            var monCoTuyChon = await _db.TuyChonMons.Where(t => idMonCuaQuan.Contains(t.MaMonAn))
                .Select(t => t.MaMonAn).Distinct().ToListAsync();
            var now = DateTime.Now;
            var kmQuan = await _db.KhuyenMais.Where(k => k.MaGianHang == id && k.TrangThai && k.NgayBatDau <= now && k.NgayKetThuc >= now).ToListAsync();
            var kmHeThong = await _db.KhuyenMais.Where(k => k.MaGianHang == null && k.TrangThai && k.NgayBatDau <= now && k.NgayKetThuc >= now).Take(3).ToListAsync();

            ViewBag.GianHang = gianHang;
            ViewBag.DiemTrungBinh = await _db.DanhGias.Where(d => !d.DaAn && d.MaGianHang == id).Select(d => (double?)d.SoSao).AverageAsync() ?? 0;
            ViewBag.SoDanhGia = await _db.DanhGias.CountAsync(d => !d.DaAn && d.MaGianHang == id);
            ViewBag.DanhGiaGanDay = danhGias;
            ViewBag.TenKhach = tenKhach;
            ViewBag.GioHang = _cart.LayGioHang();
            ViewBag.DangMo = GianHangHelper.DangMo(gianHang);
            ViewBag.NhanDon = GianHangHelper.NhanDon(gianHang);
            ViewBag.GioKinhDoanh = GianHangHelper.GioKinhDoanh(gianHang);
            ViewBag.PhiGiao = GianHangHelper.PhiGiaoMoTa;
            ViewBag.MonBanChay = danhMucs.SelectMany(d => d.MonAns).Where(m => idMonBanChay.Contains(m.MaMonAn) && m.TinhTrang == "ConHang")
                .OrderBy(m => idMonBanChay.IndexOf(m.MaMonAn)).ToList();
            ViewBag.MonCoTuyChon = monCoTuyChon.ToHashSet();
            ViewBag.KhuyenMais = kmQuan.Concat(kmHeThong).ToList();
            ViewBag.YeuThich = MaKhDangNhap is int maKh && await _db.YeuThichs.AnyAsync(y => y.MaKH == maKh && y.Loai == "GianHang" && y.MaThamChieu == id);
            ViewBag.MonYeuThich = MaKhDangNhap is int mk
                ? (await _db.YeuThichs.Where(y => y.MaKH == mk && y.Loai == "MonAn").Select(y => y.MaThamChieu).ToListAsync()).ToHashSet()
                : new HashSet<int>();
            return View(danhMucs);
        }

        // Trang chi tiết một món: ảnh, mô tả, giá, tùy chọn (size/topping/mức cay), ghi chú, đánh giá riêng của món
        [HttpGet("Mon/{id:int}")]
        public async Task<IActionResult> ChiTietMon(int id)
        {
            var mon = await _db.MonAns.Include(m => m.DanhMuc).FirstOrDefaultAsync(m => m.MaMonAn == id);
            if (mon == null || mon.DanhMuc == null) return NotFound();

            var gianHang = await _db.GianHangs.FirstAsync(g => g.MaGianHang == mon.DanhMuc.MaGianHang);

            var soSao = await _db.DanhGiaMons.Where(d => d.MaMonAn == id).Select(d => d.SoSao).ToListAsync();
            var danhGias = await (from d in _db.DanhGiaMons
                                  join k in _db.KhachHangs on d.MaKH equals k.MaKH
                                  where d.MaMonAn == id
                                  orderby d.NgayDanhGia descending
                                  select new DanhGiaMonHienThi { TenKhach = k.HoTen, SoSao = d.SoSao, NoiDung = d.NoiDung, Ngay = d.NgayDanhGia }).Take(10).ToListAsync();

            return View(new MonChiTietViewModel
            {
                Mon = mon, GianHang = gianHang,
                DiemTrungBinh = soSao.Any() ? soSao.Average() : 0, SoDanhGia = soSao.Count, DanhGias = danhGias,
                TuyChons = await _db.TuyChonMons.Where(t => t.MaMonAn == id).OrderBy(t => t.Loai).ThenBy(t => t.GiaThem).ToListAsync(),
                YeuThich = MaKhDangNhap is int maKh && await _db.YeuThichs.AnyAsync(y => y.MaKH == maKh && y.Loai == "MonAn" && y.MaThamChieu == id),
            });
        }

        /// <summary>Thêm vào giỏ: server tự tính giá (giá món + giá thêm của các tùy chọn hợp lệ), không tin giá từ client.</summary>
        [HttpPost("GianHang/{maGianHang:int}/them-gio")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ThemVaoGio(int maGianHang, int maMonAn, int soLuong = 1, bool veTrangMon = false,
            int[]? tuyChon = null, string? ghiChu = null)
        {
            var gianHang = await _db.GianHangs.FirstOrDefaultAsync(g => g.MaGianHang == maGianHang && g.TrangThaiDuyet == "DaDuyet");
            var monAn = await _db.MonAns.Include(m => m.DanhMuc).FirstOrDefaultAsync(m => m.MaMonAn == maMonAn);
            if (gianHang == null || monAn == null || monAn.DanhMuc == null || monAn.DanhMuc.MaGianHang != maGianHang) return NotFound();

            IActionResult Quay() => veTrangMon ? RedirectToAction("ChiTietMon", new { id = maMonAn }) : RedirectToAction("ChiTiet", new { id = maGianHang });

            if (!GianHangHelper.NhanDon(gianHang))
            {
                TempData["LoiThongBao"] = "Gian hàng đang đóng cửa hoặc tạm dừng nhận đơn.";
                return Quay();
            }
            if (monAn.TinhTrang != "ConHang")
            {
                TempData["LoiThongBao"] = monAn.TinhTrang == "TamNgung" ? "Món này đã ngừng bán." : "Món này hiện đã hết hàng.";
                return Quay();
            }
            soLuong = Math.Clamp(soLuong, 1, 50);

            var chon = tuyChon == null ? new List<TuyChonMon>()
                : await _db.TuyChonMons.Where(t => t.MaMonAn == maMonAn && tuyChon.Contains(t.MaTuyChon)).ToListAsync();
            // Size / mức cay: chỉ chọn tối đa 1 mỗi loại
            if (chon.Count(t => t.Loai == "Size") > 1 || chon.Count(t => t.Loai == "MucCay") > 1)
            {
                TempData["LoiThongBao"] = "Chỉ được chọn một kích cỡ và một mức cay.";
                return Quay();
            }
            var ghi = string.IsNullOrWhiteSpace(ghiChu) ? null : ghiChu.Trim();
            if (ghi != null && (!monAn.ChoGhiChu || ghi.Length > 200))
            {
                TempData["LoiThongBao"] = monAn.ChoGhiChu ? "Ghi chú tối đa 200 ký tự." : "Món này không cho ghi chú riêng.";
                return Quay();
            }

            var loi = _cart.ThemMon(maGianHang, gianHang.TenCuaHang, new CartItem
            {
                MaMonAn = monAn.MaMonAn,
                TenMon = monAn.TenMon,
                Gia = monAn.Gia + chon.Sum(t => t.GiaThem),
                SoLuong = soLuong,
                HinhAnh = monAn.HinhAnh,
                GhiChu = ghi,
                TuyChonIds = chon.Select(t => t.MaTuyChon).ToList(),
                TuyChonText = chon.Count == 0 ? null : string.Join("; ", chon.Select(t => t.TenTuyChon)),
            });

            if (loi != null) TempData["LoiThongBao"] = loi;
            else TempData["ThongBao"] = $"Đã thêm \"{monAn.TenMon}\" vào giỏ hàng.";
            return Quay();
        }
    }
}
