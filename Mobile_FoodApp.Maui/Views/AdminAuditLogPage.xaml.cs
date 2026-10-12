using FoodApp.Maui.Models;
using FoodApp.Maui.Services;

namespace FoodApp.Maui.Views;

/// <summary>Xem nhật ký thao tác — chỉ Admin tổng. Nhật ký chỉ đọc: không có nút xóa để bảo đảm tính đối soát.</summary>
public partial class AdminAuditLogPage : ContentPage
{
    private enum Loc { TatCa, QuanTri, DangNhap, BiTuChoi }
    private Loc _loc = Loc.TatCa;

    public AdminAuditLogPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (!await PhanQuyenService.KiemTraAsync(this, Quyen.XemNhatKy))
        {
            await Shell.Current.GoToAsync("..");
            return;
        }
        // Online: đọc nhật ký từ Web; offline: nhật ký trên máy.
        _tuServer = await NhatKyService.TaiTuServerAsync();
        NapDanhSach();
    }

    private List<NhatKyThaoTac>? _tuServer;

    private static Color Mau(string k) => (Color)Application.Current!.Resources[k];

    private void VeThanhLoc()
    {
        ThanhLoc.Children.Clear();
        foreach (var (nhan, loc) in new[]
        {
            ("Tất cả", Loc.TatCa), ("Thao tác quản trị", Loc.QuanTri), ("Đăng nhập / đăng xuất", Loc.DangNhap), ("Bị từ chối / thất bại", Loc.BiTuChoi),
        })
        {
            var chon = loc == _loc;
            var nut = new Button
            {
                Text = nhan, CornerRadius = 18, HeightRequest = 36, Padding = new Thickness(14, 0), FontSize = 12.5,
                BackgroundColor = chon ? Mau("Primary") : Mau("PrimarySoft"),
                TextColor = chon ? Colors.White : Mau("Primary"),
            };
            nut.Clicked += (_, _) => { _loc = loc; NapDanhSach(); };
            ThanhLoc.Children.Add(nut);
        }
    }

    private void OnTimChanged(object? sender, TextChangedEventArgs e) => NapDanhSach();

    private void NapDanhSach()
    {
        VeThanhLoc();
        var tuKhoa = (EntryTim.Text ?? "").Trim().ToLowerInvariant();
        IEnumerable<NhatKyThaoTac> ds = _tuServer ?? NhatKyService.TatCa();

        ds = _loc switch
        {
            Loc.QuanTri => ds.Where(x => (x.VaiTro.StartsWith("QuanTri") || x.VaiTro.Contains("QuanTri")) && !x.HanhDong.StartsWith("Đăng")),
            Loc.DangNhap => ds.Where(x => x.HanhDong.StartsWith("Đăng nhập") || x.HanhDong.StartsWith("Đăng xuất")),
            Loc.BiTuChoi => ds.Where(x => !x.ThanhCong),
            _ => ds,
        };
        if (tuKhoa.Length > 0)
            ds = ds.Where(x => (x.TaiKhoan + " " + x.HanhDong + " " + x.DoiTuong + " " + x.ChiTiet).ToLowerInvariant().Contains(tuKhoa));

        var list = ds.Take(300).ToList();
        DsNhatKy.ItemsSource = list;
        LblSo.Text = $"{list.Count} dòng (hiển thị tối đa 300 dòng mới nhất)";
    }
}
