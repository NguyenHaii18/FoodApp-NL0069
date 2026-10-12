using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FoodServiceApp.Web.Data;
using FoodServiceApp.Web.Models.Entities;
using FoodServiceApp.Web.Models.ViewModels;

namespace FoodServiceApp.Web.Controllers
{
    [Route("QuanTri/KhuyenMai")]
    [FoodServiceApp.Web.Filters.ChiSuperAdminGhi]
    public class AdminKhuyenMaiController : QuanTriBaseController
    {
        private readonly AppDbContext _db;

        public AdminKhuyenMaiController(AppDbContext db)
        {
            _db = db;
        }

        private static List<int> TachDs(string? s) =>
            (s ?? "").Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => int.TryParse(x, out var n) ? n : 0).Where(n => n > 0).Distinct().ToList();

        private static void Chep(KhuyenMai km, KhuyenMaiFormViewModel vm)
        {
            km.MoTa = vm.MoTa;
            km.TenChuongTrinh = string.IsNullOrWhiteSpace(vm.TenChuongTrinh) ? km.MaCode : vm.TenChuongTrinh.Trim();
            km.DieuKienApDung = vm.DieuKienApDung;
            km.NgayBatDau = vm.NgayBatDau;
            km.NgayKetThuc = vm.NgayKetThuc;
            km.TrangThai = vm.TrangThai;
            km.LoaiGiam = vm.LoaiGiam is "SoTien" or "GiamPhiVanChuyen" ? vm.LoaiGiam : "PhanTram";
            km.PhanTramGiam = km.LoaiGiam == "PhanTram" ? vm.GiaTriGiam : null;
            km.SoTienGiam = km.LoaiGiam != "PhanTram" ? vm.GiaTriGiam : null;
            km.GiamToiDa = km.LoaiGiam == "PhanTram" ? vm.GiamToiDa : null;
            km.GiaTriDonToiThieu = Math.Max(0, vm.GiaTriDonToiThieu);
            km.SoLuongToiDa = vm.SoLuongToiDa is > 0 ? vm.SoLuongToiDa : null;
            km.MoiKhachMotLan = vm.MoiKhachMotLan;
        }

