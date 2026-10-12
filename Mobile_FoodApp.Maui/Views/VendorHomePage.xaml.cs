using FoodApp.Maui.Models;
using FoodApp.Maui.Services;

namespace FoodApp.Maui.Views;

public partial class VendorHomePage : ContentPage
{
    public VendorHomePage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        NapHoSo();
        NapThongKeNhanh();
        NapDonMoi();
    }

    private void NapHoSo()
    {
        var gh = MockData.GianHangHienTai;
        LblAnhGianHang.Text = gh.HinhAnh;
        LblTenGianHang.Text = gh.TenGianHang;
        LblDiaChiGianHang.Text = gh.DiaChi;

        SwitchMoCua.Toggled -= OnSwitchMoCuaToggled;
        SwitchMoCua.IsToggled = gh.DangMoCua;
        SwitchMoCua.Toggled += OnSwitchMoCuaToggled;
        LblTrangThaiMoCua.Text = gh.DangMoCua ? "Đang mở bán" : "Đang tạm đóng";

        SwitchNhanDon.Toggled -= OnSwitchNhanDonToggled;
        SwitchNhanDon.IsToggled = gh.DangNhanDon;
        SwitchNhanDon.Toggled += OnSwitchNhanDonToggled;
        CapNhatTrangThaiNhanDon();
    }

    private void CapNhatTrangThaiNhanDon()
    {
        var gh = MockData.GianHangHienTai;
        LblTrangThaiNhanDon.Text = gh.CoTheNhanDon
            ? $"Đang nhận đơn · giờ hoạt động: {gh.GioHoatDongHienThi}"
            : $"Khách chưa đặt được: {gh.LyDoKhongNhanDon.ToLowerInvariant()}";
    }

    private void NapThongKeNhanh()
    {
        if (DoiTacService.HuyDonQuaHan(MockData.GianHangHienTai.Id) > 0)
            _ = DisplayAlert("Đơn tự hủy", "Có đơn quá 5 phút chưa phản hồi đã bị hệ thống tự hủy và hoàn tiền cho khách.", "OK");

        var donCuaToi = MockData.DonHangCuaGianHang.Where(d => d.GianHangId == MockData.GianHangHienTai.Id);

        var hoanThanhHomNay = donCuaToi.Where(d => d.TrangThai == TrangThaiDonHang.HoanThanh && d.NgayDat.Date == DateTime.Now.Date).ToList();
        LblDoanhThuHomNay.Text = $"{hoanThanhHomNay.Sum(d => d.TongTien):N0}đ";
        LblSoDonHomNay.Text = hoanThanhHomNay.Count.ToString();

        var soChoXacNhan = donCuaToi.Count(d => d.TrangThai == TrangThaiDonHang.ChoXacNhan);
        LblSoDonChoXacNhan.Text = soChoXacNhan > 0 ? $"{soChoXacNhan} đơn chờ xác nhận" : "Không có đơn chờ";

        LblSoMonAn.Text = $"{MockData.MonAnCuaGianHang(MockData.GianHangHienTai.Id).Count(m => m.ConHang && !m.TamNgungBan)} món đang bán";

        var danhGia = DoiTacService.DanhGiaCua(MockData.GianHangHienTai.Id);
        var chuaPhanHoi = danhGia.Count(d => !d.CoPhanHoi);
        LblTomTatDanhGia.Text = danhGia.Count == 0 ? "Chưa có đánh giá"
            : $"{danhGia.Average(d => d.SoSaoMonAn):0.0}★ · {danhGia.Count} lượt" + (chuaPhanHoi > 0 ? $" · {chuaPhanHoi} chưa phản hồi" : "");

        var chuaDoc = DoiTacService.SoThongBaoChuaDoc(MockData.GianHangHienTai.Id);
        LblSoThongBao.Text = chuaDoc > 0 ? $"{chuaDoc} mới" : "";
    }

    private void NapDonMoi()
    {
        var donMoi = MockData.DonHangCuaGianHang
            .Where(d => d.GianHangId == MockData.GianHangHienTai.Id && d.TrangThai == TrangThaiDonHang.ChoXacNhan)
            .OrderBy(d => d.NgayDat)
            .ToList();

        DsDonMoi.ItemsSource = donMoi;
        LblKhongCoDonMoi.IsVisible = donMoi.Count == 0;
    }

    private async void OnSwitchMoCuaToggled(object? sender, ToggledEventArgs e)
    {
        MockData.GianHangHienTai.DangMoCua = e.Value;
        VendorLocalStore.Luu();
        LblTrangThaiMoCua.Text = e.Value ? "Đang mở bán" : "Đang tạm đóng";
        await Services.DuLieuService.GhiAsync("PUT", $"api/mobile/gian-hang/{MockData.GianHangHienTai.Id}/trang-thai-kinh-doanh", new { dangMoCua = e.Value });
    }

    private async void OnSwitchNhanDonToggled(object? sender, ToggledEventArgs e)
    {
        var gh = MockData.GianHangHienTai;
        gh.DangNhanDon = e.Value;
        VendorLocalStore.Luu();
        CapNhatTrangThaiNhanDon();
        await Services.DuLieuService.GhiAsync("PUT", $"api/mobile/gian-hang/{gh.Id}/gio-hoat-dong",
            new { gioMoCua = gh.GioMoCua, gioDongCua = gh.GioDongCua, dangNhanDon = gh.DangNhanDon, dangMoCua = gh.DangMoCua });
    }

    private async void OnXacNhanNhanhClicked(object sender, EventArgs e)
    {
        if (sender is not Button { CommandParameter: DonHang don }) return;

        await DoiTacService.XacNhanDonAsync(don);

        NapThongKeNhanh();
        NapDonMoi();
        await DisplayAlert("Đã xác nhận", $"Đơn #{don.Id} đã được xác nhận và chuyển sang chuẩn bị món.", "OK");
    }

    private async void OnTuChoiNhanhClicked(object sender, EventArgs e)
    {
        if (sender is not Button { CommandParameter: DonHang don }) return;

        if (!await DoiTacService.TuChoiDonAsync(this, don)) return;   // bắt buộc có lý do

        NapThongKeNhanh();
        NapDonMoi();
    }

    private async void OnXemDonHangTapped(object sender, EventArgs e) => await Shell.Current.GoToAsync(nameof(VendorOrdersPage));
    private async void OnXemThucDonTapped(object sender, EventArgs e) => await Shell.Current.GoToAsync(nameof(VendorMenuPage));
    private async void OnXemDoanhThuTapped(object sender, EventArgs e) => await Shell.Current.GoToAsync(nameof(VendorRevenuePage));
    private async void OnXemHoSoTapped(object sender, EventArgs e) => await Shell.Current.GoToAsync(nameof(VendorProfilePage));
    private async void OnXemKhuyenMaiTapped(object sender, EventArgs e) => await Shell.Current.GoToAsync(nameof(VendorPromotionsPage));
    private async void OnXemDanhGiaTapped(object sender, EventArgs e) => await Shell.Current.GoToAsync(nameof(VendorReviewsPage));
    private async void OnXemTaiChinhTapped(object sender, EventArgs e) => await Shell.Current.GoToAsync(nameof(VendorFinancePage));
    private async void OnXemThongBaoTapped(object sender, EventArgs e) => await Shell.Current.GoToAsync(nameof(VendorNotificationsPage));
    private async void OnXemChienDichTapped(object sender, EventArgs e) => await Shell.Current.GoToAsync(nameof(VendorCampaignsPage));

    private void OnRefreshing(object? sender, EventArgs e)
    {
        NapHoSo();
        NapThongKeNhanh();
        NapDonMoi();
        RefreshTrangChu.IsRefreshing = false;
    }

    private async void OnDangXuatClicked(object sender, EventArgs e)
    {
        bool xacNhan = await DisplayAlert("Đăng xuất", "Bạn có chắc muốn đăng xuất khỏi gian hàng?", "Đăng xuất", "Hủy");
        if (!xacNhan) return;

        // Bug cũ: First(g => g.Id == "gh1") ném lỗi khi dữ liệu từ SQL không có gian hàng "gh1".
        MockData.GianHangHienTai = new GianHang();
        Services.NhatKyService.Ghi("GianHang", MockData.GianHangHienTai.Email, "Đăng xuất", MockData.GianHangHienTai.TenGianHang, Services.NhatKyService.ThietBi());
        Services.Phien.DangXuat();
        await Shell.Current.GoToAsync("//dang-nhap");
    }
}
