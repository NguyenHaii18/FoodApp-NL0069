using FoodApp.Maui.Models;

namespace FoodApp.Maui.Services;

/// <summary>
/// Đồng bộ dữ liệu SQL (qua API) vào MockData để các màn hình dùng chung.
/// Nếu không kết nối được API thì giữ nguyên dữ liệu mẫu (chế độ demo ngoại tuyến).
/// </summary>
public static class DuLieuService
{
    public static bool DangOnline { get; private set; }

    /// <summary>Tải gian hàng, món ăn, khuyến mãi. Trả về false nếu không có API.</summary>
    public static async Task<bool> TaiDanhMucAsync()
    {
        var gh = await ApiClient.GetAsync<List<GianHang>>("api/mobile/gian-hang");
        if (!gh.ThanhCong || gh.DuLieu is null) { DangOnline = false; return false; }

        DangOnline = true;
        ThayThe(MockData.DanhSachGianHang, gh.DuLieu);

        var mon = await ApiClient.GetAsync<List<MonAn>>("api/mobile/mon-an");
        if (mon.ThanhCong && mon.DuLieu is not null)
        {
            ThayThe(MockData.DanhSachMonAn, mon.DuLieu);
            MockData.DanhMucTheoGianHang.Clear();
            foreach (var nhom in mon.DuLieu.GroupBy(m => m.GianHangId))
                MockData.DanhMucTheoGianHang[nhom.Key] =
                    nhom.Select(m => m.DanhMuc).Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().ToList();
        }

        var km = await ApiClient.GetAsync<List<KhuyenMai>>("api/mobile/khuyen-mai");
        if (km.ThanhCong && km.DuLieu is not null)
            ThayThe(MockData.DanhSachKhuyenMai, km.DuLieu);

        if (MockData.DanhSachGianHang.Count > 0 &&
            MockData.DanhSachGianHang.All(g => g.Id != MockData.GianHangHienTai?.Id))
            MockData.GianHangHienTai = MockData.DanhSachGianHang[0];

        return true;
    }

    public static async Task TaiDonHangKhachAsync()
    {
        if (!DangOnline) return;
        var r = await ApiClient.GetAsync<List<DonHang>>("api/mobile/khach-hang/don-hang");
        if (r.ThanhCong && r.DuLieu is not null) ThayThe(MockData.LichSuDonHang, r.DuLieu);
    }

    public static async Task TaiDonHangGianHangAsync(string gianHangId)
    {
        if (!DangOnline) return;
        var r = await ApiClient.GetAsync<List<DonHang>>($"api/mobile/gian-hang/{gianHangId}/don-hang");
        if (r.ThanhCong && r.DuLieu is not null) ThayThe(MockData.DonHangCuaGianHang, r.DuLieu);
    }

    /// <summary>Tài xế (online): lấy đơn đã sẵn sàng giao từ API và thêm vào hàng đợi (không ghi đè đơn đang xử lý).</summary>
    public static async Task TaiDonChoTaiXeAsync()
    {
        if (!DangOnline) return;
        var r = await ApiClient.GetAsync<List<DonHang>>("api/mobile/tai-xe/don-cho-nhan");
        if (!r.ThanhCong || r.DuLieu is null) return;
        foreach (var d in r.DuLieu)
            if (MockData.DonHangChoTaiXe.All(x => x.Id != d.Id) && MockData.DonHangDaGiaoCuaTaiXe.All(x => x.Id != d.Id))
                MockData.DonHangChoTaiXe.Add(d);
    }

    public static async Task TaiQuanTriAsync()
    {
        if (!DangOnline) return;
        var kh = await ApiClient.GetAsync<List<KhachHang>>("api/mobile/admin/khach-hang");
        if (kh.ThanhCong && kh.DuLieu is not null) ThayThe(MockData.DanhSachKhachHang, kh.DuLieu);
        var vd = await ApiClient.GetAsync<List<VanDeHeThong>>("api/mobile/admin/van-de");
        if (vd.ThanhCong && vd.DuLieu is not null) ThayThe(MockData.DanhSachVanDe, vd.DuLieu);
    }

    /// <summary>Ghi thay đổi lên API (PUT/POST). Offline: bỏ qua (dữ liệu demo giữ cục bộ). Lỗi: báo người dùng.</summary>
    public static async Task<bool> GhiAsync(string method, string url, object? body = null)
    {
        if (!DangOnline) return true;
        var kq = method == "POST"
            ? await ApiClient.PostAsync<object>(url, body)
            : await ApiClient.PutAsync<object>(url, body);
        if (!kq.ThanhCong && Application.Current?.MainPage is { } p)
            await p.DisplayAlert("Không lưu được", kq.ThongDiep, "OK");
        return kq.ThanhCong;
    }

    private static void ThayThe<T>(IList<T> dich, IEnumerable<T> nguon)
    {
        var moi = nguon.ToList();
        dich.Clear();
        foreach (var x in moi) dich.Add(x);
    }
}

public class DangNhapKhachDto { public string? Token { get; set; } public KhachHang? KhachHang { get; set; } }
public class DangNhapGianHangDto { public string? Token { get; set; } public GianHang? GianHang { get; set; } }
public class DangNhapQuanTriDto { public string? Token { get; set; } public string? VaiTro { get; set; } public string? HoTen { get; set; } }
public class DatHangDto { public string? Id { get; set; } public decimal GiamGia { get; set; } public decimal PhiGiaoHang { get; set; } public decimal TongTien { get; set; } public string? PaymentUrl { get; set; } }
