using FoodApp.Maui.Models;
using FoodApp.Maui.Services;

namespace FoodApp.Maui.Views;

public partial class DriverRegisterPage : ContentPage
{
    private static readonly string[] LoaiXe = { "Xe máy", "Xe tay ga", "Xe điện", "Ô tô" };
    private string _anhGplx = "";

    public DriverRegisterPage()
    {
        InitializeComponent();
        PickerLoaiXe.ItemsSource = LoaiXe;
        PickerLoaiXe.SelectedIndex = 0;
    }

    private async void OnTaiGplxClicked(object sender, EventArgs e)
    {
        try
        {
            var path = await AnhService.ChonVaLuuAsync("gplx", "Chọn ảnh giấy phép lái xe");
            if (path is null) return;
            if (!string.IsNullOrEmpty(_anhGplx)) { try { File.Delete(_anhGplx); } catch { } }
            _anhGplx = path;
            LblGplx.Text = "✓ Đã chọn ảnh: " + Path.GetFileName(path);
        }
        catch (Exception ex) { HienLoi("Không tải được ảnh: " + ex.Message); }
    }

    private async void OnGuiClicked(object sender, EventArgs e)
    {
        LblLoi.IsVisible = false;
        var ten = EntryHoTen.Text?.Trim() ?? "";
        var sdt = TaiXeStore.ChuanHoaSdt(EntrySdt.Text);
        var cccd = new string((EntryCccd.Text ?? "").Where(char.IsDigit).ToArray());
        var bienSo = EntryBienSo.Text?.Trim().ToUpperInvariant() ?? "";
        var gplx = EntryGplx.Text?.Trim() ?? "";

        if (ten.Length < 2) { HienLoi("Vui lòng nhập họ tên"); return; }
        if (sdt.Length < 9 || sdt.Length > 11) { HienLoi("Số điện thoại phải có 9–11 chữ số"); return; }
        if (cccd.Length != 9 && cccd.Length != 12) { HienLoi("CCCD/CMND phải có 9 hoặc 12 số"); return; }
        if (bienSo.Length < 5) { HienLoi("Vui lòng nhập biển số xe"); return; }
        if (gplx.Length < 6) { HienLoi("Vui lòng nhập số giấy phép lái xe"); return; }
        if (string.IsNullOrEmpty(EntryMatKhau.Text) || EntryMatKhau.Text.Length < 6) { HienLoi("Mật khẩu tối thiểu 6 ký tự"); return; }
        if (EntryMatKhau.Text != EntryNhapLai.Text) { HienLoi("Hai mật khẩu không khớp"); return; }
        var cu = TaiXeStore.TimTheoSdt(sdt);
        if (cu is not null && cu.TrangThaiDangKy != TrangThaiDangKyGianHang.TuChoi) { HienLoi("Số điện thoại này đã đăng ký tài xế"); return; }
        if (cu is not null) MockData.DanhSachTaiXe.Remove(cu);          // hồ sơ trước bị từ chối: cho đăng ký lại

        BtnGui.IsEnabled = false;
        Dang.IsVisible = Dang.IsRunning = true;
        try
        {
            var loai = PickerLoaiXe.SelectedItem as string ?? LoaiXe[0];
            if (DuLieuService.DangOnline)
            {
                // Online: hồ sơ nằm trên server, quản trị viên duyệt ở đó.
                var kq = await ApiClient.PostAsync<object>("api/mobile/tai-xe/dang-ky",
                    new { hoTen = ten, soDienThoai = sdt, soCccd = cccd, loaiXe = loai, bienSoXe = bienSo, soGplx = gplx, matKhau = EntryMatKhau.Text });
                if (!kq.ThanhCong) { HienLoi(kq.ThongDiep); return; }
            }
            else
            {
                TaiXeStore.DangKy(new TaiXe
                {
                    HoTen = ten, SoDienThoai = sdt, SoCccd = cccd, LoaiXe = loai, BienSoXe = bienSo,
                    SoGplx = gplx, AnhGplxPath = _anhGplx,
                }, EntryMatKhau.Text);
            }

            await DisplayAlert("Đã gửi hồ sơ", "Hồ sơ của bạn đang chờ quản trị viên duyệt. Bạn có thể đăng nhập sau khi được duyệt.", "OK");
            await Shell.Current.GoToAsync("..");
        }
        finally
        {
            BtnGui.IsEnabled = true;
            Dang.IsVisible = Dang.IsRunning = false;
        }
    }

    private void HienLoi(string t) { LblLoi.Text = t; LblLoi.IsVisible = true; }
}
