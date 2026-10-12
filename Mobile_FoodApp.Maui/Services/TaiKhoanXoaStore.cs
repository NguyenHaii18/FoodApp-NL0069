using System.Text.Json;
using FoodApp.Maui.Models;

namespace FoodApp.Maui.Services;

/// <summary>Ghi nhớ các tài khoản khách đã xóa để dữ liệu mẫu không làm chúng "sống lại" khi mở lại app.</summary>
public static class TaiKhoanXoaStore
{
    private const string Key = "kh-da-xoa-v1";

    private static List<string> Ds()
    {
        try { return JsonSerializer.Deserialize<List<string>>(Preferences.Default.Get(Key, "[]")) ?? new(); }
        catch (JsonException) { return new(); }
    }

    public static void DanhDau(string email)
    {
        var d = Ds();
        if (!d.Contains(email.Trim().ToLowerInvariant())) d.Add(email.Trim().ToLowerInvariant());
        Preferences.Default.Set(Key, JsonSerializer.Serialize(d));
    }

    /// <summary>Đăng ký lại bằng email đã xóa thì bỏ đánh dấu.</summary>
    public static void BoDanhDau(string email)
    {
        var d = Ds();
        if (d.Remove(email.Trim().ToLowerInvariant())) Preferences.Default.Set(Key, JsonSerializer.Serialize(d));
    }

    public static void ApDung()
    {
        var d = Ds();
        if (d.Count == 0) return;
        foreach (var k in MockData.DanhSachKhachHang.Where(k => d.Contains(k.Email.Trim().ToLowerInvariant())).ToList())
            MockData.DanhSachKhachHang.Remove(k);
    }
}
