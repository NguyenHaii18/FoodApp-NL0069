using FoodApp.Maui.Services;

namespace FoodApp.Maui.Views;

public partial class VendorProfilePage : ContentPage
{
    public VendorProfilePage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        NapHoSo();
    }

    private void NapHoSo()
    {
        var gh = Models.MockData.GianHangHienTai;

        LblAnhHienTai.Text = gh.HinhAnh;
        HienAnh(gh);
        EntryHinhAnh.Text = gh.HinhAnh;
        EntryTenGianHang.Text = gh.TenGianHang;
        EntryDiaChi.Text = gh.DiaChi;
        EntrySdt.Text = gh.SoDienThoai;
        EntryEmail.Text = gh.Email;
        EntryThoiGianGiao.Text = gh.ThoiGianGiaoPhut.ToString();
        EditorMoTa.Text = gh.MoTa;
        SwitchDangMoCua.IsToggled = gh.DangMoCua;
        SwitchDangNhanDon.IsToggled = gh.DangNhanDon;
        TimeMoCua.Time = TimeSpan.TryParse(gh.GioMoCua, out var mo) ? mo : TimeSpan.Zero;
        TimeDongCua.Time = TimeSpan.TryParse(gh.GioDongCua, out var dong) ? dong : new TimeSpan(23, 59, 0);

        EntryGpkd.Text = gh.SoGiayPhepKinhDoanh;
        EntryMst.Text = gh.MaSoThue;
        EntryCccd.Text = gh.CccdChuQuan;
        EntryAttp.Text = gh.SoGiayChungNhanAttp;
        HienGiayPhep(gh);

        LblThanhCong.IsVisible = false;
        LblLoi.IsVisible = false;
    }

    private void HienGiayPhep(Models.GianHang gh)
    {
        ImgGiayPhep.Source = gh.CoAnhGiayPhep ? ImageSource.FromFile(gh.AnhGiayPhepPath) : null;
        LblChuaCoGiayPhep.IsVisible = !gh.CoAnhGiayPhep;
    }

    private async void OnTaiGiayPhepClicked(object sender, EventArgs e)
    {
        try
        {
            var gh = Models.MockData.GianHangHienTai;
            var path = await AnhService.ChonVaLuuAsync("gp_" + gh.Id, "Chọn ảnh giấy phép kinh doanh");
            if (path is null) return;
            if (gh.CoAnhGiayPhep) { try { File.Delete(gh.AnhGiayPhepPath); } catch { } }
            gh.AnhGiayPhepPath = path;
            VendorLocalStore.Luu();
            NhatKyService.Ghi("GianHang", gh.Email, "Cập nhật ảnh giấy phép", gh.TenGianHang);
            HienGiayPhep(gh);
            LblThanhCong.IsVisible = true;
        }
        catch (Exception ex) { HienLoi("Không tải được ảnh: " + ex.Message); }
    }

    private async void OnLuuClicked(object sender, EventArgs e)
    {
        LblLoi.IsVisible = false;
        LblThanhCong.IsVisible = false;

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
        if (!int.TryParse(EntryThoiGianGiao.Text, out var thoiGianGiao) || thoiGianGiao <= 0)
        {
            HienLoi("Thời gian giao hàng không hợp lệ");
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
        BtnLuu.IsEnabled = false;

        try
        {
            var gh = Models.MockData.GianHangHienTai;
            gh.HinhAnh = string.IsNullOrWhiteSpace(EntryHinhAnh.Text) ? gh.HinhAnh : EntryHinhAnh.Text.Trim();
            gh.TenGianHang = EntryTenGianHang.Text.Trim();
            gh.DiaChi = EntryDiaChi.Text.Trim();
            gh.SoDienThoai = EntrySdt.Text.Trim();
            gh.ThoiGianGiaoPhut = thoiGianGiao;
            gh.MoTa = EditorMoTa.Text?.Trim() ?? "";
            gh.DangMoCua = SwitchDangMoCua.IsToggled;
            gh.DangNhanDon = SwitchDangNhanDon.IsToggled;
            gh.GioMoCua = TimeMoCua.Time.ToString(@"hh\:mm");
            gh.GioDongCua = TimeDongCua.Time.ToString(@"hh\:mm");
            var attp = EntryAttp.Text?.Trim() ?? "";
            bool doiGiayTo = gh.SoGiayPhepKinhDoanh != gpkd || gh.MaSoThue != mst || gh.CccdChuQuan != cccd || gh.SoGiayChungNhanAttp != attp;
            gh.SoGiayPhepKinhDoanh = gpkd; gh.MaSoThue = mst; gh.CccdChuQuan = cccd; gh.SoGiayChungNhanAttp = attp;
            if (doiGiayTo)
            {
                NhatKyService.Ghi("GianHang", gh.Email, "Cập nhật giấy tờ pháp lý", gh.TenGianHang);
                // Web lưu GPKD + ATTP (CCCD / mã số thuế / ảnh giấy phép hiện chỉ lưu trên máy)
                await DuLieuService.GhiAsync("PUT", "api/mobile/gian-hang/giay-to", new { giayPhepKinhDoanh = gpkd, giayChungNhanAttp = attp });
            }
            VendorLocalStore.Luu();
            await DuLieuService.GhiAsync("PUT", $"api/mobile/gian-hang/{gh.Id}/gio-hoat-dong",
                new { gioMoCua = gh.GioMoCua, gioDongCua = gh.GioDongCua, dangNhanDon = gh.DangNhanDon, dangMoCua = gh.DangMoCua });

            LblAnhHienTai.Text = gh.HinhAnh;
            LblThanhCong.IsVisible = true;
        }
        finally
        {
            LoadingIndicator.IsVisible = false;
            LoadingIndicator.IsRunning = false;
            BtnLuu.IsEnabled = true;
        }
    }

    private void HienAnh(Models.GianHang gh)
    {
        ImgAnhCuaHang.Source = gh.CoAnh ? ImageSource.FromFile(gh.AnhPath) : null;
        LblChuaCoAnh.IsVisible = !gh.CoAnh;
    }

    private async void OnTaiAnhClicked(object sender, EventArgs e)
    {
        try
        {
            var gh = Models.MockData.GianHangHienTai;
            var path = await AnhService.ChonVaLuuAsync(gh.Id);
            if (path is null) return;

            if (gh.CoAnh) { try { File.Delete(gh.AnhPath); } catch { } }   // xóa ảnh cũ
            gh.AnhPath = path;
            VendorLocalStore.Luu();
            HienAnh(gh);
            LblThanhCong.IsVisible = true;
        }
        catch (Exception ex)
        {
            HienLoi("Không tải được ảnh: " + ex.Message);
        }
    }

    private void HienLoi(string thongDiep)
    {
        LblLoi.Text = thongDiep;
        LblLoi.IsVisible = true;
    }

    private async void OnDoiMatKhauClicked(object sender, EventArgs e)
        => await Shell.Current.GoToAsync($"{nameof(DoiMatKhauPage)}?VaiTro=GianHang");
}
