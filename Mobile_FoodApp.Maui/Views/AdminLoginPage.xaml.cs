using FoodApp.Maui.Models;
using FoodApp.Maui.Services;

namespace FoodApp.Maui.Views;

public partial class AdminLoginPage : ContentPage
{
    public AdminLoginPage()
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
            var email = EntryEmail.Text.Trim();
            QuanTriVien? dangNhap;
            if (DuLieuService.DangOnline)
            {
                var kq = await ApiClient.PostAsync<DangNhapQuanTriDto>("api/mobile/admin/dang-nhap", new { email, matKhau = EntryMatKhau.Text });
                if (!kq.ThanhCong)
                {
                    NhatKyService.Ghi("QuanTri", email, "Đăng nhập thất bại", chiTiet: kq.ThongDiep + " · " + NhatKyService.ThietBi(), thanhCong: false);
                    HienLoi(kq.ThongDiep);
                    return;
                }
                ApiClient.Token = kq.DuLieu?.Token;
                // Web: SuperAdmin = Admin tổng; QuanLy / NhanVien = quyền hạn chế (không nhận được vai trò thì lấy quyền thấp nhất)
                var vaiTro = string.Equals(kq.DuLieu?.VaiTro, "SuperAdmin", StringComparison.OrdinalIgnoreCase)
                    ? VaiTroQuanTri.AdminTong : VaiTroQuanTri.NhanVienHoTro;
                dangNhap = new QuanTriVien { Id = "online", HoTen = kq.DuLieu?.HoTen ?? email, Email = email, VaiTro = vaiTro };
                await DuLieuService.TaiQuanTriAsync();
            }
            else
            {
                await Task.Delay(300);
                dangNhap = QuanTriStore.TimTheoEmail(email);
                if (dangNhap is null || !QuanTriStore.KiemTraMatKhau(dangNhap, EntryMatKhau.Text))
                {
                    NhatKyService.Ghi("QuanTri", email, "Đăng nhập thất bại", chiTiet: "Sai email hoặc mật khẩu · " + NhatKyService.ThietBi(), thanhCong: false);
                    HienLoi("Email hoặc mật khẩu không đúng");
                    return;
                }
                if (dangNhap.TaiKhoanBiKhoa)
                {
                    NhatKyService.Ghi("QuanTri:" + dangNhap.VaiTro, dangNhap.Email, "Đăng nhập thất bại", chiTiet: "Tài khoản bị khóa · " + NhatKyService.ThietBi(), thanhCong: false);
                    HienLoi("Tài khoản quản trị đang bị khóa. Hãy liên hệ Admin tổng.");
                    return;
                }
            }

            MockData.QuanTriVienHienTai = dangNhap;
            NhatKyService.GhiQuanTri("Đăng nhập", chiTiet: NhatKyService.ThietBi());

            await Shell.Current.GoToAsync(nameof(AdminHomePage));
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

    private async void OnQuayLaiTapped(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}
