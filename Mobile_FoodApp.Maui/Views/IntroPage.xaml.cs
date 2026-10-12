namespace FoodApp.Maui.Views;

/// <summary>Màn intro 5 giây sau khi khách đăng nhập: logo hiện dần, thanh tiến trình chạy đủ 5s rồi vào trang chủ.</summary>
public partial class IntroPage : ContentPage
{
    private bool _daChay;

    public IntroPage()
    {
        InitializeComponent();
        Logo.Opacity = 0; Logo.Scale = 0.6;
        TxtTen.Opacity = 0; TxtTen.TranslationY = 16;
        TxtSlogan.Opacity = 0;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_daChay) return;
        _daChay = true;

        var tien = Tien.ProgressTo(1, 5000, Easing.Linear);          // tổng đúng 5 giây
        await Task.WhenAll(Logo.FadeTo(1, 700), Logo.ScaleTo(1, 900, Easing.SpringOut));
        await Task.WhenAll(TxtTen.FadeTo(1, 500), TxtTen.TranslateTo(0, 0, 500, Easing.CubicOut));
        await TxtSlogan.FadeTo(1, 500);

        var nhip = NhipLogoAsync(tien);
        await tien;
        Logo.CancelAnimations();
        await nhip;

        await Shell.Current.GoToAsync("//chinh/trang-chu");
    }

    private async Task NhipLogoAsync(Task dung)
    {
        while (!dung.IsCompleted)
        {
            await Logo.ScaleTo(1.06, 650, Easing.SinInOut);
            if (dung.IsCompleted) break;
            await Logo.ScaleTo(1.0, 650, Easing.SinInOut);
        }
    }

    protected override bool OnBackButtonPressed() => true;           // không cho quay lại trong lúc intro
}
