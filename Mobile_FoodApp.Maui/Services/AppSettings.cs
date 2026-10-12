using System.Text.Json;

namespace FoodApp.Maui.Services;

/// <summary>Đọc Resources/Raw/appsettings.json (địa chỉ API) khi app khởi động.</summary>
public static class AppSettings
{
    private sealed class Root { public ApiSection Api { get; set; } = new(); }
    public sealed class ApiSection
    {
        public string BaseUrlWindows { get; set; } = "https://localhost:5001";
        public string BaseUrlAndroidEmulator { get; set; } = "http://10.0.2.2:5000";
        public string BaseUrlThietBiThat { get; set; } = "";
        public bool DungThietBiThat { get; set; }
        public int TimeoutGiay { get; set; } = 15;
        public bool ChoPhepChungChiTuKy { get; set; } = true;
    }

    public static ApiSection Api { get; private set; } = new();

    /// <summary>Địa chỉ máy chủ do người dùng nhập ở Cài đặt (ưu tiên hơn appsettings.json). Rỗng = dùng mặc định.</summary>
    public static string BaseUrlTuyChinh
    {
        get => Preferences.Default.Get("api-base-url", "");
        set => Preferences.Default.Set("api-base-url", (value ?? "").Trim());
    }

    public static string BaseUrl => !string.IsNullOrWhiteSpace(BaseUrlTuyChinh) ? BaseUrlTuyChinh : BaseUrlMacDinh;

    public static string BaseUrlMacDinh =>
        DeviceInfo.Platform == DevicePlatform.Android
            ? (Api.DungThietBiThat && !string.IsNullOrWhiteSpace(Api.BaseUrlThietBiThat)
                ? Api.BaseUrlThietBiThat : Api.BaseUrlAndroidEmulator)
            : Api.BaseUrlWindows;

    public static async Task NapAsync()
    {
        try
        {
            using var stream = await FileSystem.OpenAppPackageFileAsync("appsettings.json");
            var root = await JsonSerializer.DeserializeAsync<Root>(stream,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (root is not null) Api = root.Api;
        }
        catch { /* thiếu file: dùng giá trị mặc định */ }
    }
}
