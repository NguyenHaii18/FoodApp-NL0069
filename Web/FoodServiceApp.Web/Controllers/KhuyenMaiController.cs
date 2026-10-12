using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FoodServiceApp.Web.Data;
using FoodServiceApp.Web.Models.Entities;
using FoodServiceApp.Web.Models.ViewModels;

namespace FoodServiceApp.Web.Controllers
{
    public class KhuyenMaiController : GianHangBaseController
    {
        private readonly AppDbContext _db;

        public KhuyenMaiController(AppDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            // Chỉ hiển thị khuyến mãi do chính gian hàng này tạo.
            // Khuyến mãi toàn hệ thống (MaGianHang = null) do Quản trị quản lý, không sửa/xoá ở đây.
            var danhSach = await _db.KhuyenMais
                .Where(k => k.MaGianHang == CurrentGianHangId)
                .OrderByDescending(k => k.NgayBatDau)
                .ToListAsync();
            return View(danhSach);
        }

        public IActionResult Create() => View(new KhuyenMaiFormViewModel());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(KhuyenMaiFormViewModel vm)
        {
            if (vm.NgayKetThuc <= vm.NgayBatDau)
                ModelState.AddModelError(nameof(vm.NgayKetThuc), "Ngày kết thúc phải sau ngày bắt đầu.");

            if (await _db.KhuyenMais.AnyAsync(k => k.MaCode == vm.MaCode))
                ModelState.AddModelError(nameof(vm.MaCode), "Mã khuyến mãi này đã tồn tại.");

            if (!ModelState.IsValid) return View(vm);

            var km = new KhuyenMai
            {
                MaGianHang = CurrentGianHangId,
                MaCode = vm.MaCode.Trim().ToUpper(),
                MoTa = vm.MoTa,
                DieuKienApDung = vm.DieuKienApDung,
                NgayBatDau = vm.NgayBatDau,
                NgayKetThuc = vm.NgayKetThuc,
                TrangThai = vm.TrangThai,
                PhanTramGiam = vm.LoaiGiam == "PhanTram" ? vm.GiaTriGiam : null,
                SoTienGiam = vm.LoaiGiam == "SoTien" ? vm.GiaTriGiam : null,
            };

            _db.KhuyenMais.Add(km);
            await _db.SaveChangesAsync();
            TempData["ThongBao"] = $"Đã tạo mã khuyến mãi {km.MaCode}.";
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Edit(int id)
        {
            var km = await _db.KhuyenMais.FirstOrDefaultAsync(k => k.MaKhuyenMai == id && k.MaGianHang == CurrentGianHangId);
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
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, KhuyenMaiFormViewModel vm)
        {
            var km = await _db.KhuyenMais.FirstOrDefaultAsync(k => k.MaKhuyenMai == id && k.MaGianHang == CurrentGianHangId);
            if (km == null) return NotFound();

            if (vm.NgayKetThuc <= vm.NgayBatDau)
                ModelState.AddModelError(nameof(vm.NgayKetThuc), "Ngày kết thúc phải sau ngày bắt đầu.");

            if (!ModelState.IsValid) return View(vm);

            km.MoTa = vm.MoTa;
            km.DieuKienApDung = vm.DieuKienApDung;
            km.NgayBatDau = vm.NgayBatDau;
            km.NgayKetThuc = vm.NgayKetThuc;
            km.TrangThai = vm.TrangThai;
            km.PhanTramGiam = vm.LoaiGiam == "PhanTram" ? vm.GiaTriGiam : null;
            km.SoTienGiam = vm.LoaiGiam == "SoTien" ? vm.GiaTriGiam : null;

            await _db.SaveChangesAsync();
            TempData["ThongBao"] = $"Đã cập nhật mã {km.MaCode}.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleTrangThai(int id)
        {
            var km = await _db.KhuyenMais.FirstOrDefaultAsync(k => k.MaKhuyenMai == id && k.MaGianHang == CurrentGianHangId);
            if (km == null) return NotFound();

            km.TrangThai = !km.TrangThai;
            await _db.SaveChangesAsync();
            return RedirectToAction("Index");
        }
    }
}
