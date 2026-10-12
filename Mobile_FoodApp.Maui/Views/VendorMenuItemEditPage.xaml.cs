using FoodApp.Maui.Models;
using FoodApp.Maui.Services;

namespace FoodApp.Maui.Views;

[QueryProperty(nameof(MonAnId), "MonAnId")]
public partial class VendorMenuItemEditPage : ContentPage
{
    private static readonly string[] TinhTrangs = { "Còn hàng", "Hết hàng", "Tạm ngừng bán" };

    public string? MonAnId { get; set; }

    private MonAn? _monDangSua;
    private string _anhPath = "";        // ảnh đang chọn (chưa chắc đã lưu)

    public VendorMenuItemEditPage()
    {
        InitializeComponent();
        PickerTinhTrang.ItemsSource = TinhTrangs;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        var danhMuc = MockData.DanhMucCuaGianHang(MockData.GianHangHienTai.Id).ToList();

        _monDangSua = string.IsNullOrEmpty(MonAnId)
            ? null
            : MockData.DanhSachMonAn.FirstOrDefault(m => m.Id == MonAnId);

        // Món cũ có danh mục không còn trong danh sách → vẫn cho chọn lại đúng danh mục đó.
        if (_monDangSua is not null && !string.IsNullOrWhiteSpace(_monDangSua.DanhMuc) &&
            !danhMuc.Contains(_monDangSua.DanhMuc, StringComparer.OrdinalIgnoreCase))
            danhMuc.Add(_monDangSua.DanhMuc);
        PickerDanhMuc.ItemsSource = danhMuc;   // phải gán nguồn TRƯỚC khi chọn mục (bản cũ chọn trước nên mất lựa chọn)

        LblLoi.IsVisible = false;
        if (_monDangSua is not null)
        {
            Title = "Sửa món ăn";
            BtnXoa.IsVisible = true;
            BtnLuu.Text = "Lưu thay đổi";

            _anhPath = _monDangSua.AnhPath;
            EntryHinhAnh.Text = _monDangSua.HinhAnh;
            EntryTenMon.Text = _monDangSua.TenMon;
            EditorMoTa.Text = _monDangSua.MoTa;
            PickerDanhMuc.SelectedItem = danhMuc.FirstOrDefault(x => string.Equals(x, _monDangSua.DanhMuc, StringComparison.OrdinalIgnoreCase));
            EntryGia.Text = ((long)_monDangSua.Gia).ToString();
            EntryGiaVon.Text = _monDangSua.GiaVon > 0 ? ((long)_monDangSua.GiaVon).ToString() : "";
            PickerTinhTrang.SelectedIndex = _monDangSua.TamNgungBan ? 2 : _monDangSua.ConHang ? 0 : 1;
            SwitchChoGhiChu.IsToggled = _monDangSua.ChoGhiChu;
            EditorSize.Text = DinhDang(_monDangSua.TuyChon, TuyChonMon.Size);
            EditorTopping.Text = DinhDang(_monDangSua.TuyChon, TuyChonMon.Topping);
            EditorMucCay.Text = DinhDang(_monDangSua.TuyChon, TuyChonMon.MucCay);
        }
        else
        {
            Title = "Thêm món ăn";
            BtnXoa.IsVisible = false;
            BtnLuu.Text = "Lưu món ăn";

            _anhPath = "";
            EntryHinhAnh.Text = "🍲";
            EntryTenMon.Text = "";
            EditorMoTa.Text = "";
            PickerDanhMuc.SelectedItem = null;
            EntryGia.Text = "";
            EntryGiaVon.Text = "";
            PickerTinhTrang.SelectedIndex = 0;
            SwitchChoGhiChu.IsToggled = true;
            EditorSize.Text = EditorTopping.Text = EditorMucCay.Text = "";
        }
        HienAnh();
    }

    private void HienAnh()
    {
        var co = !string.IsNullOrEmpty(_anhPath) && File.Exists(_anhPath);
        ImgAnhMon.Source = co ? ImageSource.FromFile(_anhPath) : null;
        LblChuaCoAnh.IsVisible = !co;
    }

    private async void OnTaiAnhClicked(object sender, EventArgs e)
    {
        try
        {
            var path = await AnhService.ChonVaLuuAsync("mon_" + MockData.GianHangHienTai.Id, "Chọn ảnh món");
            if (path is null) return;
            _anhPath = path;
            HienAnh();
        }
        catch (Exception ex)
        {
            HienLoi("Không tải được ảnh: " + ex.Message);
        }
    }

    private void OnBoAnhClicked(object sender, EventArgs e)
    {
        _anhPath = "";
        HienAnh();
    }

    // ---- tùy chọn: "Tên:giá" mỗi dòng ----
    private static string DinhDang(IEnumerable<TuyChonMon> ds, string loai) =>
        string.Join("\n", ds.Where(t => t.Loai == loai).Select(t => t.GiaThem > 0 ? $"{t.Ten}:{(long)t.GiaThem}" : t.Ten));

