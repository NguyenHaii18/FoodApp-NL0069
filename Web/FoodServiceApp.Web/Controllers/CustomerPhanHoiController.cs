using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FoodServiceApp.Web.Data;
using FoodServiceApp.Web.Models.Entities;
using FoodServiceApp.Web.Models.ViewModels;

namespace FoodServiceApp.Web.Controllers
{
    // Khách gửi phản hồi / góp ý / khiếu nại (có thể gắn với một đơn hàng) và xem trả lời của quản trị viên.
    [Route("MuaHang/PhanHoi")]
    public class CustomerPhanHoiController : CustomerBaseController
    {
        private static readonly string[] LoaiHopLe = { "GopY", "KhieuNai", "Khac" };
        private readonly AppDbContext _db;

        public CustomerPhanHoiController(AppDbContext db)
        {
            _db = db;
        }

        private async Task NapDuLieuAsync()
        {
            ViewBag.DonHangCuaToi = await _db.DonHangs.Include(d => d.GianHang)
                .Where(d => d.MaKH == CurrentMaKH)
                .OrderByDescending(d => d.NgayDat).Take(30).ToListAsync();
            ViewBag.DanhSach = await _db.PhanHois
                .Where(p => p.MaKH == CurrentMaKH)
                .OrderByDescending(p => p.NgayGui).ToListAsync();
        }

        [HttpGet("")]
        public async Task<IActionResult> Index(int? donHang)
        {
            await NapDuLieuAsync();
            return View(new PhanHoiFormViewModel { MaDonHang = donHang });
        }

        [HttpPost("{id:int}/bo-sung")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BoSung(int id, string? noiDung)
        {
            var ph = await _db.PhanHois.FirstOrDefaultAsync(p => p.MaPhanHoi == id && p.MaKH == CurrentMaKH);
            if (ph == null) return NotFound();
            if (!string.IsNullOrWhiteSpace(noiDung))
            {
                var them = $"{ph.NoiDung}\n[Bổ sung {DateTime.Now:dd/MM HH:mm}] {noiDung.Trim()}";
                ph.NoiDung = them.Length > 1000 ? them[..1000] : them;
                ph.YeuCauBoSung = null;
                ph.TrangThai = "ChuaXuLy";
                ph.TraLoi = null; ph.NgayXuLy = null;
                await _db.SaveChangesAsync();
                TempData["ThongBao"] = "Đã gửi bổ sung cho bộ phận hỗ trợ.";
            }
            return RedirectToAction("Index");
        }

        [HttpPost("gui")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Gui(PhanHoiFormViewModel model)
        {
            if (!LoaiHopLe.Contains(model.Loai))
                ModelState.AddModelError(nameof(model.Loai), "Loại phản hồi không hợp lệ.");

            int? maGianHang = null;
            if (model.MaDonHang != null)
            {
                // Chỉ cho gắn với đơn của chính khách
                var don = await _db.DonHangs.FirstOrDefaultAsync(d => d.MaDonHang == model.MaDonHang && d.MaKH == CurrentMaKH);
                if (don == null) ModelState.AddModelError(nameof(model.MaDonHang), "Đơn hàng không hợp lệ.");
                else maGianHang = don.MaGianHang;
            }

            if (!ModelState.IsValid)
            {
                await NapDuLieuAsync();
                return View("Index", model);
            }

            _db.PhanHois.Add(new PhanHoi
            {
                MaKH = CurrentMaKH,
                MaDonHang = model.MaDonHang,
                MaGianHang = maGianHang,
                Loai = model.Loai,
                TieuDe = model.TieuDe.Trim(),
                NoiDung = model.NoiDung.Trim(),
            });
            await _db.SaveChangesAsync();

            TempData["ThongBao"] = "Đã gửi phản hồi. Quản trị viên sẽ xem xét và trả lời bạn sớm.";
            return RedirectToAction("Index");
        }
    }
}
