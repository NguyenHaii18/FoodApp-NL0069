using System.Text.Json;
using FoodApp.Maui.Models;

namespace FoodApp.Maui.Services;

/// <summary>Danh sách tài khoản quản trị (Admin tổng / Nhân viên hỗ trợ), lưu trong Preferences.</summary>
public static class QuanTriStore
{
    private const string Key = "quan-tri-v1";

    public static void Nap()
    {
        try
        {
            var json = Preferences.Default.Get(Key, "");
            if (!string.IsNullOrEmpty(json))
            {
                var ds = JsonSerializer.Deserialize<List<QuanTriVien>>(json);
                if (ds is { Count: > 0 })
                {
                    MockData.DanhSachQuanTri.Clear();
                    MockData.DanhSachQuanTri.AddRange(ds);
                }
            }
        }
        catch (JsonException) { Preferences.Default.Remove(Key); }
        TaoMacDinhNeuTrong();
    }

    /// <summary>Tạo sẵn tài khoản mẫu: Admin tổng và một nhân viên hỗ trợ (đổi mật khẩu sau khi dùng thật).</summary>
    public static void TaoMacDinhNeuTrong()
    {
        if (MockData.DanhSachQuanTri.Count > 0) return;
        Them("Quản trị hệ thống", "admin@fooddelivery.vn", "admin123", VaiTroQuanTri.AdminTong, luu: false);
        Them("Nhân viên hỗ trợ", "hotro@fooddelivery.vn", "hotro123", VaiTroQuanTri.NhanVienHoTro, luu: false);
        Luu();
    }

    public static void Luu() =>
        Preferences.Default.Set(Key, JsonSerializer.Serialize(MockData.DanhSachQuanTri));

    public static QuanTriVien Them(string hoTen, string email, string matKhau, VaiTroQuanTri vaiTro, bool luu = true)
    {
        var q = new QuanTriVien
        {
            Id = IdHelper.TiepTheo("qt", MockData.DanhSachQuanTri.Select(x => x.Id)),
            HoTen = hoTen.Trim(), Email = email.Trim(), VaiTro = vaiTro, NgayTao = DateTime.Now,
        };
        DatMatKhau(q, matKhau, luu: false);
        MockData.DanhSachQuanTri.Add(q);
        if (luu) Luu();
        return q;
    }

    public static QuanTriVien? TimTheoEmail(string? email) =>
        string.IsNullOrWhiteSpace(email) ? null
        : MockData.DanhSachQuanTri.FirstOrDefault(x => string.Equals(x.Email, email.Trim(), StringComparison.OrdinalIgnoreCase));

    public static void DatMatKhau(QuanTriVien q, string matKhau, bool luu = true)
    {
        q.Muoi = MatKhauHelper.TaoMuoi();
        q.MatKhauBam = MatKhauHelper.Bam(matKhau, q.Muoi);
        if (luu) Luu();
    }

    public static bool KiemTraMatKhau(QuanTriVien q, string matKhau) => MatKhauHelper.Kiem(matKhau, q.Muoi, q.MatKhauBam);

    /// <summary>Số Admin tổng còn hoạt động — luôn phải ≥ 1 để không tự khóa hệ thống.</summary>
    public static int SoAdminTongHoatDong =>
        MockData.DanhSachQuanTri.Count(x => x.VaiTro == VaiTroQuanTri.AdminTong && !x.TaiKhoanBiKhoa);
}
