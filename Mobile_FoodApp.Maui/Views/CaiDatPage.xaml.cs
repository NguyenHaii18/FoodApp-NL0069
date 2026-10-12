using FoodApp.Maui.Models;
using FoodApp.Maui.Services;

namespace FoodApp.Maui.Views;

/// <summary>Cài đặt: giao diện (sáng/tối + màu chủ đề), thông báo, quyền riêng tư, kết nối máy chủ, xóa dữ liệu.</summary>
public partial class CaiDatPage : ContentPage
{
    private bool _dangNap;

    public CaiDatPage() { InitializeComponent(); }

    private static Color Mau(string k) => (Color)Application.Current!.Resources[k];

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _dangNap = true;
        SwPush.IsToggled = Preferences.Default.Get("cd-push", true);
        SwKm.IsToggled = Preferences.Default.Get("cd-km", true);
        SwAmThanh.IsToggled = Preferences.Default.Get("cd-amthanh", true);
        SwViTri.IsToggled = Preferences.Default.Get("cd-vitri", true);
        SwXacNhanDat.IsToggled = Preferences.Default.Get("cd-xacnhandat", false);
        _dangNap = false;
        ToMauNut();
        VeMauChuDe();
        HienMayChu();
        LblPhienBan.Text = $"Dịch Vụ Ăn Uống · phiên bản {AppInfo.Current.VersionString}";
    }

    private void OnCaiDatToggled(object? sender, ToggledEventArgs e)
    {
        if (_dangNap || sender is not Switch { StyleId: { } khoa }) return;
        Preferences.Default.Set("cd-" + khoa, e.Value);
    }

    // ---------- Giao diện ----------
    private void OnGiaoDienClicked(object? sender, EventArgs e)
    {
        if (sender is Button { CommandParameter: string che }) { ThemeService.Dat(che); ToMauNut(); }
    }

    private void ToMauNut()
    {
        foreach (var (nut, che) in new[] { (BtnSang, "light"), (BtnToi, "dark"), (BtnHeThong, "auto") })
        {
            var chon = ThemeService.Hien == che;
            nut.BackgroundColor = chon ? Mau("Primary") : Mau("PrimarySoft");
            nut.TextColor = chon ? Colors.White : Mau("Primary");
        }
    }

    private void VeMauChuDe()
    {
        KhungMau.Children.Clear();
        foreach (var b in ThemeColors.DanhSach)
        {
            var chon = b.Ten == ThemeColors.Hien;
            var cham = new Frame
            {
                WidthRequest = 38, HeightRequest = 38, CornerRadius = 19, Padding = 0, HasShadow = false,
                BackgroundColor = Color.FromArgb(b.Primary), Margin = new Thickness(0, 0, 12, 4),
                BorderColor = chon ? Mau("TextDark") : Colors.Transparent,
                Content = new Label { Text = chon ? "✓" : "", TextColor = Colors.White, FontAttributes = FontAttributes.Bold, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center },
            };
            var ten = b.Ten;
            cham.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(async () => await DoiMauAsync(ten)) });
            KhungMau.Children.Add(cham);
        }
    }

    /// <summary>Đổi màu: ghi đè tài nguyên rồi tạo lại AppShell để mọi trang lấy màu mới, giữ nguyên đăng nhập.</summary>
    private async Task DoiMauAsync(string ten)
    {
        if (ten == ThemeColors.Hien) return;
        ThemeColors.Dat(ten);
        ThemeColors.Ap();
        Application.Current!.MainPage = new AppShell();
        if (Phien.Khach is not null) await AppShell.DangNhapThanhCongAsync();
    }

    // ---------- Máy chủ ----------
    private void HienMayChu()
    {
        LblTrangThaiMayChu.Text = DuLieuService.DangOnline ? "🟢 Đã kết nối máy chủ" : "⚪ Chưa kết nối — app chạy độc lập trên máy";
        LblTrangThaiMayChu.TextColor = DuLieuService.DangOnline ? Mau("Success") : Mau("TextGrey");
        LblDiaChiMayChu.Text = AppSettings.BaseUrl + (string.IsNullOrWhiteSpace(AppSettings.BaseUrlTuyChinh) ? "  (mặc định)" : "  (tự nhập)");
    }

    private async void OnKiemTraClicked(object? sender, EventArgs e)
    {
        Loading.IsVisible = Loading.IsRunning = true;
        try
        {
            var ok = await DuLieuService.TaiDanhMucAsync();
            HienMayChu();
            await DisplayAlert(ok ? "Kết nối thành công" : "Không kết nối được",
                ok ? "Dữ liệu quán, món và khuyến mãi đã được tải từ máy chủ." : $"Không gọi được {AppSettings.BaseUrl}. Kiểm tra máy chủ Web đã chạy chưa, và địa chỉ/cổng có đúng không.", "OK");
        }
        finally { Loading.IsVisible = Loading.IsRunning = false; }
    }

    private async void OnDoiDiaChiClicked(object? sender, EventArgs e)
    {
        var moi = await DisplayPromptAsync("Địa chỉ máy chủ",
            "VD: https://192.168.1.10:5001\nĐể trống để dùng địa chỉ mặc định.", "Lưu", "Hủy",
            initialValue: AppSettings.BaseUrlTuyChinh, keyboard: Keyboard.Url);
        if (moi is null) return;
        moi = moi.Trim();
        if (moi.Length > 0 && !Uri.TryCreate(moi, UriKind.Absolute, out var u)) { await DisplayAlert("Sai định dạng", "Địa chỉ phải bắt đầu bằng http:// hoặc https://", "OK"); return; }
        if (moi.Length > 0 && !(moi.StartsWith("http://") || moi.StartsWith("https://"))) { await DisplayAlert("Sai định dạng", "Địa chỉ phải bắt đầu bằng http:// hoặc https://", "OK"); return; }
        AppSettings.BaseUrlTuyChinh = moi;
        ApiClient.TaiLai();
        OnKiemTraClicked(null, EventArgs.Empty);
    }

    // ---------- Xóa dữ liệu ----------
    private async void OnXoaDuLieuTapped(object? sender, TappedEventArgs e)
    {
        if (!await DisplayAlert("Xóa dữ liệu", "Toàn bộ tài khoản, đơn hàng, quán yêu thích, sổ địa chỉ và cài đặt trên máy này sẽ bị xóa. Không thể hoàn tác.", "Xóa hết", "Hủy")) return;
        if (!await DisplayAlert("Xác nhận lần cuối", "Bạn chắc chắn muốn đưa app về trạng thái mới cài?", "Xóa hết", "Hủy")) return;

        Preferences.Default.Clear();
        MockData.DanhSachGianHang.Clear(); MockData.DanhSachMonAn.Clear(); MockData.DanhMucTheoGianHang.Clear();
        MockData.LichSuDonHang.Clear(); MockData.DonHangChoTaiXe.Clear(); MockData.DonHangCuaGianHang.Clear();
        MockData.DonHangDaGiaoCuaTaiXe.Clear(); MockData.DonHangDangGiaoCuaTaiXe = null;
        MockData.DanhSachKhachHang.Clear(); MockData.DanhSachVanDe.Clear(); MockData.DanhSachKhuyenMai.Clear();
        MockData.GianHangHienTai = new GianHang();
        Phien.DangXuat();
        ApiClient.TaiLai();
        ThemeService.Ap();
        ThemeColors.Ap();
        Application.Current!.MainPage = new AppShell();
        await DisplayAlert("Xong", "Đã xóa dữ liệu. App về trạng thái mới cài.", "OK");
    }
}
