using FoodApp.Maui.Models;
using FoodApp.Maui.Services;

namespace FoodApp.Maui.Views;

public partial class LichSuDangNhapPage : ContentPage
{
    private sealed class DongServer
    {
        public DateTime ThoiGian { get; set; }
        public string? NoiDung { get; set; }
        public string? DiaChiIp { get; set; }
        public bool ThanhCong { get; set; }
    }

    public LichSuDangNhapPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (Phien.Khach is not { } k) { DsLichSu.ItemsSource = new List<NhatKyThaoTac>(); return; }

        // Online: 30 sự kiện gần nhất do Web ghi (GET tai-khoan/lich-su-dang-nhap). Offline: nhật ký trên máy.
        if (DuLieuService.DangOnline)
        {
            var r = await ApiClient.GetAsync<List<DongServer>>("api/mobile/tai-khoan/lich-su-dang-nhap");
            if (r.ThanhCong && r.DuLieu is not null)
            {
                DsLichSu.ItemsSource = r.DuLieu.Select(d => new NhatKyThaoTac
                {
                    ThoiGian = d.ThoiGian, VaiTro = "KhachHang", TaiKhoan = k.Email, ThanhCong = d.ThanhCong,
                    HanhDong = d.ThanhCong ? "Đăng nhập" : "Đăng nhập thất bại",
                    ChiTiet = string.IsNullOrWhiteSpace(d.DiaChiIp) ? d.NoiDung ?? "" : $"{d.NoiDung} · IP {d.DiaChiIp}",
                }).ToList();
                return;
            }
        }
        DsLichSu.ItemsSource = NhatKyService.LichSuDangNhap("KhachHang", k.Email);
    }
}
