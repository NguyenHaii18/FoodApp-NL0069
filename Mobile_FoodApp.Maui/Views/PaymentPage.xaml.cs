using FoodApp.Maui.Models;
using FoodApp.Maui.Services;
using System.Collections.ObjectModel;

namespace FoodApp.Maui.Views;

public enum HinhThucThanhToan { TienMat, ChuyenKhoan, ViDienTu }

[QueryProperty(nameof(GioHang), "GioHang")]
[QueryProperty(nameof(TenGianHang), "TenGianHang")]
[QueryProperty(nameof(GianHangId), "GianHangId")]
public partial class PaymentPage : ContentPage
{
    public ObservableCollection<MucGioHang> GioHang { get; set; } = new();
    public string TenGianHang { get; set; } = "";
    public string GianHangId { get; set; } = "";

    private HinhThucThanhToan _hinhThucChon =
        Enum.TryParse<HinhThucThanhToan>(Preferences.Default.Get("tt-mac-dinh", "TienMat"), out var ttmd) ? ttmd : HinhThucThanhToan.TienMat;
    private decimal _giamGia = 0;                 // giảm trên tiền món
    private decimal _giamShip = 0;                // giảm trên phí giao hàng
    private KhuyenMai? _kmDangDung;               // voucher đang áp dụng
    // Phí giao hàng tính theo khoảng cách quán → địa chỉ khách và khu vực (xem GiaoHangService)
    private KetQuaTuyen _tuyen = GiaoHangService.UocTinh(null, null);
    private decimal PhiGiaoHang => _tuyen.Phi;

    private void OnDiaChiChanged(object? sender, TextChangedEventArgs e) => TinhLaiPhi();

    private void TinhLaiPhi()
    {
        var diaChiQuan = MockData.DanhSachGianHang.FirstOrDefault(g => g.Id == GianHangId)?.DiaChi;
        _tuyen = GiaoHangService.UocTinh(diaChiQuan, EntryDiaChi.Text);
        CapNhatTongTien();
    }

    /// <summary>"Giao ngay" + 6 khung 30 phút, bắt đầu khoảng 1 tiếng sau.</summary>
    private readonly List<DateTime> _gioBatDau = new();

    private void NapKhungGio()
    {
        var ds = new List<string> { "Giao ngay" };
        var bd = DateTime.Now.AddHours(1);
        bd = new DateTime(bd.Year, bd.Month, bd.Day, bd.Hour, bd.Minute < 30 ? 30 : 0, 0).AddHours(bd.Minute < 30 ? 0 : 1);
        for (int i = 0; i < 6; i++)
        {
            var a = bd.AddMinutes(30 * i);
            _gioBatDau.Add(a);
            ds.Add($"{a:HH:mm} – {a.AddMinutes(30):HH:mm}");
        }
        PickerGioGiao.ItemsSource = ds;
        PickerGioGiao.SelectedIndex = 0;
    }

    private readonly (HinhThucThanhToan Loai, string Nhan, string Icon)[] _dsHinhThuc =
    {
        (HinhThucThanhToan.TienMat, "Tiền mặt khi nhận hàng", "💵"),
        (HinhThucThanhToan.ChuyenKhoan, "Chuyển khoản ngân hàng", "🏦"),
        (HinhThucThanhToan.ViDienTu, "Ví điện tử (Momo/ZaloPay)", "👛"),
    };

    public PaymentPage()
    {
        InitializeComponent();
        EntryDiaChi.Text = Phien.Khach?.DiaChi ?? "";
        NapKhungGio();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        DsChiTiet.ItemsSource = GioHang;
        VeDanhSachHinhThuc();
        TinhLaiPhi();
        CapNhatTongTien();
    }

