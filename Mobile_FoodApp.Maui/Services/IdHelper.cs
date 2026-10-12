namespace FoodApp.Maui.Services;

/// <summary>Sinh Id dạng "tiềnTố + số" không bao giờ trùng, kể cả sau khi đã xóa bản ghi (Count + 1 thì có thể trùng).</summary>
public static class IdHelper
{
    public static string TiepTheo(string tienTo, IEnumerable<string?> dsId)
    {
        int max = 0;
        foreach (var id in dsId)
        {
            if (id is null || !id.StartsWith(tienTo, StringComparison.OrdinalIgnoreCase)) continue;
            if (int.TryParse(id.AsSpan(tienTo.Length), out var n) && n > max) max = n;
        }
        return tienTo + (max + 1);
    }
}
