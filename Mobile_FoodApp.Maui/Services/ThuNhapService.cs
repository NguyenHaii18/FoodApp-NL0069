using FoodApp.Maui.Models;

namespace FoodApp.Maui.Services;

public record TongKetThuNhap(decimal Phi, decimal PhuCap, decimal Thuong, int SoDon, int SoThatBai)
{
    public decimal Tong => Phi + PhuCap + Thuong;
}

/// <summary>
/// Tính thu nhập, phụ cấp, thưởng của tài xế từ lịch sử đơn.
/// CHÍNH SÁCH DEMO (sau này lấy từ server): phụ cấp đêm 21:00–06:00 +3.000đ/đơn; thưởng ngày 5/10/15 đơn = 20k/50k/100k;
/// thưởng tuần 25/40 đơn = 80k/150k.
/// </summary>
public static class ThuNhapService
{
    public static readonly (int SoDon, decimal Thuong)[] BacNgay = { (5, 20000), (10, 50000), (15, 100000) };
    public static readonly (int SoDon, decimal Thuong)[] BacTuan = { (25, 80000), (40, 150000) };

    private static IEnumerable<DonHang> DonXong() => MockData.DonHangDaGiaoCuaTaiXe.Where(d => d.GiaoLuc is not null);
    public static IEnumerable<DonHang> DonThanhCong() => DonXong().Where(d => !d.GiaoThatBai);

    public static decimal PhuCapDon(DonHang d)
    {
        var h = (d.GiaoLuc ?? DateTime.Now).Hour;
        return h >= 21 || h < 6 ? 3000 : 0;
    }

    private static decimal ThuongTheoBac((int SoDon, decimal Thuong)[] bac, int n) =>
        bac.Where(b => n >= b.SoDon).Select(b => b.Thuong).DefaultIfEmpty(0).Max();

    public static DateTime DauTuan(DateTime d) => d.Date.AddDays(-(((int)d.DayOfWeek + 6) % 7));   // thứ Hai

    public static int SoDonNgay(DateTime ngay) => DonThanhCong().Count(d => d.GiaoLuc!.Value.Date == ngay.Date);
    public static int SoDonTuan(DateTime ngay) { var t = DauTuan(ngay); return DonThanhCong().Count(d => d.GiaoLuc!.Value.Date >= t && d.GiaoLuc!.Value.Date < t.AddDays(7)); }
    public static decimal ThuongNgay(DateTime ngay) => ThuongTheoBac(BacNgay, SoDonNgay(ngay));
    public static decimal ThuongTuan(DateTime thuHai) => ThuongTheoBac(BacTuan, SoDonTuan(thuHai));

    public static TongKetThuNhap Tong(DateTime tu, DateTime den, bool gomThuongTuan)
    {
        var don = DonThanhCong().Where(d => d.GiaoLuc!.Value.Date >= tu.Date && d.GiaoLuc!.Value.Date <= den.Date).ToList();
        decimal thuong = 0;
        for (var ngay = tu.Date; ngay <= den.Date; ngay = ngay.AddDays(1))
        {
            thuong += ThuongNgay(ngay);
            if (gomThuongTuan && ngay.DayOfWeek == DayOfWeek.Sunday) thuong += ThuongTuan(DauTuan(ngay));   // chốt thưởng tuần vào Chủ nhật
        }
        var thatBai = DonXong().Count(d => d.GiaoThatBai && d.GiaoLuc!.Value.Date >= tu.Date && d.GiaoLuc!.Value.Date <= den.Date);
        var phiThatBai = DonXong().Where(d => d.GiaoThatBai && d.TinhPhiKhiThatBai && d.GiaoLuc!.Value.Date >= tu.Date && d.GiaoLuc!.Value.Date <= den.Date).Sum(d => d.PhiGiaoHang);   // lỗi do khách vẫn được phí giao
        return new TongKetThuNhap(don.Sum(d => d.PhiGiaoHang) + phiThatBai, don.Sum(d => d.PhuCap), thuong, don.Count, thatBai);
    }

    /// <summary>Dòng gợi ý tiến độ thưởng ngày, VD "Hôm nay 3 đơn · thêm 2 đơn để nhận +20.000đ".</summary>
    public static string TienDoThuongNgay()
    {
        var n = SoDonNgay(DateTime.Today);
        var tiep = BacNgay.FirstOrDefault(b => n < b.SoDon);
        return tiep.SoDon == 0 ? $"Hôm nay {n} đơn · bạn đã đạt mốc thưởng cao nhất 🎉"
                               : $"Hôm nay {n} đơn · thêm {tiep.SoDon - n} đơn để nhận +{tiep.Thuong:N0}đ";
    }

    /// <summary>Cập nhật điểm trung bình tài xế từ các đánh giá của khách + số liệu "hôm nay" trên trang chủ.</summary>
    public static void CapNhatChiSo()
    {
        var t = MockData.TaiXeHienTai;
        var diem = MockData.DonHangDaGiaoCuaTaiXe.Where(d => d.DiemTaiXe > 0).Select(d => d.DiemTaiXe).ToList();
        t.DanhGia = diem.Count == 0 ? 0 : Math.Round(diem.Average(), 1);
        var hn = Tong(DateTime.Today, DateTime.Today, false);
        t.TongThuNhapHomNay = hn.Tong;
        t.SoDonHomNay = hn.SoDon;
    }
}
