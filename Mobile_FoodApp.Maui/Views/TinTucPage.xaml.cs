using FoodApp.Maui.Models;

namespace FoodApp.Maui.Views;

/// <summary>Danh sách tin mới / thông báo, lọc theo loại; chạm vào tin để xem nội dung.</summary>
public partial class TinTucPage : ContentPage
{
    private string _loai = "";
    private readonly HashSet<string> _mo = new();

    public TinTucPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        TinTucData.LamMoi();
        VeLoc();
        VeDanhSach();
        TinTucData.DanhDauDaXem();
    }

    private static Color Mau(string k) => (Color)Application.Current!.Resources[k];

    private void VeLoc()
    {
        ThanhLoc.Children.Clear();
        foreach (var (ma, nhan) in new[] { ("", "Tất cả"), ("KhuyenMai", "Khuyến mãi"), ("TinQuan", "Quán mới"), ("HeThong", "Hệ thống") })
        {
            var chon = ma == _loai;
            var nut = new Button
            {
                Text = nhan, CornerRadius = 18, HeightRequest = 36, Padding = new Thickness(16, 0), FontSize = 13,
                BackgroundColor = chon ? Mau("Primary") : Mau("PrimarySoft"),
                TextColor = chon ? Colors.White : Mau("Primary"),
            };
            nut.Clicked += (_, _) => { _loai = ma; VeLoc(); VeDanhSach(); };
            ThanhLoc.Children.Add(nut);
        }
    }

    private void VeDanhSach()
    {
        DanhSach.Children.Clear();
        var ds = TinTucData.DanhSach.Where(t => _loai == "" || t.Loai == _loai).OrderByDescending(t => t.Ngay).ToList();
        if (ds.Count == 0)
            DanhSach.Children.Add(new Label { Text = "Chưa có tin nào", Style = (Style)Application.Current!.Resources["SubtitleLabel"], HorizontalOptions = LayoutOptions.Center, Margin = new Thickness(0, 40) });

        foreach (var t in ds)
        {
            var mo = _mo.Contains(t.Id);
            var noiDung = new VerticalStackLayout { Spacing = 3 };
            noiDung.Children.Add(new Label { Text = $"{t.NhanLoai} · {t.NgayHienThi}", FontSize = 11, TextColor = Mau("Primary"), FontAttributes = FontAttributes.Bold });
            noiDung.Children.Add(new Label { Text = t.TieuDe, FontAttributes = FontAttributes.Bold, FontSize = 14 });
            noiDung.Children.Add(new Label { Text = mo ? t.NoiDung : t.TomTat, FontSize = 12.5, TextColor = Mau("TextGrey") });

            var g = new Grid { ColumnDefinitions = { new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Star) }, ColumnSpacing = 12 };
            g.Add(new Frame
            {
                WidthRequest = 46, HeightRequest = 46, CornerRadius = 23, Padding = 0, HasShadow = false,
                BackgroundColor = Mau("PrimarySoft"), BorderColor = Colors.Transparent, VerticalOptions = LayoutOptions.Start,
                Content = new Label { Text = t.Icon, FontSize = 22, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center },
            }, 0);
            g.Add(noiDung, 1);

            var the = new Frame { Style = (Style)Application.Current!.Resources["CardFrame"], Content = g };
            var id = t.Id;
            the.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(() => { if (!_mo.Add(id)) _mo.Remove(id); VeDanhSach(); }) });
            DanhSach.Children.Add(the);
        }
    }
}
