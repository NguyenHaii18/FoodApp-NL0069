using FoodApp.Maui.Models;
using System.Collections.ObjectModel;

namespace FoodApp.Maui.Views;

[QueryProperty(nameof(GianHang), "GianHang")]
public partial class MenuCartPage : ContentPage
{
    private readonly List<MucGioHang> _gioHang = new();

    public GianHang GianHang { get; set; } = null!;

    public MenuCartPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (GianHang is null) return;

        LblIconGianHang.Text = GianHang.HinhAnh;
        ImgAnhGianHang.Source = GianHang.CoAnh ? ImageSource.FromFile(GianHang.AnhPath) : null;
        ImgAnhGianHang.IsVisible = GianHang.CoAnh;
        LblIconGianHang.IsVisible = !GianHang.CoAnh;
        LblTenGianHang.Text = GianHang.TenGianHang;
        LblYeuThich.Text = Services.TaiKhoanService.LaYeuThich(GianHang.Id) ? "♥" : "♡";
        LblThongTinGianHang.Text = $"⭐ {GianHang.DanhGia}   {GianHang.ThoiGianGiaoPhut} phút giao";

        DsMonAn.ItemsSource = MockData.DanhSachMonAn
            .Where(m => m.GianHangId == GianHang.Id && m.ConHang)   // ẩn món tạm hết
            .ToList();
    }

    private async void OnYeuThichTapped(object? sender, TappedEventArgs e)
    {
        var gio = Services.TaiKhoanService.DaoYeuThich(GianHang.Id);
        LblYeuThich.Text = gio ? "♥" : "♡";
        await DisplayAlert(gio ? "Đã lưu" : "Đã bỏ", gio ? $"Đã thêm {GianHang.TenGianHang} vào quán yêu thích." : $"Đã bỏ {GianHang.TenGianHang} khỏi quán yêu thích.", "OK");
    }

    private void OnThemMonClicked(object sender, EventArgs e)
    {
        if (sender is not Button { CommandParameter: MonAn mon }) return;

        var dong = _gioHang.FirstOrDefault(x => x.MonAn.Id == mon.Id);
        if (dong is null)
            _gioHang.Add(new MucGioHang { MonAn = mon, SoLuong = 1 });
        else
            dong.SoLuong++;

        CapNhatThanhGioHang();
    }

    private void CapNhatThanhGioHang()
    {
        if (_gioHang.Count == 0)
        {
            BtnXemGioHang.IsVisible = false;
            return;
        }

        var tongSoLuong = _gioHang.Sum(x => x.SoLuong);
        var tongTien = _gioHang.Sum(x => x.ThanhTien);
        BtnXemGioHang.Text = $"{tongSoLuong} món   •   Xem giỏ hàng   •   {tongTien:N0}đ";
        BtnXemGioHang.IsVisible = true;
    }

    private async void OnXemGioHangClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(PaymentPage), new Dictionary<string, object>
        {
            { "GioHang", new ObservableCollection<MucGioHang>(_gioHang) },
            { "TenGianHang", GianHang.TenGianHang },
            { "GianHangId", GianHang.Id },
        });
    }
}
