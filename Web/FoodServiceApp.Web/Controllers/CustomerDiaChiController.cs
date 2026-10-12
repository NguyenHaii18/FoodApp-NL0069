using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FoodServiceApp.Web.Data;
using FoodServiceApp.Web.Models.Entities;
using FoodServiceApp.Web.Models.ViewModels;
using FoodServiceApp.Web.Services.Maps;

namespace FoodServiceApp.Web.Controllers
{
    // Sổ địa chỉ giao hàng của khách: thêm, xóa, đặt mặc định. Trang Checkout cho chọn nhanh từ sổ này.
    [Route("MuaHang/DiaChi")]
    public class CustomerDiaChiController : CustomerBaseController
    {
        private const int ToiDaDiaChi = 10;
        private readonly AppDbContext _db;
        private readonly IGoongMapsService _maps;
        private readonly IConfiguration _config;

        public CustomerDiaChiController(AppDbContext db, IGoongMapsService maps, IConfiguration config)
        {
            _db = db;
            _maps = maps;
            _config = config;
        }

        private static double Haversine(double lat1, double lng1, double lat2, double lng2)
        {
            const double R = 6371;
            double d(double x) => x * Math.PI / 180;
            var a = Math.Sin(d(lat2 - lat1) / 2) * Math.Sin(d(lat2 - lat1) / 2)
                  + Math.Cos(d(lat1)) * Math.Cos(d(lat2)) * Math.Sin(d(lng2 - lng1) / 2) * Math.Sin(d(lng2 - lng1) / 2);
            return 2 * R * Math.Asin(Math.Sqrt(a));
        }

        /// <summary>Ghép địa chỉ đầy đủ, geocode nếu chưa có tọa độ, rồi kiểm tra có quán nào trong bán kính phục vụ không.</summary>
        private async Task<string?> XuLyViTriAsync(DiaChiKhachHang dc)
        {
            var day = string.Join(", ", new[] { dc.DiaChi, dc.Phuong, dc.Quan, dc.Tinh }.Where(x => !string.IsNullOrWhiteSpace(x)));
            try
            {
                if (dc.Lat == null || dc.Lng == null)
                {
                    var g = await _maps.GeocodeAsync(day);
                    if (g != null) { dc.Lat = (decimal)g.Lat; dc.Lng = (decimal)g.Lng; }
                }
                if (dc.Lat == null || dc.Lng == null) return "Chưa xác định được vị trí trên bản đồ (có thể nhập tọa độ thủ công).";

                var banKinh = _config.GetValue<double?>("Giao:BanKinhKm") ?? 20;
                var quan = await _db.GianHangs.Where(g => g.TrangThaiDuyet == "DaDuyet").Select(g => g.DiaChi).Take(20).ToListAsync();
                double? gan = null;
                foreach (var dcQuan in quan)
                {
                    var p = await _maps.GeocodeAsync(dcQuan);
                    if (p == null) continue;
                    var km = Haversine((double)dc.Lat, (double)dc.Lng, p.Lat, p.Lng);
                    if (gan == null || km < gan) gan = km;
                }
                if (gan == null) return null;
                return gan <= banKinh ? null : $"Địa chỉ này cách quán gần nhất khoảng {gan:0.#} km, ngoài khu vực phục vụ ({banKinh:0.#} km).";
            }
            catch { return null; }
        }

        private static void Chep(DiaChiFormViewModel m, DiaChiKhachHang dc)
        {
            dc.Nhan = m.Nhan.Trim();
            dc.DiaChi = m.DiaChi.Trim();
            dc.Phuong = m.Phuong?.Trim(); dc.Quan = m.Quan?.Trim(); dc.Tinh = m.Tinh?.Trim();
            dc.HuongDan = m.HuongDan?.Trim();
            dc.Lat = m.Lat; dc.Lng = m.Lng;
        }

        [HttpGet("{id:int}/sua")]
        public async Task<IActionResult> Sua(int id)
        {
            var dc = await _db.DiaChiKhachHangs.FirstOrDefaultAsync(d => d.MaDiaChi == id && d.MaKH == CurrentMaKH);
            if (dc == null) return NotFound();
            ViewBag.DanhSach = await LayDanhSachAsync();
            ViewBag.MaSua = id;
            return View("Index", new DiaChiFormViewModel
            {
                Nhan = dc.Nhan, DiaChi = dc.DiaChi, Phuong = dc.Phuong, Quan = dc.Quan, Tinh = dc.Tinh,
                HuongDan = dc.HuongDan, Lat = dc.Lat, Lng = dc.Lng, MacDinh = dc.MacDinh,
            });
        }

