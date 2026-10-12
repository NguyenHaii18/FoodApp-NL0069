using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FoodApp.Maui.Services;

public sealed record ApiKetQua<T>(bool ThanhCong, T? DuLieu, string ThongDiep, bool MatKetNoi = false);

/// <summary>Gọi Web API (FoodServiceApp.Web). Mọi lỗi mạng được gói lại, không ném exception ra màn hình.</summary>
public static class ApiClient
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(), new ChuoiLinhHoatConverter() }
    };

    private static HttpClient? _http;
    private static HttpClient Http => _http ??= Tao();

    public static string? Token { get; set; }

    /// <summary>Tạo lại HttpClient (sau khi đổi địa chỉ máy chủ).</summary>
    public static void TaiLai() { _http?.Dispose(); _http = null; }

    private static HttpClient Tao()
    {
        var handler = new HttpClientHandler();
#if DEBUG
        // Chứng chỉ https dev của ASP.NET không được máy ảo/Android tin cậy.
        if (AppSettings.Api.ChoPhepChungChiTuKy)
            handler.ServerCertificateCustomValidationCallback = (_, _, _, _) => true;
#endif
        var c = new HttpClient(handler)
        {
            BaseAddress = new Uri(AppSettings.BaseUrl.TrimEnd('/') + "/"),
            Timeout = TimeSpan.FromSeconds(AppSettings.Api.TimeoutGiay)
        };
        return c;
    }

    public static Task<ApiKetQua<T>> GetAsync<T>(string url) => GuiAsync<T>(HttpMethod.Get, url, null);
    public static Task<ApiKetQua<T>> PostAsync<T>(string url, object? body) => GuiAsync<T>(HttpMethod.Post, url, body);
    public static Task<ApiKetQua<T>> PutAsync<T>(string url, object? body) => GuiAsync<T>(HttpMethod.Put, url, body);
    public static Task<ApiKetQua<T>> DeleteAsync<T>(string url) => GuiAsync<T>(HttpMethod.Delete, url, null);

    private static async Task<ApiKetQua<T>> GuiAsync<T>(HttpMethod method, string url, object? body)
    {
        try
        {
            using var req = new HttpRequestMessage(method, url.TrimStart('/'));
            if (!string.IsNullOrEmpty(Token))
                req.Headers.Authorization = new("Bearer", Token);
            if (body is not null)
                req.Content = JsonContent.Create(body, options: Json);

            using var res = await Http.SendAsync(req);
            var text = await res.Content.ReadAsStringAsync();

            if (!res.IsSuccessStatusCode)
                return new(false, default, DocThongDiepLoi(text, (int)res.StatusCode));

            if (string.IsNullOrWhiteSpace(text) || typeof(T) == typeof(object))
                return new(true, default, "");

            return new(true, JsonSerializer.Deserialize<T>(text, Json), "");
        }
        catch (TaskCanceledException)
        {
            return new(false, default, "Máy chủ phản hồi quá lâu", true);
        }
        catch (HttpRequestException ex)
        {
            return new(false, default, "Không kết nối được máy chủ: " + ex.Message, true);
        }
        catch (JsonException)
        {
            return new(false, default, "Dữ liệu máy chủ trả về không đúng định dạng");
        }
    }

    private static string DocThongDiepLoi(string text, int status)
    {
        try
        {
            using var doc = JsonDocument.Parse(text);
            if (doc.RootElement.ValueKind == JsonValueKind.Object)
                foreach (var k in new[] { "thongDiep", "message", "title", "error" })
                    if (doc.RootElement.TryGetProperty(k, out var v) && v.ValueKind == JsonValueKind.String)
                        return v.GetString()!;
        }
        catch { }
        return status switch
        {
            401 => "Sai tài khoản hoặc mật khẩu",
            403 => "Bạn không có quyền thực hiện",
            404 => "Không tìm thấy dữ liệu",
            _ => $"Lỗi máy chủ ({status})"
        };
    }
}

/// <summary>SQL thường trả Id là số; model MAUI dùng string nên đọc cả hai.</summary>
public sealed class ChuoiLinhHoatConverter : JsonConverter<string>
{
    public override string? Read(ref Utf8JsonReader r, Type t, JsonSerializerOptions o) => r.TokenType switch
    {
        JsonTokenType.String => r.GetString(),
        JsonTokenType.Number => r.TryGetInt64(out var l) ? l.ToString() : r.GetDouble().ToString(),
        JsonTokenType.True => "true",
        JsonTokenType.False => "false",
        _ => null
    };
    public override void Write(Utf8JsonWriter w, string v, JsonSerializerOptions o) => w.WriteStringValue(v);
}
