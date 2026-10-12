using FoodApp.Maui.Models;
using FoodApp.Maui.Services;

namespace FoodApp.Maui.Views;

/// <summary>Theo dõi phí nền tảng và số tiền thực nhận của gian hàng theo ngày / tháng / năm.</summary>
public partial class VendorFinancePage : ContentPage
{
    private KyBaoCao _ky = KyBaoCao.Thang;
    private DateTime _moc = DateTime.Today;

    public VendorFinancePage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        Nap();
        await DuLieuService.TaiDonHangGianHangAsync(MockData.GianHangHienTai.Id);
        Nap();
    }

    private static Color Mau(string key) => (Color)Application.Current!.Resources[key];

    private void OnChonKyNgay(object? s, EventArgs e) => DoiKy(KyBaoCao.Ngay);
    private void OnChonKyThang(object? s, EventArgs e) => DoiKy(KyBaoCao.Thang);
    private void OnChonKyNam(object? s, EventArgs e) => DoiKy(KyBaoCao.Nam);

    private void DoiKy(KyBaoCao ky)
    {
        _ky = ky;
        _moc = DateTime.Today;
        Nap();
    }

    private void OnKyTruoc(object? s, EventArgs e)
    {
        _moc = DoiTacService.LuiKy(_ky, _moc, -1);
        Nap();
    }

    private void OnKySau(object? s, EventArgs e)
    {
        var moi = DoiTacService.LuiKy(_ky, _moc, +1);
        if (DoiTacService.Khoang(_ky, moi).Tu > DateTime.Today) return;
        _moc = moi;
        Nap();
    }

    private static void ToNutKy(Button nut, bool dangChon)
    {
        nut.BackgroundColor = dangChon ? Mau("Primary") : Colors.White;
        nut.TextColor = dangChon ? Colors.White : Mau("TextGrey");
        nut.BorderColor = Mau("BorderColor");
        nut.BorderWidth = 1;
    }

    private void Nap()
    {
        // TODO: thay bằng GET /api/gian-hang/tai-chinh?ky=...&moc=... khi có API thật (server nên là nơi tính phí chính thức).
        var gh = MockData.GianHangHienTai;
        var donKy = DoiTacService.TrongKy(DoiTacService.DonCuaGianHang(gh.Id), _ky, _moc);
        var tc = DoiTacService.TinhTaiChinh(gh, donKy.Where(d => d.TrangThai == TrangThaiDonHang.HoanThanh));
        var tyLe = DoiTacService.TyLeChietKhau(gh);

        ToNutKy(BtnKyNgay, _ky == KyBaoCao.Ngay);
        ToNutKy(BtnKyThang, _ky == KyBaoCao.Thang);
        ToNutKy(BtnKyNam, _ky == KyBaoCao.Nam);
        LblNhanKy.Text = DoiTacService.NhanKy(_ky, _moc);
        BtnKySau.IsEnabled = DoiTacService.Khoang(_ky, DoiTacService.LuiKy(_ky, _moc, +1)).Tu <= DateTime.Today;

        LblThucNhan.Text = $"{tc.ThucNhan:N0}đ";
        LblSoDon.Text = $"{tc.SoDon} đơn hoàn thành";
        LblDoanhThuMon.Text = $"{tc.DoanhThuMon:N0}đ";
        LblGiamGia.Text = tc.GiamGiaQuanChiu > 0 ? $"−{tc.GiamGiaQuanChiu:N0}đ" : "0đ";
        LblNhanPhi.Text = $"Phí nền tảng ({tyLe:P0})";
        LblPhiNenTang.Text = tc.PhiNenTang > 0 ? $"−{tc.PhiNenTang:N0}đ" : "0đ";
        LblThucNhanChiTiet.Text = $"{tc.ThucNhan:N0}đ";
        LblGiaVon.Text = $"−{tc.TongGiaVon:N0}đ";
        LblLoiNhuan.Text = $"{tc.LoiNhuan:N0}đ";

        var ghiChu = $"Phí nền tảng = {tyLe:P0} × (tiền món − giảm giá do quán tài trợ), chỉ tính trên đơn hoàn thành. " +
                     "Phí giao hàng thuộc về tài xế nên không nằm trong doanh thu của quán. Mã khuyến mãi toàn hệ thống do nền tảng chịu, không trừ vào quán.";
        if (tc.SoDon > 0 && tc.SoDonThieuGiaVon > 0)
            ghiChu += $"\n* {tc.SoDonThieuGiaVon}/{tc.SoDon} đơn có món chưa nhập giá vốn nên lợi nhuận thực tế có thể thấp hơn.";
        LblGhiChu.Text = ghiChu;

        DsDon.ItemsSource = tc.ChiTiet;
        LblRong.IsVisible = tc.ChiTiet.Count == 0;
    }
}
