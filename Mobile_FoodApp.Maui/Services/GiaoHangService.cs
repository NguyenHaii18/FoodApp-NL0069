using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using FoodApp.Maui.Models;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.ApplicationModel.Communication;
using Microsoft.Maui.ApplicationModel.DataTransfer;

namespace FoodApp.Maui.Services;

/// <summary>Kết quả ước tính tuyến quán → khách.</summary>
public sealed class KetQuaTuyen
{
    public bool DaXacDinh { get; init; }          // false = không nhận ra khu vực từ địa chỉ
    public double Km { get; init; }
    public int Phut { get; init; }
    public string KhuVucLay { get; init; } = "";
    public string KhuVucGiao { get; init; } = "";
    public bool NgoaiThanh { get; init; }
    public decimal PhiKhoangCach { get; init; }
    public decimal PhuPhiKhuVuc { get; init; }
    public decimal Phi => PhiKhoangCach + PhuPhiKhuVuc;
}

/// <summary>
/// Nghiệp vụ giao hàng dùng chung cho khách / quán / tài xế:
/// ước tính khoảng cách + thời gian, biểu phí theo khoảng cách và khu vực, mở chỉ đường, gọi điện,
/// đồng bộ trạng thái đơn giữa các bản sao (khách – quán – tài xế).
///
/// LƯU Ý: khi chưa có dịch vụ bản đồ, khoảng cách là ƯỚC TÍNH từ tâm quận/huyện nhận ra trong địa chỉ
/// (đường chim bay × hệ số đường bộ). Khi nối Web API (Goong DistanceMatrix) thì thay hàm UocTinh.
/// </summary>
public static class GiaoHangService
{
    // ======================= BIỂU PHÍ (chỉnh tại đây) =======================
    public const decimal PhiCoBan = 15000;          // gồm 2 km đầu
    public const double KmMienPhi = 2.0;
    public const decimal PhiMoiKmThem = 5000;       // mỗi km tiếp theo (làm tròn lên)
    public const decimal PhuPhiNgoaiThanh = 10000;  // giao ra Củ Chi, Hóc Môn, Bình Chánh, Nhà Bè, Cần Giờ
    public const decimal PhiToiDa = 70000;
    public const double HeSoDuongBo = 1.25;         // đường bộ dài hơn chim bay
    public const double TocDoTrungBinhKmH = 20;     // xe máy trong nội thành
    public const int PhutChuanBi = 3;               // lấy hàng / gửi xe / tìm nhà
    public const string TinhThanhMacDinh = "TP. Hồ Chí Minh";

    public static decimal TinhPhiKhoangCach(double km)
    {
        if (km <= KmMienPhi) return PhiCoBan;
        return PhiCoBan + (decimal)Math.Ceiling(km - KmMienPhi) * PhiMoiKmThem;
    }

    /// <summary>Phí giao hàng = phí khoảng cách + phụ phí khu vực, tối đa PhiToiDa.</summary>
    public static (decimal PhiKm, decimal PhuPhi, decimal Tong) TinhPhi(double km, bool ngoaiThanh)
    {
        var phiKm = TinhPhiKhoangCach(km);
        var phuPhi = ngoaiThanh ? PhuPhiNgoaiThanh : 0;
        var tong = Math.Min(PhiToiDa, phiKm + phuPhi);
        if (phiKm + phuPhi > PhiToiDa) phiKm = Math.Max(0, tong - phuPhi);
        return (phiKm, phuPhi, tong);
    }

    public static int UocTinhPhut(double km) =>
        Math.Max(5, (int)Math.Ceiling(km / TocDoTrungBinhKmH * 60) + PhutChuanBi);

    // ======================= KHU VỰC =======================
    private sealed record KhuVuc(string Ten, double Lat, double Lng, bool NgoaiThanh);

