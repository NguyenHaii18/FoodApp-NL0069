using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FoodServiceApp.Web.Data;
using FoodServiceApp.Web.Services;

namespace FoodServiceApp.Web.Controllers
{
    public class AdminHoSoViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập họ tên")]
        public string HoTen { get; set; } = "";
        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        public string SDT { get; set; } = "";
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string? Email { get; set; }
        public string CapQuyenHan { get; set; } = "";
        public string? PhongBanPhuTrach { get; set; }
        public string? MatKhauMoi { get; set; }
        public string? XacNhanMatKhauMoi { get; set; }
    }

    [Route("QuanTri/HoSo")]
    public class AdminHoSoController : QuanTriBaseController
    {
        private readonly AppDbContext _db;

        public AdminHoSoController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var quanTri = await _db.QuanTris.FirstAsync(q => q.MaQuanTri == CurrentQuanTriId);
            var vm = new AdminHoSoViewModel
            {
                HoTen = quanTri.HoTen,
                SDT = quanTri.SDT,
                Email = quanTri.Email,
                CapQuyenHan = quanTri.CapQuyenHan,
                PhongBanPhuTrach = quanTri.PhongBanPhuTrach,
            };
            return View(vm);
        }

        [HttpPost("")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(AdminHoSoViewModel vm)
        {
            if (!string.IsNullOrEmpty(vm.MatKhauMoi) && vm.MatKhauMoi != vm.XacNhanMatKhauMoi)
                ModelState.AddModelError(nameof(vm.XacNhanMatKhauMoi), "Xác nhận mật khẩu không khớp.");

            if (!ModelState.IsValid)
            {
                var quanTriHienTai = await _db.QuanTris.FirstAsync(q => q.MaQuanTri == CurrentQuanTriId);
                vm.CapQuyenHan = quanTriHienTai.CapQuyenHan;
                return View(vm);
            }

            var quanTri = await _db.QuanTris.FirstAsync(q => q.MaQuanTri == CurrentQuanTriId);
            quanTri.HoTen = vm.HoTen;
            quanTri.SDT = vm.SDT;
            quanTri.Email = vm.Email;
            quanTri.PhongBanPhuTrach = vm.PhongBanPhuTrach;
            // CapQuyenHan (cấp quyền hạn) không tự chỉnh sửa ở đây — chỉ nên do quản trị viên cấp cao hơn thay đổi.

            if (!string.IsNullOrEmpty(vm.MatKhauMoi))
            {
                var taiKhoan = await _db.TaiKhoans.FirstAsync(t => t.MaTK == quanTri.MaTK);
                taiKhoan.MatKhau = MatKhauHelper.Hash(vm.MatKhauMoi);
            }

            await _db.SaveChangesAsync();
            vm.CapQuyenHan = quanTri.CapQuyenHan;
            TempData["ThongBao"] = "Đã cập nhật hồ sơ quản trị viên.";
            return RedirectToAction("Index");
        }
    }
}
