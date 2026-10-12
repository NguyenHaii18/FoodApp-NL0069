using FoodApp.Maui.Models;

namespace FoodApp.Maui.Views;

public partial class AdminPromotionsPage : ContentPage
{
    // null = "Tất cả", "" = "Toàn hệ thống", còn lại = GianHangId cụ thể
    private string? _dangLoc = "__all__";
    private readonly List<Button> _nutBoLoc = new();

    public AdminPromotionsPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (!await Services.PhanQuyenService.KiemTraAsync(this, Services.Quyen.QuanLyKhuyenMai)) { await Shell.Current.GoToAsync(".."); return; }
        DungBoLoc();
        NapDanhSach();
    }

    private void DungBoLoc()
    {
        KhungBoLoc.Children.Clear();
        _nutBoLoc.Clear();

        var boLoc = new List<(string Nhan, string? GianHangId)> { ("Tất cả", "__all__"), ("Toàn hệ thống", "") };
        boLoc.AddRange(MockData.DanhSachGianHang
            .Where(g => g.TrangThaiDangKy == TrangThaiDangKyGianHang.DaDuyet)
            .Select(g => (g.TenGianHang, (string?)g.Id)));

        foreach (var (nhan, gianHangId) in boLoc)
        {
            var nut = new Button
            {
                Text = nhan,
                CornerRadius = 16,
                HeightRequest = 34,
                Padding = new Thickness(14, 0),
                FontSize = 12,
                BackgroundColor = gianHangId == _dangLoc ? (Color)Application.Current!.Resources["TextDark"] : Colors.White,
                TextColor = gianHangId == _dangLoc ? Colors.White : (Color)Application.Current!.Resources["TextGrey"],
                BorderColor = (Color)Application.Current!.Resources["BorderColor"],
                BorderWidth = 1,
            };
            nut.Clicked += (_, _) => LocTheoPhamVi(gianHangId, nut);
            _nutBoLoc.Add(nut);
            KhungBoLoc.Children.Add(nut);
        }
    }

    private void LocTheoPhamVi(string? gianHangId, Button nutDuocChon)
    {
        _dangLoc = gianHangId;

        foreach (var nut in _nutBoLoc)
        {
            bool laDuocChon = nut == nutDuocChon;
            nut.BackgroundColor = laDuocChon ? (Color)Application.Current!.Resources["TextDark"] : Colors.White;
            nut.TextColor = laDuocChon ? Colors.White : (Color)Application.Current!.Resources["TextGrey"];
        }

        NapDanhSach();
    }

    private void NapDanhSach()
    {
        // TODO: thay bằng GET /api/quan-tri/khuyen-mai?gianHangId=... khi có API thật.
        var danhSach = MockData.DanhSachKhuyenMai.AsEnumerable();

        if (_dangLoc == "")
            danhSach = danhSach.Where(k => k.ApDungToanHeThong);
        else if (_dangLoc != "__all__")
            danhSach = danhSach.Where(k => k.GianHangId == _dangLoc);

        var ketQua = danhSach.OrderByDescending(k => k.NgayBatDau).ToList();
        DsKhuyenMai.ItemsSource = ketQua;
        LblRong.IsVisible = ketQua.Count == 0;
    }

    private void OnBatTatToggled(object? sender, ToggledEventArgs e)
    {
        if (sender is not Switch { BindingContext: KhuyenMai km }) return;

        km.DangHoatDong = e.Value;
        Services.VendorLocalStore.Luu();
        Services.NhatKyService.GhiQuanTri(e.Value ? "Bật khuyến mãi" : "Tắt khuyến mãi", km.MaCode);
        // TODO: gọi PUT /api/quan-tri/khuyen-mai/{id}/tinh-trang { dangHoatDong } khi có API thật.
    }

    private async void OnThemClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(AdminPromotionEditPage));
    }

    private async void OnXoaClicked(object sender, EventArgs e)
    {
        if (sender is not Button { CommandParameter: KhuyenMai km }) return;

        bool xacNhan = await DisplayAlert("Xóa khuyến mãi", $"Xóa mã \"{km.MaCode}\" ({km.PhamViHienThi})?", "Xóa", "Hủy");
        if (!xacNhan) return;

        MockData.DanhSachKhuyenMai.Remove(km);
        Services.VendorLocalStore.Luu();
        Services.NhatKyService.GhiQuanTri("Xóa khuyến mãi", km.MaCode);
        // TODO: gọi DELETE /api/quan-tri/khuyen-mai/{id} khi có API thật.

        NapDanhSach();
    }

    private void OnRefreshing(object? sender, EventArgs e)
    {
        NapDanhSach();
        RefreshDsKm.IsRefreshing = false;
    }
}
