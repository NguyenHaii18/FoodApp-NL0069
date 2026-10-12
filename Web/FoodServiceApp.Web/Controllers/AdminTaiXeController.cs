using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FoodServiceApp.Web.Data;
using FoodServiceApp.Web.Models.Entities;
using FoodServiceApp.Web.Models.ViewModels;
using FoodServiceApp.Web.Services;

namespace FoodServiceApp.Web.Controllers
{
    // Trước bản nâng cấp này, hệ thống KHÔNG có cách nào tạo tài khoản tài xế
    // (không tự đăng ký như Gian hàng, cũng không có nơi Quản trị tạo) — tài xế
    // chỉ có thể được thêm bằng tay trực tiếp trong CSDL. Controller này bổ sung
    // đúng phần còn thiếu: Quản trị viên tạo tài khoản, xem danh sách, khoá/mở.
    [Route("QuanTri/TaiXe")]
    [FoodServiceApp.Web.Filters.ChiSuperAdminGhi]
    public class AdminTaiXeController : QuanTriBaseController
    {
        private readonly AppDbContext _db;

        public AdminTaiXeController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index(string? timKiem)
        {
            var query = _db.TaiXes
                .Where(x => x.TrangThaiDuyet == "DaDuyet")
                .Join(_db.TaiKhoans, x => x.MaTK, t => t.MaTK, (x, t) => new { TaiXe = x, TaiKhoan = t });

            if (!string.IsNullOrWhiteSpace(timKiem))
                query = query.Where(x => x.TaiXe.HoTen.Contains(timKiem) || x.TaiXe.SDT.Contains(timKiem));

            var danhSach = await query.OrderBy(x => x.TaiXe.HoTen).ToListAsync();

            ViewBag.TimKiem = timKiem ?? "";
            ViewBag.SoChoDuyet = await _db.TaiXes.CountAsync(x => x.TrangThaiDuyet == "ChoDuyet");
            return View(danhSach.Select(x => (x.TaiXe, x.TaiKhoan)).ToList());
        }

        // Danh sách tài xế tự đăng ký, đang chờ Quản trị duyệt
        [HttpGet("ChoDuyet")]
        public async Task<IActionResult> ChoDuyet()
        {
            var danhSach = await _db.TaiXes
                .Where(x => x.TrangThaiDuyet == "ChoDuyet")
                .OrderBy(x => x.NgayDangKy)
                .ToListAsync();
            return View(danhSach);
        }

        [HttpPost("Duyet/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Duyet(int id)
        {
            var taiXe = await _db.TaiXes.FirstOrDefaultAsync(x => x.MaTaiXe == id);
            if (taiXe == null) return NotFound();

            taiXe.TrangThaiDuyet = "DaDuyet";
            await _db.SaveChangesAsync();

            TempData["ThongBao"] = $"Đã duyệt hồ sơ tài xế \"{taiXe.HoTen}\".";
            return RedirectToAction("ChoDuyet");
        }

        [HttpPost("BoSung/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BoSung(int id, string? yeuCau)
        {
            var tx = await _db.TaiXes.FirstOrDefaultAsync(x => x.MaTaiXe == id);
            if (tx == null) return NotFound();
            if (string.IsNullOrWhiteSpace(yeuCau)) { TempData["LoiThongBao"] = "Nhập nội dung cần bổ sung."; return RedirectToAction("ChoDuyet"); }
            tx.YeuCauBoSung = yeuCau.Trim();
            _db.ThongBaos.Add(new FoodServiceApp.Web.Models.Entities.ThongBao { MaTK = tx.MaTK, Loai = "HeThong", TieuDe = "Yêu cầu bổ sung hồ sơ tài xế", NoiDung = tx.YeuCauBoSung });
            await _db.SaveChangesAsync();
            TempData["ThongBao"] = $"Đã yêu cầu tài xế \"{tx.HoTen}\" bổ sung hồ sơ.";
            return RedirectToAction("ChoDuyet");
        }

        [HttpPost("TuChoi/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TuChoi(int id, string? lyDo)
        {
            var taiXe = await _db.TaiXes.FirstOrDefaultAsync(x => x.MaTaiXe == id);
            if (taiXe == null) return NotFound();

            if (string.IsNullOrWhiteSpace(lyDo))
            {
                TempData["LoiThongBao"] = "Vui lòng nhập lý do từ chối.";
                return RedirectToAction("ChoDuyet");
            }
            taiXe.TrangThaiDuyet = "TuChoi";
            taiXe.LyDoTuChoi = lyDo.Trim();
            _db.ThongBaos.Add(new FoodServiceApp.Web.Models.Entities.ThongBao { MaTK = taiXe.MaTK, Loai = "HeThong", TieuDe = "Hồ sơ tài xế bị từ chối", NoiDung = "Lý do: " + taiXe.LyDoTuChoi });
            await _db.SaveChangesAsync();

            TempData["ThongBao"] = $"Đã từ chối hồ sơ tài xế \"{taiXe.HoTen}\".";
            return RedirectToAction("ChoDuyet");
        }

        [HttpGet("Them")]
        public IActionResult Them() => View(new AdminCreateDriverViewModel());

        [HttpPost("Them")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Them(AdminCreateDriverViewModel model)
        {
            if (model.MatKhau != model.XacNhanMatKhau)
                ModelState.AddModelError(nameof(model.XacNhanMatKhau), "Xác nhận mật khẩu không khớp.");

            if (await _db.TaiKhoans.AnyAsync(t => t.TenDangNhap == model.TenDangNhap))
                ModelState.AddModelError(nameof(model.TenDangNhap), "Tên đăng nhập đã tồn tại.");

            if (!ModelState.IsValid) return View(model);

            var taiKhoan = new TaiKhoan
            {
                TenDangNhap = model.TenDangNhap,
                MatKhau = MatKhauHelper.Hash(model.MatKhau),
                VaiTro = "TaiXe",
            };
            _db.TaiKhoans.Add(taiKhoan);
            await _db.SaveChangesAsync();

            var taiXe = new TaiXe
            {
                MaTK = taiKhoan.MaTK,
                HoTen = model.HoTen,
                SDT = model.SDT,
                PhuongTien = model.PhuongTien,
                BienSo = model.BienSo,
                KhuVucHoatDong = model.KhuVucHoatDong,
                TrangThai = "NgoaiTuyen", // tài xế tự bật "Sẵn sàng" khi đăng nhập lần đầu
                TrangThaiDuyet = "DaDuyet", // Quản trị tạo trực tiếp -> kích hoạt luôn
            };
            _db.TaiXes.Add(taiXe);
            await _db.SaveChangesAsync();

            TempData["ThongBao"] = $"Đã tạo tài khoản tài xế \"{taiXe.HoTen}\" — tên đăng nhập: {taiKhoan.TenDangNhap}.";
            return RedirectToAction("Index");
        }

        [HttpPost("ToggleKhoa/{maTK:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleKhoa(int maTK)
        {
            var taiKhoan = await _db.TaiKhoans.FirstOrDefaultAsync(t => t.MaTK == maTK && t.VaiTro == "TaiXe");
            if (taiKhoan == null) return NotFound();

            taiKhoan.TrangThai = !taiKhoan.TrangThai;
            await _db.SaveChangesAsync();

            TempData["ThongBao"] = taiKhoan.TrangThai
                ? "Đã mở khoá tài khoản tài xế."
                : "Đã khoá tài khoản tài xế (do vi phạm/khiếu nại).";
            return RedirectToAction("Index");
        }
    }
}
