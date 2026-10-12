using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FoodServiceApp.Web.Data;
using FoodServiceApp.Web.Filters;
using FoodServiceApp.Web.Models.Entities;
using FoodServiceApp.Web.Services;

namespace FoodServiceApp.Web.Controllers
{
    /// <summary>Phân quyền quản trị: Admin tổng tạo/khóa tài khoản Nhân viên hỗ trợ (chỉ xem + xử lý phản hồi/sự cố).</summary>
    [Route("QuanTri/NhanVien")]
    [ChiSuperAdminGhi]
    public class AdminNhanVienController : QuanTriBaseController
    {
        private readonly AppDbContext _db;
        public AdminNhanVienController(AppDbContext db) { _db = db; }

        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            if (!await ChiSuperAdminGhiAttribute.LaSuperAdminAsync(HttpContext)) return Forbid();
            var ds = await (from q in _db.QuanTris join t in _db.TaiKhoans on q.MaTK equals t.MaTK orderby q.MaQuanTri select new NhanVienRow(q, t)).ToListAsync();
            return View(ds);
        }

        public record NhanVienRow(QuanTri QuanTri, TaiKhoan TaiKhoan);

        [HttpPost("Them")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Them(string tenDangNhap, string hoTen, string sdt, string? email, string capQuyen, string matKhau)
        {
            capQuyen = capQuyen is "SuperAdmin" or "QuanLy" ? capQuyen : "NhanVien";
            if (string.IsNullOrWhiteSpace(tenDangNhap) || string.IsNullOrWhiteSpace(hoTen) || (matKhau ?? "").Length < 6)
            { TempData["Loi"] = "Nhập đủ tên đăng nhập, họ tên và mật khẩu tối thiểu 6 ký tự."; return RedirectToAction(nameof(Index)); }
            if (await _db.TaiKhoans.AnyAsync(t => t.TenDangNhap == tenDangNhap.Trim()))
            { TempData["Loi"] = "Tên đăng nhập đã tồn tại."; return RedirectToAction(nameof(Index)); }

            var tk = new TaiKhoan { TenDangNhap = tenDangNhap.Trim(), MatKhau = MatKhauHelper.Hash(matKhau!), VaiTro = "QuanTri" };
            _db.TaiKhoans.Add(tk);
            await _db.SaveChangesAsync();
            _db.QuanTris.Add(new QuanTri { MaTK = tk.MaTK, HoTen = hoTen.Trim(), SDT = (sdt ?? "").Trim(), Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim(), CapQuyenHan = capQuyen });
            await _db.SaveChangesAsync();
            TempData["ThongBao"] = $"Đã tạo tài khoản {(capQuyen == "SuperAdmin" ? "Admin tổng" : capQuyen == "QuanLy" ? "Quản lý vận hành" : "Nhân viên hỗ trợ")} \"{tk.TenDangNhap}\".";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost("ToggleKhoa/{maTK:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleKhoa(int maTK)
        {
            if (maTK == CurrentMaTK) { TempData["Loi"] = "Không thể tự khóa chính mình."; return RedirectToAction(nameof(Index)); }
            var tk = await _db.TaiKhoans.FirstOrDefaultAsync(t => t.MaTK == maTK && t.VaiTro == "QuanTri");
            if (tk == null) return NotFound();
            tk.TrangThai = !tk.TrangThai;
            await _db.SaveChangesAsync();
            TempData["ThongBao"] = tk.TrangThai ? "Đã mở khóa." : "Đã khóa tài khoản.";
            return RedirectToAction(nameof(Index));
        }

        /// <summary>Admin tổng cấp quyền chi tiết theo chức năng cho Quản lý/Nhân viên (bỏ chọn hết = quay về mặc định theo cấp).</summary>
        [HttpPost("Quyen/{maTK:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CapNhatQuyen(int maTK, string[]? quyen, bool macDinh = false)
        {
            var q = await _db.QuanTris.FirstOrDefaultAsync(x => x.MaTK == maTK);
            if (q == null) return NotFound();
            if (q.CapQuyenHan == "SuperAdmin") { TempData["Loi"] = "Admin tổng luôn có toàn quyền."; return RedirectToAction(nameof(Index)); }
            var hopLe = QuyenAdmin.TatCa.Select(t => t.Khoa).ToHashSet();
            var chon = (quyen ?? Array.Empty<string>()).Where(hopLe.Contains).Distinct().ToList();
            q.QuyenChiTiet = macDinh ? null : chon.Count == 0 ? "-" : string.Join(",", chon);   // "-" = thu hồi hết
            await _db.SaveChangesAsync();
            TempData["ThongBao"] = q.QuyenChiTiet == null ? "Đã đặt về quyền mặc định theo cấp." : q.QuyenChiTiet == "-" ? "Đã thu hồi mọi quyền chức năng (chỉ còn xem)." : "Đã cập nhật quyền chi tiết.";
            return RedirectToAction(nameof(Index));
        }

        private int CurrentMaTK => int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
    }
}
