using FoodApp.Maui.Models;

namespace FoodApp.Maui.Services;

public record KetQuaVoucher(bool HopLe, string ThongDiep, decimal GiamDon = 0, decimal GiamPhiShip = 0)
{
    public decimal TongGiam => GiamDon + GiamPhiShip;
}

/// <summary>Kiểm tra voucher ở một nơi duy nhất: dùng khi nhập mã, khi chọn ưu đãi và khi bấm đặt hàng.</summary>
public static class KhuyenMaiService
{
    private static KetQuaVoucher Loi(string nd) => new(false, nd);

    public static KetQuaVoucher KiemTra(KhuyenMai km, string gianHangId, IEnumerable<MucGioHang> gio, decimal phiShip, string? emailKhach)
    {
        var ds = gio.ToList();
        var tamTinh = ds.Sum(x => x.ThanhTien);

        if (!km.DangHoatDong) return Loi("Mã khuyến mãi đang bị tắt");
        if (km.ChuaBatDau) return Loi($"Mã chỉ có hiệu lực từ {km.NgayBatDau:dd/MM/yyyy}");
        if (!km.ConHan) return Loi("Mã khuyến mãi đã hết hạn");
        if (!km.ConLuot) return Loi("Mã khuyến mãi đã hết lượt sử dụng");
        if (km.MoiKhachMotLan && !string.IsNullOrEmpty(emailKhach) &&
            km.KhachDaDung.Contains(emailKhach, StringComparer.OrdinalIgnoreCase))
            return Loi("Bạn đã dùng mã này rồi (mỗi khách chỉ dùng 1 lần)");

        // Gian hàng áp dụng
        if (!km.ApDungToanHeThong && km.GianHangId != gianHangId) return Loi($"Mã chỉ áp dụng cho {km.PhamViHienThi}");
        if (km.GianHangApDung.Count > 0 && !km.GianHangApDung.Contains(gianHangId)) return Loi("Mã không áp dụng cho gian hàng này");

        // Món / danh mục áp dụng (khớp món HOẶC danh mục)
        var dong = !km.CoGioiHanMon
            ? ds
            : ds.Where(x => km.MonApDung.Contains(x.MonAn.Id) ||
                            km.DanhMucApDung.Contains(x.MonAn.DanhMuc, StringComparer.OrdinalIgnoreCase)).ToList();
        if (dong.Count == 0) return Loi("Giỏ hàng chưa có món/danh mục thuộc chương trình này");

        // Đơn tối thiểu (tính trên cả đơn)
        if (tamTinh < km.GiaTriDonToiThieu)
            return Loi($"Cần đặt tối thiểu {km.GiaTriDonToiThieu:N0}đ (còn thiếu {km.GiaTriDonToiThieu - tamTinh:N0}đ)");

        var coSo = dong.Sum(x => x.ThanhTien);
        decimal giamDon = 0, giamShip = 0;
        switch (km.LoaiGiam)
        {
            case LoaiGiamGia.PhanTram:
                giamDon = Math.Round(coSo * km.GiaTriGiam / 100m);
                if (km.GiamToiDa is decimal tran && giamDon > tran) giamDon = tran;
                break;
            case LoaiGiamGia.SoTien:
                giamDon = Math.Min(km.GiaTriGiam, coSo);
                break;
            case LoaiGiamGia.GiamPhiVanChuyen:
                giamShip = km.GiaTriGiam > 0 ? Math.Min(km.GiaTriGiam, phiShip) : phiShip;   // 0 = miễn phí giao
                break;
        }
        giamDon = Math.Min(giamDon, tamTinh);

        var nd = km.LoaiGiam == LoaiGiamGia.GiamPhiVanChuyen ? $"Giảm {giamShip:N0}đ phí giao" : $"Giảm {giamDon:N0}đ";
        return new KetQuaVoucher(true, nd, giamDon, giamShip);
    }

    /// <summary>Các ưu đãi khách có thể thấy ở gian hàng này, kèm kết quả kiểm tra (dùng được / lý do không dùng được).</summary>
    public static List<(KhuyenMai Km, KetQuaVoucher Kq)> DanhSachUuDai(string gianHangId, IEnumerable<MucGioHang> gio, decimal phiShip, string? email) =>
        MockData.DanhSachKhuyenMai
            .Where(k => k.DangHoatDong && k.ConHan && (k.ApDungToanHeThong || k.GianHangId == gianHangId))
            .Select(k => (Km: k, Kq: KiemTra(k, gianHangId, gio, phiShip, email)))
            .OrderByDescending(x => x.Kq.HopLe).ThenByDescending(x => x.Kq.TongGiam)
            .ToList();

    /// <summary>Đơn bị hủy: trả lại lượt dùng để mã không bị mất oan.</summary>
    public static void HoanSuDung(string? maCode, string? email)
    {
        if (string.IsNullOrWhiteSpace(maCode)) return;
        var km = MockData.DanhSachKhuyenMai.FirstOrDefault(k => string.Equals(k.MaCode, maCode, StringComparison.OrdinalIgnoreCase));
        if (km is null) return;
        km.SoLuongDaDung = Math.Max(0, km.SoLuongDaDung - 1);
        if (!string.IsNullOrEmpty(email)) km.KhachDaDung.RemoveAll(e => string.Equals(e, email, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Gọi khi đơn đã đặt thành công: tăng lượt dùng và ghi nhớ khách đã dùng.</summary>
    public static void GhiNhanSuDung(KhuyenMai km, string? email)
    {
        km.SoLuongDaDung++;
        if (!string.IsNullOrEmpty(email) && !km.KhachDaDung.Contains(email, StringComparer.OrdinalIgnoreCase))
            km.KhachDaDung.Add(email);
    }
}
