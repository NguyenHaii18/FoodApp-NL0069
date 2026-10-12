using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using FoodServiceApp.Web.Data;
using FoodServiceApp.Web.Models.Entities;

namespace FoodServiceApp.Web.Services
{
    /// <summary>Ghi nhật ký (đăng nhập, thao tác, OTP...) vào bảng NhatKyHeThong. Không bao giờ ném lỗi ra ngoài.</summary>
    public static class NhatKy
    {
        public static async Task GhiAsync(AppDbContext db, HttpContext? http, string nguon, string thongDiep,
            int? maTK = null, string mucDo = "Info", int? maTrangThai = null, int? ms = null)
        {
            Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<NhatKyHeThong>? entry = null;
            try
            {
                entry = db.NhatKyHeThongs.Add(new NhatKyHeThong
                {
                    ThoiGian = DateTime.Now,
                    MucDo = mucDo,
                    Nguon = nguon,
                    ThongDiep = thongDiep.Length > 480 ? thongDiep[..480] : thongDiep,
                    MaTK = maTK,
                    DiaChiIP = http?.Connection.RemoteIpAddress?.ToString(),
                    PhuongThuc = http?.Request.Method,
                    DuongDan = http == null ? null : (http.Request.Path.Value is { Length: > 300 } p ? p[..300] : http.Request.Path.Value),
                    MaTrangThai = maTrangThai,
                    ThoiGianMs = ms,
                });
                await db.SaveChangesAsync();
            }
            catch
            {
                // nhật ký lỗi (vd chưa chạy NangCap_v9) không được làm hỏng nghiệp vụ: bỏ bản ghi khỏi context
                if (entry != null) entry.State = EntityState.Detached;
            }
        }
    }

    /// <summary>
    /// Kiểm tra mật khẩu khi ĐĂNG NHẬP, kèm: khóa tạm 15 phút sau 5 lần sai liên tiếp, ghi lịch sử đăng nhập,
    /// cập nhật LanDangNhapCuoi. Dùng thay cho MatKhauHelper.Khop ở các điểm đăng nhập.
    /// </summary>
    public static class BaoMat
    {
        public const int SoLanSaiToiDa = 5;
        public const int PhutKhoa = 15;

        public static async Task<bool> KhopAsync(AppDbContext db, HttpContext? http, string? matKhauNhap, TaiKhoan? tk)
        {
            if (tk == null) return false;

            if (tk.KhoaDen != null && tk.KhoaDen > DateTime.Now)
            {
                await NhatKy.GhiAsync(db, http, "Auth", "Đăng nhập bị từ chối: tài khoản đang bị khóa tạm", tk.MaTK, "Warn");
                return false;
            }

            if (MatKhauHelper.Khop(matKhauNhap ?? "", tk.MatKhau))
            {
                tk.SoLanDangNhapSai = 0;
                tk.KhoaDen = null;
                tk.LanDangNhapCuoi = DateTime.Now;
                try { await db.SaveChangesAsync(); } catch { }
                await NhatKy.GhiAsync(db, http, "Auth", $"Đăng nhập thành công ({tk.VaiTro})", tk.MaTK);
                return true;
            }

            tk.SoLanDangNhapSai++;
            var khoa = tk.SoLanDangNhapSai >= SoLanSaiToiDa;
            if (khoa) { tk.KhoaDen = DateTime.Now.AddMinutes(PhutKhoa); tk.SoLanDangNhapSai = 0; }
            try { await db.SaveChangesAsync(); } catch { }
            await NhatKy.GhiAsync(db, http, "Auth",
                khoa ? $"Sai mật khẩu quá {SoLanSaiToiDa} lần -> khóa tạm {PhutKhoa} phút" : "Đăng nhập sai mật khẩu",
                tk.MaTK, "Warn");
            return false;
        }

        public static string? ThongBaoKhoa(TaiKhoan? tk) =>
            tk?.KhoaDen != null && tk.KhoaDen > DateTime.Now
                ? $"Tài khoản tạm khóa do nhập sai nhiều lần, thử lại sau {(int)Math.Ceiling((tk.KhoaDen.Value - DateTime.Now).TotalMinutes)} phút."
                : null;
    }
}
