using FoodApp.Maui.Models;
using FoodApp.Maui.Services;

namespace FoodApp.Maui.Views;

/// <summary>Danh sách quán đã thả tim (thả tim ở trang menu của quán). Chạm để mở quán, chạm ♥ để bỏ.</summary>
public partial class YeuThichPage : ContentPage
{
    public YeuThichPage() { InitializeComponent(); }

    protected override void OnAppearing() { base.OnAppearing(); Ve(); }

    private static Color Mau(string k) => (Color)Application.Current!.Resources[k];

    private void Ve()
    {
        DanhSach.Children.Clear();
        var ids = TaiKhoanService.LayYeuThich();
        var quan = MockData.DanhSachGianHang.Where(g => ids.Contains(g.Id)).ToList();
        if (quan.Count == 0)
        {
            DanhSach.Children.Add(new Label { Text = "Chưa có quán yêu thích.\nMở một quán và chạm ♡ để lưu lại.", HorizontalTextAlignment = TextAlignment.Center,
                Style = (Style)Application.Current!.Resources["SubtitleLabel"], HorizontalOptions = LayoutOptions.Center, Margin = new Thickness(0, 40) });
            return;
        }

        foreach (var g in quan)
        {
            var thong = new VerticalStackLayout { Spacing = 2, VerticalOptions = LayoutOptions.Center };
            thong.Children.Add(new Label { Text = g.TenGianHang, FontAttributes = FontAttributes.Bold, FontSize = 14.5 });
            thong.Children.Add(new Label { Text = g.DiaChi, FontSize = 12, TextColor = Mau("TextGrey") });
            thong.Children.Add(new Label { Text = $"⭐ {g.DanhGia}   ⏱ {g.ThoiGianGiaoPhut} phút", FontSize = 12, TextColor = Mau("TextGrey") });
            if (g.CoKhuyenMai) thong.Children.Add(new Label { Text = g.NhanKhuyenMai, FontSize = 11.5, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#B45309") });

            var tim = new Label { Text = "♥", FontSize = 24, TextColor = Mau("Danger"), VerticalOptions = LayoutOptions.Center };
            var id = g.Id;
            tim.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(() => { TaiKhoanService.DaoYeuThich(id); Ve(); }) });

            var luoi = new Grid { ColumnDefinitions = { new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) }, ColumnSpacing = 12 };
            luoi.Add(new Label { Text = g.HinhAnh, FontSize = 32, VerticalOptions = LayoutOptions.Center }, 0);
            luoi.Add(thong, 1);
            luoi.Add(tim, 2);

            var the = new Frame { Style = (Style)Application.Current!.Resources["CardFrame"], Content = luoi };
            var gh = g;
            the.GestureRecognizers.Add(new TapGestureRecognizer
            {
                Command = new Command(async () => await Shell.Current.GoToAsync(nameof(MenuCartPage), new Dictionary<string, object> { { "GianHang", gh } }))
            });
            DanhSach.Children.Add(the);
        }
    }
}
