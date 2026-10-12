using FoodApp.Maui.Models;
using FoodApp.Maui.Services;

namespace FoodApp.Maui.Views;

[QueryProperty(nameof(DonHang), "DonHang")]
public partial class ReviewPage : ContentPage
{
    public DonHang DonHang { get; set; } = null!;

    private int _soSaoMonAn = 0;
    private int _soSaoGiaoHang = 0;
    private readonly HashSet<string> _theDaChon = new();
    private readonly string[] _dsThe = { "Món ngon", "Đóng gói đẹp", "Giao đúng giờ", "Đúng số lượng", "Nhân viên thân thiện" };

    public ReviewPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (DonHang is not null)
        {
            LblTenGianHang.Text = DonHang.TenGianHang;
            LblMaDon.Text = $"Đơn #{DonHang.Id}";
        }
        VeSao(SaoMonAn, _soSaoMonAn, v => { _soSaoMonAn = v; VeSao(SaoMonAn, _soSaoMonAn, null!); });
        VeSao(SaoGiaoHang, _soSaoGiaoHang, v => { _soSaoGiaoHang = v; VeSao(SaoGiaoHang, _soSaoGiaoHang, null!); });
        VeDsThe();
    }

    private void VeSao(HorizontalStackLayout khung, int soSao, Action<int>? onChonLai)
    {
        khung.Children.Clear();
        for (int i = 1; i <= 5; i++)
        {
            var idx = i;
            var lbl = new Label
            {
                Text = i <= soSao ? "★" : "☆",
                FontSize = 30,
                TextColor = i <= soSao ? (Color)Application.Current!.Resources["Accent"] : (Color)Application.Current!.Resources["BorderColor"],
            };
            var tap = new TapGestureRecognizer();
            tap.Tapped += (s, e) => onChonLai?.Invoke(idx);
            lbl.GestureRecognizers.Add(tap);
            khung.Children.Add(lbl);
        }
    }

    private void VeDsThe()
    {
        DsThe.Children.Clear();
        foreach (var the in _dsThe)
        {
            var chon = _theDaChon.Contains(the);
            var frame = new Frame
            {
                Padding = new Thickness(12, 6),
                Margin = new Thickness(4),
                CornerRadius = 16,
                HasShadow = false,
                BackgroundColor = chon ? (Color)Application.Current!.Resources["Primary"] : Colors.White,
                BorderColor = chon ? (Color)Application.Current!.Resources["Primary"] : (Color)Application.Current!.Resources["BorderColor"],
                Content = new Label
                {
                    Text = the,
                    FontSize = 12.5,
                    TextColor = chon ? Colors.White : (Color)Application.Current!.Resources["TextDark"],
                }
            };
            var tap = new TapGestureRecognizer();
            tap.Tapped += (s, e) =>
            {
                if (!_theDaChon.Add(the)) _theDaChon.Remove(the);
                VeDsThe();
            };
            frame.GestureRecognizers.Add(tap);
            DsThe.Children.Add(frame);
        }
    }

    private async void OnGuiDanhGiaClicked(object sender, EventArgs e)
    {
        if (_soSaoMonAn == 0)
        {
            await DisplayAlert("Thiếu thông tin", "Vui lòng chọn số sao cho món ăn", "Đóng");
            return;
        }

        // Lưu đánh giá vào đơn (giữ qua các lần mở app) rồi gửi lên API nếu đang online.
        if (DonHang is not null)
        {
            DonHang.DiemMonAn = _soSaoMonAn;
            DonHang.TheDanhGia = string.Join("|", _theDaChon);
            DonHang.NoiDungDanhGia = EditorNoiDung.Text?.Trim() ?? "";
            Services.DonHangService.DongBo(DonHang);
            await Services.DuLieuService.GhiAsync("POST", "api/mobile/danh-gia", new
            {
                donHangId = DonHang.Id, gianHangId = DonHang.GianHangId,
                soSaoMonAn = _soSaoMonAn, soSaoGiaoHang = _soSaoGiaoHang,
                theNhanXet = DonHang.TheDanhGia, noiDung = DonHang.NoiDungDanhGia,
            });
        }

        // Điểm "Tài xế & dịch vụ giao hàng" chuyển cho tài xế của đơn này
        if (DonHang is not null && _soSaoGiaoHang > 0)
        {
            DonHang.DiemTaiXe = _soSaoGiaoHang;
            DonHang.NhanXetTaiXe = EditorNoiDung.Text?.Trim() ?? "";
            Services.DonHangService.DongBo(DonHang);
            Services.ThuNhapService.CapNhatChiSo();
        }

        // Lưu để gian hàng xem được ở mục "Đánh giá", cập nhật điểm trung bình của quán và gửi thông báo cho quán.
        DoiTacService.GhiDanhGia(DonHang, _soSaoMonAn, _soSaoGiaoHang, _theDaChon, EditorNoiDung.Text ?? "");

        KhungForm.IsVisible = false;
        KhungCamOn.IsVisible = true;
    }

    private async void OnQuayLaiClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}
