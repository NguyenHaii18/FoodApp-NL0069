using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FoodServiceApp.Web.Data;
using FoodServiceApp.Web.Models.ViewModels;
using FoodServiceApp.Web.Services;

namespace FoodServiceApp.Web.Controllers
{
    [Route("MuaHang/HoSo")]
    public class CustomerHoSoController : CustomerBaseController
    {
        private readonly AppDbContext _db;

        public CustomerHoSoController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var kh = await _db.KhachHangs.FindAsync(CurrentMaKH);
            var tk = await _db.TaiKhoans.FindAsync(kh!.MaTK);
            ViewBag.AnhDaiDien = kh.AnhDaiDien;
            ViewBag.TrangThai = tk != null && tk.TrangThai ? "Đang hoạt động" : "Đã khóa";
            ViewBag.EmailXt = tk?.EmailDaXacThuc == true;
            ViewBag.SdtXt = tk?.SdtDaXacThuc == true;
            ViewBag.DongY = tk?.DongYDieuKhoanLuc;
            var vm = new CustomerHoSoViewModel
            {
                HoTen = kh.HoTen,
                SDT = kh.SDT,
                Email = kh.Email,
                DiaChiGiaoHang = kh.DiaChiGiaoHang,
            };
            return View(vm);
        }

        [HttpPost("")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(CustomerHoSoViewModel vm)
        {
            if (!string.IsNullOrEmpty(vm.MatKhauMoi) && vm.MatKhauMoi != vm.XacNhanMatKhauMoi)
                ModelState.AddModelError(nameof(vm.XacNhanMatKhauMoi), "Xác nhận mật khẩu không khớp.");

            vm.SDT = (vm.SDT ?? "").Trim();
            if (!System.Text.RegularExpressions.Regex.IsMatch(vm.SDT, @"^(\+84|0)\d{9}$"))
                ModelState.AddModelError(nameof(vm.SDT), "Số điện thoại không hợp lệ.");
            if (!string.IsNullOrWhiteSpace(vm.Email) && !new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(vm.Email))
                ModelState.AddModelError(nameof(vm.Email), "Email không hợp lệ.");
            if (await _db.KhachHangs.AnyAsync(k => k.SDT == vm.SDT && k.MaKH != CurrentMaKH))
                ModelState.AddModelError(nameof(vm.SDT), "Số điện thoại đã được tài khoản khác sử dụng.");
            if (!string.IsNullOrWhiteSpace(vm.Email) && await _db.KhachHangs.AnyAsync(k => k.Email == vm.Email && k.MaKH != CurrentMaKH))
                ModelState.AddModelError(nameof(vm.Email), "Email đã được tài khoản khác sử dụng.");

            if (!ModelState.IsValid) return View(vm);

            var kh = await _db.KhachHangs.FindAsync(CurrentMaKH);
            var doiLienHe = kh!.SDT != vm.SDT || (kh.Email ?? "") != (vm.Email ?? "");
            kh.HoTen = vm.HoTen;
            kh.SDT = vm.SDT;
            kh.Email = string.IsNullOrWhiteSpace(vm.Email) ? null : vm.Email.Trim();
            if (doiLienHe)
            {
                // đổi liên hệ -> phải xác thực lại
                var tkLH = await _db.TaiKhoans.FindAsync(kh.MaTK);
                if (tkLH != null) { tkLH.EmailDaXacThuc = false; tkLH.SdtDaXacThuc = false; }
            }
            kh.DiaChiGiaoHang = vm.DiaChiGiaoHang;

            if (!string.IsNullOrEmpty(vm.MatKhauMoi))
            {
                var taiKhoan = await _db.TaiKhoans.FindAsync(kh.MaTK);
                taiKhoan!.MatKhau = MatKhauHelper.Hash(vm.MatKhauMoi);
            }

            await _db.SaveChangesAsync();
            TempData["ThongBao"] = doiLienHe ? "Đã cập nhật hồ sơ. Vì đổi email/SĐT nên bạn cần xác thực lại trong mục Bảo mật." : "Đã cập nhật hồ sơ.";
            return RedirectToAction("Index");
        }
    }
}
