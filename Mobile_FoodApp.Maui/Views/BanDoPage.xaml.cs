using System.Globalization;
using System.Text.Json;

namespace FoodApp.Maui.Views;

public class DiaDiem
{
    public string Ten { get; set; } = "";
    public double Lat { get; set; }
    public double Lng { get; set; }
}

/// <summary>
/// Bản đồ OpenStreetMap (Leaflet trong WebView, chạy được cả Windows và Android) + tìm kiếm địa điểm bằng Nominatim.
/// Cần Internet. Không dùng khóa API.
/// </summary>
[QueryProperty(nameof(DiaChi), "DiaChi")]
[QueryProperty(nameof(ChonDiaChi), "ChonDiaChi")]
public partial class BanDoPage : ContentPage
{
    /// <summary>Gọi khi người dùng bấm "Dùng địa chỉ này" (chế độ chọn địa chỉ).</summary>
    public static Action<string>? KhiChon;

    public string DiaChi { get; set; } = "";
    public bool ChonDiaChi { get; set; }

    private static readonly HttpClient Http = TaoHttp();
    private DiaDiem? _chon;
    private DiaDiem? _cho;       // điểm chờ vẽ khi trang bản đồ tải xong
    private bool _sanSang;
    private bool _daTimLanDau;

    private static HttpClient TaoHttp()
    {
        var c = new HttpClient { Timeout = TimeSpan.FromSeconds(12) };
        c.DefaultRequestHeaders.UserAgent.ParseAdd("FoodApp.Maui/1.0 (do-an-mon-hoc)");   // Nominatim yêu cầu User-Agent
        c.DefaultRequestHeaders.AcceptLanguage.ParseAdd("vi");
        return c;
    }

    private const string Html = """
<!DOCTYPE html><html><head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1">
<link rel="stylesheet" href="https://unpkg.com/leaflet@1.9.4/dist/leaflet.css"/>
<script src="https://unpkg.com/leaflet@1.9.4/dist/leaflet.js"></script>
<style>html,body,#map{height:100%;margin:0;background:#eee}</style></head><body><div id="map"></div>
<script>
var map = L.map('map').setView([10.7769, 106.7009], 13);
L.tileLayer('https://tile.openstreetmap.org/{z}/{x}/{y}.png', {maxZoom: 19, attribution: '&copy; OpenStreetMap'}).addTo(map);
var marker = null;
function datViTri(lat, lng, nhan) {
  if (marker) map.removeLayer(marker);
  marker = L.marker([lat, lng]).addTo(map).bindPopup(nhan).openPopup();
  map.setView([lat, lng], 17);
}
map.on('click', function(e) { window.location.href = 'foodapp://pick?lat=' + e.latlng.lat + '&lng=' + e.latlng.lng; });
</script></body></html>
""";

    public BanDoPage()
    {
        InitializeComponent();
        Wv.Source = new HtmlWebViewSource { Html = Html };
        Wv.Navigated += async (_, _) =>
        {
            _sanSang = true;
            if (_cho is not null) { var d = _cho; _cho = null; await VeDiemAsync(d); }
        };
        Wv.Navigating += OnNavigating;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        BtnDung.IsVisible = ChonDiaChi;
        if (_daTimLanDau) return;
        _daTimLanDau = true;
        if (!string.IsNullOrWhiteSpace(DiaChi) && string.IsNullOrWhiteSpace(EntryTimKiem.Text))
        {
            EntryTimKiem.Text = DiaChi;
            await TimAsync(DiaChi);
        }
    }

    private async void OnTimClicked(object? sender, EventArgs e) => await TimAsync(EntryTimKiem.Text ?? "");

    private async Task TimAsync(string tuKhoa)
    {
        tuKhoa = tuKhoa.Trim();
        if (tuKhoa.Length < 2) { ThongBao("Hãy nhập ít nhất 2 ký tự để tìm."); return; }

        Dang.IsVisible = Dang.IsRunning = true;
        ThongBao(null);
        try
        {
            var url = "https://nominatim.openstreetmap.org/search?format=jsonv2&limit=6&countrycodes=vn&accept-language=vi&q="
                      + Uri.EscapeDataString(tuKhoa);
            using var doc = JsonDocument.Parse(await Http.GetStringAsync(url));

            var kq = new List<DiaDiem>();
            foreach (var x in doc.RootElement.EnumerateArray())
            {
                if (double.TryParse(x.GetProperty("lat").GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var lat) &&
                    double.TryParse(x.GetProperty("lon").GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var lng))
                    kq.Add(new DiaDiem { Ten = x.GetProperty("display_name").GetString() ?? tuKhoa, Lat = lat, Lng = lng });
            }

            DsKetQua.ItemsSource = kq;
            DsKetQua.IsVisible = kq.Count > 1;
            if (kq.Count == 0) ThongBao("Không tìm thấy địa điểm phù hợp. Thử gõ cụ thể hơn (số nhà, đường, quận).");
            else await ChonAsync(kq[0]);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            ThongBao("Không kết nối được dịch vụ bản đồ. Kiểm tra Internet rồi thử lại.");
        }
        catch (JsonException)
        {
            ThongBao("Dịch vụ bản đồ trả về dữ liệu không hợp lệ.");
        }
        finally { Dang.IsVisible = Dang.IsRunning = false; }
    }