    private void VeDanhSachHinhThuc()
    {
        DanhSachHinhThuc.Children.Clear();
        foreach (var ht in _dsHinhThuc)
        {
            var chon = ht.Loai == _hinhThucChon;
            var row = new Grid { Padding = new Thickness(12, 10) };
            row.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            row.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            row.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            row.Add(new Label { Text = ht.Icon, FontSize = 18, VerticalOptions = LayoutOptions.Center }, 0, 0);
            row.Add(new Label
            {
                Text = ht.Nhan,
                FontSize = 13.5,
                FontAttributes = chon ? FontAttributes.Bold : FontAttributes.None,
                VerticalOptions = LayoutOptions.Center
            }, 1, 0);
            row.Add(new Label
            {
                Text = chon ? "●" : "○",
                TextColor = chon ? (Color)Application.Current!.Resources["Primary"] : (Color)Application.Current!.Resources["BorderColor"],
                VerticalOptions = LayoutOptions.Center
            }, 2, 0);

            var tap = new TapGestureRecognizer();
            tap.Tapped += (s, e) => { _hinhThucChon = ht.Loai; VeDanhSachHinhThuc(); };
            row.GestureRecognizers.Add(tap);

            DanhSachHinhThuc.Children.Add(row);
        }
    }

    private decimal TamTinh => GioHang.Sum(x => x.ThanhTien);
    private decimal TongCong => Math.Max(0, TamTinh + PhiGiaoHang - _giamGia - _giamShip);

    private void CapNhatTongTien()
    {
        LblTamTinh.Text = $"{TamTinh:N0}đ";
        LblTongCong.Text = $"{TongCong:N0}đ";
        RowGiamGia.IsVisible = _giamGia + _giamShip > 0;
        if (_giamGia + _giamShip > 0) LblGiamGia.Text = $"-{_giamGia + _giamShip:N0}đ";
        var chiTietTuyen = _tuyen.DaXacDinh ? $" · ≈{_tuyen.Km:0.#} km · ~{_tuyen.Phut} phút" : " (chưa rõ khu vực)";
        LblPhiGiao.Text = _giamShip > 0 ? $"{PhiGiaoHang - _giamShip:N0}đ (đã giảm {_giamShip:N0}đ){chiTietTuyen}" : $"{PhiGiaoHang:N0}đ{chiTietTuyen}";
        BtnXacNhanDatHang.Text = $"Xác nhận đặt hàng • {TongCong:N0}đ";
    }

    private void OnApDungKhuyenMaiClicked(object sender, EventArgs e)
    {
        var maCode = EntryMaKhuyenMai.Text?.Trim();
        if (string.IsNullOrEmpty(maCode)) return;

        var km = MockData.DanhSachKhuyenMai.FirstOrDefault(k => string.Equals(k.MaCode, maCode, StringComparison.OrdinalIgnoreCase));
        if (km is null)
        {
            DatVoucher(null);
            HienKetQuaVoucher(false, "Mã khuyến mãi không tồn tại");
            return;
        }
        DatVoucher(km);
    }

    private void HienKetQuaVoucher(bool ok, string nd)
    {
        LblKetQuaKhuyenMai.Text = (ok ? "✔ " : "✘ ") + nd;
        LblKetQuaKhuyenMai.TextColor = (Color)Application.Current!.Resources[ok ? "Success" : "Danger"];
        LblKetQuaKhuyenMai.IsVisible = true;
    }

    /// <summary>Kiểm tra và áp dụng voucher vào đơn hiện tại (km = null: gỡ voucher).</summary>
    private void DatVoucher(KhuyenMai? km)
    {
        _kmDangDung = null; _giamGia = 0; _giamShip = 0;
        if (km is not null)
        {
            var kq = KhuyenMaiService.KiemTra(km, GianHangId, GioHang, PhiGiaoHang, Phien.Khach?.Email);
            HienKetQuaVoucher(kq.HopLe, kq.HopLe ? $"Áp dụng {km.MaCode}: {kq.ThongDiep}" : kq.ThongDiep);
            if (kq.HopLe) { _kmDangDung = km; _giamGia = kq.GiamDon; _giamShip = kq.GiamPhiShip; EntryMaKhuyenMai.Text = km.MaCode; }
        }
        else LblKetQuaKhuyenMai.IsVisible = false;
        CapNhatTongTien();
    }

