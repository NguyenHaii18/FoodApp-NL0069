using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FoodServiceApp.Web.Data;
using FoodServiceApp.Web.Models.ViewModels;
using FoodServiceApp.Web.Services;

namespace FoodServiceApp.Web.Controllers
{
    public class HoSoController : GianHangBaseController
    {
        private readonly AppDbContext _db;
        private readonly IFileStorageService _storage;

        public HoSoController(AppDbContext db, IFileStorageService storage)
        {
            _db = db;
            _storage = storage;
        }

        public async Task<IActionResult> Index()
        {
            var gianHang = await _db.GianHangs.FirstAsync(g => g.MaGianHang == CurrentGianHangId);
            var vm = new HoSoViewModel
            {
                TenCuaHang = gianHang.TenCuaHang,
                DiaChi = gianHang.DiaChi,
                SDT = gianHang.SDT,
                MoTa = gianHang.MoTa,
                HinhAnhHienTai = gianHang.HinhAnh,
                Email = gianHang.Email,
                GioMoCua = gianHang.GioMoCua.ToString(@"hh\:mm"),
                GioDongCua = gianHang.GioDongCua.ToString(@"hh\:mm"),
                DangNhanDon = gianHang.DangNhanDon,
                GiayPhepHienTai = gianHang.GiayPhepKinhDoanh,
                GiayAttpHienTai = gianHang.GiayChungNhanATTP,
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(HoSoViewModel vm)
        {
            if (!string.IsNullOrEmpty(vm.MatKhauMoi) && vm.MatKhauMoi != vm.XacNhanMatKhauMoi)
                ModelState.AddModelError(nameof(vm.XacNhanMatKhauMoi), "Xác nhận mật khẩu không khớp.");

            if (!ModelState.IsValid) return View(vm);

            var gianHang = await _db.GianHangs.FirstAsync(g => g.MaGianHang == CurrentGianHangId);
            gianHang.TenCuaHang = vm.TenCuaHang;
            gianHang.DiaChi = vm.DiaChi;
            gianHang.SDT = vm.SDT;
            gianHang.MoTa = vm.MoTa;
            gianHang.Email = string.IsNullOrWhiteSpace(vm.Email) ? null : vm.Email.Trim();
            if (TimeSpan.TryParse(vm.GioMoCua, out var mo)) gianHang.GioMoCua = mo;
            if (TimeSpan.TryParse(vm.GioDongCua, out var dong)) gianHang.GioDongCua = dong;
            gianHang.DangNhanDon = vm.DangNhanDon;
            if (vm.GiayPhepMoi != null && vm.GiayPhepMoi.Length > 0)
            {
                if (!string.IsNullOrEmpty(gianHang.GiayPhepKinhDoanh)) _storage.DeleteFile(gianHang.GiayPhepKinhDoanh);
                gianHang.GiayPhepKinhDoanh = await _storage.SaveFileAsync(vm.GiayPhepMoi, "giayto");
            }
            if (vm.GiayAttpMoi != null && vm.GiayAttpMoi.Length > 0)
            {
                if (!string.IsNullOrEmpty(gianHang.GiayChungNhanATTP)) _storage.DeleteFile(gianHang.GiayChungNhanATTP);
                gianHang.GiayChungNhanATTP = await _storage.SaveFileAsync(vm.GiayAttpMoi, "giayto");
            }

            if (vm.AnhMoi != null)
            {
                if (!string.IsNullOrEmpty(gianHang.HinhAnh)) _storage.DeleteFile(gianHang.HinhAnh);
                gianHang.HinhAnh = await _storage.SaveFileAsync(vm.AnhMoi, "gianhang");
            }

            if (!string.IsNullOrEmpty(vm.MatKhauMoi))
            {
                var taiKhoan = await _db.TaiKhoans.FirstAsync(t => t.MaTK == gianHang.MaTK);
                taiKhoan.MatKhau = MatKhauHelper.Hash(vm.MatKhauMoi);
            }

            await _db.SaveChangesAsync();
            TempData["ThongBao"] = "Đã cập nhật hồ sơ cửa hàng.";
            return RedirectToAction("Index");
        }
    }
}
