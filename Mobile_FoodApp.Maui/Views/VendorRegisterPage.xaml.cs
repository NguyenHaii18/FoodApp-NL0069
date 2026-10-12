using FoodApp.Maui.Models;
using FoodApp.Maui.Services;

namespace FoodApp.Maui.Views;

public partial class VendorRegisterPage : ContentPage
{
    private string _anhGiayPhep = "";

    public VendorRegisterPage()
    {
        InitializeComponent();
    }

    private async void OnTaiGiayPhepClicked(object sender, EventArgs e)
    {
        try
        {
            var path = await AnhService.ChonVaLuuAsync("gp_" + DateTime.Now.ToString("yyMMddHHmmss"), "Chọn ảnh giấy phép kinh doanh");
            if (path is null) return;
            if (!string.IsNullOrEmpty(_anhGiayPhep)) { try { File.Delete(_anhGiayPhep); } catch { } }
            _anhGiayPhep = path;
            LblGiayPhep.Text = "✓ Đã chọn ảnh: " + Path.GetFileName(path);
        }
        catch (Exception ex) { HienLoi("Không tải được ảnh: " + ex.Message); }
    }

    private async void OnGuiDangKyClicked(object sender, EventArgs e)
    {
        LblLoi.IsVisible = false;

        if (string.IsNullOrWhiteSpace(EntryTenGianHang.Text))
        {
            HienLoi("Vui lòng nhập tên gian hàng");
            return;
        }
        if (string.IsNullOrWhiteSpace(EntryDiaChi.Text))
        {
            HienLoi("Vui lòng nhập địa chỉ cửa hàng");
            return;
        }
        if (string.IsNullOrWhiteSpace(EntrySdt.Text))
        {
            HienLoi("Vui lòng nhập số điện thoại");
            return;
        }
        if (string.IsNullOrWhiteSpace(EntryEmail.Text) || !EntryEmail.Text.Contains('@'))
        {
            HienLoi("Email không hợp lệ");
            return;
        }
        if (MockData.DanhSachGianHang.Any(g => string.Equals(g.Email, EntryEmail.Text.Trim(), StringComparison.OrdinalIgnoreCase)))
        {
            HienLoi("Email này đã được đăng ký cho một gian hàng khác");
            return;
        }
        if (string.IsNullOrWhiteSpace(EntryMatKhau.Text) || EntryMatKhau.Text.Length < 6)
        {
            HienLoi("Mật khẩu tối thiểu 6 ký tự");
            return;
        }

        var gpkd = EntryGpkd.Text?.Trim() ?? "";
        var mst = new string((EntryMst.Text ?? "").Where(char.IsDigit).ToArray());
        var cccd = new string((EntryCccd.Text ?? "").Where(char.IsDigit).ToArray());
        if (gpkd.Length < 6) { HienLoi("Vui lòng nhập số giấy phép kinh doanh (ít nhất 6 ký tự)"); return; }
        if (mst.Length > 0 && mst.Length != 10 && mst.Length != 13) { HienLoi("Mã số thuế phải có 10 hoặc 13 chữ số"); return; }
        if (cccd.Length != 9 && cccd.Length != 12) { HienLoi("CCCD/CMND chủ quán phải có 9 hoặc 12 chữ số"); return; }

        LoadingIndicator.IsVisible = true;
        LoadingIndicator.IsRunning = true;
        BtnGuiDangKy.IsEnabled = false;

        try
        {
            // TODO: gọi ASP.NET Core Web API thật:
            //   POST /api/gian-hang/dang-ky { tenGianHang, diaChi, sdt, email, matKhau, moTa }
            // Hồ sơ mặc định ở trạng thái ChoDuyet, quản trị viên duyệt trong module Quản lý quản trị.
            if (DuLieuService.DangOnline)
            {
                var kq = await ApiClient.PostAsync<object>("api/mobile/gian-hang/dang-ky", new
                {
                    tenGianHang = EntryTenGianHang.Text.Trim(), diaChi = EntryDiaChi.Text.Trim(), sdt = EntrySdt.Text.Trim(),
                    email = EntryEmail.Text.Trim(), matKhau = EntryMatKhau.Text, moTa = EditorMoTa.Text?.Trim() ?? "", hinhAnh = "🍽️",
                });
                if (!kq.ThanhCong) { HienLoi(kq.ThongDiep); return; }
                // API đăng ký chưa nhận giấy tờ; chủ quán bổ sung ở Hồ sơ cửa hàng sau khi được duyệt (PUT gian-hang/giay-to).
                NhatKyService.Ghi("GianHang", EntryEmail.Text.Trim(), "Gửi hồ sơ đăng ký gian hàng", EntryTenGianHang.Text.Trim());
                await DisplayAlert("Đăng ký thành công", "Hồ sơ đã gửi lên hệ thống. Sau khi được duyệt, hãy đăng nhập và bổ sung giấy tờ trong Hồ sơ cửa hàng.", "Đã hiểu");
                await Shell.Current.GoToAsync("..");
                return;
            }

            await Task.Delay(600);

            var gianHangMoi = new GianHang
            {
                Id = Services.IdHelper.TiepTheo("gh", MockData.DanhSachGianHang.Select(x => x.Id)),
                TenGianHang = EntryTenGianHang.Text.Trim(),
                DiaChi = EntryDiaChi.Text.Trim(),
                SoDienThoai = EntrySdt.Text.Trim(),
                Email = EntryEmail.Text.Trim(),
                MatKhau = EntryMatKhau.Text,
                MoTa = EditorMoTa.Text?.Trim() ?? "",
                HinhAnh = "🍽️",
                DanhGia = 0,
                ThoiGianGiaoPhut = 30,
                DangMoCua = false,
                TrangThaiDangKy = TrangThaiDangKyGianHang.ChoDuyet,
                NgayDangKy = DateTime.Now,
                SoGiayPhepKinhDoanh = gpkd, MaSoThue = mst, CccdChuQuan = cccd, AnhGiayPhepPath = _anhGiayPhep,
                SoGiayChungNhanAttp = EntryAttp.Text?.Trim() ?? "",
            };
            NhatKyService.Ghi("GianHang", gianHangMoi.Email, "Gửi hồ sơ đăng ký gian hàng", gianHangMoi.TenGianHang);
            MockData.DanhSachGianHang.Add(gianHangMoi);
            VendorLocalStore.Luu();   // giữ hồ sơ chờ duyệt sau khi tắt app (chế độ demo)

            await DisplayAlert(
                "Đăng ký thành công",
                "Hồ sơ gian hàng đã được gửi. Quản trị viên sẽ duyệt trong thời gian sớm nhất, bạn sẽ nhận thông báo qua email khi được kích hoạt.",
                "Đã hiểu");

            await Shell.Current.GoToAsync("..");
        }
        finally
        {
            LoadingIndicator.IsVisible = false;
            LoadingIndicator.IsRunning = false;
            BtnGuiDangKy.IsEnabled = true;
        }
    }

    private void HienLoi(string thongDiep)
    {
        LblLoi.Text = thongDiep;
        LblLoi.IsVisible = true;
    }
}
