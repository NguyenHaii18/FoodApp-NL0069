using FoodApp.Maui.Models;

namespace FoodApp.Maui.Views;

public partial class AdminVendorListPage : ContentPage
{
    public AdminVendorListPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (!await Services.PhanQuyenService.KiemTraAsync(this, Services.Quyen.XemTaiKhoan)) { await Shell.Current.GoToAsync(".."); return; }
        NapDanhSach();
    }

    private void NapDanhSach()
    {
        // TODO: thay bằng GET /api/quan-tri/gian-hang khi có API thật.
        var danhSach = MockData.DanhSachGianHang
            .Where(g => g.TrangThaiDangKy == TrangThaiDangKyGianHang.DaDuyet)
            .OrderByDescending(g => g.NgayDangKy)
            .ToList();

        DsGianHang.ItemsSource = danhSach;
    }

    private async void OnKhoaMoClicked(object sender, EventArgs e)
    {
        if (sender is not Button { CommandParameter: GianHang gh }) return;
        if (!await Services.PhanQuyenService.KiemTraAsync(this, Services.Quyen.KhoaMoTaiKhoan)) return;

        if (gh.TaiKhoanBiKhoa)
        {
            gh.TaiKhoanBiKhoa = false;
            Services.VendorLocalStore.Luu();
            Services.NhatKyService.GhiQuanTri("Mở khóa gian hàng", gh.TenGianHang, gh.Email);
            await Services.DuLieuService.GhiAsync("PUT", $"api/mobile/admin/gian-hang/{gh.Id}/mo-khoa");
            NapDanhSach();
            await DisplayAlert("Đã mở khóa", $"Tài khoản gian hàng \"{gh.TenGianHang}\" đã được kích hoạt lại.", "OK");
            return;
        }

        bool xacNhan = await DisplayAlert(
            "Khóa tài khoản gian hàng",
            $"Khóa tài khoản \"{gh.TenGianHang}\"? Gian hàng sẽ không thể đăng nhập hoặc nhận đơn mới cho đến khi được mở lại.",
            "Khóa", "Hủy");
        if (!xacNhan) return;

        gh.TaiKhoanBiKhoa = true;
        Services.VendorLocalStore.Luu();
        Services.NhatKyService.GhiQuanTri("Khóa gian hàng", gh.TenGianHang, gh.Email);
        await Services.DuLieuService.GhiAsync("PUT", $"api/mobile/admin/gian-hang/{gh.Id}/khoa");

        NapDanhSach();
    }

    private void OnRefreshing(object? sender, EventArgs e)
    {
        NapDanhSach();
        RefreshDsGianHang.IsRefreshing = false;
    }
}
