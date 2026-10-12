using System.Text.Json;
using FoodApp.Maui.Models;

namespace FoodApp.Maui.Services;

public class DiaChiLuu
{
    public string Nhan { get; set; } = "Nhà";
    public string DiaChi { get; set; } = "";
    public bool MacDinh { get; set; }
}

/// <summary>Sổ địa chỉ, quán yêu thích, mã giới thiệu của khách đang đăng nhập (lưu theo email trong Preferences).</summary>
public static class TaiKhoanService
{
    private static string Khoa(string loai) => $"{loai}:{Phien.Khach?.Email?.ToLowerInvariant() ?? "khach"}";

    // ---------- Sổ địa chỉ ----------
    public static List<DiaChiLuu> LayDiaChi()
    {
        List<DiaChiLuu> ds;
        try { ds = JsonSerializer.Deserialize<List<DiaChiLuu>>(Preferences.Default.Get(Khoa("so-dia-chi"), "[]")) ?? new(); }
        catch { ds = new(); }

        // lần đầu: đưa địa chỉ nhập lúc đăng ký vào sổ
        if (ds.Count == 0 && !string.IsNullOrWhiteSpace(Phien.Khach?.DiaChi))
        {
            ds.Add(new DiaChiLuu { Nhan = "Nhà", DiaChi = Phien.Khach!.DiaChi, MacDinh = true });
            LuuDiaChi(ds);
        }
        return ds;
    }

    public static void LuuDiaChi(List<DiaChiLuu> ds)
    {
        if (ds.Count > 0 && !ds.Any(d => d.MacDinh)) ds[0].MacDinh = true;
        Preferences.Default.Set(Khoa("so-dia-chi"), JsonSerializer.Serialize(ds));
        // địa chỉ mặc định = địa chỉ hiển thị ở trang chủ và tự điền khi thanh toán
        if (Phien.Khach is not null)
        {
            Phien.Khach.DiaChi = ds.FirstOrDefault(d => d.MacDinh)?.DiaChi ?? "";
            VendorLocalStore.Luu();
        }
    }

    // ---------- Quán yêu thích ----------
    public static HashSet<string> LayYeuThich()
    {
        try { return JsonSerializer.Deserialize<HashSet<string>>(Preferences.Default.Get(Khoa("yeu-thich"), "[]")) ?? new(); }
        catch { return new(); }
    }

    public static bool LaYeuThich(string gianHangId) => LayYeuThich().Contains(gianHangId);

    /// <summary>Đảo trạng thái yêu thích; trả về true nếu giờ đang là yêu thích.</summary>
    public static bool DaoYeuThich(string gianHangId)
    {
        var ds = LayYeuThich();
        var gio = ds.Add(gianHangId);
        if (!gio) ds.Remove(gianHangId);
        Preferences.Default.Set(Khoa("yeu-thich"), JsonSerializer.Serialize(ds));
        return gio;
    }

    // ---------- Giới thiệu bạn bè ----------
    public static string MaGioiThieu
    {
        get
        {
            var goc = (Phien.Khach?.Email ?? Phien.Khach?.Id ?? "KHACH").ToUpperInvariant();
            var h = 0;
            foreach (var c in goc) h = unchecked(h * 31 + c);
            return "FOOD" + Math.Abs(h % 1_000_000).ToString("D6");
        }
    }
}
