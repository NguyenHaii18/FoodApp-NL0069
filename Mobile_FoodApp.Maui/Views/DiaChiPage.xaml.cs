using FoodApp.Maui.Services;

namespace FoodApp.Maui.Views;

/// <summary>Sổ địa chỉ: thêm / sửa / xóa / đặt mặc định. Địa chỉ mặc định hiện ở trang chủ và tự điền khi thanh toán.</summary>
public partial class DiaChiPage : ContentPage
{
    private List<DiaChiLuu> _ds = new();

    public DiaChiPage() { InitializeComponent(); }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _ds = TaiKhoanService.LayDiaChi();
        Ve();
    }

    private static Color Mau(string k) => (Color)Application.Current!.Resources[k];

    private void Ve()
    {
        DanhSach.Children.Clear();
        if (_ds.Count == 0)
            DanhSach.Children.Add(new Label { Text = "Chưa có địa chỉ nào. Bấm “Thêm địa chỉ mới”.", Style = (Style)Application.Current!.Resources["SubtitleLabel"], HorizontalOptions = LayoutOptions.Center, Margin = new Thickness(0, 40) });

        foreach (var d in _ds)
        {
            var thong = new VerticalStackLayout { Spacing = 3 };
            var dong = new HorizontalStackLayout { Spacing = 8 };
            dong.Children.Add(new Label { Text = (d.Nhan == "Công ty" ? "🏢 " : d.Nhan == "Nhà" ? "🏠 " : "📍 ") + d.Nhan, FontAttributes = FontAttributes.Bold, FontSize = 14 });
            if (d.MacDinh)
                dong.Children.Add(new Frame { BackgroundColor = Mau("PrimarySoft"), BorderColor = Colors.Transparent, CornerRadius = 8, Padding = new Thickness(8, 1), HasShadow = false,
                    Content = new Label { Text = "Mặc định", FontSize = 11, FontAttributes = FontAttributes.Bold, TextColor = Mau("Primary") } });
            thong.Children.Add(dong);
            thong.Children.Add(new Label { Text = d.DiaChi, FontSize = 12.5, TextColor = Mau("TextGrey") });

            var the = new Frame { Style = (Style)Application.Current!.Resources["CardFrame"], Content = thong };
            if (d.MacDinh) the.BorderColor = Mau("Primary");
            var item = d;
            the.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(async () => await HanhDongAsync(item)) });
            DanhSach.Children.Add(the);
        }
    }

    private async Task HanhDongAsync(DiaChiLuu d)
    {
        var chon = await DisplayActionSheet(d.Nhan, "Đóng", null, d.MacDinh ? null : "Đặt làm mặc định", "Sửa", "Xóa");
        switch (chon)
        {
            case "Đặt làm mặc định":
                foreach (var x in _ds) x.MacDinh = x == d;
                break;
            case "Sửa":
                var moi = await DisplayPromptAsync("Sửa địa chỉ", d.Nhan, "Lưu", "Hủy", initialValue: d.DiaChi);
                if (string.IsNullOrWhiteSpace(moi)) return;
                d.DiaChi = moi.Trim();
                break;
            case "Xóa":
                if (!await DisplayAlert("Xóa địa chỉ", $"Xóa “{d.Nhan}”?", "Xóa", "Hủy")) return;
                var macDinh = d.MacDinh;
                _ds.Remove(d);
                if (macDinh && _ds.Count > 0) _ds[0].MacDinh = true;
                break;
            default: return;
        }
        TaiKhoanService.LuuDiaChi(_ds);
        Ve();
    }

    private async void OnThemClicked(object? sender, EventArgs e)
    {
        var nhan = await DisplayActionSheet("Loại địa chỉ", "Hủy", null, "Nhà", "Công ty", "Khác");
        if (nhan is null or "Hủy") return;
        var dc = await DisplayPromptAsync("Địa chỉ mới", "Số nhà, đường, phường/quận, thành phố:", "Thêm", "Hủy");
        if (string.IsNullOrWhiteSpace(dc)) return;
        _ds.Add(new DiaChiLuu { Nhan = nhan, DiaChi = dc.Trim(), MacDinh = _ds.Count == 0 });
        TaiKhoanService.LuuDiaChi(_ds);
        Ve();
    }
}
