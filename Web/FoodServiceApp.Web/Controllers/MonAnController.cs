using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FoodServiceApp.Web.Data;
using FoodServiceApp.Web.Models.Entities;
using FoodServiceApp.Web.Models.ViewModels;
using FoodServiceApp.Web.Services;

namespace FoodServiceApp.Web.Controllers
{
    public class MonAnController : GianHangBaseController
    {
        private readonly AppDbContext _db;
        private readonly IFileStorageService _storage;

        public MonAnController(AppDbContext db, IFileStorageService storage)
        {
            _db = db;
            _storage = storage;
        }

        public async Task<IActionResult> Index()
        {
            var danhSach = await _db.MonAns
                .Include(m => m.DanhMuc)
                .Where(m => m.DanhMuc!.MaGianHang == CurrentGianHangId)
                .OrderBy(m => m.DanhMuc!.TenDanhMuc).ThenBy(m => m.TenMon)
                .ToListAsync();

            return View(danhSach);
        }

        public async Task<IActionResult> Create()
        {
            var vm = new MonAnFormViewModel
            {
                DanhSachDanhMuc = await _db.DanhMucs.Where(d => d.MaGianHang == CurrentGianHangId).ToListAsync()
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MonAnFormViewModel vm)
        {
            vm.TenMon = vm.TenMon?.Trim() ?? "";
            if (!ModelState.IsValid)
            {
                vm.DanhSachDanhMuc = await _db.DanhMucs.Where(d => d.MaGianHang == CurrentGianHangId).ToListAsync();
                return View(vm);
            }
            if (vm.MaDanhMuc == 0 && !string.IsNullOrWhiteSpace(vm.TenDanhMucMoi))
            {
                var tenDanhMuc = vm.TenDanhMucMoi.Trim();
                if (tenDanhMuc.Length > 100)
                {
                    ModelState.AddModelError(nameof(vm.TenDanhMucMoi), "Tên danh mục tối đa 100 ký tự.");
                    vm.DanhSachDanhMuc = await _db.DanhMucs.Where(d => d.MaGianHang == CurrentGianHangId).ToListAsync();
                    return View(vm);
                }
                var danhMuc = await _db.DanhMucs.FirstOrDefaultAsync(d =>
                    d.MaGianHang == CurrentGianHangId && d.TenDanhMuc.ToLower() == tenDanhMuc.ToLower());
                if (danhMuc == null)
                {
                    danhMuc = new DanhMuc { MaGianHang = CurrentGianHangId, TenDanhMuc = tenDanhMuc };
                    _db.DanhMucs.Add(danhMuc);
                    await _db.SaveChangesAsync();
                }
                vm.MaDanhMuc = danhMuc.MaDanhMuc;
            }

            if (vm.MaDanhMuc == 0)
                ModelState.AddModelError(nameof(vm.MaDanhMuc), "Vui lòng chọn hoặc nhập danh mục.");

            var danhMucHopLe = vm.MaDanhMuc != 0 && await _db.DanhMucs
                .AnyAsync(d => d.MaDanhMuc == vm.MaDanhMuc && d.MaGianHang == CurrentGianHangId);
            if (vm.MaDanhMuc != 0 && !danhMucHopLe)
                ModelState.AddModelError(nameof(vm.MaDanhMuc), "Danh mục không thuộc gian hàng của bạn.");

            if (danhMucHopLe && await _db.MonAns.AnyAsync(m => m.MaDanhMuc == vm.MaDanhMuc && m.TenMon.ToLower() == vm.TenMon.ToLower()))
                ModelState.AddModelError(nameof(vm.TenMon), "Tên món đã tồn tại trong danh mục này.");

            if (!ModelState.IsValid)
            {
                vm.DanhSachDanhMuc = await _db.DanhMucs.Where(d => d.MaGianHang == CurrentGianHangId).ToListAsync();
                return View(vm);
            }

            var monAn = new MonAn
            {
                MaDanhMuc = vm.MaDanhMuc,
                TenMon = vm.TenMon,
                Gia = vm.Gia,
                GiaVon = vm.GiaVon,
                MoTa = vm.MoTa,
                TinhTrang = vm.TinhTrang,
                ChoGhiChu = vm.ChoGhiChu,
            };

            if (vm.AnhMoi != null)
                monAn.HinhAnh = await _storage.SaveFileAsync(vm.AnhMoi, "monan");

            _db.MonAns.Add(monAn);
            await _db.SaveChangesAsync();

            TempData["ThongBao"] = $"Đã thêm món \"{monAn.TenMon}\".";
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Edit(int id)
        {
            var monAn = await _db.MonAns.Include(m => m.DanhMuc)
                .FirstOrDefaultAsync(m => m.MaMonAn == id && m.DanhMuc!.MaGianHang == CurrentGianHangId);
            if (monAn == null) return NotFound();

            var vm = new MonAnFormViewModel
            {
                MaMonAn = monAn.MaMonAn,
                MaDanhMuc = monAn.MaDanhMuc,
                TenMon = monAn.TenMon,
                Gia = monAn.Gia,
                MoTa = monAn.MoTa,
                HinhAnhHienTai = monAn.HinhAnh,
                GiaVon = monAn.GiaVon,
                TinhTrang = monAn.TinhTrang,
                ChoGhiChu = monAn.ChoGhiChu,
                DanhSachDanhMuc = await _db.DanhMucs.Where(d => d.MaGianHang == CurrentGianHangId).ToListAsync()
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, MonAnFormViewModel vm)
        {
            var monAn = await _db.MonAns.Include(m => m.DanhMuc)
                .FirstOrDefaultAsync(m => m.MaMonAn == id && m.DanhMuc!.MaGianHang == CurrentGianHangId);
            if (monAn == null) return NotFound();

            vm.TenMon = vm.TenMon?.Trim() ?? "";
            var danhMucHopLe = await _db.DanhMucs.AnyAsync(d =>
                d.MaDanhMuc == vm.MaDanhMuc && d.MaGianHang == CurrentGianHangId);
            if (!danhMucHopLe)
                ModelState.AddModelError(nameof(vm.MaDanhMuc), "Danh mục không thuộc gian hàng của bạn.");
            if (danhMucHopLe && await _db.MonAns.AnyAsync(m => m.MaMonAn != id && m.MaDanhMuc == vm.MaDanhMuc && m.TenMon.ToLower() == vm.TenMon.ToLower()))
                ModelState.AddModelError(nameof(vm.TenMon), "Tên món đã tồn tại trong danh mục này.");

            if (!ModelState.IsValid)
            {
                vm.DanhSachDanhMuc = await _db.DanhMucs.Where(d => d.MaGianHang == CurrentGianHangId).ToListAsync();
                return View(vm);
            }

            monAn.TenMon = vm.TenMon;
            monAn.Gia = vm.Gia;
            monAn.GiaVon = vm.GiaVon;
            monAn.MoTa = vm.MoTa;
            monAn.TinhTrang = vm.TinhTrang;
            monAn.ChoGhiChu = vm.ChoGhiChu;
            monAn.MaDanhMuc = vm.MaDanhMuc;

            if (vm.AnhMoi != null)
            {
                if (!string.IsNullOrEmpty(monAn.HinhAnh)) _storage.DeleteFile(monAn.HinhAnh);
                monAn.HinhAnh = await _storage.SaveFileAsync(vm.AnhMoi, "monan");
            }

            await _db.SaveChangesAsync();
            TempData["ThongBao"] = $"Đã cập nhật món \"{monAn.TenMon}\".";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var monAn = await _db.MonAns.Include(m => m.DanhMuc)
                .FirstOrDefaultAsync(m => m.MaMonAn == id && m.DanhMuc!.MaGianHang == CurrentGianHangId);
            if (monAn == null) return NotFound();

            var dangDuocDat = await _db.ChiTietDonHangs.AnyAsync(c => c.MaMonAn == id);
            if (dangDuocDat)
            {
                // Không xoá vĩnh viễn món đã từng có trong đơn hàng — chỉ chuyển sang tạm ngừng
                // để không phá vỡ dữ liệu lịch sử đơn hàng đã tồn tại.
                monAn.TinhTrang = "TamNgung";
                await _db.SaveChangesAsync();
                TempData["ThongBao"] = $"Món \"{monAn.TenMon}\" đã có trong đơn hàng nên không thể xoá — đã chuyển sang trạng thái Tạm ngừng.";
            }
            else
            {
                if (!string.IsNullOrEmpty(monAn.HinhAnh)) _storage.DeleteFile(monAn.HinhAnh);
                _db.MonAns.Remove(monAn);
                await _db.SaveChangesAsync();
                TempData["ThongBao"] = $"Đã xoá món \"{monAn.TenMon}\".";
            }

            return RedirectToAction("Index");
        }
    }
}
