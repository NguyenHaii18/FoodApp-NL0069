using FoodApp.Maui.Models;
using FoodApp.Maui.Services;

namespace FoodApp.Maui.Views;

/// <summary>Xem đánh giá của khách và phản hồi công khai (NV theo mục đánh giá: gian hàng có quyền phản hồi).</summary>
public partial class VendorReviewsPage : ContentPage
{
    // Loc: 0 = tất cả, 1..5 = đúng số sao, -1 = chưa phản hồi
    private readonly (string Nhan, int Loc)[] _boLoc =
    {
        ("Tất cả", 0), ("Chưa phản hồi", -1), ("5★", 5), ("4★", 4), ("3★", 3), ("2★", 2), ("1★", 1),
    };
    private int _dangLoc;
    private readonly List<Button> _nut = new();

    public VendorReviewsPage()
    {
        InitializeComponent();
        foreach (var (nhan, loc) in _boLoc)
        {
            var nut = new Button { Text = nhan, CornerRadius = 16, HeightRequest = 34, Padding = new Thickness(14, 0), FontSize = 12, BorderWidth = 1 };
            nut.Clicked += (_, _) => { _dangLoc = loc; Nap(); };
            _nut.Add(nut);
            KhungBoLoc.Children.Add(nut);
        }
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        Nap();
    }

    private static Color Mau(string key) => (Color)Application.Current!.Resources[key];

    private void Nap()
    {
        // TODO: thay bằng GET /api/mobile/gian-hang/{id}/danh-gia khi có API thật.
        var tatCa = DoiTacService.DanhGiaCua(MockData.GianHangHienTai.Id);

        if (tatCa.Count == 0)
        {
            LblDiemTrungBinh.Text = "—";
            LblSaoTrungBinh.Text = "☆☆☆☆☆";
            LblTomTat.Text = "Chưa có đánh giá";
        }
        else
        {
            var tb = tatCa.Average(d => d.SoSaoMonAn);
            var lam = (int)Math.Round(tb);
            LblDiemTrungBinh.Text = tb.ToString("0.0");
            LblSaoTrungBinh.Text = new string('★', lam) + new string('☆', 5 - lam);
            var giao = tatCa.Where(d => d.SoSaoGiaoHang > 0).ToList();
            LblTomTat.Text = $"{tatCa.Count} đánh giá · {tatCa.Count(d => !d.CoPhanHoi)} chưa phản hồi" +
                             (giao.Count > 0 ? $" · giao hàng {giao.Average(d => d.SoSaoGiaoHang):0.0}★" : "");
        }

        for (var i = 0; i < _boLoc.Length; i++)
        {
            var chon = _boLoc[i].Loc == _dangLoc;
            _nut[i].BackgroundColor = chon ? Mau("Primary") : Colors.White;
            _nut[i].TextColor = chon ? Colors.White : Mau("TextGrey");
            _nut[i].BorderColor = Mau("BorderColor");
        }

        var ds = tatCa.Where(d => _dangLoc switch
        {
            0 => true,
            -1 => !d.CoPhanHoi,
            _ => d.SoSaoMonAn == _dangLoc
        }).ToList();

        DsDanhGia.ItemsSource = ds;
        LblRong.IsVisible = ds.Count == 0;
        LblRong.Text = tatCa.Count == 0 ? "Chưa có đánh giá nào." : "Không có đánh giá phù hợp bộ lọc.";
    }

    private async void OnPhanHoiClicked(object sender, EventArgs e)
    {
        if (sender is not Button { CommandParameter: DanhGiaGianHang dg }) return;

        var noiDung = await DisplayPromptAsync("Phản hồi đánh giá",
            $"Phản hồi sẽ hiển thị công khai cho {dg.KhachTen}.", "Gửi", "Hủy",
            placeholder: "Cảm ơn bạn đã ủng hộ quán!", maxLength: 300, initialValue: dg.PhanHoi);
        if (noiDung is null) return;   // bấm Hủy

        dg.PhanHoi = noiDung.Trim();   // để trống = xóa phản hồi
        dg.PhanHoiLuc = dg.CoPhanHoi ? DateTime.Now : null;
        VendorLocalStore.Luu();
        Nap();
    }

    private void OnRefreshing(object? sender, EventArgs e)
    {
        Nap();
        RefreshDs.IsRefreshing = false;
    }
}
