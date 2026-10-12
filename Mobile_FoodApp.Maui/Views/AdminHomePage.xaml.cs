using FoodApp.Maui.Models;
using FoodApp.Maui.Services;

namespace FoodApp.Maui.Views;

public partial class AdminHomePage : ContentPage
{
    public AdminHomePage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        NapTongQuan();
    }

    private void NapTongQuan()
    {
        LblChaoAdmin.Text = $"Xin chào, {MockData.QuanTriVienHienTai.HoTen}";
        LblVaiTro.Text = $"🔐 {MockData.QuanTriVienHienTai.VaiTro.Nhan()}";
        // Ô không đủ quyền hiển thị mờ (vẫn bấm được để biết lý do)
        OStaff.Opacity = PhanQuyenService.Co(Quyen.QuanLyNhanVien) ? 1 : 0.45;
        ONhatKy.Opacity = PhanQuyenService.Co(Quyen.XemNhatKy) ? 1 : 0.45;

        var gianHangDaDuyet = MockData.DanhSachGianHang.Where(g => g.TrangThaiDangKy == TrangThaiDangKyGianHang.DaDuyet).ToList();
        LblTongGianHang.Text = gianHangDaDuyet.Count.ToString();
        LblTongKhachHang.Text = MockData.DanhSachKhachHang.Count.ToString();

        var tongDoanhThu = gianHangDaDuyet.Sum(g => MockData.DoanhThuUocTinh(g.Id));
        LblTongDoanhThu.Text = $"{tongDoanhThu:N0}đ";

        var soChoDuyet = MockData.DanhSachGianHang.Count(g => g.TrangThaiDangKy == TrangThaiDangKyGianHang.ChoDuyet);
        var soTaiXe = MockData.DanhSachTaiXe.Count(t => t.ChoDuyet);
        LblSoTaiXeChoDuyet.Text = soTaiXe > 0 ? $"{soTaiXe} hồ sơ mới" : "Không có hồ sơ mới";
        LblSoChoDuyet.Text = soChoDuyet > 0 ? $"{soChoDuyet} hồ sơ mới" : "Không có hồ sơ mới";
    }

    private async void OnNhanVienTapped(object sender, EventArgs e)
    {
        if (await PhanQuyenService.KiemTraAsync(this, Quyen.QuanLyNhanVien)) await Shell.Current.GoToAsync(nameof(AdminStaffPage));
    }

    private async void OnNhatKyTapped(object sender, EventArgs e)
    {
        if (await PhanQuyenService.KiemTraAsync(this, Quyen.XemNhatKy)) await Shell.Current.GoToAsync(nameof(AdminAuditLogPage));
    }

    private async void OnDuyetTaiXeTapped(object sender, EventArgs e) => await Shell.Current.GoToAsync(nameof(AdminDriverApprovalPage));
    private async void OnBanDoTapped(object sender, EventArgs e) => await BanDoPage.MoAsync();
    private async void OnDuyetGianHangTapped(object sender, EventArgs e) => await Shell.Current.GoToAsync(nameof(AdminVendorApprovalPage));
    private async void OnDanhSachGianHangTapped(object sender, EventArgs e) => await Shell.Current.GoToAsync(nameof(AdminVendorListPage));
    private async void OnKhachHangTapped(object sender, EventArgs e) => await Shell.Current.GoToAsync(nameof(AdminCustomerListPage));
    private async void OnVanDeTapped(object sender, EventArgs e) => await Shell.Current.GoToAsync(nameof(AdminIssuesPage));
    private async void OnKhuyenMaiTapped(object sender, EventArgs e) => await Shell.Current.GoToAsync(nameof(AdminPromotionsPage));
    private async void OnThongKeTapped(object sender, EventArgs e) => await Shell.Current.GoToAsync(nameof(AdminStatisticsPage));

    private void OnRefreshing(object? sender, EventArgs e)
    {
        NapTongQuan();
        RefreshTrangChu.IsRefreshing = false;
    }

    private async void OnDangXuatClicked(object sender, EventArgs e)
    {
        bool xacNhan = await DisplayAlert("Đăng xuất", "Bạn có chắc muốn đăng xuất khỏi cổng quản trị?", "Đăng xuất", "Hủy");
        if (!xacNhan) return;

        NhatKyService.GhiQuanTri("Đăng xuất", chiTiet: NhatKyService.ThietBi());
        Services.Phien.DangXuat();
        await Shell.Current.GoToAsync("//dang-nhap");
    }

    private async void OnDoiMatKhauClicked(object sender, EventArgs e)
        => await Shell.Current.GoToAsync($"{nameof(DoiMatKhauPage)}?VaiTro=QuanTri");
}
