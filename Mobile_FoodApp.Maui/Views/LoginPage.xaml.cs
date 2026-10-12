using FoodApp.Maui.Models;
using FoodApp.Maui.Services;

namespace FoodApp.Maui.Views;

public partial class LoginPage : ContentPage
{
    private bool _dangOChedoDangNhap = true;

    public LoginPage()
    {
        InitializeComponent();
    }

    private void OnTabDangNhapClicked(object sender, EventArgs e) => ChuyenCheDo(true);
    private void OnTabDangKyClicked(object sender, EventArgs e) => ChuyenCheDo(false);

    private void ChuyenCheDo(bool laDangNhap)
    {
        _dangOChedoDangNhap = laDangNhap;
        KhungDangKy.IsVisible = !laDangNhap;
        BtnXacNhan.Text = laDangNhap ? "Đăng nhập" : "Tạo tài khoản";
        LblPhuDe.Text = laDangNhap ? "Đăng nhập để tiếp tục đặt món" : "Tạo tài khoản mới để bắt đầu";

        TabDangNhap.BackgroundColor = laDangNhap ? Colors.White : Colors.Transparent;
        TabDangNhap.TextColor = laDangNhap ? (Color)Application.Current!.Resources["Primary"] : (Color)Application.Current!.Resources["TextGrey"];
        TabDangKy.BackgroundColor = laDangNhap ? Colors.Transparent : Colors.White;
        TabDangKy.TextColor = laDangNhap ? (Color)Application.Current!.Resources["TextGrey"] : (Color)Application.Current!.Resources["Primary"];
    }

