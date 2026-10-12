using System.Net.Mail;
using FoodApp.Maui.Models;
using FoodApp.Maui.Services;

namespace FoodApp.Maui.Views;

/// <summary>Quên mật khẩu bằng OTP cho khách hàng (email), gian hàng (email) và tài xế (số điện thoại).</summary>
[QueryProperty(nameof(VaiTro), "VaiTro")]
public partial class QuenMatKhauPage : ContentPage
{
    public string VaiTro { get; set; } = "KhachHang";   // KhachHang | GianHang | TaiXe

    private string _dinhDanh = "";                      // email (đã hạ chữ thường) hoặc số điện thoại đã chuẩn hóa

    public QuenMatKhauPage()
    {
        InitializeComponent();
    }

    private bool LaTaiXe => VaiTro == "TaiXe";

    protected override void OnAppearing()
    {
        base.OnAppearing();
        EntryDinhDanh.Placeholder = LaTaiXe ? "Số điện thoại đã đăng ký" : "Email đã đăng ký";
        EntryDinhDanh.Keyboard = LaTaiXe ? Keyboard.Telephone : Keyboard.Email;
        LblHuongDan.Text = LaTaiXe
            ? "Nhập số điện thoại đã đăng ký tài xế. Mã OTP sẽ được gửi qua SMS."
            : "Nhập email đã đăng ký. Mã OTP sẽ được gửi tới email đó.";
    }

    private static bool EmailHopLe(string e) =>
        MailAddress.TryCreate(e, out var m) && m.Address == e && e.Contains('.');

    private async void OnGuiMaClicked(object sender, EventArgs e)
    {
        LblLoi.IsVisible = false;
        var nhap = EntryDinhDanh.Text?.Trim() ?? "";
        _dinhDanh = LaTaiXe ? TaiXeStore.ChuanHoaSdt(nhap) : nhap.ToLowerInvariant();

        if (LaTaiXe ? (_dinhDanh.Length < 9 || _dinhDanh.Length > 11) : !EmailHopLe(_dinhDanh))
        { HienLoi(LaTaiXe ? "Số điện thoại phải có 9–11 chữ số" : "Email không hợp lệ"); return; }

        // Offline: kiểm tra tài khoản có tồn tại. Online: server tự kiểm tra khi gửi OTP.
        if (!DuLieuService.DangOnline && !TonTai()) { HienLoi("Không tìm thấy tài khoản với thông tin này."); return; }

        BtnGuiMa.IsEnabled = false;
        Dang.IsVisible = Dang.IsRunning = true;
        try
        {
            var kq = await OtpService.GuiAsync(OtpService.DatLaiMatKhau, _dinhDanh);
            if (!kq.ThanhCong) { HienLoi(kq.ThongDiep); return; }
            KhungBuoc2.IsVisible = true;
            KhungMaDemo.IsVisible = kq.MaDemo is not null;
            LblGhiChuDemo.Text = kq.ThongDiep;
            LblMaDemo.Text = kq.MaDemo ?? "";
        }
        finally { BtnGuiMa.IsEnabled = true; Dang.IsVisible = Dang.IsRunning = false; }
    }

    private bool TonTai() => VaiTro switch
    {
        "GianHang" => MockData.DanhSachGianHang.Any(g => string.Equals(g.Email, _dinhDanh, StringComparison.OrdinalIgnoreCase)),
        "TaiXe" => TaiXeStore.TimTheoSdt(_dinhDanh) is not null,
        _ => MockData.DanhSachKhachHang.Any(k => string.Equals(k.Email, _dinhDanh, StringComparison.OrdinalIgnoreCase)),
    };

    private async void OnDatLaiClicked(object sender, EventArgs e)
    {
        LblLoi.IsVisible = false;
        if (string.IsNullOrEmpty(EntryMatKhauMoi.Text) || EntryMatKhauMoi.Text.Length < 6) { HienLoi("Mật khẩu mới tối thiểu 6 ký tự"); return; }
        if (EntryMatKhauMoi.Text != EntryNhapLai.Text) { HienLoi("Hai mật khẩu không khớp"); return; }

        BtnDatLai.IsEnabled = false;
        Dang.IsVisible = Dang.IsRunning = true;
        try
        {
            if (DuLieuService.DangOnline)
            {
                // Web kiểm OTP (6 số, 5 phút, sai tối đa 5 lần) cùng lúc với đặt mật khẩu mới.
                var kq = await ApiClient.PostAsync<object>("api/mobile/tai-khoan/dat-lai-mat-khau",
                    new { taiKhoan = _dinhDanh, otp = EntryOtp.Text?.Trim(), matKhauMoi = EntryMatKhauMoi.Text });
                if (!kq.ThanhCong) { HienLoi(kq.ThongDiep); return; }
            }
            else
            {
                var (ok, tb) = await OtpService.KiemAsync(OtpService.DatLaiMatKhau, _dinhDanh, EntryOtp.Text);
                if (!ok) { HienLoi(tb); return; }

                switch (VaiTro)
                {
                    case "GianHang":
                        MockData.DanhSachGianHang.First(g => string.Equals(g.Email, _dinhDanh, StringComparison.OrdinalIgnoreCase)).MatKhau = EntryMatKhauMoi.Text;
                        VendorLocalStore.Luu();
                        break;
                    case "TaiXe":
                        TaiXeStore.DatMatKhau(TaiXeStore.TimTheoSdt(_dinhDanh)!, EntryMatKhauMoi.Text);
                        break;
                    default:
                        MockData.DanhSachKhachHang.First(k => string.Equals(k.Email, _dinhDanh, StringComparison.OrdinalIgnoreCase)).MatKhau = EntryMatKhauMoi.Text;
                        VendorLocalStore.Luu();
                        break;
                }
            }

            NhatKyService.Ghi(VaiTro, _dinhDanh, "Đặt lại mật khẩu bằng OTP");
            await DisplayAlert("Thành công", "Đã đặt lại mật khẩu. Hãy đăng nhập bằng mật khẩu mới.", "OK");
            await Shell.Current.GoToAsync("..");
        }
        finally { BtnDatLai.IsEnabled = true; Dang.IsVisible = Dang.IsRunning = false; }
    }

    private void HienLoi(string t) { LblLoi.Text = t; LblLoi.IsVisible = true; }
}
