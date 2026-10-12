using System.Collections.ObjectModel;
using FoodApp.Maui.Models;
using FoodApp.Maui.Services;

namespace FoodApp.Maui.Views;

/// <summary>Một dòng tin nhắn đã tính sẵn màu và căn lề (bên phải = của tôi).</summary>
public class TinNhanHienThi
{
    public string NoiDung { get; set; } = "";
    public string Gio { get; set; } = "";
    public bool LaCuaToi { get; set; }
    public LayoutOptions CanLe => LaCuaToi ? LayoutOptions.End : LayoutOptions.Start;
    public Color Nen { get; set; } = Colors.White;
    public Color Chu { get; set; } = Colors.Black;
    public Color ChuPhu { get; set; } = Colors.Gray;
}

[QueryProperty(nameof(DonHangId), "DonHangId")]
[QueryProperty(nameof(VaiTro), "VaiTro")]
[QueryProperty(nameof(TenDoiPhuong), "TenDoiPhuong")]
[QueryProperty(nameof(Sdt), "Sdt")]
public partial class ChatPage : ContentPage
{
    public string DonHangId { get; set; } = "";
    public string VaiTro { get; set; } = "KhachHang";        // vai trò của người đang dùng máy: KhachHang | TaiXe
    public string TenDoiPhuong { get; set; } = "";
    public string Sdt { get; set; } = "";

    private readonly ObservableCollection<TinNhanHienThi> _ds = new();
    private IDispatcherTimer? _timer;
    private int _soTinDaVe = -1;

    public ChatPage()
    {
        InitializeComponent();
        DsTinNhan.ItemsSource = _ds;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        bool laTaiXe = VaiTro == "TaiXe";
        LblIcon.Text = laTaiXe ? "👤" : "🛵";
        LblTen.Text = string.IsNullOrWhiteSpace(TenDoiPhuong) ? (laTaiXe ? "Khách hàng" : "Tài xế") : TenDoiPhuong;
        LblPhu.Text = $"Đơn #{DonHangId}";

        // Câu trả lời nhanh cho tài xế
        HangNhanh.Children.Clear();
        KhungNhanh.IsVisible = laTaiXe;
        if (laTaiXe)
            foreach (var cau in new[] { "Mình đang đến lấy hàng", "Mình sắp tới nơi", "Bạn có ở nhà không?", "Mình đã đến, bạn ra nhận giúp nhé" })
            {
                var nut = new Button { Text = cau, FontSize = 12, CornerRadius = 16, HeightRequest = 34, Padding = new Thickness(12, 0),
                                       BackgroundColor = (Color)Application.Current!.Resources["PrimarySoft"], TextColor = (Color)Application.Current.Resources["Primary"] };
                nut.Clicked += async (_, _) => await GuiAsync(cau);
                HangNhanh.Children.Add(nut);
            }

        VeLai();
        _timer = Dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromSeconds(2.5);
        _timer.Tick += async (_, _) =>
        {
            await ChatService.DongBoAsync(DonHangId);   // lấy tin mới từ server (nếu đang online)
            VeLai();
        };
        _timer.Start();
        await ChatService.DongBoAsync(DonHangId);
        VeLai();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _timer?.Stop();
        _timer = null;
    }

    private static Color Mau(string k) => (Color)Application.Current!.Resources[k];
    private static bool Toi => Application.Current?.RequestedTheme == AppTheme.Dark;

    private void VeLai()
    {
        var tin = ChatService.Lay(DonHangId);
        if (tin.Count == _soTinDaVe) return;     // không có gì mới thì không vẽ lại
        _soTinDaVe = tin.Count;

        _ds.Clear();
        foreach (var t in tin)
        {
            bool cuaToi = t.NguoiGui == VaiTro;
            _ds.Add(new TinNhanHienThi
            {
                NoiDung = t.NoiDung,
                Gio = t.ThoiGian.Date == DateTime.Today ? t.ThoiGian.ToString("HH:mm") : t.ThoiGian.ToString("dd/MM HH:mm"),
                LaCuaToi = cuaToi,
                Nen = cuaToi ? Mau("Primary") : (Toi ? Mau("CardColorDark") : Mau("PrimarySoft")),
                Chu = cuaToi ? Colors.White : (Toi ? Mau("TextOnDark") : Mau("TextDark")),
                ChuPhu = cuaToi ? Color.FromArgb("#CCFFFFFF") : (Toi ? Mau("TextGreyDark") : Mau("TextGrey")),
            });
        }
        if (_ds.Count > 0)
            DsTinNhan.ScrollTo(_ds.Count - 1, position: ScrollToPosition.End, animate: false);
    }

    private async Task GuiAsync(string noiDung)
    {
        if (string.IsNullOrWhiteSpace(noiDung)) return;
        var ten = VaiTro == "TaiXe" ? MockData.TaiXeHienTai.HoTen : (Phien.Khach?.HoTen ?? "Khách hàng");
        await ChatService.GuiAsync(DonHangId, VaiTro, ten, noiDung);
        VeLai();
    }

    private async void OnGuiClicked(object? sender, EventArgs e)
    {
        var nd = EntryNoiDung.Text;
        EntryNoiDung.Text = "";
        await GuiAsync(nd ?? "");
        EntryNoiDung.Focus();
    }

    private async void OnGoiTapped(object? sender, EventArgs e) => await LienLac.GoiAsync(Sdt);

    /// <summary>Mở trang trò chuyện của một đơn hàng.</summary>
    public static Task MoAsync(string donId, string vaiTro, string tenDoiPhuong, string sdt) =>
        Shell.Current.GoToAsync(nameof(ChatPage), new Dictionary<string, object>
        {
            { "DonHangId", donId }, { "VaiTro", vaiTro }, { "TenDoiPhuong", tenDoiPhuong }, { "Sdt", sdt },
        });
}
