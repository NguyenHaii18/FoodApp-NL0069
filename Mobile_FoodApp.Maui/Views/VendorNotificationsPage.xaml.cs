using FoodApp.Maui.Models;
using FoodApp.Maui.Services;

namespace FoodApp.Maui.Views;

/// <summary>Thông báo của gian hàng: đơn mới, hủy đơn, đánh giá mới.</summary>
public partial class VendorNotificationsPage : ContentPage
{
    public VendorNotificationsPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        DoiTacService.HuyDonQuaHan(MockData.GianHangHienTai.Id);   // đơn quá hạn tự hủy sẽ sinh thêm thông báo
        Nap();
    }

    private void Nap()
    {
        // TODO: thay bằng GET /api/mobile/gian-hang/{id}/thong-bao (+ đẩy thông báo thời gian thực) khi có API thật.
        var ds = DoiTacService.ThongBaoCua(MockData.GianHangHienTai.Id);
        DsThongBao.ItemsSource = ds;
        LblRong.IsVisible = ds.Count == 0;
    }

    private void OnDocTatCaClicked(object sender, EventArgs e)
    {
        DoiTacService.DanhDauDaDocTatCa(MockData.GianHangHienTai.Id);
        Nap();
    }

    private async void OnThongBaoTapped(object? sender, TappedEventArgs e)
    {
        if (sender is not Frame { BindingContext: ThongBaoGianHang tb }) return;

        tb.DaDoc = true;
        VendorLocalStore.Luu();

        switch (tb.Loai)
        {
            case ThongBaoGianHang.DonMoi:
            case ThongBaoGianHang.HuyDon:
                await Shell.Current.GoToAsync(nameof(VendorOrdersPage));
                break;
            case ThongBaoGianHang.DanhGiaMoi:
                await Shell.Current.GoToAsync(nameof(VendorReviewsPage));
                break;
            default:
                Nap();
                break;
        }
    }

    private void OnRefreshing(object? sender, EventArgs e)
    {
        DoiTacService.HuyDonQuaHan(MockData.GianHangHienTai.Id);
        Nap();
        RefreshDs.IsRefreshing = false;
    }
}