    private async void OnXacNhanClicked(object sender, EventArgs e)
    {
        LblLoi.IsVisible = false;

        if (string.IsNullOrWhiteSpace(EntryEmail.Text) || !EntryEmail.Text.Contains('@'))
        {
            HienLoi("Email không hợp lệ");
            return;
        }
        if (string.IsNullOrWhiteSpace(EntryMatKhau.Text) || EntryMatKhau.Text.Length < 6)
        {
            HienLoi("Mật khẩu tối thiểu 6 ký tự");
            return;
        }
        if (!_dangOChedoDangNhap && string.IsNullOrWhiteSpace(EntryHoTen.Text))
        {
            HienLoi("Vui lòng nhập họ tên");
            return;
        }

        if (!_dangOChedoDangNhap && string.IsNullOrWhiteSpace(EntryDiaChi.Text))
        {
            HienLoi("Vui lòng nhập địa chỉ giao hàng");
            return;
        }

        // Bug cũ: chế độ Đăng ký không kiểm tra trùng email và không thực sự lưu
        // tài khoản mới vào MockData, nên bấm "Tạo tài khoản" không đăng ký được gì cả.
        if (!DuLieuService.DangOnline && !_dangOChedoDangNhap && MockData.DanhSachKhachHang.Any(k =>
                string.Equals(k.Email, EntryEmail.Text.Trim(), StringComparison.OrdinalIgnoreCase)))
        {
            HienLoi("Email này đã được đăng ký, vui lòng đăng nhập");
            return;
        }

        LoadingIndicator.IsVisible = true;
        LoadingIndicator.IsRunning = true;
        BtnXacNhan.IsEnabled = false;

        try
        {
            // TODO: gọi ASP.NET Core Web API thật:
            //   POST /api/khach-hang/dang-nhap  { email, matKhau }
            //   POST /api/khach-hang/dang-ky    { hoTen, sdt, email, matKhau }
            // Tạm thời giả lập độ trễ mạng để hoàn thiện luồng giao diện.
            if (DuLieuService.DangOnline)
            {
                var kq = _dangOChedoDangNhap
                    ? await ApiClient.PostAsync<DangNhapKhachDto>("api/mobile/khach-hang/dang-nhap",
                        new { email = EntryEmail.Text.Trim(), matKhau = EntryMatKhau.Text })
                    : await ApiClient.PostAsync<DangNhapKhachDto>("api/mobile/khach-hang/dang-ky",
                        new { hoTen = EntryHoTen.Text.Trim(), sdt = EntrySdt.Text?.Trim() ?? "", diaChi = EntryDiaChi.Text?.Trim() ?? "", email = EntryEmail.Text.Trim(), matKhau = EntryMatKhau.Text });
                if (!kq.ThanhCong)
                {
                    if (_dangOChedoDangNhap) NhatKyService.Ghi("KhachHang", EntryEmail.Text.Trim(), "Đăng nhập thất bại", chiTiet: kq.ThongDiep + " · " + NhatKyService.ThietBi(), thanhCong: false);
                    HienLoi(kq.ThongDiep);
                    return;
                }
                ApiClient.Token = kq.DuLieu?.Token;
                if (kq.DuLieu?.KhachHang is { } k && MockData.DanhSachKhachHang.All(x => x.Id != k.Id))
                    MockData.DanhSachKhachHang.Add(k);
                Phien.Khach = kq.DuLieu?.KhachHang ?? new KhachHang { Email = EntryEmail.Text.Trim(), HoTen = EntryHoTen.Text?.Trim() ?? "" };
                if (!_dangOChedoDangNhap && string.IsNullOrWhiteSpace(Phien.Khach.DiaChi)) Phien.Khach.DiaChi = EntryDiaChi.Text?.Trim() ?? "";
                await DuLieuService.TaiDonHangKhachAsync();
                await HoanTatDangNhapAsync(Phien.Khach);
                return;
            }

            await Task.Delay(300);

            if (_dangOChedoDangNhap)
            {
                var taiKhoan = MockData.DanhSachKhachHang.FirstOrDefault(k =>
                    string.Equals(k.Email, EntryEmail.Text.Trim(), StringComparison.OrdinalIgnoreCase));

                if (taiKhoan == null || taiKhoan.MatKhau != EntryMatKhau.Text)
                {
                    NhatKyService.Ghi("KhachHang", EntryEmail.Text.Trim(), "Đăng nhập thất bại", chiTiet: "Sai email hoặc mật khẩu · " + NhatKyService.ThietBi(), thanhCong: false);
                    HienLoi("Email hoặc mật khẩu không đúng");
                    return;
                }
                if (taiKhoan.TaiKhoanBiKhoa)
                {
                    NhatKyService.Ghi("KhachHang", taiKhoan.Email, "Đăng nhập thất bại", chiTiet: "Tài khoản bị khóa · " + NhatKyService.ThietBi(), thanhCong: false);
                    HienLoi("Tài khoản của bạn đã bị khóa");
                    return;
                }
            }
            else
            {
                var khachHangMoi = new KhachHang
                {
                    Id = Services.IdHelper.TiepTheo("kh", MockData.DanhSachKhachHang.Select(x => x.Id)),
                    HoTen = EntryHoTen.Text.Trim(),
                    Email = EntryEmail.Text.Trim(),
                    MatKhau = EntryMatKhau.Text,
                    SoDienThoai = EntrySdt.Text?.Trim() ?? "",
                    DiaChi = EntryDiaChi.Text?.Trim() ?? "",
                    NgayDangKy = DateTime.Now,
                    TongSoDon = 0,
                };
                MockData.DanhSachKhachHang.Add(khachHangMoi);
                VendorLocalStore.Luu();   // giữ tài khoản mới sau khi tắt app (chế độ demo)
            }

            Phien.Khach = MockData.DanhSachKhachHang.FirstOrDefault(k =>
                string.Equals(k.Email, EntryEmail.Text.Trim(), StringComparison.OrdinalIgnoreCase));
            await HoanTatDangNhapAsync(Phien.Khach!);
        }
        finally
        {
            LoadingIndicator.IsVisible = false;
            LoadingIndicator.IsRunning = false;
            BtnXacNhan.IsEnabled = true;
        }
    }

    /// <summary>Ghi nhật ký; tài khoản mới phải xác thực email bằng OTP (bấm "Để sau" vẫn dùng được, xác thực lại trong Hồ sơ).</summary>
    private async Task HoanTatDangNhapAsync(KhachHang khach)
    {
        if (!_dangOChedoDangNhap)
        {
            TaiKhoanXoaStore.BoDanhDau(khach.Email);
            NhatKyService.Ghi("KhachHang", khach.Email, "Đăng ký tài khoản", chiTiet: NhatKyService.ThietBi());
            khach.EmailDaXacThuc = await XacThucOtpPage.CanXacThucAsync(OtpService.XacThucEmail, khach.Email, "Xác thực email");
            VendorLocalStore.Luu();
        }
        NhatKyService.Ghi("KhachHang", khach.Email, "Đăng nhập", chiTiet: NhatKyService.ThietBi());
        await AppShell.DangNhapThanhCongAsync();
    }

    private void HienLoi(string thongDiep)
    {
        LblLoi.Text = thongDiep;
        LblLoi.IsVisible = true;
    }

    private async void OnQuenMatKhauTapped(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(QuenMatKhauPage), new Dictionary<string, object> { { "VaiTro", "KhachHang" } });
    }

    private async void OnDangNhapTaiXeTapped(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(DriverLoginPage));
    }

    private async void OnDangNhapGianHangTapped(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(VendorLoginPage));
    }

    private async void OnDangNhapQuanTriTapped(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(AdminLoginPage));
    }
}