    private async void OnKetQuaTapped(object? sender, TappedEventArgs e)
    {
        if (e.Parameter is DiaDiem d) await ChonAsync(d);
    }

    private async Task ChonAsync(DiaDiem d)
    {
        _chon = d;
        LblDaChon.Text = d.Ten;
        BtnDung.IsEnabled = true;
        if (!_sanSang) { _cho = d; return; }
        await VeDiemAsync(d);
    }

    private async Task VeDiemAsync(DiaDiem d)
    {
        var js = string.Create(CultureInfo.InvariantCulture, $"datViTri({d.Lat}, {d.Lng}, {JsonSerializer.Serialize(d.Ten)});");
        try { await Wv.EvaluateJavaScriptAsync(js); } catch { /* WebView chưa sẵn sàng */ }
    }

    // Chạm vào bản đồ: trang JS điều hướng tới foodapp://pick?lat=..&lng=.. → tra ngược ra địa chỉ.
    private async void OnNavigating(object? sender, WebNavigatingEventArgs e)
    {
        if (!e.Url.StartsWith("foodapp://pick", StringComparison.OrdinalIgnoreCase)) return;
        e.Cancel = true;

        var q = System.Web.HttpUtility.ParseQueryString(new Uri(e.Url).Query);
        if (!double.TryParse(q["lat"], NumberStyles.Float, CultureInfo.InvariantCulture, out var lat) ||
            !double.TryParse(q["lng"], NumberStyles.Float, CultureInfo.InvariantCulture, out var lng)) return;

        Dang.IsVisible = Dang.IsRunning = true;
        try
        {
            var url = string.Create(CultureInfo.InvariantCulture,
                $"https://nominatim.openstreetmap.org/reverse?format=jsonv2&accept-language=vi&lat={lat}&lon={lng}");
            using var doc = JsonDocument.Parse(await Http.GetStringAsync(url));
            var ten = doc.RootElement.TryGetProperty("display_name", out var n) ? n.GetString() : null;
            await ChonAsync(new DiaDiem { Ten = string.IsNullOrWhiteSpace(ten) ? $"{lat:0.00000}, {lng:0.00000}" : ten!, Lat = lat, Lng = lng });
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            await ChonAsync(new DiaDiem { Ten = string.Create(CultureInfo.InvariantCulture, $"{lat:0.00000}, {lng:0.00000}"), Lat = lat, Lng = lng });
        }
        finally { Dang.IsVisible = Dang.IsRunning = false; }
    }

    private async void OnDungClicked(object? sender, EventArgs e)
    {
        if (_chon is null) return;
        KhiChon?.Invoke(_chon.Ten);
        KhiChon = null;
        await Shell.Current.GoToAsync("..");
    }

    private async void OnGoogleClicked(object? sender, EventArgs e)
    {
        var q = _chon?.Ten ?? EntryTimKiem.Text;
        if (string.IsNullOrWhiteSpace(q)) { ThongBao("Chưa có địa điểm để mở."); return; }
        try { await Launcher.Default.OpenAsync(new Uri("https://www.google.com/maps/search/?api=1&query=" + Uri.EscapeDataString(q))); }
        catch (Exception) { ThongBao("Không mở được Google Maps trên thiết bị này."); }
    }

    private void ThongBao(string? t)
    {
        LblThongBao.Text = t ?? "";
        LblThongBao.IsVisible = !string.IsNullOrEmpty(t);
    }

    /// <summary>Mở bản đồ. <paramref name="chonDiaChi"/> = true thì có nút "Dùng địa chỉ này" trả địa chỉ về qua <paramref name="khiChon"/>.</summary>
    public static Task MoAsync(string diaChi = "", bool chonDiaChi = false, Action<string>? khiChon = null)
    {
        KhiChon = khiChon;
        return Shell.Current.GoToAsync(nameof(BanDoPage), new Dictionary<string, object>
        {
            { "DiaChi", diaChi }, { "ChonDiaChi", chonDiaChi },
        });
    }
}
