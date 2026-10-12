using System.Text.Json;
using FoodApp.Maui.Models;

namespace FoodApp.Maui.Services;

/// <summary>
/// Tin nhắn giữa khách hàng và tài xế theo từng đơn hàng. Lưu trên máy (Preferences); khi có API thì đồng bộ qua
/// GET api/mobile/don-hang/{id}/tin-nhan và POST api/mobile/tin-nhan.
/// </summary>
public static class ChatService
{
    private const string Key = "tin-nhan";
    private const int ToiDa = 600;
    private static List<TinNhan>? _ds;

    private static List<TinNhan> Ds => _ds ??= Nap();

    private static List<TinNhan> Nap()
    {
        try { return JsonSerializer.Deserialize<List<TinNhan>>(Preferences.Default.Get(Key, "[]")) ?? new(); }
        catch { return new(); }
    }

    private static void Luu()
    {
        if (Ds.Count > ToiDa) Ds.RemoveRange(0, Ds.Count - ToiDa);
        Preferences.Default.Set(Key, JsonSerializer.Serialize(Ds));
    }

    public static List<TinNhan> Lay(string donId) =>
        Ds.Where(t => t.DonHangId == donId).OrderBy(t => t.ThoiGian).ToList();

    public static TinNhan? CuoiCung(string donId) =>
        Ds.Where(t => t.DonHangId == donId).OrderByDescending(t => t.ThoiGian).FirstOrDefault();

    public static async Task<TinNhan> GuiAsync(string donId, string nguoiGui, string tenNguoiGui, string noiDung)
    {
        var tn = new TinNhan { DonHangId = donId, NguoiGui = nguoiGui, TenNguoiGui = tenNguoiGui, NoiDung = noiDung.Trim() };
        Ds.Add(tn);
        Luu();
        if (DuLieuService.DangOnline)
            await ApiClient.PostAsync<object>("api/mobile/tin-nhan",
                new { id = tn.Id, donHangId = donId, nguoiGui, noiDung = tn.NoiDung });   // lỗi mạng: tin vẫn nằm trên máy
        return tn;
    }

    /// <summary>Kéo tin mới từ server (nếu có). Trả về true khi có tin mới.</summary>
    public static async Task<bool> DongBoAsync(string donId)
    {
        if (!DuLieuService.DangOnline) return false;
        var r = await ApiClient.GetAsync<List<TinNhan>>($"api/mobile/don-hang/{donId}/tin-nhan");
        if (!r.ThanhCong || r.DuLieu is null) return false;

        var moi = r.DuLieu.Where(x => Ds.All(d => d.Id != x.Id)).ToList();
        if (moi.Count == 0) return false;
        Ds.AddRange(moi);
        Luu();
        return true;
    }
}