    private async void OnChonUuDaiClicked(object sender, EventArgs e)
    {
        var ds = KhuyenMaiService.DanhSachUuDai(GianHangId, GioHang, PhiGiaoHang, Phien.Khach?.Email);
        if (ds.Count == 0) { await DisplayAlert("Ưu đãi", "Hiện chưa có ưu đãi nào cho gian hàng này.", "OK"); return; }

        var nhan = ds.Select(x => (x.Kq.HopLe ? "✔ " : "✘ ") + $"{x.Km.MaCode} · {x.Km.GiaTriGiamHienThi}" +
                                  (x.Kq.HopLe ? $" (-{x.Kq.TongGiam:N0}đ)" : $" — {x.Kq.ThongDiep}")).ToArray();
        var chon = await DisplayActionSheet("Chọn ưu đãi", "Đóng", _kmDangDung is null ? null : "Bỏ mã đang dùng", nhan);
        if (chon is null or "Đóng") return;
        if (chon == "Bỏ mã đang dùng") { EntryMaKhuyenMai.Text = ""; DatVoucher(null); return; }

        var i = Array.IndexOf(nhan, chon);
        if (i >= 0) DatVoucher(ds[i].Km);       // không dùng được thì hiện lý do ngay dưới ô nhập mã
    }

    private async void OnChonSoDiaChiTapped(object? sender, TappedEventArgs e)
    {
        var ds = TaiKhoanService.LayDiaChi();
        if (ds.Count == 0) { await DisplayAlert("Sổ địa chỉ trống", "Thêm địa chỉ trong Tài khoản → Sổ địa chỉ.", "OK"); return; }
        var chon = await DisplayActionSheet("Chọn địa chỉ", "Đóng", null, ds.Select(d => $"{d.Nhan}: {d.DiaChi}").ToArray());
        if (chon is null or "Đóng") return;
        EntryDiaChi.Text = chon[(chon.IndexOf(": ", StringComparison.Ordinal) + 2)..];
    }

