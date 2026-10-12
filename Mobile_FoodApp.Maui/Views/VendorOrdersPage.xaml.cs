using FoodApp.Maui.Models;
using FoodApp.Maui.Services;
using Microsoft.Maui.Dispatching;

namespace FoodApp.Maui.Views;

public partial class VendorOrdersPage : ContentPage
{
    // null = "Tất cả"
    private readonly (string Nhan, TrangThaiDonHang? TrangThai)[] _boLoc =
    {
        ("Tất cả", null),
        ("Chờ xác nhận", TrangThaiDonHang.ChoXacNhan),
        ("Đang chuẩn bị", TrangThaiDonHang.DangLam),
        ("Đang giao", TrangThaiDonHang.DangGiao),
        ("Hoàn thành", TrangThaiDonHang.HoanThanh),
        ("Đã hủy", TrangThaiDonHang.DaHuy),
    };

    private TrangThaiDonHang? _dangLoc;
    private readonly List<Button> _nutBoLoc = new();

    public VendorOrdersPage()
    {
        InitializeComponent();
        DungBoLoc();
    }

    private IDispatcherTimer? _boDem;

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        NapDanhSach();
        await Services.DuLieuService.TaiDonHangGianHangAsync(MockData.GianHangHienTai.Id);
        NapDanhSach();

        // Cập nhật đếm ngược 5 phút phản hồi và tự hủy đơn quá hạn (NV04).
        _boDem ??= Dispatcher.CreateTimer();
        _boDem.Interval = TimeSpan.FromSeconds(30);
        _boDem.Tick -= OnBoDemTick;
        _boDem.Tick += OnBoDemTick;
        _boDem.Start();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _boDem?.Stop();
    }

    private void OnBoDemTick(object? sender, EventArgs e) => NapDanhSach();

    private void DungBoLoc()
    {
        foreach (var (nhan, trangThai) in _boLoc)
        {
            var nut = new Button
            {
                Text = nhan,
                CornerRadius = 16,
                HeightRequest = 34,
                Padding = new Thickness(14, 0),
                FontSize = 12,
                BackgroundColor = trangThai is null ? (Color)Application.Current!.Resources["Primary"] : Colors.White,
                TextColor = trangThai is null ? Colors.White : (Color)Application.Current!.Resources["TextGrey"],
                BorderColor = (Color)Application.Current!.Resources["BorderColor"],
                BorderWidth = 1,
            };
            nut.Clicked += (_, _) => LocTheoTrangThai(trangThai, nut);
            _nutBoLoc.Add(nut);
            KhungBoLoc.Children.Add(nut);
        }
    }

    private void LocTheoTrangThai(TrangThaiDonHang? trangThai, Button nutDuocChon)
    {
        _dangLoc = trangThai;

        foreach (var nut in _nutBoLoc)
        {
            bool laDuocChon = nut == nutDuocChon;
            nut.BackgroundColor = laDuocChon ? (Color)Application.Current!.Resources["Primary"] : Colors.White;
            nut.TextColor = laDuocChon ? Colors.White : (Color)Application.Current!.Resources["TextGrey"];
        }

        NapDanhSach();
    }

    private void NapDanhSach()
    {
        // TODO: thay bằng GET /api/gian-hang/don-hang?trangThai=... khi có API thật.
        if (DoiTacService.HuyDonQuaHan(MockData.GianHangHienTai.Id) > 0)
            _ = DisplayAlert("Đơn tự hủy", "Có đơn quá 5 phút chưa phản hồi đã bị hệ thống tự hủy và hoàn tiền cho khách.", "OK");
        var danhSach = MockData.DonHangCuaGianHang
            .Where(d => d.GianHangId == MockData.GianHangHienTai.Id)
            .Where(d => _dangLoc is null || d.TrangThai == _dangLoc)
            .OrderByDescending(d => d.NgayDat)
            .ToList();

        DsDonHang.ItemsSource = danhSach;
        LblRong.IsVisible = danhSach.Count == 0;
    }

    private async void OnXacNhanClicked(object sender, EventArgs e)
    {
        if (sender is not Button { CommandParameter: DonHang don }) return;

        await DoiTacService.XacNhanDonAsync(don);

        NapDanhSach();
        await DisplayAlert("Đã xác nhận", $"Đơn #{don.Id} đã được xác nhận, hãy bắt đầu chuẩn bị món.", "OK");
    }

    private async void OnTuChoiClicked(object sender, EventArgs e)
    {
        if (sender is not Button { CommandParameter: DonHang don }) return;

        if (await DoiTacService.TuChoiDonAsync(this, don)) NapDanhSach();
    }

    private async void OnSanSangGiaoClicked(object sender, EventArgs e)
    {
        if (sender is not Button { CommandParameter: DonHang don }) return;
        if (don.TrangThai != TrangThaiDonHang.DangLam || don.SanSangGiao) return;

        await DoiTacService.BanGiaoTaiXeAsync(don);

        NapDanhSach();
        await DisplayAlert("Đã bàn giao", $"Đơn #{don.Id} đã sẵn sàng, đang chờ tài xế đến lấy.", "OK");
    }

    private void OnRefreshing(object? sender, EventArgs e)
    {
        NapDanhSach();
        RefreshDsDon.IsRefreshing = false;
    }
}
