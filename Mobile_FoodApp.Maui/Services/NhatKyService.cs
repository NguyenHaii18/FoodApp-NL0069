using System.Text.Json;
using FoodApp.Maui.Models;

namespace FoodApp.Maui.Services;

/// <summary>
/// Nhật ký thao tác: mọi hành động quản trị quan trọng + đăng nhập / đăng xuất / đổi mật khẩu / xóa tài khoản.
/// Lưu trên máy (Preferences), tối đa <see cref="ToiDa"/> dòng mới nhất. Khi có server: POST api/mobile/nhat-ky (tùy chọn).
/// </summary>
public static class NhatKyService
{
    private const string Key = "nhat-ky-v1";
    public const int ToiDa = 1500;
    private static List<NhatKyThaoTac>? _ds;

    private static List<NhatKyThaoTac> Ds => _ds ??= Nap();

    private static List<NhatKyThaoTac> Nap()
    {
        try { return JsonSerializer.Deserialize<List<NhatKyThaoTac>>(Preferences.Default.Get(Key, "[]")) ?? new(); }
        catch (JsonException) { return new(); }
    }

    private static void Luu()
    {
        if (Ds.Count > ToiDa) Ds.RemoveRange(0, Ds.Count - ToiDa);
        Preferences.Default.Set(Key, JsonSerializer.Serialize(Ds));
    }

    public static void Ghi(string vaiTro, string taiKhoan, string hanhDong, string doiTuong = "", string chiTiet = "", bool thanhCong = true)
    {
        Ds.Add(new NhatKyThaoTac
        {
            VaiTro = vaiTro, TaiKhoan = taiKhoan, HanhDong = hanhDong, DoiTuong = doiTuong, ChiTiet = chiTiet, ThanhCong = thanhCong,
        });
        Luu();
    }

    /// <summary>Ghi thao tác của quản trị viên đang đăng nhập.</summary>
    public static void GhiQuanTri(string hanhDong, string doiTuong = "", string chiTiet = "", bool thanhCong = true)
    {
        var q = MockData.QuanTriVienHienTai;
        Ghi("QuanTri:" + q.VaiTro, q.Email, hanhDong, doiTuong, chiTiet, thanhCong);
    }

    /// <summary>Mô tả thiết bị đăng nhập, ví dụ "Windows · Desktop".</summary>
    public static string ThietBi() => $"{DeviceInfo.Platform} · {DeviceInfo.Model}";

    private sealed class DongNhatKyServer
    {
        public DateTime ThoiGian { get; set; }
        public string? MucDo { get; set; }
        public string? Nguon { get; set; }
        public string? ThongDiep { get; set; }
        public int? MaTK { get; set; }
        public string? DiaChiIP { get; set; }
        public string? PhuongThuc { get; set; }
        public string? DuongDan { get; set; }
        public int? MaTrangThai { get; set; }
    }

    /// <summary>Online (Admin tổng): lấy nhật ký từ Web — GET api/mobile/admin/nhat-ky. Trả null nếu không lấy được.</summary>
    public static async Task<List<NhatKyThaoTac>?> TaiTuServerAsync()
    {
        if (!DuLieuService.DangOnline) return null;
        var r = await ApiClient.GetAsync<List<DongNhatKyServer>>("api/mobile/admin/nhat-ky?trang=1&kichThuoc=300");
        if (!r.ThanhCong || r.DuLieu is null) return null;
        return r.DuLieu.Select(d => new NhatKyThaoTac
        {
            ThoiGian = d.ThoiGian,
            VaiTro = "Server:" + (d.Nguon ?? ""),
            TaiKhoan = d.MaTK?.ToString() ?? "",
            HanhDong = d.ThongDiep ?? "",
            ChiTiet = $"{d.PhuongThuc} {d.DuongDan} → {d.MaTrangThai}  ·  IP {d.DiaChiIP}".Trim(),
            ThanhCong = d.MucDo is null or "Info",
        }).OrderByDescending(x => x.ThoiGian).ToList();
    }

    public static List<NhatKyThaoTac> TatCa() => Ds.OrderByDescending(x => x.ThoiGian).ToList();

    /// <summary>Lịch sử đăng nhập (thành công và thất bại) của một tài khoản.</summary>
    public static List<NhatKyThaoTac> LichSuDangNhap(string vaiTro, string taiKhoan) =>
        Ds.Where(x => x.VaiTro == vaiTro && x.HanhDong.StartsWith("Đăng nhập")
                      && string.Equals(x.TaiKhoan, taiKhoan, StringComparison.OrdinalIgnoreCase))
          .OrderByDescending(x => x.ThoiGian).Take(100).ToList();

    public static void Xoa()
    {
        Ds.Clear();
        Luu();
    }
}
