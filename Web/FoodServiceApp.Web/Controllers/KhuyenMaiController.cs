using FoodServiceApp.Web.Services;
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
            await using var transaction = await _db.Database.BeginTransactionAsync();
            var service = new PromotionManagementService(_db);
            var error = await service.ValidateAsync(vm, CurrentGianHangId);
            if (error != null) ModelState.AddModelError("", error);
            if (!ModelState.IsValid) return View(vm);
            var km = new KhuyenMai { MaGianHang = CurrentGianHangId };
            _db.KhuyenMais.Add(km);
            try
            {
                await service.SaveAsync(km, vm);
                await transaction.CommitAsync();
            }
            catch (DbUpdateException)
            {
                await transaction.RollbackAsync();
                _db.ChangeTracker.Clear();
                ModelState.AddModelError("", "Mã voucher đã tồn tại hoặc phạm vi dữ liệu đã thay đổi. Vui lòng kiểm tra lại.");
                return View(vm);
            }
            TempData["ThongBao"] = $"Đã tạo mã khuyến mãi {km.MaCode}.";
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Edit(int id)
        {
            var km = await _db.KhuyenMais.FirstOrDefaultAsync(k => k.MaKhuyenMai == id && k.MaGianHang == CurrentGianHangId);
            if (km == null) return NotFound();

            var vm = await new PromotionManagementService(_db).FormAsync(km);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, KhuyenMaiFormViewModel vm)
        {
            var km = await _db.KhuyenMais.FirstOrDefaultAsync(k => k.MaKhuyenMai == id && k.MaGianHang == CurrentGianHangId);
            if (km == null) return NotFound();

            await using var transaction = await _db.Database.BeginTransactionAsync();
            var service = new PromotionManagementService(_db);
            // Hold a database update lock before validating mutable usage limits.
            await _db.KhuyenMais.FromSqlInterpolated($"SELECT * FROM dbo.KhuyenMai WITH (UPDLOCK,HOLDLOCK) WHERE MaKhuyenMai = {id}").LoadAsync();
            await _db.Entry(km).ReloadAsync();
            var error = await service.ValidateAsync(vm, CurrentGianHangId, id);
            if (error != null) ModelState.AddModelError("", error);
            if (!ModelState.IsValid) return View(vm);
            try
            {
                await service.SaveAsync(km, vm);
                await transaction.CommitAsync();
            }
            catch (DbUpdateException)
            {
                await transaction.RollbackAsync();
                _db.ChangeTracker.Clear();
                ModelState.AddModelError("", "Mã voucher đã tồn tại hoặc phạm vi dữ liệu đã thay đổi. Vui lòng kiểm tra lại.");
                return View(vm);
            }
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
