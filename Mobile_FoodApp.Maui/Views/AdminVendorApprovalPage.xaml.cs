using FoodApp.Maui.Models;

namespace FoodApp.Maui.Views;

public partial class AdminVendorApprovalPage : ContentPage
{
    public AdminVendorApprovalPage()
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
        // TODO: thay bằng GET /api/quan-tri/gian-hang?trangThaiDangKy=ChoDuyet khi có API thật.
        var choDuyet = MockData.DanhSachGianHang
            .Where(g => g.TrangThaiDangKy == TrangThaiDangKyGianHang.ChoDuyet)
            .OrderBy(g => g.NgayDangKy)
            .ToList();

        DsChoDuyet.ItemsSource = choDuyet;
        LblRong.IsVisible = choDuyet.Count == 0;
    }

    private async void OnDuyetClicked(object sender, EventArgs e)
    {
        if (sender is not Button { CommandParameter: GianHang gh }) return;
        if (!await Services.PhanQuyenService.KiemTraAsync(this, Services.Quyen.DuyetHoSo)) return;

        if (!gh.DaCoGiayTo &&
            !await DisplayAlert("Hồ sơ thiếu giấy tờ", $"\"{gh.TenGianHang}\" chưa có đủ số giấy phép / CCCD chủ quán. Vẫn duyệt?", "Vẫn duyệt", "Quay lại"))
            return;

        gh.TrangThaiDangKy = TrangThaiDangKyGianHang.DaDuyet;
        gh.LyDoTuChoi = "";
        gh.DangMoCua = true;
        Services.VendorLocalStore.Luu();
        await Services.DuLieuService.GhiAsync("PUT", $"api/mobile/admin/gian-hang/{gh.Id}/duyet");
        Services.NhatKyService.GhiQuanTri("Duyệt hồ sơ gian hàng", gh.TenGianHang, gh.Email);

        NapDanhSach();
        await DisplayAlert("Đã duyệt", $"Gian hàng \"{gh.TenGianHang}\" đã được kích hoạt trên nền tảng.", "OK");
    }

    private async void OnTuChoiClicked(object sender, EventArgs e)
    {
        if (sender is not Button { CommandParameter: GianHang gh }) return;
        if (!await Services.PhanQuyenService.KiemTraAsync(this, Services.Quyen.DuyetHoSo)) return;

        var lyDo = await DisplayPromptAsync("Từ chối hồ sơ", $"Lý do từ chối hồ sơ của \"{gh.TenGianHang}\" (chủ quán sẽ thấy khi đăng nhập):",
            "Từ chối", "Để sau", "VD: Giấy phép không rõ / không hợp lệ", maxLength: 200);
        if (lyDo is null) return;
        if (string.IsNullOrWhiteSpace(lyDo)) lyDo = "Hồ sơ không đạt yêu cầu";

        gh.TrangThaiDangKy = TrangThaiDangKyGianHang.TuChoi;
        gh.LyDoTuChoi = lyDo.Trim();
        Services.VendorLocalStore.Luu();
        await Services.DuLieuService.GhiAsync("PUT", $"api/mobile/admin/gian-hang/{gh.Id}/tu-choi", new { lyDo = gh.LyDoTuChoi });
        Services.NhatKyService.GhiQuanTri("Từ chối hồ sơ gian hàng", gh.TenGianHang, gh.LyDoTuChoi);

        NapDanhSach();
    }

    private void OnRefreshing(object? sender, EventArgs e)
    {
        NapDanhSach();
        RefreshDsChoDuyet.IsRefreshing = false;
    }
}
