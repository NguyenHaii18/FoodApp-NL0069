using FoodApp.Maui.Models;

namespace FoodApp.Maui.Views;

/// <summary>Dòng thời gian: cập nhật trạng thái đơn của khách và mã giảm giá đang có (lấy từ dữ liệu thật của app).</summary>
public partial class ActivityPage : ContentPage
{
    public ActivityPage() { InitializeComponent(); }

    protected override void OnAppearing() { base.OnAppearing(); Ve(); }

    private static Color Mau(string k) => (Color)Application.Current!.Resources[k];

    private void Ve()
    {
        var muc = new List<(DateTime Luc, string Icon, string TieuDe, string MoTa, bool Noi)>();

        foreach (var d in MockData.LichSuDonHang.Where(OrderTrackingPage.DonCuaToi))
        {
            var (icon, td, mt) = d.TrangThai switch
            {
                TrangThaiDonHang.ChoXacNhan => ("⏳", $"Đơn #{d.Id} đang chờ xác nhận", $"Chờ {d.TenGianHang} xác nhận đơn"),
                TrangThaiDonHang.DangLam => ("👨‍🍳", $"Đơn #{d.Id} đang được chuẩn bị", $"{d.TenGianHang} đang làm món cho bạn"),
                TrangThaiDonHang.DangGiao => ("🛵", $"Đơn #{d.Id} đang được giao", "Shipper đang trên đường đến bạn"),
                TrangThaiDonHang.HoanThanh => ("✅", $"Đơn #{d.Id} đã hoàn thành", $"Đánh giá {d.TenGianHang} ở tab Đơn hàng"),
                _ => ("❌", $"Đơn #{d.Id} đã hủy", d.DaHoanTien ? "Đã hoàn tiền" : d.TenGianHang),
            };
            muc.Add((d.NgayDat, icon, td, mt, d.TrangThai is TrangThaiDonHang.ChoXacNhan or TrangThaiDonHang.DangLam or TrangThaiDonHang.DangGiao));
        }
        foreach (var k in MockData.DanhSachKhuyenMai.Where(k => k.DangHoatDong && k.ConHan && !k.ChuaBatDau && k.ConLuot))
            muc.Add((k.NgayBatDau, "🏷️", $"Mã giảm giá: {k.MaCode}", k.GiaTriGiamHienThi + " · " + k.PhamViHienThi, false));

        DanhSach.Children.Clear();
        LblTrong.IsVisible = muc.Count == 0;
        foreach (var m in muc.OrderByDescending(x => x.Luc))
        {
            var cot = new VerticalStackLayout { Spacing = 1 };
            cot.Children.Add(new Label { Text = m.TieuDe, FontAttributes = FontAttributes.Bold, FontSize = 13 });
            cot.Children.Add(new Label { Text = m.MoTa, FontSize = 11.5, TextColor = Mau("TextGrey") });
            cot.Children.Add(new Label { Text = m.Luc.ToString("dd/MM/yyyy HH:mm"), FontSize = 10.5, TextColor = Mau("TextGrey") });
            var h = new HorizontalStackLayout { Spacing = 10 };
            h.Children.Add(new Label { Text = m.Icon, FontSize = 20, VerticalOptions = LayoutOptions.Start });
            h.Children.Add(cot);
            var the = new Frame { Style = (Style)Application.Current!.Resources["CardFrame"], Content = h };
            if (m.Noi) { the.BackgroundColor = Mau("PrimarySoft"); the.BorderColor = Mau("BorderColor"); }
            DanhSach.Children.Add(the);
        }
    }
}
