using FoodApp.Maui.Models;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Dispatching;

namespace FoodApp.Maui.Services;

/// <summary>
/// Chia sẻ vị trí tài xế theo thời gian thực cho đơn đang giao: định kỳ đọc GPS, ghi vào đơn
/// (khách thấy ở tab Đơn hàng) và gửi lên API khi online. Chỉ chạy khi tài xế bật chia sẻ.
/// </summary>
public static class ViTriTaiXeService
{
    private const string KhoaBat = "tx-chia-se-vi-tri";
    private static readonly TimeSpan ChuKy = TimeSpan.FromSeconds(15);

    private static IDispatcherTimer? _timer;
    private static string? _donId;

    /// <summary>Tài xế có muốn chia sẻ vị trí không (mặc định bật).</summary>
    public static bool DuocPhepChiaSe
    {
        get => Preferences.Default.Get(KhoaBat, true);
        set => Preferences.Default.Set(KhoaBat, value);
    }

    public static bool DangChay => _timer?.IsRunning == true;

    /// <summary>Thông báo gần nhất để hiện cho tài xế (vd: chưa bật GPS).</summary>
    public static string TrangThai { get; private set; } = "Chưa chia sẻ vị trí";

    public static event Action? DaCapNhat;

    /// <summary>Vị trí đọc được gần nhất (gửi kèm khi hoàn thành / giao thất bại làm bằng chứng).</summary>
    public static (double Lat, double Lng)? ViTriCuoi { get; private set; }

    /// <summary>Bắt đầu chia sẻ cho đơn. Xin quyền vị trí nếu cần. Trả về true nếu đang chạy.</summary>
    public static async Task<bool> BatDauAsync(DonHang don)
    {
        if (!DuocPhepChiaSe) { Dung("Đang tắt chia sẻ vị trí"); return false; }
        if (DangChay && _donId == don.Id) return true;

        PermissionStatus quyen;
        try
        {
            quyen = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (quyen != PermissionStatus.Granted)
                quyen = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
        }
        catch (Exception) { TrangThai = "Thiết bị không hỗ trợ định vị"; return false; }

        if (quyen != PermissionStatus.Granted)
        {
            TrangThai = "Chưa được cấp quyền vị trí — khách chưa theo dõi được bạn";
            return false;
        }

        _donId = don.Id;
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null) return false;

        _timer ??= dispatcher.CreateTimer();
        _timer.Interval = ChuKy;
        _timer.Tick -= OnTick;
        _timer.Tick += OnTick;
        _timer.Start();
        TrangThai = "Đang xác định vị trí…";
        await CapNhatMotLanAsync();
        return true;
    }

    public static void Dung(string? thongBao = null)
    {
        _timer?.Stop();
        _donId = null;
        TrangThai = thongBao ?? "Chưa chia sẻ vị trí";
        DaCapNhat?.Invoke();
    }

    private static async void OnTick(object? sender, EventArgs e) => await CapNhatMotLanAsync();

    private static async Task CapNhatMotLanAsync()
    {
        var id = _donId;
        if (id is null) return;
        try
        {
            var vt = await Geolocation.Default.GetLocationAsync(
                new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(8)));
            if (vt is null) { TrangThai = "Chưa lấy được vị trí, đang thử lại…"; DaCapNhat?.Invoke(); return; }

            var luc = DateTime.Now;
            ViTriCuoi = (vt.Latitude, vt.Longitude);
            GiaoHangService.CapNhatMoiBanSao(id, d =>
            {
                d.TaiXeLat = vt.Latitude; d.TaiXeLng = vt.Longitude; d.ViTriCapNhatLuc = luc;
            });
            TrangThai = $"📡 Đang chia sẻ vị trí · cập nhật {luc:HH:mm:ss}";

            if (DuLieuService.DangOnline)
            {
                // Server nhận donHangId kiểu số (trước đây gửi chuỗi nên bị từ chối âm thầm)
                if (!int.TryParse(id, out var maDon)) { TrangThai = "Mã đơn không hợp lệ, không gửi được vị trí"; DaCapNhat?.Invoke(); return; }
                var kq = await ApiClient.PutAsync<object>("api/mobile/tai-xe/vi-tri", new { donHangId = maDon, viDo = vt.Latitude, kinhDo = vt.Longitude });
                if (!kq.ThanhCong) TrangThai = $"⚠ Chưa gửi được vị trí lên máy chủ ({kq.ThongDiep}) — sẽ thử lại sau 15 giây";
            }
        }
        catch (FeatureNotEnabledException) { TrangThai = "GPS đang tắt — hãy bật định vị để khách theo dõi"; }
        catch (FeatureNotSupportedException) { TrangThai = "Thiết bị không có GPS"; }
        catch (PermissionException) { TrangThai = "Chưa được cấp quyền vị trí"; }
        catch (Exception) { TrangThai = "Không lấy được vị trí, đang thử lại…"; }
        DaCapNhat?.Invoke();
    }
}
