using FoodApp.Maui.Models;
using FoodApp.Maui.Services;

namespace FoodApp.Maui.Views;

public partial class MessagesPage : ContentPage
{
    public MessagesPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        VeDanhSach();
    }

    private static Color Mau(string k) => (Color)Application.Current!.Resources[k];

    /// <summary>Mỗi đơn đã có tài xế (hoặc đã có tin nhắn) là một cuộc trò chuyện.</summary>
    private void VeDanhSach()
    {
        var email = Phien.Khach?.Email;
        var don = MockData.LichSuDonHang
            .Where(d => string.Equals(d.KhachEmail, email, StringComparison.OrdinalIgnoreCase)
                        && (!string.IsNullOrEmpty(d.TaiXeId) || ChatService.CuoiCung(d.Id) is not null))
            .OrderByDescending(d => ChatService.CuoiCung(d.Id)?.ThoiGian ?? d.NgayDat)
            .ToList();

        DanhSach.Children.Clear();
        KhungTrong.IsVisible = don.Count == 0;

        foreach (var d in don)
        {
            var cuoi = ChatService.CuoiCung(d.Id);
            var ten = string.IsNullOrWhiteSpace(d.TaiXeTen) ? "Tài xế" : d.TaiXeTen;

            var avatar = new Frame
            {
                WidthRequest = 46, HeightRequest = 46, CornerRadius = 23, Padding = 0, HasShadow = false, BorderColor = Colors.Transparent,
                BackgroundColor = Mau("PrimarySoft"),
                Content = new Label { Text = "🛵", FontSize = 22, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center },
            };
            var chu = new VerticalStackLayout { Spacing = 1, VerticalOptions = LayoutOptions.Center };
            chu.Children.Add(new Label { Text = $"{ten} · {d.TenGianHang}", FontAttributes = FontAttributes.Bold, FontSize = 14, LineBreakMode = LineBreakMode.TailTruncation });
            chu.Children.Add(new Label
            {
                Text = cuoi is null ? "Bắt đầu trò chuyện" : $"{(cuoi.NguoiGui == "KhachHang" ? "Bạn: " : "")}{cuoi.NoiDung}",
                FontSize = 12.5, TextColor = Mau("TextGrey"), LineBreakMode = LineBreakMode.TailTruncation,
            });
            var gio = new Label
            {
                Text = cuoi is null ? "" : (cuoi.ThoiGian.Date == DateTime.Today ? cuoi.ThoiGian.ToString("HH:mm") : cuoi.ThoiGian.ToString("dd/MM")),
                FontSize = 11, TextColor = Mau("TextGrey"), VerticalOptions = LayoutOptions.Start,
            };

            var hang = new Grid { ColumnSpacing = 12, ColumnDefinitions = { new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) } };
            hang.Add(avatar, 0); hang.Add(chu, 1); hang.Add(gio, 2);

            var the = new Frame { Style = (Style)Application.Current!.Resources["CardFrame"], Padding = new Thickness(14, 12), Content = hang };
            var donId = d.Id; var sdt = d.TaiXeSdt;
            the.GestureRecognizers.Add(new TapGestureRecognizer
            {
                Command = new Command(async () => await ChatPage.MoAsync(donId, "KhachHang", ten, sdt)),
            });
            DanhSach.Children.Add(the);
        }
    }
}
