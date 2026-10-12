using CommunityToolkit.Maui.Core.Primitives;

namespace FoodApp.Maui.Views;

/// <summary>
/// Video intro hiện ĐẦU TIÊN mỗi lần mở app. Kết thúc video, bấm "Bỏ qua", lỗi phát video hoặc quá 12 giây
/// đều tự chuyển sang trang đăng nhập — video không bao giờ chặn người dùng.
/// </summary>
public partial class KhoiDongVideoPage : ContentPage
{
    private bool _daChuyen;

    public KhoiDongVideoPage()
    {
        InitializeComponent();
        Video.Source = CommunityToolkit.Maui.Views.MediaSource.FromResource("intro.mp4");
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await Task.Delay(TimeSpan.FromSeconds(12));      // chốt chặn
        await VaoAppAsync();
    }

    private async void Video_MediaEnded(object? sender, EventArgs e) => await VaoAppAsync();

    private async void Video_MediaFailed(object? sender, MediaFailedEventArgs e) => await VaoAppAsync();

    private async void BoQua_Clicked(object? sender, EventArgs e) => await VaoAppAsync();

    private async Task VaoAppAsync()
    {
        if (_daChuyen) return;
        _daChuyen = true;
        try { Video.Stop(); } catch { /* bỏ qua */ }
        await MainThread.InvokeOnMainThreadAsync(async () => await Shell.Current.GoToAsync("//dang-nhap"));
    }

    protected override bool OnBackButtonPressed() => true;
}
