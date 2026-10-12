using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FoodServiceApp.Web.Data;
using FoodServiceApp.Web.Models.Entities;

namespace FoodServiceApp.Web.Controllers
{
    /// <summary>Gian hàng / món ăn yêu thích của khách.</summary>
    [Route("MuaHang/YeuThich")]
    public class CustomerYeuThichController : CustomerBaseController
    {
        private readonly AppDbContext _db;
        public CustomerYeuThichController(AppDbContext db) { _db = db; }

        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var ids = await _db.YeuThichs.Where(y => y.MaKH == CurrentMaKH).OrderByDescending(y => y.NgayTao).ToListAsync();
            var idQuan = ids.Where(y => y.Loai == "GianHang").Select(y => y.MaThamChieu).ToList();
            var idMon = ids.Where(y => y.Loai == "MonAn").Select(y => y.MaThamChieu).ToList();
            ViewBag.Quan = await _db.GianHangs.Where(g => idQuan.Contains(g.MaGianHang)).ToListAsync();
            ViewBag.Mon = await _db.MonAns.Include(m => m.DanhMuc).ThenInclude(d => d!.GianHang).Where(m => idMon.Contains(m.MaMonAn)).ToListAsync();
            return View();
        }

        [HttpPost("bat-tat")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BatTat(string loai, int id, string? quayVe)
        {
            if (loai != "GianHang" && loai != "MonAn") return BadRequest();
            var co = await _db.YeuThichs.FirstOrDefaultAsync(y => y.MaKH == CurrentMaKH && y.Loai == loai && y.MaThamChieu == id);
            if (co != null) _db.YeuThichs.Remove(co);
            else
            {
                var tonTai = loai == "GianHang" ? await _db.GianHangs.AnyAsync(g => g.MaGianHang == id) : await _db.MonAns.AnyAsync(m => m.MaMonAn == id);
                if (!tonTai) return NotFound();
                _db.YeuThichs.Add(new YeuThich { MaKH = CurrentMaKH, Loai = loai, MaThamChieu = id });
            }
            await _db.SaveChangesAsync();
            return !string.IsNullOrEmpty(quayVe) && Url.IsLocalUrl(quayVe) ? Redirect(quayVe) : RedirectToAction(nameof(Index));
        }
    }
}
