using FoodApp.Maui.Models;
using FoodApp.Maui.Services;

namespace FoodApp.Maui.Views;

public partial class DriverHomePage : ContentPage
{
    public DriverHomePage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        TaiLai();
        _ = DongBoOnlineAsync();
    }

    /// <summary>Online: kéo đơn chờ nhận từ API rồi vẽ lại danh sách. Offline: không làm gì.</summary>
    private async Task DongBoOnlineAsync()
    {
        await DuLieuService.TaiDonChoTaiXeAsync();
        NapDanhSachDonChoNhan();
    }

    private void TaiLai()
    {
        ThuNhapService.CapNhatChiSo();      // thu nhập hôm nay + điểm đánh giá tính lại từ lịch sử
        BtnThongBao.Text = $"🔔 Thông báo ({DriverNotificationsPage.Dem()})";
        NapHoSo();
        CapNhatDonDangGiao();
        NapDanhSachDonChoNhan();
        NapLichSu();
    }

    private void NapHoSo()
    {
        var taiXe = MockData.TaiXeHienTai;

        // Ảnh đại diện: ưu tiên ảnh người dùng chọn, không có thì dùng biểu tượng
        ImgAnhTaiXe.Source = taiXe.CoAnh ? ImageSource.FromFile(taiXe.AnhPath) : null;
        ImgAnhTaiXe.IsVisible = taiXe.CoAnh;
        LblAnhTaiXe.Text = taiXe.Anh;
        LblAnhTaiXe.IsVisible = !taiXe.CoAnh;

        LblTenTaiXe.Text = string.IsNullOrWhiteSpace(taiXe.HoTen) ? "Tài xế" : taiXe.HoTen;
        LblBienSo.Text = string.IsNullOrWhiteSpace(taiXe.BienSoXe)
            ? "Chưa có biển số — bấm Hồ sơ để cập nhật"
            : $"{taiXe.LoaiXe} · {taiXe.BienSoXe}";
        LblThuNhapHomNay.Text = $"{taiXe.TongThuNhapHomNay:N0}đ";
        LblSoDonHomNay.Text = taiXe.SoDonHomNay.ToString();
        LblDanhGia.Text = taiXe.DanhGia > 0 ? taiXe.DanhGia.ToString("0.0") : "Mới";

        SwitchOnline.Toggled -= OnSwitchOnlineToggled;
        SwitchHoatDong.Toggled -= OnSwitchHoatDongToggled;
        SwitchOnline.IsToggled = taiXe.DangOnline;
        SwitchHoatDong.IsToggled = taiXe.DangHoatDong;
        SwitchHoatDong.IsEnabled = taiXe.DangOnline;      // offline thì không có "sẵn sàng"
        SwitchOnline.Toggled += OnSwitchOnlineToggled;
        SwitchHoatDong.Toggled += OnSwitchHoatDongToggled;
        LblTrangThaiHoatDong.Text = taiXe.TrangThaiLamViec;
    }

    /// <summary>Đồng bộ trạng thái lên Web: SanSang = online và sẵn sàng; NgoaiTuyen = còn lại (Web chỉ có 2 giá trị này).</summary>
    private async Task GuiTrangThaiAsync()
    {
        var t = MockData.TaiXeHienTai;
        TaiXeStore.Luu();
        LblTrangThaiHoatDong.Text = t.TrangThaiLamViec;
        SwitchHoatDong.IsEnabled = t.DangOnline;
        NapDanhSachDonChoNhan();
        if (DuLieuService.DangOnline)
            await DuLieuService.GhiAsync("PUT", "api/mobile/tai-xe/trang-thai", new { trangThai = t.NhanDuocDon ? "SanSang" : "NgoaiTuyen" });
    }

    private async void OnSwitchOnlineToggled(object? sender, ToggledEventArgs e)
    {
        var t = MockData.TaiXeHienTai;
        if (!e.Value && MockData.DonHangDangGiaoCuaTaiXe is not null)
        {
            await DisplayAlert("Đang giao đơn", "Bạn đang có đơn chưa hoàn tất. Hãy giao xong rồi mới chuyển sang Offline.", "Đóng");
            SwitchOnline.Toggled -= OnSwitchOnlineToggled; SwitchOnline.IsToggled = true; SwitchOnline.Toggled += OnSwitchOnlineToggled;
            return;
        }
        t.DangOnline = e.Value;
        NhatKyService.Ghi("TaiXe", t.SoDienThoai, e.Value ? "Chuyển Online" : "Chuyển Offline");
        await GuiTrangThaiAsync();
    }

    private async void OnSwitchHoatDongToggled(object? sender, ToggledEventArgs e)
    {
        MockData.TaiXeHienTai.DangHoatDong = e.Value;
        NhatKyService.Ghi("TaiXe", MockData.TaiXeHienTai.SoDienThoai, e.Value ? "Sẵn sàng nhận đơn" : "Tạm không nhận đơn");
        await GuiTrangThaiAsync();
    }

    private void CapNhatDonDangGiao()
    {
        var don = MockData.DonHangDangGiaoCuaTaiXe;
        if (don is null)
        {
            KhungDonDangGiao.IsVisible = false;
            return;
        }

        KhungDonDangGiao.IsVisible = true;
        LblMaDonDangGiao.Text = $"#{don.Id}";
        LblGianHangDangGiao.Text = don.TenGianHang;
        LblDiaChiGiaoDangGiao.Text = don.TrangThai == TrangThaiDonHang.DangGiao
            ? $"🏁 Đang giao đến: {don.DiaChiGiao}"
            : $"🍳 Đến lấy hàng tại: {don.DiaChiLayHang}";
    }

    private void NapDanhSachDonChoNhan()
    {
        // TODO: thay bằng GET /api/tai-xe/don-cho-nhan khi có API thật.
        var dangHoatDong = MockData.TaiXeHienTai.NhanDuocDon;   // phải online VÀ sẵn sàng
        var daNhanDon = MockData.DonHangDangGiaoCuaTaiXe is not null;

        var ds = (dangHoatDong && !daNhanDon)
            ? MockData.DonHangChoTaiXe
                // đơn hẹn giờ chỉ hiện cho tài xế trong vòng 30 phút trước giờ giao
                .Where(d => d.TaiXeId is null && !_daTuChoi.Contains(d.Id) && !KhachDaHuy(d) && d.SanSangGiao && d.TrangThai == TrangThaiDonHang.DangLam && (!d.LaDonHenGio || d.HenGiaoLuc is null || d.HenGiaoLuc <= DateTime.Now.AddMinutes(30)))
                .ToList()
            : new List<DonHang>();
        DsDonChoNhan.ItemsSource = ds;

        ChipSoDon.IsVisible = ds.Count > 0;
        LblSoDonCho.Text = $"{ds.Count} đơn";

        // trạng thái rỗng, có lời nhắn rõ ràng cho từng trường hợp
        string? thongDiep = null; string icon = "😴";
        if (!dangHoatDong) { thongDiep = "Bật \"Nhận đơn giao hàng\" để xem đơn gần bạn."; icon = "😴"; }
        else if (daNhanDon) { thongDiep = "Hoàn thành đơn đang giao để nhận đơn tiếp theo."; icon = "🛵"; }
        else if (ds.Count == 0) { thongDiep = "Chưa có đơn nào. Kéo xuống để làm mới."; icon = "🔍"; }

        KhungTrong.IsVisible = thongDiep is not null;
        LblIconTrong.Text = icon;
        LblTatCa.Text = thongDiep ?? "";
    }

    private void NapLichSu()
    {
        var gan = MockData.DonHangDaGiaoCuaTaiXe.Reverse().Take(5).ToList();
        DsDaGiao.ItemsSource = gan;
        KhungLichSu.IsVisible = gan.Count > 0;
    }

    // ----- Từ chối đơn / lối tắt sang các trang mới -----
    private static readonly HashSet<string> _daTuChoi = new();

    private static bool KhachDaHuy(DonHang d) =>
        MockData.LichSuDonHang.Any(x => x.Id == d.Id && x.TrangThai == TrangThaiDonHang.DaHuy);

    private async void OnTuChoiClicked(object sender, EventArgs e)
    {
        if (sender is not Button { CommandParameter: DonHang don }) return;
        var ly = await DisplayActionSheet($"Từ chối đơn #{don.Id}?", "Hủy", null,
            "Quá xa", "Đang bận / sắp nghỉ", "Không phù hợp phương tiện", "Lý do khác");
        if (ly is null or "Hủy") return;
        _daTuChoi.Add(don.Id);                 // đơn vẫn ở hàng đợi cho tài xế khác, chỉ ẩn với bạn
        NapDanhSachDonChoNhan();
    }

    private async void OnThuNhapClicked(object sender, EventArgs e) => await Shell.Current.GoToAsync(nameof(DriverEarningsPage));
    private async void OnDanhGiaClicked(object sender, EventArgs e) => await Shell.Current.GoToAsync(nameof(DriverRatingPage));
    private async void OnThongBaoClicked(object sender, EventArgs e) => await Shell.Current.GoToAsync(nameof(DriverNotificationsPage));

    private async void OnHoSoTapped(object sender, EventArgs e)
        => await Shell.Current.GoToAsync(nameof(DriverProfilePage));

    private async void OnNhanDonClicked(object sender, EventArgs e)
    {
        if (sender is not Button { CommandParameter: DonHang don }) return;

        if (MockData.DonHangDangGiaoCuaTaiXe is not null)
        {
            await DisplayAlert("Không thể nhận đơn", "Bạn cần hoàn thành đơn đang giao trước.", "Đóng");
            return;
        }

        bool xacNhan = await DisplayAlert(
            "Nhận đơn giao hàng",
            $"Nhận đơn #{don.Id} từ {don.TenGianHang}?\nPhí giao: {don.PhiGiaoHang:N0}đ\nThu khách ({don.HinhThucThanhToan}): {don.TongThanhToan:N0}đ",
            "Nhận đơn", "Để sau");
        if (!xacNhan) return;

        var taiXe = MockData.TaiXeHienTai;
        don.TaiXeId = taiXe.Id;
        don.TaiXeTen = taiXe.HoTen;          // khách thấy tên + gọi/nhắn được cho tài xế
        don.TaiXeSdt = taiXe.SoDienThoai;
        don.TaiXeBienSo = taiXe.BienSoXe;
        MockData.DonHangDangGiaoCuaTaiXe = don;
        DonHangService.DongBo(don);
        await DuLieuService.GhiAsync("PUT", $"api/mobile/don-hang/{don.Id}/tai-xe-nhan", new { taiXeId = taiXe.Id });

        CapNhatDonDangGiao();
        NapDanhSachDonChoNhan();

        await Shell.Current.GoToAsync(nameof(DriverDeliveryDetailPage));
    }

    private async void OnNhanTinKhachClicked(object sender, EventArgs e)
    {
        var don = MockData.DonHangDangGiaoCuaTaiXe;
        if (don is null) return;
        await ChatPage.MoAsync(don.Id, "TaiXe", string.IsNullOrWhiteSpace(don.KhachTen) ? "Khách hàng" : don.KhachTen, don.KhachSdt);
    }

    private async void OnXemDonDangGiaoClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(DriverDeliveryDetailPage));
    }

    private async void OnRefreshing(object? sender, EventArgs e)
    {
        TaiLai();
        await DongBoOnlineAsync();
        RefreshDanhSach.IsRefreshing = false;
    }

    private async void OnDangXuatClicked(object sender, EventArgs e)
    {
        bool xacNhan = await DisplayAlert("Đăng xuất", "Bạn có chắc muốn đăng xuất?", "Đăng xuất", "Hủy");
        if (!xacNhan) return;

        ViTriTaiXeService.Dung();
        var tx = MockData.TaiXeHienTai;
        tx.DangOnline = false;
        TaiXeStore.Luu();
        NhatKyService.Ghi("TaiXe", tx.SoDienThoai, "Đăng xuất", chiTiet: NhatKyService.ThietBi());
        if (DuLieuService.DangOnline)      // báo Offline cho Web khi còn token
            await ApiClient.PutAsync<object>("api/mobile/tai-xe/trang-thai", new { trangThai = "NgoaiTuyen" });
        ApiClient.Token = null;
        await Shell.Current.GoToAsync("//dang-nhap");
    }
}
