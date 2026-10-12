using FoodApp.Maui.Models;
using FoodApp.Maui.Services;

namespace FoodApp.Maui.Views;

public partial class VendorLoginPage : ContentPage
{
    public VendorLoginPage()
    {
        InitializeComponent();
    }

    private async void OnDangNhapClicked(object sender, EventArgs e)
    {
        LblLoi.IsVisible = false;

        if (string.IsNullOrWhiteSpace(EntryEmail.Text) || !EntryEmail.Text.Contains('@'))
        {
            HienLoi("Email không hợp lệ");
            return;
        }
        if (string.IsNullOrWhiteSpace(EntryMatKhau.Text))
        {
            HienLoi("Vui lòng nhập mật khẩu");
            return;
        }

        LoadingIndicator.IsVisible = true;
        LoadingIndicator.IsRunning = true;
        BtnDangNhap.IsEnabled = false;

        try
        {
            GianHang? gianHang;
            if (DuLieuService.DangOnline)
            {
                var kq = await ApiClient.PostAsync<DangNhapGianHangDto>("api/mobile/gian-hang/dang-nhap",
                    new { email = EntryEmail.Text.Trim(), matKhau = EntryMatKhau.Text });
                if (!kq.ThanhCong || kq.DuLieu?.GianHang is null) { HienLoi(kq.ThongDiep); return; }
                ApiClient.Token = kq.DuLieu.Token;
                gianHang = kq.DuLieu.GianHang;
                var cu = MockData.DanhSachGianHang.FirstOrDefault(g => g.Id == gianHang.Id);
                if (cu is not null) MockData.DanhSachGianHang.Remove(cu);
                MockData.DanhSachGianHang.Add(gianHang);
                await DuLieuService.TaiDonHangGianHangAsync(gianHang.Id);
            }
            else
            {
                await Task.Delay(300);
                gianHang = MockData.DanhSachGianHang.FirstOrDefault(g =>
                    string.Equals(g.Email, EntryEmail.Text.Trim(), StringComparison.OrdinalIgnoreCase) &&
                    g.MatKhau == EntryMatKhau.Text);
            }

            if (gianHang is null)
            {
                NhatKyService.Ghi("GianHang", EntryEmail.Text.Trim(), "Đăng nhập thất bại", chiTiet: "Sai email hoặc mật khẩu · " + NhatKyService.ThietBi(), thanhCong: false);
                HienLoi("Email hoặc mật khẩu không đúng");
                return;
            }

            if (gianHang.TrangThaiDangKy == TrangThaiDangKyGianHang.ChoDuyet)
            {
                HienLoi("Hồ sơ gian hàng của bạn đang chờ quản trị viên duyệt");
                return;
            }
            if (gianHang.TrangThaiDangKy == TrangThaiDangKyGianHang.TuChoi)
            {
                HienLoi("Hồ sơ gian hàng đã bị từ chối" + (string.IsNullOrWhiteSpace(gianHang.LyDoTuChoi) ? "" : $": {gianHang.LyDoTuChoi}") + ". Vui lòng liên hệ quản trị viên");
                return;
            }
            if (gianHang.TaiKhoanBiKhoa)
            {
                NhatKyService.Ghi("GianHang", gianHang.Email, "Đăng nhập thất bại", chiTiet: "Tài khoản bị khóa · " + NhatKyService.ThietBi(), thanhCong: false);
                HienLoi("Tài khoản gian hàng đang bị khóa. Vui lòng liên hệ quản trị viên");
                return;
            }

            NhatKyService.Ghi("GianHang", gianHang.Email, "Đăng nhập", gianHang.TenGianHang, NhatKyService.ThietBi());
            MockData.GianHangHienTai = gianHang;
            await Shell.Current.GoToAsync(nameof(VendorHomePage));
        }
        finally
        {
            LoadingIndicator.IsVisible = false;
            LoadingIndicator.IsRunning = false;
            BtnDangNhap.IsEnabled = true;
        }
    }

    private void HienLoi(string thongDiep)
    {
        LblLoi.Text = thongDiep;
        LblLoi.IsVisible = true;
    }

    private async void OnQuenMatKhauTapped(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(QuenMatKhauPage), new Dictionary<string, object> { { "VaiTro", "GianHang" } });
    }

    private async void OnDangKyTapped(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(VendorRegisterPage));
    }

    private async void OnQuayLaiTapped(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}
