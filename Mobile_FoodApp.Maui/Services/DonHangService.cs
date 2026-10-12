using FoodApp.Maui.Models;

namespace FoodApp.Maui.Services;

/// <summary>Giữ trạng thái đơn hàng đồng nhất giữa các danh sách (khách / gian hàng / tài xế), kể cả khi là các đối tượng khác nhau.</summary>
public static class DonHangService
{
    public static void DongBo(DonHang nguon)
    {
        IEnumerable<DonHang> TatCa() =>
            MockData.LichSuDonHang
                .Concat(MockData.DonHangCuaGianHang)
                .Concat(MockData.DonHangChoTaiXe)
                .Concat(MockData.DonHangDaGiaoCuaTaiXe);

        foreach (var d in TatCa().Where(x => x.Id == nguon.Id && !ReferenceEquals(x, nguon)))
        {
            d.TrangThai = nguon.TrangThai;
            d.SanSangGiao = nguon.SanSangGiao;
            d.TaiXeId = nguon.TaiXeId;
            d.TaiXeTen = nguon.TaiXeTen;
            d.TaiXeSdt = nguon.TaiXeSdt;
            d.DaHoanTien = nguon.DaHoanTien;
            d.BuocTaiXe = nguon.BuocTaiXe;
            d.GiaoLuc = nguon.GiaoLuc;
            d.LyDoThatBai = nguon.LyDoThatBai;
            d.AnhGiaoPath = nguon.AnhGiaoPath;
            d.PhuCap = nguon.PhuCap;
            d.DiemTaiXe = nguon.DiemTaiXe;
            d.NhanXetTaiXe = nguon.NhanXetTaiXe;
            d.DiemMonAn = nguon.DiemMonAn;
            d.TheDanhGia = nguon.TheDanhGia;
            d.NoiDungDanhGia = nguon.NoiDungDanhGia;
            d.DaLayHang = nguon.DaLayHang; d.LayHangLuc = nguon.LayHangLuc;
            d.TaiXeBienSo = nguon.TaiXeBienSo; d.TaiXeLat = nguon.TaiXeLat; d.TaiXeLng = nguon.TaiXeLng; d.ViTriCapNhatLuc = nguon.ViTriCapNhatLuc;
            d.SoLanGoiKhach = nguon.SoLanGoiKhach;
            d.KhoangCachKm = nguon.KhoangCachKm; d.KhoangCachThucTeKm = nguon.KhoangCachThucTeKm; d.ThoiGianDuKienPhut = nguon.ThoiGianDuKienPhut;
            d.PhiGiaoHang = nguon.PhiGiaoHang; d.PhiKhoangCach = nguon.PhiKhoangCach; d.PhuPhiKhuVuc = nguon.PhuPhiKhuVuc; d.PhiDaDieuChinh = nguon.PhiDaDieuChinh;
            d.GiaoThatBaiLuc = nguon.GiaoThatBaiLuc; d.TinhPhiKhiThatBai = nguon.TinhPhiKhiThatBai;
            d.DaLayHang = nguon.DaLayHang; d.LayHangLuc = nguon.LayHangLuc;
            d.TaiXeBienSo = nguon.TaiXeBienSo;
            d.TaiXeLat = nguon.TaiXeLat; d.TaiXeLng = nguon.TaiXeLng; d.ViTriCapNhatLuc = nguon.ViTriCapNhatLuc;
            d.SoLanGoiKhach = nguon.SoLanGoiKhach;
            d.KhoangCachKm = nguon.KhoangCachKm; d.KhoangCachThucTeKm = nguon.KhoangCachThucTeKm; d.ThoiGianDuKienPhut = nguon.ThoiGianDuKienPhut;
            d.PhiGiaoHang = nguon.PhiGiaoHang; d.PhiKhoangCach = nguon.PhiKhoangCach; d.PhuPhiKhuVuc = nguon.PhuPhiKhuVuc; d.PhiDaDieuChinh = nguon.PhiDaDieuChinh;
            d.TinhPhiKhiThatBai = nguon.TinhPhiKhiThatBai; d.GiaoThatBaiLuc = nguon.GiaoThatBaiLuc;
        }
        VendorLocalStore.Luu();
    }
}
