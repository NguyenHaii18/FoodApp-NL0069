using FoodApp.Maui.Models;
using FoodApp.Maui.Services;

namespace FoodApp.Maui.Views;

/// <summary>Doanh thu ngày / tháng / năm và báo cáo: đơn hàng, món bán chạy, doanh thu, lợi nhuận (NV09).</summary>
public partial class VendorRevenuePage : ContentPage
{
    private KyBaoCao _ky = KyBaoCao.Ngay;
    private DateTime _moc = DateTime.Today;

    public VendorRevenuePage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        NapBaoCao();
        await DuLieuService.TaiDonHangGianHangAsync(MockData.GianHangHienTai.Id);
        NapBaoCao();
    }

    private static Color Mau(string key) => (Color)Application.Current!.Resources[key];

    private void OnChonKyNgay(object? s, EventArgs e) => DoiKy(KyBaoCao.Ngay);
    private void OnChonKyThang(object? s, EventArgs e) => DoiKy(KyBaoCao.Thang);
    private void OnChonKyNam(object? s, EventArgs e) => DoiKy(KyBaoCao.Nam);

    private void DoiKy(KyBaoCao ky)
    {
        _ky = ky;
        _moc = DateTime.Today;
        NapBaoCao();
    }

    private void OnKyTruoc(object? s, EventArgs e)
    {
        _moc = DoiTacService.LuiKy(_ky, _moc, -1);
        NapBaoCao();
    }

    private void OnKySau(object? s, EventArgs e)
    {
        var moi = DoiTacService.LuiKy(_ky, _moc, +1);
        if (DoiTacService.Khoang(_ky, moi).Tu > DateTime.Today) return;   // không xem kỳ ở tương lai
        _moc = moi;
        NapBaoCao();
    }

    private async void OnXemTaiChinhClicked(object sender, EventArgs e) => await Shell.Current.GoToAsync(nameof(VendorFinancePage));

    private void ToNutKy(Button nut, bool dangChon)
    {
        nut.BackgroundColor = dangChon ? Mau("Primary") : Colors.White;
        nut.TextColor = dangChon ? Colors.White : Mau("TextGrey");
        nut.BorderColor = Mau("BorderColor");
        nut.BorderWidth = 1;
    }

    private void NapBaoCao()
    {
        // TODO: thay bằng GET /api/gian-hang/doanh-thu?ky=ngay|thang|nam&moc=... khi có API thật.
        var gh = MockData.GianHangHienTai;
        var tatCa = DoiTacService.DonCuaGianHang(gh.Id).ToList();
        var trongKy = DoiTacService.TrongKy(tatCa, _ky, _moc);
        var hoanThanh = trongKy.Where(d => d.TrangThai == TrangThaiDonHang.HoanThanh).OrderByDescending(d => d.NgayDat).ToList();
        var tc = DoiTacService.TinhTaiChinh(gh, hoanThanh);

        ToNutKy(BtnKyNgay, _ky == KyBaoCao.Ngay);
        ToNutKy(BtnKyThang, _ky == KyBaoCao.Thang);
        ToNutKy(BtnKyNam, _ky == KyBaoCao.Nam);
        LblNhanKy.Text = DoiTacService.NhanKy(_ky, _moc);
        BtnKySau.IsEnabled = DoiTacService.Khoang(_ky, DoiTacService.LuiKy(_ky, _moc, +1)).Tu <= DateTime.Today;

        LblDoanhThu.Text = $"{tc.DoanhThuMon:N0}đ";
        LblTongDonHoanThanh.Text = tc.SoDon.ToString();
        LblGiaTriTrungBinh.Text = $"{tc.GiaTriTrungBinh:N0}đ";
        LblLoiNhuan.Text = $"{tc.LoiNhuan:N0}đ";
        LblGhiChuLoiNhuan.IsVisible = tc.SoDon > 0 && tc.SoDonThieuGiaVon > 0;
        LblGhiChuLoiNhuan.Text = $"* {tc.SoDonThieuGiaVon}/{tc.SoDon} đơn có món chưa nhập giá vốn (vào Thực đơn → Sửa món) nên lợi nhuận thực tế có thể thấp hơn. " +
                                 "Lợi nhuận = thực nhận (đã trừ phí nền tảng) − giá vốn.";

        NapXuHuong(tatCa);
        NapTrangThai(trongKy);
        NapMonBanChay(hoanThanh);

        DsDonHoanThanh.ItemsSource = hoanThanh;
        LblRong.IsVisible = hoanThanh.Count == 0;
    }

    /// <summary>Biểu đồ thanh ngang: 7 ngày / 12 tháng / 5 năm gần nhất tính đến kỳ đang xem.</summary>
    private void NapXuHuong(List<DonHang> tatCa)
    {
        var (soKy, tieuDe) = _ky switch
        {
            KyBaoCao.Ngay => (7, "Doanh thu 7 ngày gần nhất"),
            KyBaoCao.Thang => (12, "Doanh thu 12 tháng gần nhất"),
            _ => (5, "Doanh thu 5 năm gần nhất"),
        };
        LblTieuDeXuHuong.Text = tieuDe;
        KhungXuHuong.Children.Clear();

        var dong = new List<(string Nhan, decimal Tien, bool HienTai)>();
        for (var i = soKy - 1; i >= 0; i--)
        {
            var moc = DoiTacService.LuiKy(_ky, _moc, -i);
            var tien = DoiTacService.TrongKy(tatCa, _ky, moc)
                .Where(d => d.TrangThai == TrangThaiDonHang.HoanThanh).Sum(d => d.TongTien);
            var nhan = _ky switch
            {
                KyBaoCao.Ngay => moc.ToString("dd/MM"),
                KyBaoCao.Thang => moc.ToString("MM/yy"),
                _ => moc.Year.ToString(),
            };
            dong.Add((nhan, tien, i == 0));
        }

        var max = dong.Max(d => d.Tien);
        foreach (var (nhan, tien, hienTai) in dong)
        {
            var luoi = new Grid
            {
                ColumnSpacing = 8,
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = new GridLength(52) },
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = GridLength.Auto },
                },
            };
            var lblNhan = new Label { Text = nhan, FontSize = 12, VerticalOptions = LayoutOptions.Center,
                FontAttributes = hienTai ? FontAttributes.Bold : FontAttributes.None, TextColor = hienTai ? Mau("Primary") : Mau("TextGrey") };
            var thanh = new ProgressBar { Progress = max > 0 ? (double)(tien / max) : 0, ProgressColor = hienTai ? Mau("Primary") : Mau("PrimaryLight"),
                VerticalOptions = LayoutOptions.Center };
            var lblTien = new Label { Text = $"{tien:N0}đ", FontSize = 12, VerticalOptions = LayoutOptions.Center,
                FontAttributes = hienTai ? FontAttributes.Bold : FontAttributes.None };
            Grid.SetColumn(lblNhan, 0); Grid.SetColumn(thanh, 1); Grid.SetColumn(lblTien, 2);
            luoi.Children.Add(lblNhan); luoi.Children.Add(thanh); luoi.Children.Add(lblTien);
            KhungXuHuong.Children.Add(luoi);
        }
    }

    private void NapTrangThai(List<DonHang> trongKy)
    {
        KhungTrangThai.Children.Clear();
        if (trongKy.Count == 0)
        {
            KhungTrangThai.Children.Add(new Label { Text = "Chưa có đơn hàng trong kỳ này.", FontSize = 13, TextColor = Mau("TextGrey") });
            return;
        }
        foreach (var tt in Enum.GetValues<TrangThaiDonHang>())
        {
            var so = trongKy.Count(d => d.TrangThai == tt);
            ThemDong(KhungTrangThai, tt.Nhan(), $"{so} đơn", tt == TrangThaiDonHang.DaHuy && so > 0 ? Mau("Danger") : Mau("Primary"));
        }
        ThemDong(KhungTrangThai, "Tổng số đơn", $"{trongKy.Count} đơn", Mau("TextDark"), true);
    }

    private void NapMonBanChay(List<DonHang> donHoanThanh)
    {
        KhungMonBanChay.Children.Clear();

        var monBanChay = donHoanThanh
            .SelectMany(d => d.ChiTiet)
            .GroupBy(ct => ct.MonAn.TenMon)
            .Select(g => new { TenMon = g.Key, SoLuong = g.Sum(ct => ct.SoLuong), DoanhThu = g.Sum(ct => ct.ThanhTien) })
            .OrderByDescending(x => x.SoLuong)
            .Take(5)
            .ToList();

        if (monBanChay.Count == 0)
        {
            KhungMonBanChay.Children.Add(new Label { Text = "Chưa có dữ liệu bán hàng trong kỳ này.", FontSize = 13, TextColor = Mau("TextGrey") });
            return;
        }

        foreach (var mon in monBanChay)
            ThemDong(KhungMonBanChay, mon.TenMon, $"{mon.SoLuong} phần · {mon.DoanhThu:N0}đ", Mau("Primary"));
    }

    private static void ThemDong(VerticalStackLayout cha, string trai, string phai, Color mauPhai, bool dam = false)
    {
        var dong = new Grid { ColumnDefinitions = { new ColumnDefinition { Width = GridLength.Star }, new ColumnDefinition { Width = GridLength.Auto } } };
        var lblTrai = new Label { Text = trai, FontSize = 13, FontAttributes = dam ? FontAttributes.Bold : FontAttributes.None };
        var lblPhai = new Label { Text = phai, FontSize = 13, FontAttributes = FontAttributes.Bold, TextColor = mauPhai };
        Grid.SetColumn(lblTrai, 0);
        Grid.SetColumn(lblPhai, 1);
        dong.Children.Add(lblTrai);
        dong.Children.Add(lblPhai);
        cha.Children.Add(dong);
    }
}
