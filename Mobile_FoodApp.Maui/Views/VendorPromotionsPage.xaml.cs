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

    private async void NapDanhSach()
    {
        if ((Services.DuLieuService.DangOnline || !string.IsNullOrEmpty(Services.ApiClient.Token)))
        {
            var result = await Services.ApiClient.GetAsync<List<KhuyenMai>>("api/mobile/promotions/vendor");
            if (!result.ThanhCong) { DsKhuyenMai.ItemsSource = null; await DisplayAlert("Không tải được voucher", result.ThongDiep, "OK"); return; }
            DsKhuyenMai.ItemsSource = result.DuLieu;
            LblRong.IsVisible = result.DuLieu?.Count == 0;
            return;
        }
        // TODO: thay bằng GET /api/gian-hang/khuyen-mai khi có API thật.
        var danhSach = MockData.DanhSachKhuyenMai
            .Where(k => k.GianHangId == MockData.GianHangHienTai.Id)
            .OrderByDescending(k => k.NgayBatDau)
            .ToList();

        DsKhuyenMai.ItemsSource = danhSach;
        LblRong.IsVisible = danhSach.Count == 0;
    }

    private async void OnBatTatToggled(object? sender, ToggledEventArgs e)
    {
        if (sender is not Switch { BindingContext: KhuyenMai km }) return;

        if ((Services.DuLieuService.DangOnline || !string.IsNullOrEmpty(Services.ApiClient.Token)))
        {
            if (km.DangHoatDong == e.Value) return;
            var result = await Services.ApiClient.PutAsync<object>($"api/mobile/promotions/vendor/{km.Id}/status", new { dangHoatDong = e.Value });
            if (!result.ThanhCong) { await DisplayAlert("Chưa đổi trạng thái", result.ThongDiep, "OK"); NapDanhSach(); return; }
            km.DangHoatDong = e.Value; return;
        }
        km.DangHoatDong = e.Value;
        Services.VendorLocalStore.Luu();
        // TODO: gọi PUT /api/gian-hang/khuyen-mai/{id}/tinh-trang { dangHoatDong } khi có API thật.
    }

    private async void OnThemClicked(object sender, EventArgs e)
    {
        if ((Services.DuLieuService.DangOnline || !string.IsNullOrEmpty(Services.ApiClient.Token))) { await Navigation.PushAsync(new PromotionEditorPage(false)); return; }
        await Shell.Current.GoToAsync(nameof(VendorPromotionEditPage));
    }

    private async void OnSuaClicked(object sender, EventArgs e)
    {
        if (sender is not Button { CommandParameter: KhuyenMai km }) return;

        if ((Services.DuLieuService.DangOnline || !string.IsNullOrEmpty(Services.ApiClient.Token))) { await Navigation.PushAsync(new PromotionEditorPage(false, km.Id)); return; }
        await Shell.Current.GoToAsync(nameof(VendorPromotionEditPage), new Dictionary<string, object>
        {
            { "KhuyenMaiId", km.Id }
        });
    }

    private async void OnXoaClicked(object sender, EventArgs e)
    {
        if (sender is not Button { CommandParameter: KhuyenMai km }) return;

        if ((Services.DuLieuService.DangOnline || !string.IsNullOrEmpty(Services.ApiClient.Token)))
        {
            if (!await DisplayAlert("Ngừng voucher", "Tắt voucher và giữ lịch sử sử dụng?", "Tắt", "Hủy")) return;
            var result = await Services.ApiClient.PutAsync<object>($"api/mobile/promotions/vendor/{km.Id}/status", new { dangHoatDong = false });
            if (!result.ThanhCong) await DisplayAlert("Chưa tắt voucher", result.ThongDiep, "OK");
            NapDanhSach(); return;
        }
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
