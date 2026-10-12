using System.Net.Mail;
using FoodApp.Maui.Models;
using FoodApp.Maui.Services;

namespace FoodApp.Maui.Views;

/// <summary>Chỉ Admin tổng: thêm / khóa / đổi vai trò / đặt lại mật khẩu cho tài khoản quản trị.</summary>
public partial class AdminStaffPage : ContentPage
{
    private static readonly string[] VaiTro = { "Nhân viên hỗ trợ", "Admin tổng" };

    public AdminStaffPage()
    {
        InitializeComponent();
        PickerVaiTro.ItemsSource = VaiTro;
        PickerVaiTro.SelectedIndex = 0;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (!await PhanQuyenService.KiemTraAsync(this, Quyen.QuanLyNhanVien))
        {
            await Shell.Current.GoToAsync("..");
            return;
        }
        if (DuLieuService.DangOnline)
        {
            await DisplayAlert("Quản lý trên website", "Khi đang kết nối máy chủ, tài khoản quản trị (Admin tổng / Quản lý / Nhân viên hỗ trợ) được tạo và phân quyền tại website: Quản trị → Nhân viên.", "Đã hiểu");
            await Shell.Current.GoToAsync("..");
            return;
        }
        NapDanhSach();
    }

    private void NapDanhSach()
    {
        DsQuanTri.ItemsSource = null;
        DsQuanTri.ItemsSource = MockData.DanhSachQuanTri.OrderBy(q => q.VaiTro).ThenBy(q => q.NgayTao).ToList();
    }

    private static bool LaChinhMinh(QuanTriVien q) =>
        string.Equals(q.Email, MockData.QuanTriVienHienTai.Email, StringComparison.OrdinalIgnoreCase);

    private async void OnThemClicked(object sender, EventArgs e)
    {
        if (!await PhanQuyenService.KiemTraAsync(this, Quyen.QuanLyNhanVien)) return;
        LblLoi.IsVisible = false;

        var ten = EntryHoTen.Text?.Trim() ?? "";
        var email = EntryEmail.Text?.Trim() ?? "";
        if (ten.Length < 2) { HienLoi("Vui lòng nhập họ tên"); return; }
        if (!MailAddress.TryCreate(email, out var m) || m.Address != email || !email.Contains('.')) { HienLoi("Email không hợp lệ"); return; }
        if (QuanTriStore.TimTheoEmail(email) is not null) { HienLoi("Email này đã có tài khoản quản trị"); return; }
        if (string.IsNullOrEmpty(EntryMatKhau.Text) || EntryMatKhau.Text.Length < 6) { HienLoi("Mật khẩu tạm tối thiểu 6 ký tự"); return; }

        var vaiTro = PickerVaiTro.SelectedIndex == 1 ? VaiTroQuanTri.AdminTong : VaiTroQuanTri.NhanVienHoTro;
        QuanTriStore.Them(ten, email, EntryMatKhau.Text, vaiTro);
        NhatKyService.GhiQuanTri("Tạo tài khoản quản trị", email, vaiTro.Nhan());

        EntryHoTen.Text = EntryEmail.Text = EntryMatKhau.Text = "";
        NapDanhSach();
        await DisplayAlert("Đã thêm", $"Đã tạo tài khoản {vaiTro.Nhan()} cho {ten}.", "OK");
    }

    private async void OnKhoaClicked(object sender, EventArgs e)
    {
        if (sender is not Button { CommandParameter: QuanTriVien q }) return;
        if (!await PhanQuyenService.KiemTraAsync(this, Quyen.QuanLyNhanVien)) return;

        if (LaChinhMinh(q)) { await DisplayAlert("Không thể thực hiện", "Bạn không thể tự khóa tài khoản của mình.", "OK"); return; }
        if (!q.TaiKhoanBiKhoa && q.VaiTro == VaiTroQuanTri.AdminTong && QuanTriStore.SoAdminTongHoatDong <= 1)
        { await DisplayAlert("Không thể thực hiện", "Hệ thống phải còn ít nhất một Admin tổng hoạt động.", "OK"); return; }

        var khoa = !q.TaiKhoanBiKhoa;
        if (!await DisplayAlert(khoa ? "Khóa tài khoản" : "Mở khóa", $"{(khoa ? "Khóa" : "Mở khóa")} tài khoản {q.Email}?", "Đồng ý", "Hủy")) return;
        q.TaiKhoanBiKhoa = khoa;
        QuanTriStore.Luu();
        NhatKyService.GhiQuanTri(khoa ? "Khóa tài khoản quản trị" : "Mở khóa tài khoản quản trị", q.Email);
        NapDanhSach();
    }

    private async void OnVaiTroClicked(object sender, EventArgs e)
    {
        if (sender is not Button { CommandParameter: QuanTriVien q }) return;
        if (!await PhanQuyenService.KiemTraAsync(this, Quyen.QuanLyNhanVien)) return;

        if (LaChinhMinh(q)) { await DisplayAlert("Không thể thực hiện", "Bạn không thể tự đổi vai trò của mình.", "OK"); return; }
        if (q.VaiTro == VaiTroQuanTri.AdminTong && !q.TaiKhoanBiKhoa && QuanTriStore.SoAdminTongHoatDong <= 1)
        { await DisplayAlert("Không thể thực hiện", "Hệ thống phải còn ít nhất một Admin tổng hoạt động.", "OK"); return; }

        var moi = q.VaiTro == VaiTroQuanTri.AdminTong ? VaiTroQuanTri.NhanVienHoTro : VaiTroQuanTri.AdminTong;
        if (!await DisplayAlert("Đổi vai trò", $"Đổi {q.Email} thành \"{moi.Nhan()}\"?", "Đồng ý", "Hủy")) return;
        var cu = q.VaiTro;
        q.VaiTro = moi;
        QuanTriStore.Luu();
        NhatKyService.GhiQuanTri("Đổi vai trò quản trị", q.Email, $"{cu.Nhan()} → {moi.Nhan()}");
        NapDanhSach();
    }

    private async void OnDatLaiMatKhauClicked(object sender, EventArgs e)
    {
        if (sender is not Button { CommandParameter: QuanTriVien q }) return;
        if (!await PhanQuyenService.KiemTraAsync(this, Quyen.QuanLyNhanVien)) return;

        var mk = await DisplayPromptAsync("Đặt lại mật khẩu", $"Mật khẩu mới cho {q.Email} (tối thiểu 6 ký tự):",
            "Đặt lại", "Hủy", "Mật khẩu mới", maxLength: 64);
        if (mk is null) return;
        if (mk.Length < 6) { await DisplayAlert("Chưa hợp lệ", "Mật khẩu tối thiểu 6 ký tự.", "OK"); return; }

        QuanTriStore.DatMatKhau(q, mk);
        NhatKyService.GhiQuanTri("Đặt lại mật khẩu quản trị", q.Email);
        await DisplayAlert("Đã đặt lại", "Hãy báo mật khẩu mới cho người dùng và yêu cầu họ đổi lại.", "OK");
    }

    private void HienLoi(string t) { LblLoi.Text = t; LblLoi.IsVisible = true; }
}
