using System.Security.Cryptography;
using System.Text.Json;
using FoodApp.Maui.Models;

namespace FoodApp.Maui.Services;

/// <summary>Lưu danh sách hồ sơ tài xế (kèm trạng thái xét duyệt, mật khẩu băm, ảnh) trong Preferences.</summary>
public static class TaiXeStore
{
    private const string Key = "ds-tai-xe";
    private const string KeyCu = "ho-so-tai-xe";   // bản cũ chỉ lưu MỘT tài xế

    private sealed class HoSoCu
    {
        public string HoTen { get; set; } = "";
        public string SoDienThoai { get; set; } = "";
        public string BienSoXe { get; set; } = "";
        public string LoaiXe { get; set; } = "Xe máy";
        public string Anh { get; set; } = "🛵";
        public string AnhPath { get; set; } = "";
        public string Muoi { get; set; } = "";
        public string MatKhauBam { get; set; } = "";
    }

    public static void Nap()
    {
        try
        {
            var json = Preferences.Default.Get(Key, "");
            if (!string.IsNullOrEmpty(json))
            {
                var ds = JsonSerializer.Deserialize<List<TaiXe>>(json) ?? new();
                MockData.DanhSachTaiXe.Clear();
                MockData.DanhSachTaiXe.AddRange(ds);
                return;
            }

            // Chuyển hồ sơ tài xế của bản trước sang danh sách mới (đã duyệt sẵn để không bị khóa ngoài)
            var cu = Preferences.Default.Get(KeyCu, "");
            if (string.IsNullOrEmpty(cu)) return;
            var d = JsonSerializer.Deserialize<HoSoCu>(cu);
            if (d is null || string.IsNullOrWhiteSpace(d.SoDienThoai)) return;
            MockData.DanhSachTaiXe.Add(new TaiXe
            {
                Id = "tx", HoTen = string.IsNullOrWhiteSpace(d.HoTen) ? "Tài xế" : d.HoTen,
                SoDienThoai = d.SoDienThoai, BienSoXe = d.BienSoXe, LoaiXe = d.LoaiXe, Anh = d.Anh, AnhPath = d.AnhPath,
                Muoi = d.Muoi, MatKhauBam = d.MatKhauBam, TrangThaiDangKy = TrangThaiDangKyGianHang.DaDuyet,
            });
            Luu();
        }
        catch { /* dữ liệu hỏng: bắt đầu với danh sách trống */ }
    }

    public static void Luu() =>
        Preferences.Default.Set(Key, JsonSerializer.Serialize(MockData.DanhSachTaiXe));

    public static TaiXe? TimTheoSdt(string? sdt)
    {
        var s = ChuanHoaSdt(sdt);
        return s.Length == 0 ? null : MockData.DanhSachTaiXe.FirstOrDefault(t => ChuanHoaSdt(t.SoDienThoai) == s);
    }

    /// <summary>Đăng ký hồ sơ mới: trạng thái Chờ duyệt, chưa đăng nhập được cho tới khi quản trị viên duyệt.</summary>
    public static TaiXe DangKy(TaiXe t, string matKhau)
    {
        t.Id = "tx" + DateTime.Now.ToString("yyMMddHHmmss");
        t.TrangThaiDangKy = TrangThaiDangKyGianHang.ChoDuyet;
        t.NgayDangKy = DateTime.Now;
        DatMatKhau(t, matKhau, luu: false);
        MockData.DanhSachTaiXe.Add(t);
        Luu();
        return t;
    }

    // ---------- Mật khẩu ----------
    private static string Bam(string matKhau, string muoi) =>
        Convert.ToBase64String(Rfc2898DeriveBytes.Pbkdf2(
            matKhau, Convert.FromBase64String(muoi), 100_000, HashAlgorithmName.SHA256, 32));

    public static bool CoMatKhau(TaiXe t) => !string.IsNullOrEmpty(t.MatKhauBam);

    public static void DatMatKhau(TaiXe t, string matKhau, bool luu = true)
    {
        t.Muoi = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));
        t.MatKhauBam = Bam(matKhau, t.Muoi);
        if (luu) Luu();
    }

    public static bool KiemTraMatKhau(TaiXe t, string matKhau) =>
        CoMatKhau(t) && CryptographicOperations.FixedTimeEquals(
            Convert.FromBase64String(Bam(matKhau, t.Muoi)), Convert.FromBase64String(t.MatKhauBam));

    /// <summary>Chuẩn hóa SĐT: chỉ giữ chữ số, đổi +84 thành 0.</summary>
    public static string ChuanHoaSdt(string? sdt)
    {
        var d = new string((sdt ?? "").Where(char.IsDigit).ToArray());
        return d.StartsWith("84") && d.Length >= 11 ? "0" + d[2..] : d;
    }
}

public class DangNhapTaiXeDto { public string? Token { get; set; } }
