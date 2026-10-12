using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FoodServiceApp.Web.Data;
using FoodServiceApp.Web.Models.Entities;
using FoodServiceApp.Web.Services;

namespace FoodServiceApp.Web.Controllers
{
    /// <summary>Điều phối giao hàng: đơn cần giao, tài xế đang hoạt động, phân công/đổi/gỡ tài xế (có lý do + lưu lịch sử), đơn giao quá hạn.</summary>
    [Route("QuanTri/DieuPhoi")]
    [FoodServiceApp.Web.Filters.CoQuyenGhi("DonHang")]
    public class AdminDieuPhoiController : AdminOpsBase
    {
        private readonly CauHinhService _cauHinh;
        public AdminDieuPhoiController(AppDbContext db, CauHinhService cauHinh) : base(db) { _cauHinh = cauHinh; }

        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var now = DateTime.Now;
            var tre = (int)await _cauHinh.LaySoAsync("Don.DuKienPhut", 45) + (int)await _cauHinh.LaySoAsync("Don.TreSauPhut", 30);

            // Đơn đã được quán xác nhận nhưng chưa có tài xế (kể cả đơn tài xế đã từ chối / không tìm được tài xế)
            ViewBag.CanGiao = await Db.DonHangs.Include(d => d.GianHang).Include(d => d.KhachHang)
                .Where(d => d.TrangThai == "DaXacNhan" && d.MaTaiXe == null).OrderBy(d => d.NgayDat).ToListAsync();
            ViewBag.DangGiao = await Db.DonHangs.Include(d => d.GianHang).Include(d => d.GiaoHang).ThenInclude(g => g!.TaiXe)
                .Where(d => (d.TrangThai == "DangGiao" || d.TrangThai == "DaXacNhan") && d.MaTaiXe != null).OrderBy(d => d.NgayDat).ToListAsync();
            ViewBag.TreSauPhut = tre;

            var dangBan = await Db.DonHangs.Where(d => (d.TrangThai == "DangGiao" || d.TrangThai == "DaXacNhan") && d.MaTaiXe != null)
                .GroupBy(d => d.MaTaiXe!.Value).Select(g => new { Ma = g.Key, So = g.Count() }).ToDictionaryAsync(x => x.Ma, x => x.So);
            ViewBag.DangBan = dangBan;
            ViewBag.TaiXes = await Db.TaiXes.Where(t => t.TrangThaiDuyet == "DaDuyet" && Db.TaiKhoans.Any(k => k.MaTK == t.MaTK && k.TrangThai))
                .OrderBy(t => t.TrangThai == "SanSang" ? 0 : 1).ThenBy(t => t.HoTen).ToListAsync();
            ViewBag.Log = await Db.DieuPhoiLogs.OrderByDescending(l => l.ThoiGian).Take(30).ToListAsync();
            return View();
        }

