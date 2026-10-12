using FoodApp.Maui.Models;

namespace FoodApp.Maui.Views;

public partial class AdminCustomerListPage : ContentPage
{
    public AdminCustomerListPage()
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
        // TODO: thay bằng GET /api/quan-tri/khach-hang khi có API thật.
        DsKhachHang.ItemsSource = MockData.DanhSachKhachHang.OrderByDescending(k => k.NgayDangKy).ToList();
    }

    private async void OnKhoaMoClicked(object sender, EventArgs e)
    {
        if (sender is not Button { CommandParameter: KhachHang kh }) return;
        if (!await Services.PhanQuyenService.KiemTraAsync(this, Services.Quyen.KhoaMoTaiKhoan)) return;

        if (kh.TaiKhoanBiKhoa)
        {
            kh.TaiKhoanBiKhoa = false;
            Services.VendorLocalStore.Luu();
            Services.NhatKyService.GhiQuanTri("Mở khóa khách hàng", kh.HoTen, kh.Email);
            await Services.DuLieuService.GhiAsync("PUT", $"api/mobile/admin/khach-hang/{kh.Id}/mo-khoa");
            NapDanhSach();
            await DisplayAlert("Đã mở khóa", $"Tài khoản \"{kh.HoTen}\" đã được kích hoạt lại.", "OK");
            return;
        }

        bool xacNhan = await DisplayAlert(
            "Khóa tài khoản khách hàng",
            $"Khóa tài khoản \"{kh.HoTen}\"? Khách hàng sẽ không thể đăng nhập cho đến khi được mở lại.",
            "Khóa", "Hủy");
        if (!xacNhan) return;

        kh.TaiKhoanBiKhoa = true;
        Services.VendorLocalStore.Luu();
        Services.NhatKyService.GhiQuanTri("Khóa khách hàng", kh.HoTen, kh.Email);
        await Services.DuLieuService.GhiAsync("PUT", $"api/mobile/admin/khach-hang/{kh.Id}/khoa");

        NapDanhSach();
    }
}
