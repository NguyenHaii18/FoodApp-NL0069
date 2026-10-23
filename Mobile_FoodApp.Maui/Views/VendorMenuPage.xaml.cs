using FoodApp.Maui.Models;
using FoodApp.Maui.Services;

namespace FoodApp.Maui.Views;

public partial class VendorMenuPage : ContentPage
{
    private string? _danhMucDangLoc;
    public VendorMenuPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await DuLieuService.TaiThucDonGianHangAsync(MockData.GianHangHienTai.Id);
        NapDanhSach();
    }

    private void NapDanhSach()
    {
        NapNutDanhMuc();
        var thuTu = MockData.DanhMucCuaGianHang(MockData.GianHangHienTai.Id);
        int Hang(MonAn m) { var i = thuTu.FindIndex(x => string.Equals(x, m.DanhMuc, StringComparison.OrdinalIgnoreCase)); return i < 0 ? int.MaxValue : i; }
        var danhSach = MockData.MonAnCuaGianHang(MockData.GianHangHienTai.Id)
            .Where(m => _danhMucDangLoc is null || m.DanhMuc == _danhMucDangLoc)
            .OrderBy(Hang).ToList();   // OrderBy ổn định: trong cùng danh mục giữ nguyên thứ tự thêm món
        DsMonAn.ItemsSource = danhSach;
        LblRong.IsVisible = danhSach.Count == 0;
    }

    private void NapNutDanhMuc()
    {
        KhungDanhMuc.Children.Clear();
        AddFilter("Tất cả", null);
        foreach (var ten in MockData.DanhMucCuaGianHang(MockData.GianHangHienTai.Id)) AddFilter(ten, ten);
    }

    private void AddFilter(string ten, string? value)
    {
        var selected = value == _danhMucDangLoc;
        var button = new Button { Text = ten, CornerRadius = 18, Padding = new Thickness(14, 0), HeightRequest = 36,
            BackgroundColor = selected ? (Color)Application.Current!.Resources["Primary"] : Colors.White,
            TextColor = selected ? Colors.White : (Color)Application.Current!.Resources["TextGrey"],
            BorderColor = (Color)Application.Current!.Resources["BorderColor"], BorderWidth = 1 };
        button.Clicked += (_, _) => { _danhMucDangLoc = value; NapDanhSach(); };
        KhungDanhMuc.Children.Add(button);
    }

    private async void OnQuanLyDanhMucClicked(object sender, EventArgs e)
    {
        var gianHangId = MockData.GianHangHienTai.Id;
        var ds = MockData.DanhMucCuaGianHang(gianHangId);
        var action = await DisplayActionSheet("Quản lý danh mục", "Đóng", null, "Thêm danh mục", "Đổi tên danh mục", "Xóa danh mục");
        if (action == "Thêm danh mục")
        {
            var ten = await DisplayPromptAsync("Thêm danh mục", "Tên danh mục", "Lưu", "Hủy", maxLength: 40);
            if (string.IsNullOrWhiteSpace(ten)) return;
            if (ds.Contains(ten.Trim(), StringComparer.OrdinalIgnoreCase)) { await DisplayAlert("Danh mục đã tồn tại", "Hãy chọn tên khác.", "Đóng"); return; }
            if (!await DuLieuService.GhiAsync("POST", $"api/mobile/gian-hang/{gianHangId}/danh-muc", new { tenDanhMuc = ten.Trim() })) return;
            ds.Add(ten.Trim());
            await DuLieuService.TaiThucDonGianHangAsync(gianHangId);
        }
        else if (action == "Đổi tên danh mục")
        {
            if (ds.Count == 0) { await DisplayAlert("Chưa có danh mục", "Hãy thêm danh mục trước.", "Đóng"); return; }
            var old = await DisplayActionSheet("Chọn danh mục", "Hủy", null, ds.ToArray());
            if (string.IsNullOrEmpty(old) || old == "Hủy") return;
            var ten = await DisplayPromptAsync("Đổi tên danh mục", "Tên mới", "Lưu", "Hủy", initialValue: old, maxLength: 40);
            if (string.IsNullOrWhiteSpace(ten)) return;
            ten = ten.Trim();
            if (ten != old && ds.Contains(ten, StringComparer.OrdinalIgnoreCase)) { await DisplayAlert("Danh mục đã tồn tại", "Hãy chọn tên khác.", "Đóng"); return; }
            var categoryId = DuLieuService.MaDanhMuc(gianHangId, old);
            if (DuLieuService.CoPhienMayChu && categoryId is null) { await DisplayAlert("Không thể cập nhật", "Không tìm thấy danh mục trên máy chủ.", "Đóng"); return; }
            if (categoryId is not null && !await DuLieuService.GhiAsync("PUT", $"api/mobile/gian-hang/{gianHangId}/danh-muc/{categoryId}", new { tenDanhMuc = ten })) return;
            ds[ds.IndexOf(old)] = ten;
            foreach (var mon in MockData.MonAnCuaGianHang(gianHangId).Where(m => m.DanhMuc == old)) mon.DanhMuc = ten;
            await DuLieuService.TaiThucDonGianHangAsync(gianHangId);
        }
        else if (action == "Xóa danh mục")
        {
            if (ds.Count == 0) { await DisplayAlert("Chưa có danh mục", "Không có danh mục để xóa.", "Đóng"); return; }
            var ten = await DisplayActionSheet("Chọn danh mục", "Hủy", null, ds.ToArray());
            if (string.IsNullOrEmpty(ten) || ten == "Hủy") return;
            var monTrongDanhMuc = MockData.MonAnCuaGianHang(gianHangId).Where(m => m.DanhMuc == ten).ToList();
            if (monTrongDanhMuc.Count > 0)
            {
                if (DuLieuService.CoPhienMayChu) { await DisplayAlert("Danh mục đang có món", "Hãy sửa từng món sang danh mục khác trước khi xóa.", "Đóng"); return; }
                var chuyenDen = ds.Where(x => x != ten).ToArray();
                if (chuyenDen.Length == 0) { await DisplayAlert("Không thể xóa", "Hãy tạo danh mục khác hoặc chuyển/xóa các món trước.", "Đóng"); return; }
                var dich = await DisplayActionSheet("Chuyển món sang danh mục", "Hủy", null, chuyenDen);
                if (string.IsNullOrEmpty(dich) || dich == "Hủy") return;
                foreach (var mon in monTrongDanhMuc) mon.DanhMuc = dich;
            }
            var categoryId = DuLieuService.MaDanhMuc(gianHangId, ten);
            if (DuLieuService.CoPhienMayChu && categoryId is null) { await DisplayAlert("Không thể xóa", "Không tìm thấy danh mục trên máy chủ.", "Đóng"); return; }
            if (categoryId is not null && !await DuLieuService.GhiAsync("DELETE", $"api/mobile/gian-hang/{gianHangId}/danh-muc/{categoryId}")) return;
            ds.Remove(ten);
            await DuLieuService.TaiThucDonGianHangAsync(gianHangId);
            if (_danhMucDangLoc == ten) _danhMucDangLoc = null;
        }
        VendorLocalStore.Luu();
        NapDanhSach();
    }

    private async void OnDoiTinhTrangClicked(object sender, EventArgs e)
    {
        if (sender is not Button { CommandParameter: MonAn mon }) return;

        var chon = await DisplayActionSheet($"Tình trạng: {mon.TenMon}", "Đóng", null,
            "Còn hàng", "Hết hàng", "Tạm ngừng bán");
        if (chon is null or "Đóng") return;

        mon.ConHang = chon == "Còn hàng";
        mon.TamNgungBan = chon == "Tạm ngừng bán";   // ConHang luôn false khi tạm ngừng → khách không thấy món
        var tinhTrang = mon.TamNgungBan ? "TamNgung" : mon.ConHang ? "ConHang" : "HetHang";
        if (!await DuLieuService.GhiAsync("PUT", $"api/mobile/gian-hang/{MockData.GianHangHienTai.Id}/thuc-don/{mon.Id}/trang-thai", new { tinhTrang })) return;
        VendorLocalStore.Luu();
        NapDanhSach();
    }

    /// <summary>Đổi thứ tự hiển thị của danh mục trên thực đơn (thứ tự trong danh sách = thứ tự hiển thị).</summary>
    private async void OnSapXepDanhMucClicked(object sender, EventArgs e)
    {
        var ds = MockData.DanhMucCuaGianHang(MockData.GianHangHienTai.Id);
        if (ds.Count < 2) { await DisplayAlert("Chưa đủ danh mục", "Cần ít nhất 2 danh mục để sắp xếp.", "Đóng"); return; }

        while (true)
        {
            var nhan = ds.Select((t, i) => $"{i + 1}. {t}").ToArray();
            var chon = await DisplayActionSheet("Chọn danh mục cần di chuyển", "Xong", null, nhan);
            if (string.IsNullOrEmpty(chon) || chon == "Xong") break;

            var vt = Array.IndexOf(nhan, chon);
            if (vt < 0) break;

            var huong = await DisplayActionSheet($"Di chuyển \"{ds[vt]}\"", "Quay lại", null,
                "Lên đầu", "Lên 1 bậc", "Xuống 1 bậc", "Xuống cuối");
            var dich = huong switch
            {
                "Lên đầu" => 0,
                "Lên 1 bậc" => vt - 1,
                "Xuống 1 bậc" => vt + 1,
                "Xuống cuối" => ds.Count - 1,
                _ => vt
            };
            dich = Math.Clamp(dich, 0, ds.Count - 1);
            if (dich == vt) continue;

            var ten = ds[vt];
            ds.RemoveAt(vt);
            ds.Insert(dich, ten);
            VendorLocalStore.Luu();
            NapDanhSach();
        }
    }

    private async void OnThemMonClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(VendorMenuItemEditPage));
    }

    private async void OnSuaClicked(object sender, EventArgs e)
    {
        if (sender is not Button { CommandParameter: MonAn mon }) return;

        await Shell.Current.GoToAsync(nameof(VendorMenuItemEditPage), new Dictionary<string, object>
        {
            { "MonAnId", mon.Id }
        });
    }

    private async void OnXoaClicked(object sender, EventArgs e)
    {
        if (sender is not Button { CommandParameter: MonAn mon }) return;

        bool xacNhan = await DisplayAlert("Xóa món ăn", $"Xóa \"{mon.TenMon}\" khỏi thực đơn?", "Xóa", "Hủy");
        if (!xacNhan) return;

        if (!await DuLieuService.GhiAsync("DELETE", $"api/mobile/gian-hang/{MockData.GianHangHienTai.Id}/thuc-don/{mon.Id}")) return;

        MockData.DanhSachMonAn.Remove(mon);
        VendorLocalStore.Luu();

        NapDanhSach();
    }

    private async void OnRefreshing(object? sender, EventArgs e)
    {
        await DuLieuService.TaiThucDonGianHangAsync(MockData.GianHangHienTai.Id);
        NapDanhSach();
        RefreshThucDon.IsRefreshing = false;
    }
}
