namespace FoodApp.Maui.Services;

/// <summary>Chế độ giao diện: "light" | "dark" | "auto" (theo hệ thống). Lưu trong Preferences.</summary>
public static class ThemeService
{
    private const string Key = "giao-dien";

    public static string Hien => Preferences.Default.Get(Key, "auto");

    public static void Dat(string che)
    {
        Preferences.Default.Set(Key, che);
        Ap();
    }

    public static void Ap()
    {
        if (Application.Current is null) return;
        Application.Current.UserAppTheme = Hien switch
        {
            "light" => AppTheme.Light,
            "dark" => AppTheme.Dark,
            _ => AppTheme.Unspecified,
        };
    }
}
