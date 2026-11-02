namespace FoodApp.Maui.Models;

/// <summary>Tạo khóa ổn định để chỉ gộp các dòng có cùng món, cùng tập tùy chọn và cùng ghi chú.</summary>
public static class KhoaGioHang
{
    public static string Tao(string monAnId, IEnumerable<string> optionKeys, string? ghiChu)
    {
        var options = optionKeys
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase);
        var note = string.Join(' ', (ghiChu ?? "").Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)).ToUpperInvariant();
        return $"{monAnId}|{string.Join(',', options)}|{note}";
    }
}
