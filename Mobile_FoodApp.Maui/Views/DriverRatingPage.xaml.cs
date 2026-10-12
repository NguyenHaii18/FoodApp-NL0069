using FoodApp.Maui.Models;
using FoodApp.Maui.Services;

namespace FoodApp.Maui.Views;

public partial class DriverRatingPage : ContentPage
{
    public DriverRatingPage() => InitializeComponent();

    protected override void OnAppearing()
    {
        base.OnAppearing();
        ThuNhapService.CapNhatChiSo();
        var ds = MockData.DonHangDaGiaoCuaTaiXe.Where(d => d.DiemTaiXe > 0).OrderByDescending(d => d.GiaoLuc).ToList();
        var diem = MockData.TaiXeHienTai.DanhGia;
        var lam = (int)Math.Round(diem);
        LblDiem.Text = ds.Count == 0 ? "Mới" : diem.ToString("0.0");
        LblSao.Text = ds.Count == 0 ? "☆☆☆☆☆" : new string('★', lam) + new string('☆', 5 - lam);
        LblSoLuot.Text = ds.Count == 0 ? "Chưa có đánh giá. Hoàn thành đơn để nhận đánh giá từ khách." : $"{ds.Count} lượt đánh giá";

        DsDanhGia.Children.Clear();
        foreach (var d in ds)
        {
            var cot = new VerticalStackLayout { Spacing = 3 };
            cot.Children.Add(new Label { Text = $"{new string('★', d.DiemTaiXe)}{new string('☆', 5 - d.DiemTaiXe)}  ·  {d.KhachTen}", FontAttributes = FontAttributes.Bold, FontSize = 13.5 });
            if (!string.IsNullOrWhiteSpace(d.NhanXetTaiXe)) cot.Children.Add(new Label { Text = d.NhanXetTaiXe, FontSize = 13 });
            cot.Children.Add(new Label { Text = $"Đơn #{d.Id} · {d.GiaoLuc:dd/MM/yyyy}", FontSize = 11.5, TextColor = (Color)Application.Current!.Resources["TextGrey"] });
            DsDanhGia.Children.Add(new Frame { Content = cot, Padding = new Thickness(14, 10), CornerRadius = 12, HasShadow = false, BorderColor = (Color)Application.Current!.Resources["PrimarySoft"] });
        }
    }
}
