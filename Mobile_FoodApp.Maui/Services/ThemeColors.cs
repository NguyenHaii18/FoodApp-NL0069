namespace FoodApp.Maui.Services;

/// <summary>Màu chủ đề (Tím / Xanh lá / Cam / Xanh dương). Ghi đè các khóa màu trong Application.Resources.</summary>
public static class ThemeColors
{
    private const string Key = "mau-chu-de";

    public sealed record Bang(string Ten, string Icon, string Primary, string Dark, string Light, string Soft, string Accent, string Nen, string Vien);

    public static readonly Bang[] DanhSach =
    {
        new("Tím",      "🟣", "#6D28D9", "#5B21B6", "#A78BFA", "#F3EEFF", "#8B5CF6", "#FAF8FF", "#E4DAF7"),
        new("Xanh lá",  "🟢", "#00995E", "#007A4A", "#5FD6A0", "#E8F8F0", "#12B76A", "#F4FAF7", "#D5EBDF"),
        new("Cam",      "🟠", "#EA580C", "#C2410C", "#FDBA74", "#FFF1E6", "#F97316", "#FFFAF5", "#F8DEC9"),
        new("Xanh dương","🔵", "#1D4ED8", "#1E40AF", "#93C5FD", "#EAF1FF", "#3B82F6", "#F7FAFF", "#D6E2F7"),
    };

    public static string Hien => Preferences.Default.Get(Key, "Tím");

    public static void Dat(string ten) => Preferences.Default.Set(Key, ten);

    /// <summary>Áp màu đã chọn vào tài nguyên của app. Gọi trước khi tạo AppShell.</summary>
    public static void Ap()
    {
        if (Application.Current is null) return;
        var b = DanhSach.FirstOrDefault(x => x.Ten == Hien) ?? DanhSach[0];
        var r = Application.Current.Resources;
        r["Primary"] = Color.FromArgb(b.Primary);
        r["PrimaryDark"] = Color.FromArgb(b.Dark);
        r["PrimaryLight"] = Color.FromArgb(b.Light);
        r["PrimarySoft"] = Color.FromArgb(b.Soft);
        r["Accent"] = Color.FromArgb(b.Accent);
        r["Background"] = Color.FromArgb(b.Nen);
        r["BorderColor"] = Color.FromArgb(b.Vien);
    }
}
