using FoodApp.Maui.Models;
using FoodApp.Maui.Services;

namespace FoodApp.Maui.Views;

public partial class DriverLoginPage : ContentPage
{
    public DriverLoginPage()
    {
        InitializeComponent();
    }

    private async void OnDangNhapClicked(object sender, EventArgs e)
    {
        LblLoi.IsVisible = false;
        var sdt = TaiXeStore.ChuanHoaSdt(EntrySdt.Text);
        if (sdt.Length == 0) { HienLoi("Nhập số điện thoại"); return; }
        if (sdt.Length < 9 || sdt.Length > 11) { HienLoi("Số điện thoại phải có 9–11 chữ số"); return; }
        if (string.IsNullOrEmpty(EntryMatKhau.Text) || EntryMatKhau.Text.Length < 6) { HienLoi("Mật khẩu tối thiểu 6 ký tự"); return; }

        BtnDangNhap.IsEnabled = false;
        LoadingIndicator.IsVisible = LoadingIndicator.IsRunning = true;
        try
        {
            TaiXe? taiXe;
            if (DuLieuService.DangOnline)
            {
                var kq = await ApiClient.PostAsync<DangNhapTaiXeDto>("api/mobile/tai-xe/dang-nhap", new { sdt, matKhau = EntryMatKhau.Text });
                if (!kq.ThanhCong)
                {
                    NhatKyService.Ghi("TaiXe", sdt, "Đăng nhập thất bại", chiTiet: kq.ThongDiep + " · " + NhatKyService.ThietBi(), thanhCong: false);
                    HienLoi(kq.ThongDiep);
                    return;
                }
                ApiClient.Token = kq.DuLieu?.Token;
                taiXe = TaiXeStore.TimTheoSdt(sdt) ?? new TaiXe { Id = "tx" + sdt, HoTen = "Tài xế", SoDienThoai = sdt };
            }
            else
            {
                taiXe = TaiXeStore.TimTheoSdt(sdt);
                if (taiXe is null) { HienLoi("Số điện thoại chưa đăng ký tài xế. Bấm \"Đăng ký làm tài xế\" bên dưới."); return; }
                if (!TaiXeStore.CoMatKhau(taiXe)) TaiXeStore.DatMatKhau(taiXe, EntryMatKhau.Text);   // hồ sơ cũ chưa có mật khẩu
                else if (!TaiXeStore.KiemTraMatKhau(taiXe, EntryMatKhau.Text))
                {
                    NhatKyService.Ghi("TaiXe", sdt, "Đăng nhập thất bại", chiTiet: "Sai mật khẩu · " + NhatKyService.ThietBi(), thanhCong: false);
                    HienLoi("Sai số điện thoại hoặc mật khẩu");
                    return;
                }
            }

            // Xét duyệt: chỉ tài xế đã được quản trị viên duyệt mới vào được.
            if (taiXe.TaiKhoanBiKhoa)
            {
                NhatKyService.Ghi("TaiXe", sdt, "Đăng nhập thất bại", chiTiet: "Tài khoản bị khóa · " + NhatKyService.ThietBi(), thanhCong: false);
                HienLoi("Tài khoản của bạn đang bị khóa. Vui lòng liên hệ quản trị viên.");
                return;
            }
            if (taiXe.TrangThaiDangKy == TrangThaiDangKyGianHang.ChoDuyet) { HienLoi("Hồ sơ của bạn đang chờ quản trị viên duyệt. Vui lòng quay lại sau."); return; }
            if (taiXe.TrangThaiDangKy == TrangThaiDangKyGianHang.TuChoi)
            {
                HienLoi($"Hồ sơ bị từ chối: {(string.IsNullOrWhiteSpace(taiXe.LyDoTuChoi) ? "không đạt yêu cầu" : taiXe.LyDoTuChoi)}. Bạn có thể đăng ký lại.");
                return;
            }

            if (taiXe.NgayThongKe.Date != DateTime.Today)          // sang ngày mới: làm lại thống kê "hôm nay"
            {
                taiXe.TongThuNhapHomNay = 0; taiXe.SoDonHomNay = 0; taiXe.NgayThongKe = DateTime.Today;
            }

            MockData.TaiXeHienTai = taiXe;
            taiXe.DangOnline = true;                 // đăng nhập = vào ca (online); "sẵn sàng" giữ theo lựa chọn lần trước
            NhatKyService.Ghi("TaiXe", sdt, "Đăng nhập", chiTiet: NhatKyService.ThietBi());
            // Khôi phục đơn đang giao dở của tài xế này (nếu có)
            MockData.DonHangDangGiaoCuaTaiXe = MockData.DonHangChoTaiXe.FirstOrDefault(d =>
                d.TaiXeId == taiXe.Id && (d.TrangThai == TrangThaiDonHang.DangLam || d.TrangThai == TrangThaiDonHang.DangGiao));
            TaiXeStore.Luu();
            if (DuLieuService.DangOnline)
                await ApiClient.PutAsync<object>("api/mobile/tai-xe/trang-thai", new { trangThai = taiXe.NhanDuocDon ? "SanSang" : "NgoaiTuyen" });

            await Task.Delay(150);
            await Shell.Current.GoToAsync(nameof(DriverHomePage));
        }
        finally
        {
            BtnDangNhap.IsEnabled = true;
            LoadingIndicator.IsVisible = LoadingIndicator.IsRunning = false;
        }
    }

    private async void OnQuenMatKhauTapped(object sender, EventArgs e)
        => await Shell.Current.GoToAsync(nameof(QuenMatKhauPage), new Dictionary<string, object> { { "VaiTro", "TaiXe" } });

    private async void OnDangKyClicked(object sender, EventArgs e) => await Shell.Current.GoToAsync(nameof(DriverRegisterPage));

    private void HienLoi(string t) { LblLoi.Text = t; LblLoi.IsVisible = true; }

    private async void OnQuayLaiTapped(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}
