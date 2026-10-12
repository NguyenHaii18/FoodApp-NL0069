using FoodApp.Maui.Models;

namespace FoodApp.Maui.Views;

public partial class TrangChuPage : ContentPage
{
    public TrangChuPage()
    {
        InitializeComponent();
        DsDanhMuc.ItemsSource = _danhMuc.Keys.ToList();
        DsGianHang.ItemsSource = MockData.DanhSachGianHang;
    }

    private string _loai = "";
    private readonly Dictionary<string, string> _danhMuc = new()
    {
        ["✨ Tất cả"] = "", ["🍚 Cơm"] = "com", ["🍗 Gà rán"] = "ga", ["🍜 Bún/Phở"] = "bun", ["🧋 Trà sữa"] = "tra",
        ["🍕 Pizza"] = "pizza", ["🥖 Bánh mì"] = "banh", ["🍣 Món Nhật"] = "nhat", ["☕ Cà phê"] = "cafe", ["🍧 Chè"] = "che",
    };

    private void OnDanhMucClicked(object? sender, EventArgs e)
    {
        if (sender is Button { CommandParameter: string nhan } && _danhMuc.TryGetValue(nhan, out var loai))
        {
            _loai = loai;
            LocDanhSach();
        }
    }

    private async void OnGoiYTapped(object? sender, TappedEventArgs e)
    {
        if (e.Parameter is not MonAn mon) return;
        var gh = MockData.DanhSachGianHang.FirstOrDefault(g => g.Id == mon.GianHangId);
        if (gh is null) return;
        await Shell.Current.GoToAsync(nameof(MenuCartPage), new Dictionary<string, object> { { "GianHang", gh } });
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        HienDiaChi();
        NapUuDaiVaTin();
        await Services.DuLieuService.TaiDanhMucAsync();
        LocDanhSach();
    }

    /// <summary>Hiện địa chỉ của khách đang đăng nhập (nhập lúc tạo tài khoản).</summary>
    /// <summary>Nạp ưu đãi đang dùng được, tin mới và số chấm đỏ ở chuông.</summary>
    private void NapUuDaiVaTin()
    {
        var km = MockData.DanhSachKhuyenMai
            .Where(k => k.DangHoatDong && k.ConHan && !k.ChuaBatDau && k.ConLuot)
            .OrderByDescending(k => k.ApDungToanHeThong).Take(6).ToList();
        var hienKm = Preferences.Default.Get("cd-km", true);   // Cài đặt → Khuyến mãi & tin mới
        DsUuDai.ItemsSource = km;
        DsUuDai.IsVisible = TieuDeUuDai.IsVisible = hienKm && km.Count > 0;

        TinTucData.LamMoi();
        var tin = TinTucData.DanhSach.OrderByDescending(t => t.Ngay).Take(5).ToList();
        DsTinMoi.ItemsSource = tin;
        DsTinMoi.IsVisible = TieuDeTin.IsVisible = hienKm && tin.Count > 0;
        var n = TinTucData.SoChuaXem;
        LblChuong.Text = n > 0 ? $"🔔 {n}" : "🔔";
    }

    private async void OnUuDaiTapped(object? sender, TappedEventArgs e)
    {
        if (e.Parameter is not KhuyenMai km) return;
        await Clipboard.Default.SetTextAsync(km.MaCode);
        await DisplayAlert("Đã sao chép mã " + km.MaCode,
            $"{km.GiaTriGiamHienThi}\n{km.PhamViHienThi}\nĐơn tối thiểu {km.GiaTriDonToiThieu:N0}đ · HSD {km.NgayKetThuc:dd/MM/yyyy}\n\nDán mã vào ô khuyến mãi khi thanh toán.", "OK");
    }

    private async void OnTinTapped(object? sender, TappedEventArgs e)
    {
        if (e.Parameter is TinTuc t) await DisplayAlert($"{t.Icon} {t.TieuDe}", t.NoiDung, "Đóng");
    }

    private async void OnXemTatCaTinTapped(object? sender, TappedEventArgs e) => await Shell.Current.GoToAsync(nameof(TinTucPage));
    private async void OnChuongTapped(object? sender, TappedEventArgs e) => await Shell.Current.GoToAsync(nameof(TinTucPage));

    private void HienDiaChi()
    {
        var dc = Services.Phien.Khach?.DiaChi;
        LblDiaChi.Text = string.IsNullOrWhiteSpace(dc) ? "Thêm địa chỉ giao hàng" : dc;
    }

    private async void OnDiaChiTapped(object? sender, TappedEventArgs e)
    {
        if (Services.Phien.Khach is null) return;
        var moi = await DisplayPromptAsync("Địa chỉ giao hàng", "Nhập địa chỉ nhận món:",
            "Lưu", "Hủy", initialValue: Services.Phien.Khach.DiaChi);
        if (string.IsNullOrWhiteSpace(moi)) return;
        Services.Phien.Khach.DiaChi = moi.Trim();
        Services.VendorLocalStore.Luu();
        HienDiaChi();
    }

    private void OnTimKiemChanged(object sender, TextChangedEventArgs e) => LocDanhSach();

    /// <summary>Tìm theo tên/địa chỉ gian hàng hoặc tên món (không phân biệt hoa thường và dấu).</summary>
    private void LocDanhSach()
    {
        var tuKhoa = BoDau(EntryTimKiem.Text ?? "");
        var ds = MockData.DanhSachGianHang.Where(g =>
            g.TrangThaiDangKy == TrangThaiDangKyGianHang.DaDuyet && !g.TaiKhoanBiKhoa);

        if (tuKhoa.Length > 0)
            ds = ds.Where(g =>
                BoDau(g.TenGianHang).Contains(tuKhoa) || BoDau(g.DiaChi).Contains(tuKhoa) ||
                MockData.DanhSachMonAn.Any(m => m.GianHangId == g.Id && BoDau(m.TenMon).Contains(tuKhoa)));

        if (_loai.Length > 0) ds = ds.Where(g => g.PhanLoai == _loai);
        var quan = ds.ToList();
        DsGianHang.ItemsSource = quan;

        // Gợi ý món: lấy từ các quán đang hiển thị, tối đa 8 món (khớp từ khóa nếu có)
        var ids = quan.Select(g => g.Id).ToHashSet();
        DsGoiY.ItemsSource = MockData.DanhSachMonAn
            .Where(m => ids.Contains(m.GianHangId) && m.ConHang && (tuKhoa.Length == 0 || BoDau(m.TenMon).Contains(tuKhoa)))
            .Take(8).ToList();
    }

    private static string BoDau(string s)
    {
        var norm = s.Trim().ToLowerInvariant().Replace('đ', 'd').Normalize(System.Text.NormalizationForm.FormD);
        var sb = new System.Text.StringBuilder();
        foreach (var c in norm)
            if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        return sb.ToString();
    }

    private async void OnGianHangTapped(object sender, EventArgs e)
    {
        if (sender is Frame { GestureRecognizers: [TapGestureRecognizer { CommandParameter: GianHang gh }] })
        {
            await Shell.Current.GoToAsync(nameof(MenuCartPage), new Dictionary<string, object>
            {
                { "GianHang", gh }
            });
        }
    }

    private async void OnTaiKhoanTapped(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(ProfilePage));
    }

    private async void OnBanDoTapped(object? sender, TappedEventArgs e)
        => await BanDoPage.MoAsync(EntryTimKiem.Text ?? "");
}