    private static readonly Dictionary<string, KhuVuc> QuanSo = new()
    {
        ["1"] = new("Quận 1", 10.7769, 106.7009, false),   ["3"] = new("Quận 3", 10.7840, 106.6844, false),
        ["4"] = new("Quận 4", 10.7578, 106.7013, false),   ["5"] = new("Quận 5", 10.7540, 106.6634, false),
        ["6"] = new("Quận 6", 10.7480, 106.6350, false),   ["7"] = new("Quận 7", 10.7340, 106.7220, false),
        ["8"] = new("Quận 8", 10.7240, 106.6280, false),   ["10"] = new("Quận 10", 10.7746, 106.6679, false),
        ["11"] = new("Quận 11", 10.7629, 106.6500, false), ["12"] = new("Quận 12", 10.8672, 106.6413, false),
        ["2"] = new("Quận 2", 10.7872, 106.7498, false),   ["9"] = new("Quận 9", 10.8428, 106.8287, false),
    };

    // khóa đã bỏ dấu, viết thường
    private static readonly Dictionary<string, KhuVuc> QuanTen = new()
    {
        ["binh thanh"] = new("Bình Thạnh", 10.8106, 106.7091, false),
        ["go vap"] = new("Gò Vấp", 10.8386, 106.6652, false),
        ["phu nhuan"] = new("Phú Nhuận", 10.7991, 106.6802, false),
        ["tan binh"] = new("Tân Bình", 10.8016, 106.6528, false),
        ["tan phu"] = new("Tân Phú", 10.7900, 106.6281, false),
        ["binh tan"] = new("Bình Tân", 10.7653, 106.6037, false),
        ["thu duc"] = new("Thủ Đức", 10.8494, 106.7537, false),
        ["hoc mon"] = new("Hóc Môn", 10.8856, 106.5953, true),
        ["cu chi"] = new("Củ Chi", 10.9733, 106.4930, true),
        ["binh chanh"] = new("Bình Chánh", 10.6870, 106.5740, true),
        ["nha be"] = new("Nhà Bè", 10.6950, 106.7370, true),
        ["can gio"] = new("Cần Giờ", 10.4114, 106.9540, true),
    };

    private static readonly Regex MauQuanSo = new(@"(?:^|[^a-z])(?:q\.?|quan)\s*0?(\d{1,2})(?!\d)", RegexOptions.Compiled);

