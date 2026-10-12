using Microsoft.EntityFrameworkCore;
using FoodServiceApp.Web.Data;
using FoodServiceApp.Web.Models.Entities;

namespace FoodServiceApp.Web.Services
{
    /// <summary>Đọc/ghi cấu hình phí & chính sách trong bảng CauHinhHeThong (có lưu lịch sử người đổi). Lỗi DB -> dùng giá trị mặc định.</summary>
    public class CauHinhService
    {
        private readonly AppDbContext _db;
        public CauHinhService(AppDbContext db) { _db = db; }

        public async Task<string?> LayChuoiAsync(string khoa, string? macDinh = null)
        {
            try { return (await _db.CauHinhHeThongs.AsNoTracking().FirstOrDefaultAsync(c => c.Khoa == khoa))?.GiaTri ?? macDinh; }
            catch { return macDinh; }
        }

        public async Task<decimal> LaySoAsync(string khoa, decimal macDinh)
        {
            var s = await LayChuoiAsync(khoa);
            return decimal.TryParse(s, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : macDinh;
        }

        /// <summary>Tỷ lệ hoa hồng mặc định cho gian hàng mới (cấu hình HoaHong.MacDinh, 0..1).</summary>
        public static async Task<decimal> HoaHongMacDinhAsync(AppDbContext db)
        {
            try
            {
                var s = (await db.CauHinhHeThongs.AsNoTracking().FirstOrDefaultAsync(c => c.Khoa == "HoaHong.MacDinh"))?.GiaTri;
                return decimal.TryParse(s, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var v) && v >= 0 && v <= 1 ? v : 0.15m;
            }
            catch { return 0.15m; }
        }

        public async Task DatAsync(string khoa, string giaTri, int? maTK)
        {
            var c = await _db.CauHinhHeThongs.FirstOrDefaultAsync(x => x.Khoa == khoa);
            if (c == null) { c = new CauHinhHeThong { Khoa = khoa }; _db.CauHinhHeThongs.Add(c); }
            _db.CauHinhHeThongLichSus.Add(new CauHinhHeThongLichSu { Khoa = khoa, GiaTriCu = c.GiaTri == "" ? null : c.GiaTri, GiaTriMoi = giaTri, MaTK = maTK });
            c.GiaTri = giaTri; c.MaTKSua = maTK; c.NgaySua = DateTime.Now;
            await _db.SaveChangesAsync();
        }
    }
}
