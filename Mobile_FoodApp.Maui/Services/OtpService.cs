using System.Security.Cryptography;

namespace FoodApp.Maui.Services;

/// <summary>
/// Mã OTP 6 số (hiệu lực 5 phút, tối đa 5 lần nhập sai, gửi lại sau 60 giây).
/// Online: server gửi email/SMS thật qua api/mobile/otp/gui và kiểm tra ở api/mobile/otp/xac-thuc.
/// Offline (demo): app tự sinh mã và HIỂN THỊ ngay trên màn hình vì không có dịch vụ gửi email/SMS.
/// </summary>
public static class OtpService
{
    public const int PhutHieuLuc = 5;
    public const int GiayChoGuiLai = 60;
    public const int ToiDaSai = 5;

    public const string XacThucEmail = "XacThucEmail";
    public const string DatLaiMatKhau = "DatLaiMatKhau";
    public const string DoiEmail = "DoiEmail";

    private sealed class Ma
    {
        public string Code { get; set; } = "";
        public DateTime HetHan { get; set; }
        public DateTime LanGuiCuoi { get; set; }
        public int SoLanSai { get; set; }
        public bool DoServer { get; set; }
    }

    private static readonly Dictionary<string, Ma> _ma = new();
    private static string Khoa(string mucDich, string nguoiNhan) => mucDich + "|" + nguoiNhan.Trim().ToLowerInvariant();

    public sealed record KetQuaGui(bool ThanhCong, string ThongDiep, string? MaDemo);

    public static int GiayConLaiDeGuiLai(string mucDich, string nguoiNhan) =>
        _ma.TryGetValue(Khoa(mucDich, nguoiNhan), out var m)
            ? Math.Max(0, GiayChoGuiLai - (int)(DateTime.Now - m.LanGuiCuoi).TotalSeconds) : 0;

    private sealed class OtpDto { public string? ThongDiep { get; set; } public string? MaDev { get; set; } }

    /// <summary>Gửi OTP. Online dùng API của Web: quên mật khẩu → tai-khoan/quen-mat-khau; xác thực email (đã đăng nhập) → tai-khoan/xac-thuc/gui-otp.</summary>
    public static async Task<KetQuaGui> GuiAsync(string mucDich, string nguoiNhan)
    {
        var k = Khoa(mucDich, nguoiNhan);
        var cho = GiayConLaiDeGuiLai(mucDich, nguoiNhan);
        if (cho > 0) return new(false, $"Vui lòng chờ {cho} giây để gửi lại mã.", null);

        if (DuLieuService.DangOnline)
        {
            if (mucDich == DoiEmail)
                return new(false, "Khi đang kết nối máy chủ, vui lòng đổi email trên website.", null);

            var r = mucDich == DatLaiMatKhau
                ? await ApiClient.PostAsync<OtpDto>("api/mobile/tai-khoan/quen-mat-khau", new { taiKhoan = nguoiNhan })
                : await ApiClient.PostAsync<OtpDto>("api/mobile/tai-khoan/xac-thuc/gui-otp", new { kenh = "email" });
            if (!r.ThanhCong) return new(false, r.ThongDiep, null);

            _ma[k] = new Ma { LanGuiCuoi = DateTime.Now, HetHan = DateTime.Now.AddMinutes(PhutHieuLuc), DoServer = true };
            // maDev chỉ có khi Web chưa cấu hình SMTP và bật Otp:HienMaDev (chế độ demo).
            var maDev = string.IsNullOrWhiteSpace(r.DuLieu?.MaDev) ? null : r.DuLieu!.MaDev;
            return new(true, r.DuLieu?.ThongDiep ?? $"Đã gửi mã OTP tới {nguoiNhan}.", maDev);
        }

        var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        _ma[k] = new Ma { Code = code, LanGuiCuoi = DateTime.Now, HetHan = DateTime.Now.AddMinutes(PhutHieuLuc) };
        return new(true, "Chế độ demo: không gửi email/SMS thật, mã hiển thị ngay bên dưới.", code);
    }

    public static async Task<(bool Ok, string ThongDiep)> KiemAsync(string mucDich, string nguoiNhan, string? ma)
    {
        ma = (ma ?? "").Trim();
        if (ma.Length != 6 || !ma.All(char.IsDigit)) return (false, "Mã OTP gồm 6 chữ số.");

        var k = Khoa(mucDich, nguoiNhan);
        if (!_ma.TryGetValue(k, out var m)) return (false, "Bạn chưa yêu cầu mã OTP. Hãy bấm Gửi mã.");
        if (DateTime.Now > m.HetHan) { _ma.Remove(k); return (false, "Mã OTP đã hết hạn. Hãy gửi lại mã mới."); }
        if (m.SoLanSai >= ToiDaSai) return (false, "Bạn nhập sai quá nhiều lần. Hãy gửi lại mã mới sau ít phút.");

        if (m.DoServer)
        {
            // Đặt lại mật khẩu: Web kiểm OTP cùng lúc với mật khẩu mới (tai-khoan/dat-lai-mat-khau), nên ở đây chỉ kiểm định dạng.
            if (mucDich == DatLaiMatKhau) return (true, "");

            var r = await ApiClient.PostAsync<object>("api/mobile/tai-khoan/xac-thuc/xac-nhan", new { kenh = "email", otp = ma });
            if (r.ThanhCong) { _ma.Remove(k); return (true, ""); }
            m.SoLanSai++;
            return (false, r.ThongDiep);
        }

        if (m.Code != ma)
        {
            m.SoLanSai++;
            return (false, $"Mã OTP không đúng. Còn {Math.Max(0, ToiDaSai - m.SoLanSai)} lần thử.");
        }
        _ma.Remove(k);
        return (true, "");
    }

    public sealed class TrangThaiXacThucDto { public bool EmailDaXacThuc { get; set; } public bool SdtDaXacThuc { get; set; } }
}