    private async void OnXacNhanDatHangClicked(object sender, EventArgs e)
    {
        if (Preferences.Default.Get("cd-xacnhandat", false) && GioHang.Count > 0 &&
            !await DisplayAlert("Xác nhận đặt hàng", $"Đặt {GioHang.Sum(x => x.SoLuong)} món tại {TenGianHang}, tổng {TongCong:N0}đ?", "Đặt hàng", "Xem lại"))
            return;

        // Gian hàng đóng cửa / ngoài giờ / tạm dừng nhận đơn tại thời điểm đặt → không cho tạo đơn.
        var ghDat = MockData.DanhSachGianHang.FirstOrDefault(g => g.Id == GianHangId);
        if (ghDat is not null && !ghDat.CoTheNhanDon)
        {
            await DisplayAlert("Không thể đặt hàng", ghDat.LyDoKhongNhanDon + ". Vui lòng quay lại sau hoặc chọn quán khác.", "OK");
            return;
        }

        // Kiểm tra lại voucher ngay lúc đặt hàng (có thể đã hết hạn/hết lượt/giỏ hàng đã đổi từ lúc áp dụng)
        if (_kmDangDung is not null)
        {
            var kq = KhuyenMaiService.KiemTra(_kmDangDung, GianHangId, GioHang, PhiGiaoHang, Phien.Khach?.Email);
            if (!kq.HopLe)
            {
                var lyDo = kq.ThongDiep;
                DatVoucher(null);
                await DisplayAlert("Mã khuyến mãi không còn hợp lệ", lyDo + "\nMã đã được gỡ khỏi đơn, vui lòng kiểm tra lại tổng tiền.", "OK");
                return;
            }
            _giamGia = kq.GiamDon; _giamShip = kq.GiamPhiShip;
            CapNhatTongTien();
        }

        LoadingXacNhan.IsVisible = true;
        LoadingXacNhan.IsRunning = true;
        BtnXacNhanDatHang.IsEnabled = false;

        try
        {
            // TODO: gọi POST /api/don-hang { gian_hang_id, dia_chi_giao, ghi_chu,
            //   hinh_thuc_thanh_toan, ma_khuyen_mai, chi_tiet: [...] }
            // rồi POST /api/thanh-toan để xử lý thanh toán theo hình thức đã chọn.
            if (GioHang.Count == 0)
            {
                await DisplayAlert("Giỏ hàng trống", "Hãy chọn món trước khi đặt hàng.", "OK");
                return;
            }

            var don = new DonHang
            {
                Id = "DH" + DateTime.Now.ToString("yyMMddHHmmss"),
                GianHangId = GianHangId,
                KhachEmail = Phien.Khach?.Email ?? "",
                KhachTen = Phien.Khach?.HoTen ?? "",
                KhachSdt = Phien.Khach?.SoDienThoai ?? "",
                TenGianHang = TenGianHang,
                ChiTiet = GioHang.ToList(),
                TrangThai = TrangThaiDonHang.ChoXacNhan,
                NgayDat = DateTime.Now,
                DiaChiGiao = string.IsNullOrWhiteSpace(EntryDiaChi.Text) ? "Địa chỉ mặc định của khách" : EntryDiaChi.Text.Trim(),
                GioHenGiao = PickerGioGiao.SelectedIndex > 0 ? (string)PickerGioGiao.SelectedItem : "",
                HenGiaoLuc = PickerGioGiao.SelectedIndex > 0 ? _gioBatDau[PickerGioGiao.SelectedIndex - 1] : null,
                HinhThucThanhToan = _dsHinhThuc.First(h => h.Loai == _hinhThucChon).Nhan,
                MaKhuyenMai = _kmDangDung?.MaCode ?? "",
                GiamGia = _giamGia + _giamShip,           // gồm cả phần giảm phí giao hàng
                PhiGiaoHang = PhiGiaoHang,
                DiaChiLayHang = MockData.DanhSachGianHang.FirstOrDefault(g => g.Id == GianHangId)?.DiaChi ?? "",
            };
            GiaoHangService.GanTuyenVaoDon(don, _tuyen);                       // khoảng cách, khu vực, phí theo biểu phí
            don.MaXacNhanGiao = Random.Shared.Next(1000, 10000).ToString();   // khách đọc mã này cho tài xế khi nhận hàng

            if (DuLieuService.DangOnline)
            {
                var kq = await ApiClient.PostAsync<DatHangDto>("api/mobile/don-hang", new
                {
                    gianHangId = GianHangId,
                    diaChiGiao = don.DiaChiGiao,
                    hinhThucThanhToan = _hinhThucChon.ToString(),
                    maKhuyenMai = _kmDangDung?.MaCode,
                    chiTiet = GioHang.Select(x => new { monAnId = x.MonAn.Id, soLuong = x.SoLuong })
                });
                if (!kq.ThanhCong) { await DisplayAlert("Đặt hàng thất bại", kq.ThongDiep, "OK"); return; }
                if (!string.IsNullOrEmpty(kq.DuLieu?.Id)) don.Id = kq.DuLieu!.Id!;
                await DuLieuService.TaiDonHangKhachAsync();
            }
            else
            {
                await Task.Delay(300);
            }

            // Bug cũ: bấm "Xác nhận" chỉ chuyển tab mà không tạo đơn nào → tab Đơn hàng luôn trống.
            if (MockData.LichSuDonHang.All(d => d.Id != don.Id))
                MockData.LichSuDonHang.Insert(0, don);

            // Nối luồng: đơn khách đặt xuất hiện trong hàng đợi của gian hàng (cùng một đối tượng nên
            // khi quán xác nhận / bàn giao, khách thấy trạng thái đổi theo).
            if (MockData.DonHangCuaGianHang.All(d => d.Id != don.Id))
                MockData.DonHangCuaGianHang.Insert(0, don);
            if (!DuLieuService.DangOnline && _kmDangDung is not null)
                KhuyenMaiService.GhiNhanSuDung(_kmDangDung, Phien.Khach?.Email);     // tăng lượt dùng, ghi nhớ khách đã dùng
            DoiTacService.GuiThongBao(GianHangId, ThongBaoGianHang.DonMoi, "Có đơn hàng mới",
                $"Đơn #{don.Id} · {don.ChiTiet.Sum(x => x.SoLuong)} món · {don.TongTien:N0}đ. Hãy phản hồi trong {DonHang.PhutPhanHoiToiDa} phút.", don.Id);
            VendorLocalStore.Luu();

            await Shell.Current.GoToAsync("//chinh/don-hang");
        }
        finally
        {
            LoadingXacNhan.IsVisible = false;
            LoadingXacNhan.IsRunning = false;
            BtnXacNhanDatHang.IsEnabled = true;
        }
    }

    private async void OnChonBanDoTapped(object? sender, TappedEventArgs e)
        => await BanDoPage.MoAsync(EntryDiaChi.Text ?? "", chonDiaChi: true, khiChon: dc => EntryDiaChi.Text = dc);
}
