using FoodApp.Maui.Models;

namespace FoodApp.Maui.Views;

public partial class AdminPromotionEditPage : ContentPage
{
    public AdminPromotionEditPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (!await Services.PhanQuyenService.KiemTraAsync(this, Services.Quyen.QuanLyKhuyenMai)) { await Shell.Current.GoToAsync(".."); return; }

        PickerLoaiGiam.SelectedIndex = 0;
        SwitchMoiKhachMotLan.IsToggled = false;
        DateBatDau.Date = DateTime.Today;
        DateKetThuc.Date = DateTime.Today.AddMonths(1);
        CapNhatNhanGiaTri();
    }


    private LoaiGiamGia LoaiTuPicker() => PickerLoaiGiam.SelectedIndex switch
    {
        0 => LoaiGiamGia.PhanTram,
        1 => LoaiGiamGia.SoTien,
        _ => LoaiGiamGia.GiamPhiVanChuyen,
    };

    private static List<string> TachDs(string? s) =>
        (s ?? "").Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

    /// <summary>Đổi tên món người dùng nhập thành Id món (chỉ món của gian hàng nếu có gianHangId).</summary>
    private static (List<string> Mon, string? Loi) DocMonApDung(string? text, string? gianHangId)
    {
        var ids = new List<string>();
        foreach (var ten in TachDs(text))
        {
            var kq = MockData.DanhSachMonAn.Where(m => string.Equals(m.TenMon, ten, StringComparison.OrdinalIgnoreCase)
                                                    && (gianHangId is null || m.GianHangId == gianHangId)).ToList();
            if (kq.Count == 0) return (ids, $"Không tìm thấy món \"{ten}\"" + (gianHangId is null ? "" : " trong thực đơn của bạn"));
            ids.AddRange(kq.Select(m => m.Id));
        }
        return (ids.Distinct().ToList(), null);
    }

    private void OnLoaiGiamChanged(object? sender, EventArgs e) => CapNhatNhanGiaTri();

    private void CapNhatNhanGiaTri()
    {
        bool laPhanTram = PickerLoaiGiam.SelectedIndex == 0;
        LblNhanGiaTri.Text = LoaiTuPicker() switch
        {
            LoaiGiamGia.PhanTram => "Giá trị giảm (%) *",
            LoaiGiamGia.SoTien => "Giá trị giảm (đ) *",
            _ => "Giảm tối đa vào phí giao (đ) — nhập 0 = miễn phí giao *",
        };
        LblGiamToiDa.IsVisible = laPhanTram;
        EntryGiamToiDa.IsVisible = laPhanTram;
    }

    private async void OnLuuClicked(object sender, EventArgs e)
    {
        LblLoi.IsVisible = false;

        if (string.IsNullOrWhiteSpace(EntryMaCode.Text))
        {
            HienLoi("Vui lòng nhập mã khuyến mãi");
            return;
        }
        var maCode = EntryMaCode.Text.Trim().ToUpper();
        if (MockData.DanhSachKhuyenMai.Any(k => string.Equals(k.MaCode, maCode, StringComparison.OrdinalIgnoreCase)))
        {
            HienLoi("Mã này đã tồn tại, vui lòng chọn mã khác");
            return;
        }
        if (string.IsNullOrWhiteSpace(EntryTenChuongTrinh.Text))
        {
            HienLoi("Vui lòng nhập tên chương trình");
            return;
        }
        var laPhanTram = PickerLoaiGiam.SelectedIndex == 0;
        if (!decimal.TryParse(EntryGiaTriGiam.Text, out var giaTriGiam) || giaTriGiam < 0 ||
            (giaTriGiam == 0 && LoaiTuPicker() != LoaiGiamGia.GiamPhiVanChuyen))
        {
            HienLoi("Giá trị giảm không hợp lệ");
            return;
        }
        if (laPhanTram && giaTriGiam > 100)
        {
            HienLoi("Giảm theo % không được vượt quá 100");
            return;
        }
        decimal? giamToiDa = null;
        if (laPhanTram && !string.IsNullOrWhiteSpace(EntryGiamToiDa.Text))
        {
            if (!decimal.TryParse(EntryGiamToiDa.Text, out var gtd) || gtd <= 0)
            {
                HienLoi("Mức giảm tối đa không hợp lệ");
                return;
            }
            giamToiDa = gtd;
        }
        if (!decimal.TryParse(EntryDonToiThieu.Text, out var donToiThieu) || donToiThieu < 0)
        {
            donToiThieu = 0;
        }
        if (!int.TryParse(EntrySoLuongToiDa.Text, out var soLuongToiDa) || soLuongToiDa <= 0)
        {
            HienLoi("Số lượt sử dụng tối đa không hợp lệ");
            return;
        }
        if (DateKetThuc.Date < DateBatDau.Date)
        {
            HienLoi("Ngày kết thúc phải sau ngày bắt đầu");
            return;
        }

        var (monIds, loiMon) = DocMonApDung(EntryMonApDung.Text, null);
        if (loiMon is not null) { HienLoi(loiMon); return; }
        var dsDanhMuc = TachDs(EntryDanhMucApDung.Text);
        var dsGianHang = TachDs(EntryGianHangApDung.Text);
        var gianHangSai = dsGianHang.FirstOrDefault(g => MockData.DanhSachGianHang.All(x => x.Id != g));
        if (gianHangSai is not null) { HienLoi($"Không có gian hàng mã \"{gianHangSai}\""); return; }

        LoadingIndicator.IsVisible = true;
        LoadingIndicator.IsRunning = true;
        BtnLuu.IsEnabled = false;

        try
        {
            // TODO: gọi POST /api/quan-tri/khuyen-mai khi có API thật.
            await Task.Delay(400);

            var kmMoi = new KhuyenMai
            {
                Id = Services.IdHelper.TiepTheo("km", MockData.DanhSachKhuyenMai.Select(x => x.Id)),
                MaCode = maCode,
                TenChuongTrinh = EntryTenChuongTrinh.Text.Trim(),
                MoTa = EditorMoTa.Text?.Trim() ?? "",
                LoaiGiam = LoaiTuPicker(),
                GiaTriGiam = giaTriGiam,
                GiamToiDa = laPhanTram ? giamToiDa : null,
                GiaTriDonToiThieu = donToiThieu,
                SoLuongToiDa = soLuongToiDa,
                SoLuongDaDung = 0,
                NgayBatDau = DateBatDau.Date,
                NgayKetThuc = DateKetThuc.Date.AddDays(1).AddSeconds(-1),   // hết hạn lúc 23:59:59 của ngày kết thúc
                DangHoatDong = true,
                MoiKhachMotLan = SwitchMoiKhachMotLan.IsToggled,
                MonApDung = monIds,
                DanhMucApDung = dsDanhMuc,
                GianHangApDung = dsGianHang,
                GianHangId = null, // quản trị viên tạo => áp dụng toàn hệ thống
            };
            MockData.DanhSachKhuyenMai.Add(kmMoi);

            Services.NhatKyService.GhiQuanTri("Lưu khuyến mãi toàn hệ thống", EntryMaCode?.Text ?? "");
        Services.VendorLocalStore.Luu();
            await Shell.Current.GoToAsync("..");
        }
        finally
        {
            LoadingIndicator.IsVisible = false;
            LoadingIndicator.IsRunning = false;
            BtnLuu.IsEnabled = true;
        }
    }

    private void HienLoi(string thongDiep)
    {
        LblLoi.Text = thongDiep;
        LblLoi.IsVisible = true;
    }
}
