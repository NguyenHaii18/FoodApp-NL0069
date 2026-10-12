using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FoodServiceApp.Web.Data;
using FoodServiceApp.Web.Services;

namespace FoodServiceApp.Web.Controllers
{
    public class DriverHoSoViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập họ tên")]
        public string HoTen { get; set; } = "";
        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        public string SDT { get; set; } = "";
        public string? PhuongTien { get; set; }
        public string? BienSo { get; set; }
        public string? KhuVucHoatDong { get; set; }
        public string? MatKhauMoi { get; set; }
        public string? XacNhanMatKhauMoi { get; set; }
    }

    [Route("TaiXe/HoSo")]
    public class DriverHoSoController : TaiXeBaseController
    {
        private readonly AppDbContext _db;

        public DriverHoSoController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var taiXe = await _db.TaiXes.FirstAsync(t => t.MaTaiXe == CurrentTaiXeId);
            var vm = new DriverHoSoViewModel
            {
                HoTen = taiXe.HoTen,
                SDT = taiXe.SDT,
                PhuongTien = taiXe.PhuongTien,
                BienSo = taiXe.BienSo,
                KhuVucHoatDong = taiXe.KhuVucHoatDong,
            };
            return View(vm);
        }

        [HttpPost("")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(DriverHoSoViewModel vm)
        {
            if (!string.IsNullOrEmpty(vm.MatKhauMoi) && vm.MatKhauMoi != vm.XacNhanMatKhauMoi)
                ModelState.AddModelError(nameof(vm.XacNhanMatKhauMoi), "Xác nhận mật khẩu không khớp.");

            if (!ModelState.IsValid) return View(vm);

            var taiXe = await _db.TaiXes.FirstAsync(t => t.MaTaiXe == CurrentTaiXeId);
            taiXe.HoTen = vm.HoTen;
            taiXe.SDT = vm.SDT;
            taiXe.PhuongTien = vm.PhuongTien;
            taiXe.BienSo = vm.BienSo;
            taiXe.KhuVucHoatDong = vm.KhuVucHoatDong;

            if (!string.IsNullOrEmpty(vm.MatKhauMoi))
            {
                var taiKhoan = await _db.TaiKhoans.FirstAsync(t => t.MaTK == taiXe.MaTK);
                taiKhoan.MatKhau = MatKhauHelper.Hash(vm.MatKhauMoi);
            }

            await _db.SaveChangesAsync();
            TempData["ThongBao"] = "Đã cập nhật hồ sơ.";
            return RedirectToAction("Index");
        }
    }
}
