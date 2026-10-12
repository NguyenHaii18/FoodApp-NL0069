using FoodApp.Maui.Services;

namespace FoodApp.Maui.Views;

/// <summary>Màn hình nhập mã OTP dùng chung (xác thực email khi đăng ký, đổi email...). Dùng <see cref="CanXacThucAsync"/> để mở và chờ kết quả.</summary>
[QueryProperty(nameof(MucDich), "MucDich")]
[QueryProperty(nameof(NguoiNhan), "NguoiNhan")]
[QueryProperty(nameof(TieuDe), "TieuDe")]
public partial class XacThucOtpPage : ContentPage
{
    public string MucDich { get; set; } = OtpService.XacThucEmail;
    public string NguoiNhan { get; set; } = "";
    public string TieuDe { get; set; } = "Xác thực email";

    private static TaskCompletionSource<bool>? _tcs;
    private IDispatcherTimer? _timer;
    private bool _daGui;

    /// <summary>Mở trang OTP, trả về true nếu người dùng nhập đúng mã; false nếu bấm "Để sau" hoặc quay lại.</summary>
    public static async Task<bool> CanXacThucAsync(string mucDich, string nguoiNhan, string tieuDe)
    {
        _tcs = new TaskCompletionSource<bool>();
        await Shell.Current.GoToAsync(nameof(XacThucOtpPage), new Dictionary<string, object>
        {
            { "MucDich", mucDich }, { "NguoiNhan", nguoiNhan }, { "TieuDe", tieuDe },
        });
        return await _tcs.Task;
    }

    public XacThucOtpPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        LblTieuDe.Text = TieuDe;
        LblHuongDan.Text = $"Mã xác thực gồm 6 số sẽ được gửi tới\n{NguoiNhan}\n(hiệu lực {OtpService.PhutHieuLuc} phút).";

        _timer = Dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromSeconds(1);
        _timer.Tick += (_, _) => CapNhatNutGuiLai();
        _timer.Start();

        if (!_daGui) { _daGui = true; await GuiAsync(); }
        CapNhatNutGuiLai();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _timer?.Stop();
        _timer = null;
        _tcs?.TrySetResult(false);      // thoát mà chưa xác thực
    }

    private void CapNhatNutGuiLai()
    {
        var cho = OtpService.GiayConLaiDeGuiLai(MucDich, NguoiNhan);
        BtnGuiLai.IsEnabled = cho == 0;
        BtnGuiLai.Text = cho == 0 ? "Gửi lại mã" : $"Gửi lại mã ({cho}s)";
    }

    private async Task GuiAsync()
    {
        LblLoi.IsVisible = false;
        Dang.IsVisible = Dang.IsRunning = true;
        try
        {
            var kq = await OtpService.GuiAsync(MucDich, NguoiNhan);
            if (!kq.ThanhCong) { HienLoi(kq.ThongDiep); return; }
            KhungMaDemo.IsVisible = kq.MaDemo is not null;
            LblGhiChuDemo.Text = kq.ThongDiep;
            LblMaDemo.Text = kq.MaDemo ?? "";
        }
        finally { Dang.IsVisible = Dang.IsRunning = false; }
    }

    private async void OnGuiLaiClicked(object sender, EventArgs e)
    {
        EntryOtp.Text = "";
        await GuiAsync();
        CapNhatNutGuiLai();
    }

    private async void OnXacNhanClicked(object sender, EventArgs e)
    {
        LblLoi.IsVisible = false;
        BtnXacNhan.IsEnabled = false;
        try
        {
            var (ok, tb) = await OtpService.KiemAsync(MucDich, NguoiNhan, EntryOtp.Text);
            if (!ok) { HienLoi(tb); return; }
            _tcs?.TrySetResult(true);
            await Shell.Current.GoToAsync("..");
        }
        finally { BtnXacNhan.IsEnabled = true; }
    }

    private async void OnBoQuaTapped(object sender, EventArgs e)
    {
        _tcs?.TrySetResult(false);
        await Shell.Current.GoToAsync("..");
    }

    private void HienLoi(string t) { LblLoi.Text = t; LblLoi.IsVisible = true; }
}
