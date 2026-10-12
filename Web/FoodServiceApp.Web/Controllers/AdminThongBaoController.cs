using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FoodServiceApp.Web.Data;
using FoodServiceApp.Web.Models.Entities;

namespace FoodServiceApp.Web.Controllers
{
    /// <summary>Gửi thông báo hệ thống: toàn hệ thống, theo vai trò, hoặc một nhóm tài khoản cụ thể.</summary>
    [Route("QuanTri/ThongBao")]
    [FoodServiceApp.Web.Filters.CoQuyenGhi("ThongBao")]
    public class AdminThongBaoController : AdminOpsBase
    {
        public AdminThongBaoController(AppDbContext db) : base(db) { }

        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            ViewBag.GanDay = await Db.ThongBaos.Where(t => t.Loai == "HeThong").OrderByDescending(t => t.NgayTao).Take(300)
                .GroupBy(t => new { t.TieuDe, t.NoiDung }).Select(g => new { g.Key.TieuDe, g.Key.NoiDung, Ngay = g.Max(x => x.NgayTao), SoNguoi = g.Count() })
                .OrderByDescending(x => x.Ngay).Take(15).ToListAsync();
            return View();
        }

        [HttpPost("gui")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Gui(string doiTuong, string? dsTaiKhoan, string tieuDe, string noiDung)
        {
            tieuDe = (tieuDe ?? "").Trim(); noiDung = (noiDung ?? "").Trim();
            if (tieuDe.Length < 3 || noiDung.Length < 3) { TempData["LoiThongBao"] = "Nhập tiêu đề và nội dung."; return RedirectToAction(nameof(Index)); }

            List<int> maTKs;
            switch (doiTuong)
            {
                case "TatCa": maTKs = await Db.TaiKhoans.Where(t => t.TrangThai && t.VaiTro != "QuanTri").Select(t => t.MaTK).ToListAsync(); break;
                case "KhachHang": case "GianHang": case "TaiXe":
                    maTKs = await Db.TaiKhoans.Where(t => t.TrangThai && t.VaiTro == doiTuong).Select(t => t.MaTK).ToListAsync(); break;
                case "Nhom":
                    // danh sách tên đăng nhập hoặc mã TK, ngăn cách bằng dấu phẩy/xuống dòng
                    var tokens = (dsTaiKhoan ?? "").Split(new[] { ',', '\n', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Distinct().Take(500).ToList();
                    maTKs = await Db.TaiKhoans.Where(t => tokens.Contains(t.TenDangNhap)).Select(t => t.MaTK).ToListAsync();
                    break;
                default: TempData["LoiThongBao"] = "Đối tượng không hợp lệ."; return RedirectToAction(nameof(Index));
            }
            if (!maTKs.Any()) { TempData["LoiThongBao"] = "Không tìm thấy tài khoản nhận nào."; return RedirectToAction(nameof(Index)); }

            foreach (var m in maTKs) Db.ThongBaos.Add(new ThongBao { MaTK = m, Loai = "HeThong", TieuDe = tieuDe, NoiDung = noiDung });
            await Db.SaveChangesAsync();
            TempData["ThongBao"] = $"Đã gửi thông báo tới {maTKs.Count} tài khoản.";
            return RedirectToAction(nameof(Index));
        }
    }
}
