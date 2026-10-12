using FoodApp.Maui.Models;
using FoodApp.Maui.Services;

namespace FoodApp.Maui.Views;

public partial class DriverNotificationsPage : ContentPage
{
    public DriverNotificationsPage() => InitializeComponent();

    /// <summary>Tạo thông báo từ trạng thái hiện tại: đơn mới, thay đổi đơn, thưởng, hệ thống.</summary>
    public static List<(string Icon, string TieuDe, string NoiDung, DateTime Luc)> TaoDanhSach()
    {
        var ds = new List<(string Icon, string TieuDe, string NoiDung, DateTime Luc)>();
        var t = MockData.TaiXeHienTai;

        if (t.NhanDuocDon && MockData.DonHangDangGiaoCuaTaiXe is null)
            foreach (var d in MockData.DonHangChoTaiXe.Where(d => d.TaiXeId is null && d.SanSangGiao && d.TrangThai == TrangThaiDonHang.DangLam
                         && !MockData.LichSuDonHang.Any(x => x.Id == d.Id && x.TrangThai == TrangThaiDonHang.DaHuy)))
                ds.Add(("📦", "Đơn mới sẵn sàng", $"{d.TenGianHang} · #{d.Id} · +{d.PhiGiaoHang:N0}đ", d.NgayDat));

        var dang = MockData.DonHangDangGiaoCuaTaiXe;
        if (dang is not null)
        {
            if (MockData.LichSuDonHang.Any(x => x.Id == dang.Id && x.TrangThai == TrangThaiDonHang.DaHuy))
                ds.Add(("⚠️", "Khách đã hủy đơn đang giao", $"Đơn #{dang.Id} – {dang.TenGianHang}. Hãy liên hệ hỗ trợ nếu đã lấy hàng.", DateTime.Now));
            else
                ds.Add(("🛵", "Bạn đang có đơn", $"#{dang.Id} · {dang.TenGianHang} → {dang.DiaChiGiao}", dang.NgayDat));
        }

        foreach (var d in MockData.DonHangDaGiaoCuaTaiXe.Where(d => d.GiaoThatBai && d.GiaoLuc is not null).OrderByDescending(d => d.GiaoLuc).Take(5))
            ds.Add(("❌", "Đơn không giao được", $"#{d.Id}: {d.LyDoThatBai}", d.GiaoLuc!.Value));

        foreach (var d in MockData.DonHangDaGiaoCuaTaiXe.Where(d => d.DiemTaiXe > 0).OrderByDescending(d => d.GiaoLuc).Take(5))
            ds.Add(("⭐", $"Khách chấm {d.DiemTaiXe} sao", string.IsNullOrWhiteSpace(d.NhanXetTaiXe) ? $"Đơn #{d.Id}" : $"Đơn #{d.Id}: {d.NhanXetTaiXe}", d.GiaoLuc ?? DateTime.Now));

        var thuong = ThuNhapService.ThuongNgay(DateTime.Today);
        if (thuong > 0) ds.Add(("🎁", "Bạn đạt thưởng hôm nay", $"+{thuong:N0}đ (thưởng ngày)", DateTime.Today));
        else ds.Add(("🎯", "Mục tiêu thưởng", ThuNhapService.TienDoThuongNgay(), DateTime.Today));

        ds.Add(("📢", "Thông báo hệ thống", "Luôn đội mũ bảo hiểm và tuân thủ luật giao thông khi giao hàng.", DateTime.Today));
        return ds.OrderByDescending(x => x.Luc).ToList();
    }

    public static int Dem() => TaoDanhSach().Count(x => x.Icon is "📦" or "⚠️" or "❌" or "⭐" or "🎁");

    protected override void OnAppearing()
    {
        base.OnAppearing();
        DsThongBao.Children.Clear();
        foreach (var (icon, tieuDe, nd, luc) in TaoDanhSach())
        {
            var cot = new VerticalStackLayout { Spacing = 2 };
            cot.Children.Add(new Label { Text = tieuDe, FontAttributes = FontAttributes.Bold, FontSize = 13.5 });
            cot.Children.Add(new Label { Text = nd, FontSize = 12.5 });
            cot.Children.Add(new Label { Text = luc.ToString("dd/MM HH:mm"), FontSize = 11, TextColor = (Color)Application.Current!.Resources["TextGrey"] });
            var luoi = new Grid { ColumnSpacing = 10, ColumnDefinitions = { new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Star) } };
            luoi.Add(new Label { Text = icon, FontSize = 24, VerticalOptions = LayoutOptions.Start }, 0);
            luoi.Add(cot, 1);
            DsThongBao.Children.Add(new Frame { Content = luoi, Padding = new Thickness(14, 10), CornerRadius = 12, HasShadow = false, BorderColor = (Color)Application.Current!.Resources["PrimarySoft"] });
        }
    }
}
