namespace FoodApp.Maui.Services;

/// <summary>Gọi điện bằng trình quay số của thiết bị. Gọi thoại ngay trong app (VoIP) cần dịch vụ máy chủ nên chưa làm.</summary>
public static class LienLac
{
    public static async Task GoiAsync(string? sdt)
    {
        var trang = Application.Current?.MainPage;
        if (string.IsNullOrWhiteSpace(sdt))
        {
            if (trang is not null) await trang.DisplayAlert("Chưa có số điện thoại", "Chưa có số điện thoại để gọi.", "Đóng");
            return;
        }
        sdt = sdt.Trim();

        try
        {
            if (PhoneDialer.Default.IsSupported)
            {
                PhoneDialer.Default.Open(sdt);
                return;
            }
        }
        catch (Exception) { /* thiết bị không cho mở: dùng cách sao chép bên dưới */ }

        await Clipboard.Default.SetTextAsync(sdt);
        if (trang is not null) await trang.DisplayAlert("Gọi điện", $"Thiết bị này không gọi điện được. Đã sao chép số {sdt}.", "OK");
    }
}
