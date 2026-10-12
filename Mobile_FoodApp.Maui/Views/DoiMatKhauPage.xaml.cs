using FoodApp.Maui.Models;
using FoodApp.Maui.Services;

namespace FoodApp.Maui.Views;

/// <summary>Dùng chung cho 3 vai trò: KhachHang, GianHang, QuanTri.</summary>
[QueryProperty(nameof(VaiTro), "VaiTro")]
public partial class DoiMatKhauPage : ContentPage
{
    public string VaiTro { get; set; } = "KhachHang";

    public DoiMatKhauPage()
    {
        InitializeComponent();
    }

    private async void OnLuuClicked(object sender, EventArgs e)
    {
        LblLoi.IsVisible = false;
        if (string.IsNullOrEmpty(EntryCu.Text)) { HienLoi("Vui lòng nhập mật khẩu hiện tại"); return; }
        if (string.IsNullOrEmpty(EntryMoi.Text) || EntryMoi.Text.Length < 6) { HienLoi("Mật khẩu mới tối thiểu 6 ký tự"); return; }
        if (EntryMoi.Text != EntryNhapLai.Text) { HienLoi("Hai mật khẩu mới không khớp"); return; }
        if (EntryMoi.Text == EntryCu.Text) { HienLoi("Mật khẩu mới phải khác mật khẩu cũ"); return; }

        BtnLuu.IsEnabled = false;
        LoadingIndicator.IsVisible = LoadingIndicator.IsRunning = true;
        try
        {
            if (DuLieuService.DangOnline)
            {
                var kq = await ApiClient.PutAsync<object>("api/mobile/doi-mat-khau",
                    new { vaiTro = VaiTro, matKhauCu = EntryCu.Text, matKhauMoi = EntryMoi.Text });
                if (!kq.ThanhCong) { HienLoi(kq.ThongDiep); return; }
            }
            else if (!DoiOffline()) return;

            var (vaiTroLog, tk) = VaiTro switch
            {
                "GianHang" => ("GianHang", MockData.GianHangHienTai.Email),
                "QuanTri" => ("QuanTri:" + MockData.QuanTriVienHienTai.VaiTro, MockData.QuanTriVienHienTai.Email),
                "TaiXe" => ("TaiXe", MockData.TaiXeHienTai.SoDienThoai),
                _ => ("KhachHang", Phien.Khach?.Email ?? ""),
            };
            NhatKyService.Ghi(vaiTroLog, tk, "Đổi mật khẩu");
            await DisplayAlert("Thành công", "Đã đổi mật khẩu.", "OK");
            await Shell.Current.GoToAsync("..");
        }
        finally
        {
            BtnLuu.IsEnabled = true;
            LoadingIndicator.IsVisible = LoadingIndicator.IsRunning = false;
        }
    }

    // Chế độ demo ngoại tuyến: đổi trực tiếp trên dữ liệu mẫu.
    private bool DoiOffline()
    {
        switch (VaiTro)
        {
            case "GianHang":
                var gh = MockData.GianHangHienTai;
                if (gh.MatKhau != EntryCu.Text) { HienLoi("Mật khẩu hiện tại không đúng"); return false; }
                gh.MatKhau = EntryMoi.Text!;
                VendorLocalStore.Luu();
                return true;
            case "QuanTri":
                var ad = MockData.QuanTriVienHienTai;
                if (!QuanTriStore.KiemTraMatKhau(ad, EntryCu.Text ?? "")) { HienLoi("Mật khẩu hiện tại không đúng"); return false; }
                QuanTriStore.DatMatKhau(ad, EntryMoi.Text!);     // lưu dạng băm, giữ sau khi tắt app
                return true;
            case "TaiXe":
                var tx = MockData.TaiXeHienTai;
                if (!TaiXeStore.KiemTraMatKhau(tx, EntryCu.Text ?? "")) { HienLoi("Mật khẩu hiện tại không đúng"); return false; }
                TaiXeStore.DatMatKhau(tx, EntryMoi.Text!);
                return true;
            default:
                var kh = Phien.Khach;
                if (kh is null || kh.MatKhau != EntryCu.Text) { HienLoi("Mật khẩu hiện tại không đúng"); return false; }
                kh.MatKhau = EntryMoi.Text!;
                VendorLocalStore.Luu();
                return true;
        }
    }

    private void HienLoi(string t) { LblLoi.Text = t; LblLoi.IsVisible = true; }
}
