using FoodServiceApp.Web.Services;
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
            await using var transaction = await _db.Database.BeginTransactionAsync();
            var service = new PromotionManagementService(_db);
            var error = await service.ValidateAsync(vm, null);
            if (error != null) ModelState.AddModelError("", error);
            if (!ModelState.IsValid) return View(vm);
            var km = new KhuyenMai { MaGianHang = null };
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
            TempData["ThongBao"] = $"Đã tạo mã khuyến mãi toàn hệ thống {km.MaCode}.";
            return RedirectToAction("Index");
        }

        [HttpGet("Edit/{id:int}")]
        public async Task<IActionResult> Edit(int id)
        {
            var km = await _db.KhuyenMais.FirstOrDefaultAsync(k => k.MaKhuyenMai == id && k.MaGianHang == null);
            if (km == null) return NotFound();

            var vm = await new PromotionManagementService(_db).FormAsync(km);
            return View(vm);
        }

        [HttpPost("Edit/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, KhuyenMaiFormViewModel vm)
        {
            var km = await _db.KhuyenMais.FirstOrDefaultAsync(k => k.MaKhuyenMai == id && k.MaGianHang == null);
            if (km == null) return NotFound();

            await using var transaction = await _db.Database.BeginTransactionAsync();
            var service = new PromotionManagementService(_db);
            // Hold a database update lock before validating mutable usage limits.
            await _db.KhuyenMais.FromSqlInterpolated($"SELECT * FROM dbo.KhuyenMai WITH (UPDLOCK,HOLDLOCK) WHERE MaKhuyenMai = {id}").LoadAsync();
            await _db.Entry(km).ReloadAsync();
            var error = await service.ValidateAsync(vm, null, id);
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
