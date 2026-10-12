using FoodApp.Maui.Models;

namespace FoodApp.Maui.Views;

public partial class AdminIssuesPage : ContentPage
{
    public AdminIssuesPage()
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
        // TODO: thay bằng GET /api/quan-tri/van-de khi có API thật.
        DsVanDe.ItemsSource = MockData.DanhSachVanDe
            .OrderBy(v => v.TrangThai == TrangThaiVanDe.DaGiaiQuyet)
            .ThenByDescending(v => v.NgayBaoCao)
            .ToList();
    }

    private async void OnDangXuLyClicked(object sender, EventArgs e)
    {
        if (sender is not Button { CommandParameter: VanDeHeThong vanDe }) return;

        if (!await Services.PhanQuyenService.KiemTraAsync(this, Services.Quyen.XuLyVanDe)) return;
        vanDe.TrangThai = TrangThaiVanDe.DangXuLy;
        Services.VendorLocalStore.Luu();
        Services.NhatKyService.GhiQuanTri("Nhận xử lý vấn đề", vanDe.TieuDe);
        await Services.DuLieuService.GhiAsync("PUT", $"api/mobile/admin/van-de/{vanDe.Id}/dang-xu-ly");

        NapDanhSach();
    }

    private async void OnGiaiQuyetClicked(object sender, EventArgs e)
    {
        if (sender is not Button { CommandParameter: VanDeHeThong vanDe }) return;

        if (!await Services.PhanQuyenService.KiemTraAsync(this, Services.Quyen.XuLyVanDe)) return;
        vanDe.TrangThai = TrangThaiVanDe.DaGiaiQuyet;
        Services.VendorLocalStore.Luu();
        Services.NhatKyService.GhiQuanTri("Giải quyết vấn đề", vanDe.TieuDe);
        await Services.DuLieuService.GhiAsync("PUT", $"api/mobile/admin/van-de/{vanDe.Id}/giai-quyet");

        NapDanhSach();
        await DisplayAlert("Đã cập nhật", $"Vấn đề \"{vanDe.TieuDe}\" đã được đánh dấu giải quyết xong.", "OK");
    }
}
