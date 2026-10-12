using FoodApp.Maui.Models;
using FoodApp.Maui.Services;

namespace FoodApp.Maui.Views;

public partial class VendorPromotionsPage : ContentPage
{
    public VendorPromotionsPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        NapDanhSach();
    }

    private void NapDanhSach()
    {
        // TODO: thay bằng GET /api/gian-hang/khuyen-mai khi có API thật.
        var danhSach = MockData.DanhSachKhuyenMai
            .Where(k => k.GianHangId == MockData.GianHangHienTai.Id)
            .OrderByDescending(k => k.NgayBatDau)
            .ToList();

        DsKhuyenMai.ItemsSource = danhSach;
        LblRong.IsVisible = danhSach.Count == 0;
    }

    private void OnBatTatToggled(object? sender, ToggledEventArgs e)
    {
        if (sender is not Switch { BindingContext: KhuyenMai km }) return;

        km.DangHoatDong = e.Value;
        Services.VendorLocalStore.Luu();
        // TODO: gọi PUT /api/gian-hang/khuyen-mai/{id}/tinh-trang { dangHoatDong } khi có API thật.
    }

    private async void OnThemClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(VendorPromotionEditPage));
    }

    private async void OnSuaClicked(object sender, EventArgs e)
    {
        if (sender is not Button { CommandParameter: KhuyenMai km }) return;

        await Shell.Current.GoToAsync(nameof(VendorPromotionEditPage), new Dictionary<string, object>
        {
            { "KhuyenMaiId", km.Id }
        });
    }

    private async void OnXoaClicked(object sender, EventArgs e)
    {
        if (sender is not Button { CommandParameter: KhuyenMai km }) return;

        bool xacNhan = await DisplayAlert("Xóa khuyến mãi", $"Xóa mã \"{km.MaCode}\"? Khách hàng sẽ không thể áp dụng mã này nữa.", "Xóa", "Hủy");
        if (!xacNhan) return;

        MockData.DanhSachKhuyenMai.Remove(km);
        Services.VendorLocalStore.Luu();
        // TODO: gọi DELETE /api/gian-hang/khuyen-mai/{id} khi có API thật.

        NapDanhSach();
    }

    private void OnRefreshing(object? sender, EventArgs e)
    {
        NapDanhSach();
        RefreshDsKm.IsRefreshing = false;
    }
}
