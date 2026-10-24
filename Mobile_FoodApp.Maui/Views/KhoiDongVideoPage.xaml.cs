using CommunityToolkit.Maui.Core.Primitives;

namespace FoodApp.Maui.Views;

/// <summary>
/// Video intro hiện ĐẦU TIÊN mỗi lần mở app. Kết thúc video, bấm "Bỏ qua", lỗi phát video hoặc quá 12 giây
/// đều tự chuyển sang trang đăng nhập — video không bao giờ chặn người dùng.
/// </summary>
public partial class KhoiDongVideoPage : ContentPage
{
    private bool _daChuyen;
#if !WINDOWS
    private CommunityToolkit.Maui.Views.MediaElement? _video;
#endif

    public KhoiDongVideoPage()
    {
        InitializeComponent();
#if !WINDOWS
        _video = new CommunityToolkit.Maui.Views.MediaElement
        {
            Source = CommunityToolkit.Maui.Views.MediaSource.FromResource("intro.mp4"),
            ShouldAutoPlay = true,
            ShouldShowPlaybackControls = false,
            ShouldMute = true,
            Aspect = Aspect.AspectFit,
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Fill
        };
        _video.MediaEnded += Video_MediaEnded;
        _video.MediaFailed += Video_MediaFailed;
        IntroGrid.Children.Insert(0, _video);
#endif
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        // Windows dùng splash dự phòng vì MediaElement có thể làm WinUI crash khi
        // DisplayRequest không được hệ điều hành cung cấp. Nền tảng khác vẫn phát video.
#if WINDOWS
        await Task.Delay(TimeSpan.FromSeconds(2));
#else
        await Task.Delay(TimeSpan.FromSeconds(12));      // chốt chặn video
#endif
        await VaoAppAsync();
    }

    private async void Video_MediaEnded(object? sender, EventArgs e) => await VaoAppAsync();

    private async void Video_MediaFailed(object? sender, MediaFailedEventArgs e) => await VaoAppAsync();

    private async void BoQua_Clicked(object? sender, EventArgs e) => await VaoAppAsync();

    private async Task VaoAppAsync()
    {
        if (_daChuyen) return;
        _daChuyen = true;
#if !WINDOWS
        try { _video?.Stop(); } catch { /* bỏ qua */ }
#endif
        await MainThread.InvokeOnMainThreadAsync(async () => await Shell.Current.GoToAsync("//dang-nhap"));
    }

    protected override bool OnBackButtonPressed() => true;
}