    private static string BoDau(string s)
    {
        var d = s.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(d.Length);
        foreach (var c in d)
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark) sb.Append(c);
        return sb.ToString().Normalize(NormalizationForm.FormC).Replace('đ', 'd').Replace('Đ', 'D').ToLowerInvariant();
    }

    private static KhuVuc? NhanRaKhuVuc(string? diaChi)
    {
        if (string.IsNullOrWhiteSpace(diaChi)) return null;
        var t = BoDau(diaChi);
        var m = MauQuanSo.Match(t);
        if (m.Success && QuanSo.TryGetValue(m.Groups[1].Value, out var q)) return q;
        foreach (var (khoa, kv) in QuanTen) if (t.Contains(khoa)) return kv;
        return null;
    }

    private static double Haversine(double lat1, double lng1, double lat2, double lng2)
    {
        const double R = 6371;
        double Rad(double x) => x * Math.PI / 180;
        var dLat = Rad(lat2 - lat1); var dLng = Rad(lng2 - lng1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(Rad(lat1)) * Math.Cos(Rad(lat2)) * Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
        return 2 * R * Math.Asin(Math.Sqrt(a));
    }

    // ======================= ƯỚC TÍNH TUYẾN =======================
    public static KetQuaTuyen UocTinh(string? diaChiLay, string? diaChiGiao)
    {
        var a = NhanRaKhuVuc(diaChiLay);
        var b = NhanRaKhuVuc(diaChiGiao);
        if (a is null || b is null)
        {
            // Không nhận ra khu vực: tính phí cơ bản, phụ phí theo khu vực nhận ra được (nếu có).
            var ngoai = b?.NgoaiThanh ?? false;
            var (pk, pp, _) = TinhPhi(0, ngoai);
            return new KetQuaTuyen
            {
                DaXacDinh = false, KhuVucLay = a?.Ten ?? "", KhuVucGiao = b?.Ten ?? "",
                NgoaiThanh = ngoai, PhiKhoangCach = pk, PhuPhiKhuVuc = pp,
            };
        }

        double km = a.Ten == b.Ten
            ? 2.0                                                              // cùng quận: ước tính 2 km
            : Math.Max(1.0, Haversine(a.Lat, a.Lng, b.Lat, b.Lng) * HeSoDuongBo);
        km = Math.Round(km, 1);
        var ngoaiThanh = a.NgoaiThanh || b.NgoaiThanh;
        var (phiKm, phuPhi, _) = TinhPhi(km, ngoaiThanh);
        return new KetQuaTuyen
        {
            DaXacDinh = true, Km = km, Phut = UocTinhPhut(km),
            KhuVucLay = a.Ten, KhuVucGiao = b.Ten, NgoaiThanh = ngoaiThanh,
            PhiKhoangCach = phiKm, PhuPhiKhuVuc = phuPhi,
        };
    }

    /// <summary>Ghi kết quả tuyến + phí vào đơn (dùng khi tạo đơn).</summary>
    public static void GanTuyenVaoDon(DonHang don, KetQuaTuyen k)
    {
        don.KhoangCachKm = k.DaXacDinh ? k.Km : 0;
        don.ThoiGianDuKienPhut = k.Phut;
        don.KhuVucLay = k.KhuVucLay; don.KhuVucGiao = k.KhuVucGiao; don.NgoaiThanh = k.NgoaiThanh;
        don.PhiKhoangCach = k.PhiKhoangCach; don.PhuPhiKhuVuc = k.PhuPhiKhuVuc;
        don.PhiGiaoHang = k.Phi;
    }

    /// <summary>Đơn cũ chưa có thông tin tuyến: tính bù khi mở (không đổi phí khách đã trả).</summary>
    public static void BoSungTuyenNeuThieu(DonHang don)
    {
        if (don.KhoangCachKm > 0 || don.KhuVucGiao.Length > 0) return;
        var k = UocTinh(don.DiaChiLayHang, don.DiaChiGiao);
        don.KhoangCachKm = k.DaXacDinh ? k.Km : 0;
        don.ThoiGianDuKienPhut = k.Phut;
        don.KhuVucLay = k.KhuVucLay; don.KhuVucGiao = k.KhuVucGiao; don.NgoaiThanh = k.NgoaiThanh;
        if (don.PhiKhoangCach == 0 && don.PhuPhiKhuVuc == 0)
        {
            var phuPhi = k.NgoaiThanh ? PhuPhiNgoaiThanh : 0;
            don.PhuPhiKhuVuc = Math.Min(phuPhi, don.PhiGiaoHang);
            don.PhiKhoangCach = don.PhiGiaoHang - don.PhuPhiKhuVuc;
        }
    }

    /// <summary>Quãng đường & thời gian còn lại tới khách, tính từ vị trí tài xế (nếu có). Null = chưa đủ dữ liệu.</summary>
    public static (double Km, int Phut)? ConLaiDenKhach(DonHang don)
    {
        if (don.TaiXeLat is not { } lat || don.TaiXeLng is not { } lng) return null;
        var dich = NhanRaKhuVuc(don.DiaChiGiao);
        if (dich is null) return null;
        var km = Math.Max(0.3, Math.Round(Haversine(lat, lng, dich.Lat, dich.Lng) * HeSoDuongBo, 1));
        return (km, UocTinhPhut(km));
    }

    // ======================= CHỈ ĐƯỜNG / GỌI ĐIỆN =======================
    private static string ChuanHoaDiaChi(string diaChi)
    {
        var d = diaChi.Trim();
        var khongDau = BoDau(d);
        return khongDau.Contains("ho chi minh") || khongDau.Contains("hcm") || khongDau.Contains("sai gon")
            ? d : $"{d}, {TinhThanhMacDinh}";
    }

    /// <summary>Mở ứng dụng bản đồ chỉ đường từ vị trí hiện tại đến địa chỉ.</summary>
    public static async Task<bool> MoChiDuongAsync(string? diaChi)
    {
        if (string.IsNullOrWhiteSpace(diaChi)) return false;
        var url = "https://www.google.com/maps/dir/?api=1&travelmode=two-wheeler&destination=" +
                  Uri.EscapeDataString(ChuanHoaDiaChi(diaChi));
        try { return await Launcher.Default.OpenAsync(new Uri(url)); }
        catch (Exception) { return false; }
    }

    public static async Task<bool> MoViTriAsync(double lat, double lng)
    {
        var url = "https://www.google.com/maps/search/?api=1&query=" +
                  lat.ToString("0.00000", CultureInfo.InvariantCulture) + "," + lng.ToString("0.00000", CultureInfo.InvariantCulture);
        try { return await Launcher.Default.OpenAsync(new Uri(url)); }
        catch (Exception) { return false; }
    }

    /// <summary>Gọi số điện thoại; thiết bị không gọi được (máy tính) thì sao chép số.</summary>
    public static async Task GoiDienAsync(Page trang, string? sdt, string doiTuong)
    {
        sdt = TaiXeStore.ChuanHoaSdt(sdt);
        if (string.IsNullOrEmpty(sdt))
        {
            await trang.DisplayAlert("Chưa có số điện thoại", $"{doiTuong} chưa cung cấp số điện thoại.", "Đóng");
            return;
        }
        try
        {
            if (PhoneDialer.Default.IsSupported) { PhoneDialer.Default.Open(sdt); return; }
        }
        catch (Exception) { /* dùng cách sao chép bên dưới */ }
        await Clipboard.Default.SetTextAsync(sdt);
        await trang.DisplayAlert($"Gọi {doiTuong.ToLowerInvariant()}", $"Thiết bị này không gọi điện được. Đã sao chép số {sdt}.", "OK");
    }

    public static string SdtQuan(DonHang don) =>
        MockData.DanhSachGianHang.FirstOrDefault(g => g.Id == don.GianHangId)?.SoDienThoai ?? "";

    // ======================= ĐỒNG BỘ CÁC BẢN SAO CỦA ĐƠN =======================
    /// <summary>
    /// Một đơn có thể nằm ở nhiều danh sách (khách / quán / chờ tài xế / đã giao) dưới dạng các đối tượng khác nhau;
    /// áp thay đổi lên tất cả để các bên thấy cùng trạng thái.
    /// </summary>
    public static void CapNhatMoiBanSao(string id, Action<DonHang> sua)
    {
        var tatCa = MockData.LichSuDonHang
            .Concat(MockData.DonHangCuaGianHang)
            .Concat(MockData.DonHangChoTaiXe)
            .Concat(MockData.DonHangDaGiaoCuaTaiXe);
        if (MockData.DonHangDangGiaoCuaTaiXe is { } dg) tatCa = tatCa.Append(dg);
        foreach (var d in tatCa.Where(x => x.Id == id).Distinct()) sua(d);
    }

    /// <summary>Mở lại app: đưa đơn tài xế đã nhận nhưng chưa giao xong trở lại màn hình giao hàng.</summary>
    public static void KhoiPhucDonDangGiao()
    {
        if (MockData.DonHangDangGiaoCuaTaiXe is not null) return;
        MockData.DonHangDangGiaoCuaTaiXe = MockData.DonHangChoTaiXe.FirstOrDefault(d =>
            d.TaiXeId == MockData.TaiXeHienTai.Id &&
            d.TrangThai is TrangThaiDonHang.DangLam or TrangThaiDonHang.DangGiao);
    }
}