        private async Task LuuPhamViAsync(int ma, KhuyenMaiFormViewModel vm)
        {
            _db.KhuyenMaiGianHangs.RemoveRange(_db.KhuyenMaiGianHangs.Where(x => x.MaKhuyenMai == ma));
            _db.KhuyenMaiDanhMucs.RemoveRange(_db.KhuyenMaiDanhMucs.Where(x => x.MaKhuyenMai == ma));
            _db.KhuyenMaiMonAns.RemoveRange(_db.KhuyenMaiMonAns.Where(x => x.MaKhuyenMai == ma));
            var gh = await _db.GianHangs.Where(g => TachDs(vm.DsGianHang).Contains(g.MaGianHang)).Select(g => g.MaGianHang).ToListAsync();
            var dm = await _db.DanhMucs.Where(d => TachDs(vm.DsDanhMuc).Contains(d.MaDanhMuc)).Select(d => d.MaDanhMuc).ToListAsync();
            var mon = await _db.MonAns.Where(m => TachDs(vm.DsMonAn).Contains(m.MaMonAn)).Select(m => m.MaMonAn).ToListAsync();
            foreach (var x in gh) _db.KhuyenMaiGianHangs.Add(new KhuyenMaiGianHang { MaKhuyenMai = ma, MaGianHang = x });
            foreach (var x in dm) _db.KhuyenMaiDanhMucs.Add(new KhuyenMaiDanhMuc { MaKhuyenMai = ma, MaDanhMuc = x });
            foreach (var x in mon) _db.KhuyenMaiMonAns.Add(new KhuyenMaiMonAn { MaKhuyenMai = ma, MaMonAn = x });
            await _db.SaveChangesAsync();
        }

        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var danhSach = await _db.KhuyenMais
                .Where(k => k.MaGianHang == null)
                .OrderByDescending(k => k.NgayBatDau)
                .ToListAsync();
            // Chi phí khuyến mãi = tổng tiền giảm của các đơn (không hủy) đã áp mã; số khách đã dùng
            var ids = danhSach.Select(k => k.MaKhuyenMai).ToList();
            ViewBag.ChiPhi = await _db.DonHangs
                .Where(d => d.MaKhuyenMaiApDung != null && ids.Contains(d.MaKhuyenMaiApDung.Value) && d.TrangThai != "DaHuy")
                .GroupBy(d => d.MaKhuyenMaiApDung!.Value)
                .Select(g => new { Ma = g.Key, Tien = g.Sum(x => x.TienGiam) })
                .ToDictionaryAsync(x => x.Ma, x => x.Tien);
            // Cảnh báo lạm dụng: khách dùng cùng một mã từ 3 lần trở lên
            ViewBag.LamDung = await _db.KhuyenMaiKhachDungs
                .GroupBy(x => new { x.MaKhuyenMai, x.MaKH }).Where(g => g.Count() >= 3)
                .Select(g => new { g.Key.MaKhuyenMai, g.Key.MaKH, SoLan = g.Count() }).ToListAsync();
            return View(danhSach);
        }

        [HttpGet("Create")]
        public IActionResult Create() => View(new KhuyenMaiFormViewModel());

        [HttpPost("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(KhuyenMaiFormViewModel vm)
        {
            if (vm.NgayKetThuc <= vm.NgayBatDau)
                ModelState.AddModelError(nameof(vm.NgayKetThuc), "Ngày kết thúc phải sau ngày bắt đầu.");

            if (await _db.KhuyenMais.AnyAsync(k => k.MaCode == vm.MaCode))
                ModelState.AddModelError(nameof(vm.MaCode), "Mã khuyến mãi này đã tồn tại.");

            if (!ModelState.IsValid) return View(vm);

            var km = new KhuyenMai { MaGianHang = null, MaCode = vm.MaCode.Trim().ToUpper() };
            Chep(km, vm);

            _db.KhuyenMais.Add(km);
            await _db.SaveChangesAsync();
            await LuuPhamViAsync(km.MaKhuyenMai, vm);
            TempData["ThongBao"] = $"Đã tạo mã khuyến mãi toàn hệ thống {km.MaCode}.";
            return RedirectToAction("Index");
        }

        [HttpGet("Edit/{id:int}")]
        public async Task<IActionResult> Edit(int id)
        {
            var km = await _db.KhuyenMais.FirstOrDefaultAsync(k => k.MaKhuyenMai == id && k.MaGianHang == null);
            if (km == null) return NotFound();

            var vm = new KhuyenMaiFormViewModel
            {
                MaKhuyenMai = km.MaKhuyenMai,
                MaCode = km.MaCode,
                MoTa = km.MoTa,
                DieuKienApDung = km.DieuKienApDung,
                LoaiGiam = km.PhanTramGiam != null ? "PhanTram" : "SoTien",
                GiaTriGiam = km.PhanTramGiam ?? km.SoTienGiam ?? 0,
                NgayBatDau = km.NgayBatDau,
                NgayKetThuc = km.NgayKetThuc,
                TrangThai = km.TrangThai,
                TenChuongTrinh = km.TenChuongTrinh, GiamToiDa = km.GiamToiDa, GiaTriDonToiThieu = km.GiaTriDonToiThieu,
                SoLuongToiDa = km.SoLuongToiDa, MoiKhachMotLan = km.MoiKhachMotLan,
                DsGianHang = string.Join(",", _db.KhuyenMaiGianHangs.Where(x => x.MaKhuyenMai == id).Select(x => x.MaGianHang)),
                DsDanhMuc = string.Join(",", _db.KhuyenMaiDanhMucs.Where(x => x.MaKhuyenMai == id).Select(x => x.MaDanhMuc)),
                DsMonAn = string.Join(",", _db.KhuyenMaiMonAns.Where(x => x.MaKhuyenMai == id).Select(x => x.MaMonAn)),
            };
            vm.LoaiGiam = km.LoaiGiam;
            vm.GiaTriGiam = km.PhanTramGiam ?? km.SoTienGiam ?? 0;
            return View(vm);
        }

        [HttpPost("Edit/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, KhuyenMaiFormViewModel vm)
        {
            var km = await _db.KhuyenMais.FirstOrDefaultAsync(k => k.MaKhuyenMai == id && k.MaGianHang == null);
            if (km == null) return NotFound();

            if (vm.NgayKetThuc <= vm.NgayBatDau)
                ModelState.AddModelError(nameof(vm.NgayKetThuc), "Ngày kết thúc phải sau ngày bắt đầu.");

            if (!ModelState.IsValid) return View(vm);

            Chep(km, vm);

            await _db.SaveChangesAsync();
            await LuuPhamViAsync(km.MaKhuyenMai, vm);
            TempData["ThongBao"] = $"Đã cập nhật mã {km.MaCode}.";
            return RedirectToAction("Index");
        }

        [HttpPost("ToggleTrangThai/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleTrangThai(int id)
        {
            var km = await _db.KhuyenMais.FirstOrDefaultAsync(k => k.MaKhuyenMai == id && k.MaGianHang == null);
            if (km == null) return NotFound();

            km.TrangThai = !km.TrangThai;
            await _db.SaveChangesAsync();
            return RedirectToAction("Index");
        }
    }
}