        /// <summary>Phân công lần đầu hoặc đổi tài xế khi đơn đang giao.</summary>
        [HttpPost("phan-cong/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PhanCong(int id, int maTaiXe, string? lyDo)
        {
            lyDo = (lyDo ?? "").Trim();
            if (lyDo.Length < 3) { TempData["LoiThongBao"] = "Vui lòng nhập lý do phân công/đổi tài xế."; return RedirectToAction(nameof(Index)); }
            var don = await Db.DonHangs.Include(d => d.GiaoHang).FirstOrDefaultAsync(d => d.MaDonHang == id);
            var tx = await Db.TaiXes.FirstOrDefaultAsync(t => t.MaTaiXe == maTaiXe && t.TrangThaiDuyet == "DaDuyet");
            if (don == null || tx == null) return NotFound();
            if (don.TrangThai != "DaXacNhan" && don.TrangThai != "DangGiao") { TempData["LoiThongBao"] = "Chỉ phân công cho đơn đã xác nhận hoặc đang giao."; return RedirectToAction(nameof(Index)); }
            if (!await Db.TaiKhoans.AnyAsync(k => k.MaTK == tx.MaTK && k.TrangThai)) { TempData["LoiThongBao"] = "Tài xế này đang bị khóa."; return RedirectToAction(nameof(Index)); }

            var cu = don.MaTaiXe;
            if (don.GiaoHang != null) { don.GiaoHang.MaTaiXe = maTaiXe; don.GiaoHang.TrangThai = "DaNhan"; don.GiaoHang.ThoiGianNhan = DateTime.Now; }
            else Db.GiaoHangs.Add(new GiaoHang { MaDonHang = id, MaTaiXe = maTaiXe, TrangThai = "DaNhan", ThoiGianNhan = DateTime.Now });
            don.MaTaiXe = maTaiXe;
            // Luồng thống nhất: đơn ở "DaXacNhan" (tài xế đang đến quán); tài xế bấm "Đã lấy hàng" mới sang "DangGiao"
            don.TrangThai = "DaXacNhan";
            tx.TrangThai = "DangGiao";

            Db.DieuPhoiLogs.Add(new DieuPhoiLog { MaDonHang = id, HanhDong = cu == null ? "PhanCong" : "DoiTaiXe", MaTaiXeCu = cu, MaTaiXeMoi = maTaiXe, LyDo = lyDo, MaTK = MaTK });
            // thông báo cho tài xế được phân công (và tài xế cũ nếu đổi)
            Db.ThongBaos.Add(new ThongBao { MaTK = tx.MaTK, Loai = "DonHang", TieuDe = "Bạn được phân công đơn mới", NoiDung = $"Đơn #{id} được quản trị phân công cho bạn. Lý do: {lyDo}", MaThamChieu = id.ToString() });
            if (cu != null && cu != maTaiXe)
            {
                var maTkCu = await Db.TaiXes.Where(t => t.MaTaiXe == cu).Select(t => t.MaTK).FirstOrDefaultAsync();
                if (maTkCu != 0) Db.ThongBaos.Add(new ThongBao { MaTK = maTkCu, Loai = "DonHang", TieuDe = "Đơn đã được chuyển cho tài xế khác", NoiDung = $"Đơn #{id} đã được quản trị chuyển đi. Lý do: {lyDo}", MaThamChieu = id.ToString() });
            }
            await Db.SaveChangesAsync();
            if (cu != null && cu != maTaiXe) { await GiaoHangXuLy.GiaiPhongTaiXeAsync(Db, cu, id); await Db.SaveChangesAsync(); }
            TempData["ThongBao"] = $"Đã giao đơn #{id} cho {tx.HoTen}.";
            return RedirectToAction(nameof(Index));
        }

        /// <summary>Gỡ tài xế (tài xế không thể tiếp tục): đơn về lại danh sách chờ nhận.</summary>
        [HttpPost("go-tai-xe/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GoTaiXe(int id, string? lyDo)
        {
            lyDo = (lyDo ?? "").Trim();
            if (lyDo.Length < 3) { TempData["LoiThongBao"] = "Vui lòng nhập lý do."; return RedirectToAction(nameof(Index)); }
            var don = await Db.DonHangs.Include(d => d.GiaoHang).FirstOrDefaultAsync(d => d.MaDonHang == id && d.MaTaiXe != null && (d.TrangThai == "DangGiao" || d.TrangThai == "DaXacNhan"));
            if (don == null) return NotFound();
            var cu = don.MaTaiXe;
            if (don.GiaoHang != null) Db.GiaoHangs.Remove(don.GiaoHang);
            don.MaTaiXe = null;
            don.TrangThai = "DaXacNhan";
            Db.DieuPhoiLogs.Add(new DieuPhoiLog { MaDonHang = id, HanhDong = "GoTaiXe", MaTaiXeCu = cu, LyDo = lyDo, MaTK = MaTK });
            await Db.SaveChangesAsync();
            { await GiaoHangXuLy.GiaiPhongTaiXeAsync(Db, cu, id); await Db.SaveChangesAsync(); }
            TempData["ThongBao"] = $"Đã gỡ tài xế khỏi đơn #{id}, đơn trở lại danh sách chờ nhận.";
            return RedirectToAction(nameof(Index));
        }
    }
}
