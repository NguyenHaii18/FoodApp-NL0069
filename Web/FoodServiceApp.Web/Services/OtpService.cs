using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using FoodServiceApp.Web.Data;
using FoodServiceApp.Web.Models.Entities;

namespace FoodServiceApp.Web.Services
{
    /// <summary>
    /// Sinh/kiểm tra OTP 6 số (hiệu lực 5 phút, tối đa 5 lần nhập sai, tối thiểu 30 giây giữa 2 lần gửi).
    /// Gửi email qua SMTP nếu cấu hình mục "Smtp" trong appsettings.json; nếu chưa cấu hình thì ghi mã ra log
    /// và (khi Otp:HienMaDev = true) trả mã về để demo không cần mail server.
    /// </summary>
    public class OtpService
    {
        private readonly AppDbContext _db;
        private readonly IConfiguration _cfg;
        private readonly ILogger<OtpService> _log;

        public OtpService(AppDbContext db, IConfiguration cfg, ILogger<OtpService> log)
        {
            _db = db; _cfg = cfg; _log = log;
        }

        public bool HienMaDev => _cfg.GetValue<bool?>("Otp:HienMaDev") ?? true;

        private string Bam(string nguoiNhan, string mucDich, string otp)
        {
            var key = Encoding.UTF8.GetBytes(_cfg["Jwt:Key"] ?? "otp-key");
            using var h = new HMACSHA256(key);
            return Convert.ToHexString(h.ComputeHash(Encoding.UTF8.GetBytes($"{nguoiNhan}|{mucDich}|{otp}"))).ToLowerInvariant();
        }

        /// <summary>Tạo OTP và gửi. Trả (thanhCong, thongDiep, maDev).</summary>
        public async Task<(bool ok, string msg, string? maDev)> GuiAsync(string mucDich, string kenh, string nguoiNhan, int? maTK)
        {
            var gan = await _db.OtpXacThucs
                .Where(o => o.NguoiNhan == nguoiNhan && o.MucDich == mucDich)
                .OrderByDescending(o => o.NgayTao).FirstOrDefaultAsync();
            if (gan != null && (DateTime.Now - gan.NgayTao).TotalSeconds < 30)
                return (false, "Vui lòng đợi 30 giây trước khi gửi lại mã.", null);

            var otp = RandomNumberGenerator.GetInt32(0, 1000000).ToString("D6");
            _db.OtpXacThucs.Add(new OtpXacThuc
            {
                MucDich = mucDich, Kenh = kenh, NguoiNhan = nguoiNhan, MaTK = maTK,
                OtpHash = Bam(nguoiNhan, mucDich, otp),
                NgayTao = DateTime.Now, HetHan = DateTime.Now.AddMinutes(5),
            });
            await _db.SaveChangesAsync();

            var daGui = await GuiMailAsync(kenh, nguoiNhan, otp);
            if (!daGui) _log.LogWarning("[OTP DEMO] {Kenh} {NguoiNhan} -> {Otp} ({MucDich})", kenh, nguoiNhan, otp, mucDich);
            return (true, daGui ? "Đã gửi mã xác thực." : "Đã tạo mã xác thực (chưa cấu hình SMTP/SMS nên mã được hiển thị để demo).",
                    daGui || !HienMaDev ? null : otp);
        }

        /// <summary>Kiểm tra mã; đúng thì đánh dấu đã dùng.</summary>
        public async Task<(bool ok, string msg)> KiemTraAsync(string mucDich, string nguoiNhan, string? otp)
        {
            var o = await _db.OtpXacThucs
                .Where(x => x.NguoiNhan == nguoiNhan && x.MucDich == mucDich && !x.DaDung)
                .OrderByDescending(x => x.NgayTao).FirstOrDefaultAsync();
            if (o == null) return (false, "Chưa có mã xác thực, hãy yêu cầu gửi mã.");
            if (o.HetHan < DateTime.Now) return (false, "Mã đã hết hạn, hãy gửi lại.");
            if (o.SoLanSai >= 5) return (false, "Nhập sai quá 5 lần, hãy gửi lại mã mới.");
            if (!CryptographicOperations.FixedTimeEquals(
                    Encoding.ASCII.GetBytes(o.OtpHash), Encoding.ASCII.GetBytes(Bam(nguoiNhan, mucDich, (otp ?? "").Trim()))))
            {
                o.SoLanSai++;
                await _db.SaveChangesAsync();
                return (false, "Mã xác thực không đúng.");
            }
            o.DaDung = true;
            await _db.SaveChangesAsync();
            return (true, "OK");
        }

        private async Task<bool> GuiMailAsync(string kenh, string nguoiNhan, string otp)
        {
            var host = _cfg["Smtp:Host"];
            if (kenh != "email" || string.IsNullOrWhiteSpace(host) || !nguoiNhan.Contains('@')) return false;
            try
            {
                using var client = new System.Net.Mail.SmtpClient(host, _cfg.GetValue<int?>("Smtp:Port") ?? 587)
                {
                    EnableSsl = _cfg.GetValue<bool?>("Smtp:Ssl") ?? true,
                    Credentials = new System.Net.NetworkCredential(_cfg["Smtp:User"], _cfg["Smtp:Password"]),
                };
                var from = _cfg["Smtp:From"] ?? _cfg["Smtp:User"] ?? "no-reply@foodservice.local";
                using var msg = new System.Net.Mail.MailMessage(from, nguoiNhan, "Mã xác thực FoodService",
                    $"Mã xác thực của bạn là {otp}. Mã có hiệu lực 5 phút, không chia sẻ mã này cho bất kỳ ai.");
                await client.SendMailAsync(msg);
                return true;
            }
            catch (Exception ex) { _log.LogError(ex, "Gửi email OTP thất bại"); return false; }
        }
    }
}
