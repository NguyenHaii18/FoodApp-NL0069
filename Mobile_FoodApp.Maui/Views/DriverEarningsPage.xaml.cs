using FoodApp.Maui.Models;
using FoodApp.Maui.Services;

namespace FoodApp.Maui.Views;

public partial class DriverEarningsPage : ContentPage
{
    private string _ky = "ngay";

    public DriverEarningsPage() => InitializeComponent();

    protected override void OnAppearing()
    {
        base.OnAppearing();
        Ve();
    }

    private void OnKyClicked(object? sender, EventArgs e)
    {
        if (sender is Button { CommandParameter: string ky }) { _ky = ky; Ve(); }
    }

    private (DateTime Tu, DateTime Den, string Nhan) Khoang()
    {
        var h = DateTime.Today;
        switch (_ky)
        {
            case "tuan": var t = ThuNhapService.DauTuan(h); return (t, t.AddDays(6), $"Tuần {t:dd/MM} – {t.AddDays(6):dd/MM/yyyy}");
            case "thang": var d = new DateTime(h.Year, h.Month, 1); return (d, d.AddMonths(1).AddDays(-1), $"Tháng {h:MM/yyyy}");
            default: return (h, h, $"Hôm nay {h:dd/MM/yyyy}");
        }
    }

    private void Ve()
    {
        var chinh = (Color)Application.Current!.Resources["Primary"];
        var nhat = (Color)Application.Current.Resources["PrimarySoft"];
        foreach (var (nut, ky) in new[] { (BtnNgay, "ngay"), (BtnTuan, "tuan"), (BtnThang, "thang") })
        {
            nut.BackgroundColor = _ky == ky ? chinh : nhat;
            nut.TextColor = _ky == ky ? Colors.White : chinh;
        }

        var (tu, den, nhan) = Khoang();
        LblKy.Text = nhan;
        var tk = ThuNhapService.Tong(tu, den, gomThuongTuan: _ky != "ngay");
        LblTong.Text = $"{tk.Tong:N0}đ";
        LblChiTiet.Text = $"Phí giao {tk.Phi:N0}đ · Phụ cấp {tk.PhuCap:N0}đ · Thưởng {tk.Thuong:N0}đ\n" +
                          $"{tk.SoDon} đơn thành công" + (tk.SoThatBai > 0 ? $" · {tk.SoThatBai} đơn không thành công" : "");
        LblTienDo.Text = ThuNhapService.TienDoThuongNgay() + $"\nTuần này: {ThuNhapService.SoDonTuan(DateTime.Today)} đơn";

        DsLichSu.Children.Clear();
        var ds = MockData.DonHangDaGiaoCuaTaiXe
            .Where(d => d.GiaoLuc is { } g && g.Date >= tu.Date && g.Date <= den.Date)
            .OrderByDescending(d => d.GiaoLuc).ToList();
        if (ds.Count == 0)
            DsLichSu.Children.Add(new Label { Text = "Chưa có đơn nào trong kỳ này.", FontSize = 13, TextColor = (Color)Application.Current.Resources["TextGrey"] });
        foreach (var d in ds)
        {
            var ok = !d.GiaoThatBai;
            var cot = new VerticalStackLayout { Spacing = 2 };
            cot.Children.Add(new Label { Text = $"{d.TenGianHang} · #{d.Id}", FontAttributes = FontAttributes.Bold, FontSize = 13.5 });
            cot.Children.Add(new Label { Text = $"{d.GiaoLuc:dd/MM HH:mm} · " + (ok ? "Giao thành công" : $"❌ {d.LyDoThatBai}"), FontSize = 12 });
            if (ok && d.DiemTaiXe > 0) cot.Children.Add(new Label { Text = $"Khách chấm {new string('★', d.DiemTaiXe)}", FontSize = 12 });
            var luoi = new Grid { ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) } };
            luoi.Add(cot, 0);
            luoi.Add(new Label
            {
                Text = ok ? $"+{d.PhiGiaoHang + d.PhuCap:N0}đ" : "0đ", FontAttributes = FontAttributes.Bold, VerticalOptions = LayoutOptions.Center,
                TextColor = (Color)Application.Current.Resources[ok ? "Success" : "Danger"],
            }, 1);
            DsLichSu.Children.Add(new Frame { Content = luoi, Padding = new Thickness(14, 10), CornerRadius = 12, HasShadow = false, BorderColor = (Color)Application.Current.Resources["PrimarySoft"] });
        }
    }
}
