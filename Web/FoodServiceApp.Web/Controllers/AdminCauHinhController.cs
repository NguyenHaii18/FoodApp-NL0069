using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FoodServiceApp.Web.Data;
using FoodServiceApp.Web.Services;

namespace FoodServiceApp.Web.Controllers
{
    /// <summary>Cài đặt phí & chính sách (lưu trong CSDL, có lịch sử người đổi + thời điểm). Chỉ Admin tổng được ghi.</summary>
    [Route("QuanTri/CauHinh")]
    [FoodServiceApp.Web.Filters.ChiSuperAdminGhi]
    public class AdminCauHinhController : AdminOpsBase
    {
        private readonly CauHinhService _cauHinh;
        public AdminCauHinhController(AppDbContext db, CauHinhService cauHinh) : base(db) { _cauHinh = cauHinh; }

        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            ViewBag.Ds = await Db.CauHinhHeThongs.OrderBy(c => c.Khoa).ToListAsync();
            var ls = await Db.CauHinhHeThongLichSus.OrderByDescending(l => l.ThoiGian).Take(40).ToListAsync();
            var ma = ls.Where(l => l.MaTK != null).Select(l => l.MaTK!.Value).Distinct().ToList();
            ViewBag.Ten = await Db.TaiKhoans.Where(t => ma.Contains(t.MaTK)).ToDictionaryAsync(t => t.MaTK, t => t.TenDangNhap);
            ViewBag.LichSu = ls;
            return View();
        }

        [HttpPost("luu")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Luu(string khoa, string giaTri)
        {
            giaTri = (giaTri ?? "").Trim();
            var c = await Db.CauHinhHeThongs.AsNoTracking().FirstOrDefaultAsync(x => x.Khoa == khoa);
            if (c == null) return NotFound();
            var laChu = khoa.StartsWith("QuyDinh.") || khoa.StartsWith("HoanTien.ChinhSach");
            if (giaTri.Length == 0 || giaTri.Length > 300) { TempData["LoiThongBao"] = "Giá trị không được để trống và tối đa 300 ký tự."; return RedirectToAction(nameof(Index)); }
            if (!laChu)
            {
                if (!decimal.TryParse(giaTri, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var v) || v < 0)
                { TempData["LoiThongBao"] = "Giá trị phải là số không âm (dùng dấu chấm cho số thập phân, vd 0.15)."; return RedirectToAction(nameof(Index)); }
                if (khoa == "HoaHong.MacDinh" && v > 1) { TempData["LoiThongBao"] = "Hoa hồng là tỷ lệ từ 0 đến 1 (vd 0.15 = 15%)."; return RedirectToAction(nameof(Index)); }
            }
            if (c.GiaTri == giaTri) { TempData["ThongBao"] = "Giá trị không thay đổi."; return RedirectToAction(nameof(Index)); }
            await _cauHinh.DatAsync(khoa, giaTri, MaTK);
            TempData["ThongBao"] = $"Đã cập nhật {khoa}.";
            return RedirectToAction(nameof(Index));
        }
    }
}
