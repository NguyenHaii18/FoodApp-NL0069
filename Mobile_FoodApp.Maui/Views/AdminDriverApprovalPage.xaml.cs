using FoodApp.Maui.Models;
using FoodApp.Maui.Services;

namespace FoodApp.Maui.Views;

public partial class AdminDriverApprovalPage : ContentPage
{
    private TrangThaiDangKyGianHang _loc = TrangThaiDangKyGianHang.ChoDuyet;

    public AdminDriverApprovalPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (!await PhanQuyenService.KiemTraAsync(this, Quyen.XemTaiKhoan)) { await Shell.Current.GoToAsync(".."); return; }
        await TaiTuServerAsync();
        NapDanhSach();
    }

    // Online: lấy danh sách hồ sơ tài xế từ server. Offline: dùng danh sách lưu trên máy.
    private static async Task TaiTuServerAsync()
    {
        if (!DuLieuService.DangOnline) return;
        var r = await ApiClient.GetAsync<List<TaiXe>>("api/mobile/admin/tai-xe");
        if (!r.ThanhCong || r.DuLieu is null) return;
        MockData.DanhSachTaiXe.Clear();
        MockData.DanhSachTaiXe.AddRange(r.DuLieu);
    }

    private static Color Mau(string k) => (Color)Application.Current!.Resources[k];

    private void VeThanhLoc()
    {
        ThanhLoc.Children.Clear();
        foreach (var (nhan, tt) in new[]
        {
            ("Chờ duyệt", TrangThaiDangKyGianHang.ChoDuyet),
            ("Đã duyệt", TrangThaiDangKyGianHang.DaDuyet),
            ("Từ chối", TrangThaiDangKyGianHang.TuChoi),
        })
        {
            var chon = tt == _loc;
            var so = MockData.DanhSachTaiXe.Count(t => t.TrangThaiDangKy == tt);
            var nut = new Button
            {
                Text = $"{nhan} ({so})", CornerRadius = 18, HeightRequest = 36, Padding = new Thickness(14, 0), FontSize = 12.5,
                BackgroundColor = chon ? Mau("Primary") : Mau("PrimarySoft"),
                TextColor = chon ? Colors.White : Mau("Primary"),
            };
            nut.Clicked += (_, _) => { _loc = tt; NapDanhSach(); };
            ThanhLoc.Children.Add(nut);
        }
    }

    private void NapDanhSach()
    {
        VeThanhLoc();
        var ds = MockData.DanhSachTaiXe.Where(t => t.TrangThaiDangKy == _loc).OrderBy(t => t.NgayDangKy).ToList();
        DsTaiXe.ItemsSource = ds;
        LblRong.IsVisible = ds.Count == 0;
        LblRong.Text = _loc switch
        {
            TrangThaiDangKyGianHang.ChoDuyet => "Không có hồ sơ tài xế nào đang chờ duyệt.",
            TrangThaiDangKyGianHang.DaDuyet => "Chưa có tài xế nào được duyệt.",
            _ => "Chưa có hồ sơ nào bị từ chối.",
        };
    }

    private async void OnDuyetClicked(object sender, EventArgs e)
    {
        if (sender is not Button { CommandParameter: TaiXe t }) return;
        if (!await PhanQuyenService.KiemTraAsync(this, Quyen.DuyetHoSo)) return;
        if (!await DuLieuService.GhiAsync("PUT", $"api/mobile/admin/tai-xe/{t.Id}/duyet")) return;
        NhatKyService.GhiQuanTri("Duyệt hồ sơ tài xế", t.HoTen, t.SoDienThoai);

        t.TrangThaiDangKy = TrangThaiDangKyGianHang.DaDuyet;
        t.LyDoTuChoi = "";
        TaiXeStore.Luu();
        NapDanhSach();
        await DisplayAlert("Đã duyệt", $"Tài xế \"{t.HoTen}\" đã có thể đăng nhập và nhận đơn.", "OK");
    }

    private async void OnTuChoiClicked(object sender, EventArgs e)
    {
        if (sender is not Button { CommandParameter: TaiXe t }) return;
        if (!await PhanQuyenService.KiemTraAsync(this, Quyen.DuyetHoSo)) return;

        var lyDo = await DisplayPromptAsync("Từ chối hồ sơ", $"Lý do từ chối hồ sơ của \"{t.HoTen}\" (tài xế sẽ thấy khi đăng nhập):",
            "Từ chối", "Hủy", "VD: Ảnh giấy phép không rõ", maxLength: 200);
        if (lyDo is null) return;                                         // bấm Hủy
        if (string.IsNullOrWhiteSpace(lyDo)) lyDo = "Hồ sơ không đạt yêu cầu";

        if (!await DuLieuService.GhiAsync("PUT", $"api/mobile/admin/tai-xe/{t.Id}/tu-choi", new { lyDo })) return;

        NhatKyService.GhiQuanTri("Từ chối hồ sơ tài xế", t.HoTen, lyDo.Trim());
        t.TrangThaiDangKy = TrangThaiDangKyGianHang.TuChoi;
        t.LyDoTuChoi = lyDo.Trim();
        TaiXeStore.Luu();
        NapDanhSach();
    }

    private async void OnKhoaClicked(object sender, EventArgs e)
    {
        if (sender is not Button { CommandParameter: TaiXe t }) return;
        if (!await PhanQuyenService.KiemTraAsync(this, Quyen.KhoaMoTaiKhoan)) return;
        var khoa = !t.TaiKhoanBiKhoa;
        if (!await DisplayAlert(khoa ? "Khóa tài khoản" : "Mở khóa", $"{(khoa ? "Khóa" : "Mở khóa")} tài khoản của \"{t.HoTen}\"?", "Đồng ý", "Hủy")) return;
        if (!await DuLieuService.GhiAsync("PUT", $"api/mobile/admin/tai-xe/{t.Id}/{(khoa ? "khoa" : "mo-khoa")}")) return;

        t.TaiKhoanBiKhoa = khoa;
        NhatKyService.GhiQuanTri(khoa ? "Khóa tài xế" : "Mở khóa tài xế", t.HoTen, t.SoDienThoai);
        if (khoa) { t.DangHoatDong = false; t.DangOnline = false; }
        TaiXeStore.Luu();
        NapDanhSach();
    }

    private async void OnRefreshing(object? sender, EventArgs e)
    {
        await TaiTuServerAsync();
        NapDanhSach();
        RefreshDs.IsRefreshing = false;
    }
}
