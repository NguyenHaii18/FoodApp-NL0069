using FoodApp.Maui.Models;

namespace FoodApp.Maui.Views;

public partial class AdminStatisticsPage : ContentPage
{
    private readonly (string Nhan, int? SoNgay)[] _boLoc =
    {
        ("Hôm nay", 0),
        ("7 ngày qua", 7),
        ("30 ngày qua", 30),
        ("Tháng này", -1),
        ("Năm nay", -2),
        ("Tất cả", null),
    };

    // Chi phí vận hành: nền tảng thu phí trên doanh thu mỗi đơn hoàn thành (đổi tỉ lệ tại đây).
    private const decimal TyLePhiNenTang = 0.10m;

    private int _kyDangChon = 0; // 0 = ngày, 1 = tháng, 2 = năm
    private readonly List<Button> _nutKy = new();

    private int? _soNgayDangLoc = 7;
    private readonly List<Button> _nutBoLoc = new();

    public AdminStatisticsPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (!await Services.PhanQuyenService.KiemTraAsync(this, Services.Quyen.XemThongKe)) { await Shell.Current.GoToAsync(".."); return; }
        DungBoLoc();
        DungNutKy();
        NapThongKe();
    }

    private void DungNutKy()
    {
        KhungKy.Children.Clear();
        _nutKy.Clear();
        var nhan = new[] { "Theo ngày", "Theo tháng", "Theo năm" };
        for (int i = 0; i < nhan.Length; i++)
        {
            int ky = i;
            var nut = new Button { Text = nhan[i], CornerRadius = 16, HeightRequest = 34, Padding = new Thickness(14, 0), FontSize = 12, BorderWidth = 1,
                                   BorderColor = (Color)Application.Current!.Resources["BorderColor"] };
            nut.Clicked += (_, _) => { _kyDangChon = ky; ToMauNutKy(); NapDoanhThuTheoKy(); };
            _nutKy.Add(nut);
            KhungKy.Children.Add(nut);
        }
        ToMauNutKy();
    }

    private void ToMauNutKy()
    {
        for (int i = 0; i < _nutKy.Count; i++)
        {
            bool chon = i == _kyDangChon;
            _nutKy[i].BackgroundColor = chon ? (Color)Application.Current!.Resources["TextDark"] : Colors.White;
            _nutKy[i].TextColor = chon ? Colors.White : (Color)Application.Current!.Resources["TextGrey"];
        }
    }

    /// <summary>Doanh thu gộp theo ngày (30 ngày gần nhất), tháng (12 tháng) hoặc năm — dựa trên đơn hoàn thành.</summary>
    private void NapDoanhThuTheoKy()
    {
        KhungDoanhThuKy.Children.Clear();
        var hoanThanh = MockData.TatCaDonHangHeThong.Where(d => d.TrangThai == TrangThaiDonHang.HoanThanh).ToList();

        var nhom = (_kyDangChon switch
        {
            0 => hoanThanh.Where(d => d.NgayDat >= DateTime.Now.Date.AddDays(-29))
                          .GroupBy(d => d.NgayDat.Date).Select(g => (Moc: g.Key, Nhan: g.Key.ToString("dd/MM/yyyy"), Tien: g.Sum(d => d.TongTien), So: g.Count())),
            1 => hoanThanh.Where(d => d.NgayDat >= new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(-11))
                          .GroupBy(d => new DateTime(d.NgayDat.Year, d.NgayDat.Month, 1)).Select(g => (Moc: g.Key, Nhan: g.Key.ToString("MM/yyyy"), Tien: g.Sum(d => d.TongTien), So: g.Count())),
            _ => hoanThanh.GroupBy(d => new DateTime(d.NgayDat.Year, 1, 1)).Select(g => (Moc: g.Key, Nhan: g.Key.ToString("yyyy"), Tien: g.Sum(d => d.TongTien), So: g.Count())),
        }).OrderByDescending(x => x.Moc).ToList();

        if (nhom.Count == 0)
        {
            KhungDoanhThuKy.Children.Add(new Label { Text = "Chưa có dữ liệu doanh thu.", FontSize = 13, TextColor = (Color)Application.Current!.Resources["TextGrey"] });
            return;
        }

        var cao = nhom.Max(x => x.Tien);
        foreach (var k in nhom)
        {
            var tren = new Grid { ColumnDefinitions = new ColumnDefinitionCollection { new(GridLength.Star), new(GridLength.Auto) } };
            var a = new Label { Text = $"{k.Nhan}  •  {k.So} đơn", FontSize = 12.5 };
            var b = new Label { Text = $"{k.Tien:N0}đ", FontSize = 12.5, FontAttributes = FontAttributes.Bold };
            Grid.SetColumn(a, 0); Grid.SetColumn(b, 1);
            tren.Children.Add(a); tren.Children.Add(b);

            var thanh = new Grid();
            thanh.Children.Add(new BoxView { HeightRequest = 6, CornerRadius = 3, Color = (Color)Application.Current!.Resources["Background"] });
            thanh.Children.Add(new BoxView
            {
                HeightRequest = 6, CornerRadius = 3, Color = (Color)Application.Current!.Resources["Primary"],
                HorizontalOptions = LayoutOptions.Start,
                WidthRequest = Math.Max(6, 260 * (cao > 0 ? (double)(k.Tien / cao) : 0)),
            });
            KhungDoanhThuKy.Children.Add(new VerticalStackLayout { Spacing = 4, Children = { tren, thanh } });
        }
    }

    private void DungBoLoc()
    {
        KhungBoLoc.Children.Clear();
        _nutBoLoc.Clear();

        foreach (var (nhan, soNgay) in _boLoc)
        {
            var nut = new Button
            {
                Text = nhan,
                CornerRadius = 16,
                HeightRequest = 34,
                Padding = new Thickness(14, 0),
                FontSize = 12,
                BackgroundColor = soNgay == _soNgayDangLoc ? (Color)Application.Current!.Resources["TextDark"] : Colors.White,
                TextColor = soNgay == _soNgayDangLoc ? Colors.White : (Color)Application.Current!.Resources["TextGrey"],
                BorderColor = (Color)Application.Current!.Resources["BorderColor"],
                BorderWidth = 1,
            };
            nut.Clicked += (_, _) => LocTheoThoiGian(soNgay, nut);
            _nutBoLoc.Add(nut);
            KhungBoLoc.Children.Add(nut);
        }
    }

    private void LocTheoThoiGian(int? soNgay, Button nutDuocChon)
    {
        _soNgayDangLoc = soNgay;

        foreach (var nut in _nutBoLoc)
        {
            bool laDuocChon = nut == nutDuocChon;
            nut.BackgroundColor = laDuocChon ? (Color)Application.Current!.Resources["TextDark"] : Colors.White;
            nut.TextColor = laDuocChon ? Colors.White : (Color)Application.Current!.Resources["TextGrey"];
        }

        NapThongKe();
    }

    private bool TrongKhoangLoc(DateTime ngay)
    {
        if (_soNgayDangLoc is null) return true;           // "Tất cả"
        if (_soNgayDangLoc == 0) return ngay.Date == DateTime.Now.Date; // "Hôm nay"
        if (_soNgayDangLoc == -1) return ngay.Year == DateTime.Now.Year && ngay.Month == DateTime.Now.Month; // "Tháng này"
        if (_soNgayDangLoc == -2) return ngay.Year == DateTime.Now.Year;                                    // "Năm nay"
        return ngay >= DateTime.Now.AddDays(-_soNgayDangLoc.Value);
    }

    private void NapThongKe()
    {
        // TODO: thay toàn bộ khối này bằng GET /api/quan-tri/thong-ke?tuNgay=...&denNgay=... khi có API + CSDL thật.
        var donTrongKhoang = MockData.TatCaDonHangHeThong.Where(d => TrongKhoangLoc(d.NgayDat)).ToList();
        var donHoanThanh = donTrongKhoang.Where(d => d.TrangThai == TrangThaiDonHang.HoanThanh).ToList();
        var donDaHuy = donTrongKhoang.Where(d => d.TrangThai == TrangThaiDonHang.DaHuy).ToList();

        LblTongDoanhThu.Text = $"{donHoanThanh.Sum(d => d.TongTien):N0}đ";
        LblTongDonHoanThanh.Text = donHoanThanh.Count.ToString();
        LblGiaTriTrungBinh.Text = donHoanThanh.Count > 0 ? $"{donHoanThanh.Average(d => d.TongTien):N0}đ" : "0đ";
        LblTiLeHuy.Text = donTrongKhoang.Count > 0
            ? $"{(double)donDaHuy.Count / donTrongKhoang.Count * 100:0.#}%"
            : "0%";

        var tongDoanhThu = donHoanThanh.Sum(d => d.TongTien);
        LblNhanPhi.Text = $"Phí nền tảng ({TyLePhiNenTang * 100:0.#}%)";
        LblPhiNenTang.Text = $"{tongDoanhThu * TyLePhiNenTang:N0}đ";
        LblThucNhanDoiTac.Text = $"{tongDoanhThu * (1 - TyLePhiNenTang):N0}đ";

        NapDoanhThuTheoKy();
        NapDoanhSoGianHang(donHoanThanh);
        NapMonBanChay(donHoanThanh);
        NapHieuQuaKhuyenMai();
    }

    private void NapDoanhSoGianHang(List<DonHang> donHoanThanh)
    {
        KhungDoanhSoGianHang.Children.Clear();

        var doanhSo = donHoanThanh
            .GroupBy(d => d.GianHangId)
            .Select(g => new
            {
                GianHangId = g.Key,
                Ten = MockData.DanhSachGianHang.FirstOrDefault(x => x.Id == g.Key)?.TenGianHang ?? "Không rõ",
                DoanhThu = g.Sum(d => d.TongTien),
                SoDon = g.Count(),
            })
            .OrderByDescending(x => x.DoanhThu)
            .ToList();

        FrameDoanhSoGianHang.IsVisible = doanhSo.Count > 0;
        LblRongGianHang.IsVisible = doanhSo.Count == 0;
        if (doanhSo.Count == 0) return;

        var doanhThuCaoNhat = doanhSo[0].DoanhThu;
        foreach (var gh in doanhSo)
        {
            var hang = new VerticalStackLayout { Spacing = 4 };

            var hangTren = new Grid { ColumnDefinitions = new ColumnDefinitionCollection { new(GridLength.Star), new(GridLength.Auto) } };
            var lblTen = new Label { Text = gh.Ten, FontSize = 13, FontAttributes = FontAttributes.Bold };
            var lblDoanhThu = new Label { Text = $"{gh.DoanhThu:N0}đ", FontSize = 13, FontAttributes = FontAttributes.Bold, TextColor = (Color)Application.Current!.Resources["TextDark"] };
            Grid.SetColumn(lblTen, 0);
            Grid.SetColumn(lblDoanhThu, 1);
            hangTren.Children.Add(lblTen);
            hangTren.Children.Add(lblDoanhThu);

            var lblSoDon = new Label { Text = $"{gh.SoDon} đơn hoàn thành  •  phí nền tảng {gh.DoanhThu * TyLePhiNenTang:N0}đ", FontSize = 11, TextColor = (Color)Application.Current!.Resources["TextGrey"] };

            var nenThanh = new BoxView { HeightRequest = 6, CornerRadius = 3, Color = (Color)Application.Current!.Resources["Background"] };
            var tyLe = doanhThuCaoNhat > 0 ? (double)(gh.DoanhThu / doanhThuCaoNhat) : 0;
            var thanhTien = new BoxView
            {
                HeightRequest = 6,
                CornerRadius = 3,
                Color = (Color)Application.Current!.Resources["Primary"],
                HorizontalOptions = LayoutOptions.Start,
                WidthRequest = Math.Max(6, 260 * tyLe),
            };
            var lopThanh = new Grid();
            lopThanh.Children.Add(nenThanh);
            lopThanh.Children.Add(thanhTien);

            hang.Children.Add(hangTren);
            hang.Children.Add(lblSoDon);
            hang.Children.Add(lopThanh);
            KhungDoanhSoGianHang.Children.Add(hang);
        }
    }

    private void NapMonBanChay(List<DonHang> donHoanThanh)
    {
        KhungMonBanChay.Children.Clear();

        var monBanChay = donHoanThanh
            .SelectMany(d => d.ChiTiet)
            .GroupBy(ct => ct.MonAn.TenMon)
            .Select(g => new { TenMon = g.Key, SoLuong = g.Sum(ct => ct.SoLuong) })
            .OrderByDescending(x => x.SoLuong)
            .Take(5)
            .ToList();

        if (monBanChay.Count == 0)
        {
            KhungMonBanChay.Children.Add(new Label
            {
                Text = "Chưa có dữ liệu bán hàng trong khoảng thời gian này.",
                FontSize = 13,
                TextColor = (Color)Application.Current!.Resources["TextGrey"],
            });
            return;
        }

        int hang = 1;
        foreach (var mon in monBanChay)
        {
            var dong = new Grid { ColumnDefinitions = new ColumnDefinitionCollection { new(GridLength.Auto), new(GridLength.Star), new(GridLength.Auto) }, ColumnSpacing = 8 };
            var lblHang = new Label { Text = $"#{hang}", FontSize = 13, FontAttributes = FontAttributes.Bold, TextColor = (Color)Application.Current!.Resources["Accent"], WidthRequest = 24 };
            var lblTen = new Label { Text = mon.TenMon, FontSize = 13, VerticalOptions = LayoutOptions.Center };
            var lblSoLuong = new Label { Text = $"{mon.SoLuong} phần", FontSize = 13, FontAttributes = FontAttributes.Bold };
            Grid.SetColumn(lblHang, 0);
            Grid.SetColumn(lblTen, 1);
            Grid.SetColumn(lblSoLuong, 2);
            dong.Children.Add(lblHang);
            dong.Children.Add(lblTen);
            dong.Children.Add(lblSoLuong);
            KhungMonBanChay.Children.Add(dong);
            hang++;
        }
    }

    private void NapHieuQuaKhuyenMai()
    {
        KhungKhuyenMai.Children.Clear();

        // Lưu ý: dữ liệu mock hiện chưa gắn kết quả áp mã với từng đơn hàng cụ thể,
        // nên "hiệu quả" ở đây dựa trên số lượt đã dùng (SoLuongDaDung) được lưu sẵn
        // trên từng KhuyenMai. Khi có API + CSDL thật, thay bằng thống kê join theo đơn hàng.
        var danhSach = MockData.DanhSachKhuyenMai.OrderByDescending(k => k.SoLuongDaDung).ToList();

        if (danhSach.Count == 0)
        {
            KhungKhuyenMai.Children.Add(new Label
            {
                Text = "Chưa có chương trình khuyến mãi nào.",
                FontSize = 13,
                TextColor = (Color)Application.Current!.Resources["TextGrey"],
            });
            return;
        }

        foreach (var km in danhSach)
        {
            var hang = new VerticalStackLayout { Spacing = 3 };

            var hangTren = new Grid { ColumnDefinitions = new ColumnDefinitionCollection { new(GridLength.Star), new(GridLength.Auto) } };
            var lblMa = new Label { Text = $"{km.MaCode}  •  {km.PhamViHienThi}", FontSize = 12.5, FontAttributes = FontAttributes.Bold };
            var lblTrangThai = new Label { Text = km.TrangThaiHienThi, FontSize = 11, TextColor = (Color)Application.Current!.Resources["TextGrey"] };
            Grid.SetColumn(lblMa, 0);
            Grid.SetColumn(lblTrangThai, 1);
            hangTren.Children.Add(lblMa);
            hangTren.Children.Add(lblTrangThai);

            var tyLeDung = km.SoLuongToiDa > 0 ? (double)km.SoLuongDaDung / km.SoLuongToiDa : 0;
            var lblLuotDung = new Label
            {
                FontSize = 11.5,
                TextColor = (Color)Application.Current!.Resources["TextGrey"],
                Text = $"Đã dùng {km.SoLuongDaDung}/{km.SoLuongToiDa} lượt ({tyLeDung * 100:0.#}%)",
            };

            var nenThanh = new BoxView { HeightRequest = 6, CornerRadius = 3, Color = (Color)Application.Current!.Resources["Background"] };
            var thanhTien = new BoxView
            {
                HeightRequest = 6,
                CornerRadius = 3,
                Color = (Color)Application.Current!.Resources["Accent"],
                HorizontalOptions = LayoutOptions.Start,
                WidthRequest = Math.Max(6, 260 * Math.Min(tyLeDung, 1)),
            };
            var lopThanh = new Grid();
            lopThanh.Children.Add(nenThanh);
            lopThanh.Children.Add(thanhTien);

            hang.Children.Add(hangTren);
            hang.Children.Add(lblLuotDung);
            hang.Children.Add(lopThanh);
            KhungKhuyenMai.Children.Add(hang);
        }
    }

    private void OnRefreshing(object? sender, EventArgs e)
    {
        NapThongKe();
        RefreshThongKe.IsRefreshing = false;
    }
}