    /// <returns>null nếu có dòng sai định dạng (lỗi ghi vào <paramref name="loi"/>).</returns>
    private static List<TuyChonMon>? PhanTich(string loai, string? text, out string loi)
    {
        loi = "";
        var kq = new List<TuyChonMon>();
        foreach (var dongTho in (text ?? "").Replace("\r", "").Split('\n'))
        {
            var dong = dongTho.Trim();
            if (dong.Length == 0) continue;
            var ten = dong;
            decimal gia = 0;
            var i = dong.LastIndexOf(':');
            if (i >= 0)
            {
                ten = dong[..i].Trim();
                var soTho = new string(dong[(i + 1)..].Where(char.IsDigit).ToArray());
                if (soTho.Length > 9) { loi = $"Tùy chọn {TuyChonMon.NhanLoai(loai)}: phụ phí của \"{ten}\" quá lớn"; return null; }
                if (soTho.Length > 0) gia = decimal.Parse(soTho);
            }
            if (ten.Length == 0) { loi = $"Tùy chọn {TuyChonMon.NhanLoai(loai)}: dòng \"{dong}\" thiếu tên"; return null; }
            if (kq.Any(x => string.Equals(x.Ten, ten, StringComparison.OrdinalIgnoreCase))) { loi = $"Tùy chọn {TuyChonMon.NhanLoai(loai)}: \"{ten}\" bị trùng"; return null; }
            kq.Add(new TuyChonMon { Loai = loai, Ten = ten, GiaThem = gia });
        }
        return kq;
    }

    private async void OnLuuClicked(object sender, EventArgs e)
    {
        LblLoi.IsVisible = false;

        if (string.IsNullOrWhiteSpace(EntryTenMon.Text))
        {
            HienLoi("Vui lòng nhập tên món");
            return;
        }
        if (PickerDanhMuc.SelectedItem is null)
        {
            HienLoi("Hãy tạo danh mục trước khi thêm món ăn");
            return;
        }
        if (!decimal.TryParse(EntryGia.Text, out var gia) || gia <= 0)
        {
            HienLoi("Giá bán không hợp lệ");
            return;
        }
        decimal giaVon = 0;
        if (!string.IsNullOrWhiteSpace(EntryGiaVon.Text) && (!decimal.TryParse(EntryGiaVon.Text, out giaVon) || giaVon < 0))
        {
            HienLoi("Giá vốn không hợp lệ");
            return;
        }
        if (giaVon > gia)
        {
            if (!await DisplayAlert("Giá vốn cao hơn giá bán", "Món này sẽ bị lỗ trên mỗi phần. Vẫn lưu?", "Vẫn lưu", "Xem lại")) return;
        }

        var tuyChon = new List<TuyChonMon>();
        foreach (var (loai, editor) in new[] { (TuyChonMon.Size, EditorSize), (TuyChonMon.Topping, EditorTopping), (TuyChonMon.MucCay, EditorMucCay) })
        {
            var ds = PhanTich(loai, editor.Text, out var loi);
            if (ds is null) { HienLoi(loi); return; }
            tuyChon.AddRange(ds);
        }

        LoadingIndicator.IsVisible = true;
        LoadingIndicator.IsRunning = true;
        BtnLuu.IsEnabled = false;

        try
        {
            var hinhAnh = string.IsNullOrWhiteSpace(EntryHinhAnh.Text) ? "🍲" : EntryHinhAnh.Text.Trim();
            var danhMuc = (string)PickerDanhMuc.SelectedItem;
            var tinhTrang = PickerTinhTrang.SelectedIndex < 0 ? 0 : PickerTinhTrang.SelectedIndex;   // 0 còn / 1 hết / 2 tạm ngừng

            var mon = _monDangSua ?? new MonAn
            {
                Id = IdHelper.TiepTheo("ma", MockData.DanhSachMonAn.Select(x => x.Id)),   // max+1: không trùng sau khi xóa món
                GianHangId = MockData.GianHangHienTai.Id,
            };

            if (!string.IsNullOrEmpty(mon.AnhPath) && mon.AnhPath != _anhPath)
            {
                try { File.Delete(mon.AnhPath); } catch { }   // dọn ảnh cũ đã bị thay/bỏ
            }

            mon.TenMon = EntryTenMon.Text.Trim();
            mon.MoTa = EditorMoTa.Text?.Trim() ?? "";
            mon.DanhMuc = danhMuc;
            mon.Gia = gia;
            mon.GiaVon = giaVon;
            mon.HinhAnh = hinhAnh;
            mon.AnhPath = _anhPath;
            mon.ConHang = tinhTrang == 0;
            mon.TamNgungBan = tinhTrang == 2;
            mon.ChoGhiChu = SwitchChoGhiChu.IsToggled;
            mon.TuyChon = tuyChon;

            if (_monDangSua is null) MockData.DanhSachMonAn.Add(mon);
            VendorLocalStore.Luu();

            await Shell.Current.GoToAsync("..");
        }
        finally
        {
            LoadingIndicator.IsVisible = false;
            LoadingIndicator.IsRunning = false;
            BtnLuu.IsEnabled = true;
        }
    }

    private async void OnXoaClicked(object sender, EventArgs e)
    {
        if (_monDangSua is null) return;

        bool xacNhan = await DisplayAlert("Xóa món ăn", $"Xóa \"{_monDangSua.TenMon}\" khỏi thực đơn?", "Xóa", "Hủy");
        if (!xacNhan) return;

        if (_monDangSua.CoAnh) { try { File.Delete(_monDangSua.AnhPath); } catch { } }
        MockData.DanhSachMonAn.Remove(_monDangSua);
        VendorLocalStore.Luu();

        await Shell.Current.GoToAsync("..");
    }

    private void HienLoi(string thongDiep)
    {
        LblLoi.Text = thongDiep;
        LblLoi.IsVisible = true;
    }
}