        [HttpPost("{id:int}/sua")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Sua(int id, DiaChiFormViewModel model)
        {
            var dc = await _db.DiaChiKhachHangs.FirstOrDefaultAsync(d => d.MaDiaChi == id && d.MaKH == CurrentMaKH);
            if (dc == null) return NotFound();
            if (!ModelState.IsValid)
            {
                ViewBag.DanhSach = await LayDanhSachAsync(); ViewBag.MaSua = id;
                return View("Index", model);
            }
            Chep(model, dc);
            if (model.MacDinh && !dc.MacDinh)
            {
                foreach (var c in await _db.DiaChiKhachHangs.Where(d => d.MaKH == CurrentMaKH && d.MacDinh).ToListAsync()) c.MacDinh = false;
                dc.MacDinh = true;
            }
            var canhBao = await XuLyViTriAsync(dc);
            await _db.SaveChangesAsync();
            TempData["ThongBao"] = "Đã cập nhật địa chỉ.";
            if (canhBao != null) TempData["LoiThongBao"] = canhBao;
            return RedirectToAction("Index");
        }

        private async Task<List<DiaChiKhachHang>> LayDanhSachAsync() =>
            await _db.DiaChiKhachHangs.Where(d => d.MaKH == CurrentMaKH)
                .OrderByDescending(d => d.MacDinh).ThenBy(d => d.MaDiaChi).ToListAsync();

        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            ViewBag.DanhSach = await LayDanhSachAsync();
            return View(new DiaChiFormViewModel());
        }

        [HttpPost("them")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Them(DiaChiFormViewModel model)
        {
            var soHienCo = await _db.DiaChiKhachHangs.CountAsync(d => d.MaKH == CurrentMaKH);
            if (soHienCo >= ToiDaDiaChi)
                ModelState.AddModelError("", $"Mỗi tài khoản chỉ lưu tối đa {ToiDaDiaChi} địa chỉ.");

            if (!ModelState.IsValid)
            {
                ViewBag.DanhSach = await LayDanhSachAsync();
                return View("Index", model);
            }

            // Địa chỉ đầu tiên luôn là mặc định
            var laMacDinh = model.MacDinh || soHienCo == 0;
            if (laMacDinh)
            {
                var cu = await _db.DiaChiKhachHangs.Where(d => d.MaKH == CurrentMaKH && d.MacDinh).ToListAsync();
                foreach (var c in cu) c.MacDinh = false;
            }

            var moi = new DiaChiKhachHang { MaKH = CurrentMaKH, MacDinh = laMacDinh };
            Chep(model, moi);
            var canhBao = await XuLyViTriAsync(moi);
            _db.DiaChiKhachHangs.Add(moi);
            await _db.SaveChangesAsync();

            if (canhBao != null) TempData["LoiThongBao"] = canhBao;
            TempData["ThongBao"] = "Đã thêm địa chỉ giao hàng.";
            return RedirectToAction("Index");
        }

        [HttpPost("{id:int}/mac-dinh")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DatMacDinh(int id)
        {
            var ds = await _db.DiaChiKhachHangs.Where(d => d.MaKH == CurrentMaKH).ToListAsync();
            var chon = ds.FirstOrDefault(d => d.MaDiaChi == id);
            if (chon == null) return NotFound();

            foreach (var d in ds) d.MacDinh = d.MaDiaChi == id;
            await _db.SaveChangesAsync();

            TempData["ThongBao"] = "Đã đặt làm địa chỉ mặc định.";
            return RedirectToAction("Index");
        }

        [HttpPost("{id:int}/xoa")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Xoa(int id)
        {
            var dc = await _db.DiaChiKhachHangs.FirstOrDefaultAsync(d => d.MaDiaChi == id && d.MaKH == CurrentMaKH);
            if (dc == null) return NotFound();

            var laMacDinh = dc.MacDinh;
            _db.DiaChiKhachHangs.Remove(dc);
            await _db.SaveChangesAsync();

            // Nếu xóa địa chỉ mặc định thì chuyển mặc định sang địa chỉ còn lại đầu tiên
            if (laMacDinh)
            {
                var conLai = await _db.DiaChiKhachHangs.Where(d => d.MaKH == CurrentMaKH).OrderBy(d => d.MaDiaChi).FirstOrDefaultAsync();
                if (conLai != null) { conLai.MacDinh = true; await _db.SaveChangesAsync(); }
            }

            TempData["ThongBao"] = "Đã xóa địa chỉ.";
            return RedirectToAction("Index");
        }
    }
}
