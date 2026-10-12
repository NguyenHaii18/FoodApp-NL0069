using FoodApp.Maui.Models;
using FoodApp.Maui.Services;

namespace FoodApp.Maui.Views;

public partial class DriverProfilePage : ContentPage
{
    private static readonly string[] LoaiXe = { "Xe máy", "Xe tay ga", "Xe điện", "Ô tô" };

    public DriverProfilePage()
    {
        InitializeComponent();
        PickerLoaiXe.ItemsSource = LoaiXe;
        BtnChup.IsVisible = MediaPicker.Default.IsCaptureSupported;   // máy không có camera thì ẩn nút chụp
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        var t = MockData.TaiXeHienTai;
        EntryHoTen.Text = t.HoTen;
        EntrySdt.Text = t.SoDienThoai;
        EntryBienSo.Text = t.BienSoXe;
        EntryEmoji.Text = t.Anh;
        PickerLoaiXe.SelectedItem = LoaiXe.Contains(t.LoaiXe) ? t.LoaiXe : LoaiXe[0];
        HienAnh();
    }

    private void HienAnh()
    {
        var t = MockData.TaiXeHienTai;
        ImgAnh.Source = t.CoAnh ? ImageSource.FromFile(t.AnhPath) : null;
        ImgAnh.IsVisible = t.CoAnh;
        LblAnh.Text = t.Anh;
        LblAnh.IsVisible = !t.CoAnh;
        LblTenDau.Text = string.IsNullOrWhiteSpace(t.HoTen) ? "Tài xế" : t.HoTen;
        BtnXoaAnh.IsVisible = t.CoAnh;
    }

    private async void OnChonAnhClicked(object sender, EventArgs e) => await DatAnhAsync(chup: false);
    private async void OnChupAnhClicked(object sender, EventArgs e) => await DatAnhAsync(chup: true);

    private async Task DatAnhAsync(bool chup)
    {
        LblLoi.IsVisible = false;
        try
        {
            var t = MockData.TaiXeHienTai;
            var path = await AnhService.ChonVaLuuAsync("taixe_" + t.Id, "Chọn ảnh đại diện", chup);
            if (path is null) return;                                          // người dùng hủy

            if (t.CoAnh) { try { File.Delete(t.AnhPath); } catch { } }         // xóa ảnh cũ cho đỡ tốn bộ nhớ
            t.AnhPath = path;
            TaiXeStore.Luu();
            HienAnh();
            LblThanhCong.Text = "✓ Đã đổi ảnh đại diện";
            LblThanhCong.IsVisible = true;
        }
        catch (PermissionException)
        {
            HienLoi("Ứng dụng chưa được cấp quyền camera/ảnh. Hãy cấp quyền trong Cài đặt rồi thử lại.");
        }
        catch (Exception ex)
        {
            HienLoi("Không đặt được ảnh: " + ex.Message);
        }
    }

    private async void OnXoaAnhClicked(object sender, EventArgs e)
    {
        var t = MockData.TaiXeHienTai;
        if (!t.CoAnh) return;
        if (!await DisplayAlert("Xóa ảnh", "Xóa ảnh đại diện hiện tại?", "Xóa", "Hủy")) return;
        try { File.Delete(t.AnhPath); } catch { }
        t.AnhPath = "";
        TaiXeStore.Luu();
        HienAnh();
        LblThanhCong.Text = "✓ Đã xóa ảnh";
        LblThanhCong.IsVisible = true;
    }

    private async void OnLuuClicked(object sender, EventArgs e)
    {
        LblLoi.IsVisible = false;
        LblThanhCong.IsVisible = false;

        var ten = EntryHoTen.Text?.Trim() ?? "";
        var sdt = EntrySdt.Text?.Trim() ?? "";
        if (ten.Length < 2) { HienLoi("Vui lòng nhập họ tên"); return; }
        var soChu = sdt.Count(char.IsDigit);
        if (sdt.Length > 0 && (soChu < 9 || soChu > 11)) { HienLoi("Số điện thoại phải có 9–11 chữ số"); return; }

        var trung = TaiXeStore.TimTheoSdt(sdt);
        if (trung is not null && !ReferenceEquals(trung, MockData.TaiXeHienTai)) { HienLoi("Số điện thoại này đã thuộc về tài xế khác"); return; }

        var t = MockData.TaiXeHienTai;
        t.HoTen = ten;
        t.SoDienThoai = sdt;
        t.BienSoXe = EntryBienSo.Text?.Trim().ToUpperInvariant() ?? "";
        t.LoaiXe = PickerLoaiXe.SelectedItem as string ?? LoaiXe[0];
        t.Anh = string.IsNullOrWhiteSpace(EntryEmoji.Text) ? "🛵" : EntryEmoji.Text.Trim();
        TaiXeStore.Luu();
        await DuLieuService.GhiAsync("PUT", "api/mobile/tai-xe/ho-so",
            new { hoTen = t.HoTen, soDienThoai = t.SoDienThoai, bienSoXe = t.BienSoXe, loaiXe = t.LoaiXe });

        HienAnh();
        LblThanhCong.Text = "✓ Đã lưu hồ sơ";
        LblThanhCong.IsVisible = true;
    }

    private async void OnDoiMatKhauClicked(object sender, EventArgs e)
        => await Shell.Current.GoToAsync($"{nameof(DoiMatKhauPage)}?VaiTro=TaiXe");

    private void HienLoi(string t) { LblLoi.Text = t; LblLoi.IsVisible = true; }
}
