using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FoodServiceApp.Web.Models.Entities
{
    /// <summary>Sổ giao dịch thanh toán: mỗi lần tạo/đổi trạng thái thanh toán (thu, hoàn, thất bại, đối soát) ghi một dòng.</summary>
    [Table("GiaoDichThanhToan")]
    public class GiaoDichThanhToan
    {
        [Key] public long MaGiaoDichTT { get; set; }
        public int MaThanhToan { get; set; }
        public int MaDonHang { get; set; }
        public string HanhDong { get; set; } = "";          // TaoMoi | Thu | ThatBai | Hoan | DoiSoat | CapNhat
        public string HinhThuc { get; set; } = "";
        public string? TuTrangThai { get; set; }
        public string DenTrangThai { get; set; } = "";
        public decimal SoTien { get; set; }
        public string? MaGiaoDichCong { get; set; }
        public string? GhiChu { get; set; }
        public DateTime ThoiGian { get; set; } = DateTime.Now;
    }

    /// <summary>Lịch sử từng bước giao hàng (nhận đơn, lấy hàng, giao xong, thất bại, đổi tài xế).</summary>
    [Table("LichSuGiaoHang")]
    public class LichSuGiaoHang
    {
        [Key] public long MaLichSuGH { get; set; }
        public int MaGiaoHang { get; set; }
        public int MaDonHang { get; set; }
        public int MaTaiXe { get; set; }
        public string? TuTrangThai { get; set; }
        public string DenTrangThai { get; set; } = "";
        public double? ViDo { get; set; }
        public double? KinhDo { get; set; }
        public string? GhiChu { get; set; }
        public DateTime ThoiGian { get; set; } = DateTime.Now;
    }

    [Table("LichSuTrangThaiDon")]
    public class LichSuTrangThaiDon
    {
        [Key] public long MaLichSu { get; set; }
        public int MaDonHang { get; set; }
        public string? TuTrangThai { get; set; }
        public string DenTrangThai { get; set; } = "";
        public string? LyDo { get; set; }
        public int? MaTK { get; set; }
        public DateTime ThoiGian { get; set; } = DateTime.Now;
    }

    [Table("YeuCauHoanTien")]
    public class YeuCauHoanTien
    {
        [Key] public int MaYeuCau { get; set; }
        public int MaDonHang { get; set; }
        public int? MaKH { get; set; }
        public decimal SoTien { get; set; }
        public string LyDo { get; set; } = "";
        public string Nguon { get; set; } = "KhachHang";   // KhachHang | QuanTri | KhieuNai
        public string TrangThai { get; set; } = "ChoDuyet"; // ChoDuyet | DaDuyet | TuChoi
        public int? MaTKXuLy { get; set; }
        public string? KetQua { get; set; }
        public DateTime NgayTao { get; set; } = DateTime.Now;
        public DateTime? NgayXuLy { get; set; }
    }

    [Table("CauHinhHeThong")]
    public class CauHinhHeThong
    {
        [Key] public string Khoa { get; set; } = "";
        public string GiaTri { get; set; } = "";
        public string? MoTa { get; set; }
        public int? MaTKSua { get; set; }
        public DateTime NgaySua { get; set; } = DateTime.Now;
    }

    [Table("CauHinhHeThongLichSu")]
    public class CauHinhHeThongLichSu
    {
        [Key] public long MaLichSu { get; set; }
        public string Khoa { get; set; } = "";
        public string? GiaTriCu { get; set; }
        public string GiaTriMoi { get; set; } = "";
        public int? MaTK { get; set; }
        public DateTime ThoiGian { get; set; } = DateTime.Now;
    }

    [Table("DieuPhoiLog")]
    public class DieuPhoiLog
    {
        [Key] public long MaLog { get; set; }
        public int MaDonHang { get; set; }
        public string HanhDong { get; set; } = "";
        public int? MaTaiXeCu { get; set; }
        public int? MaTaiXeMoi { get; set; }
        public string LyDo { get; set; } = "";
        public int? MaTK { get; set; }
        public DateTime ThoiGian { get; set; } = DateTime.Now;
    }

    // Bảng nối có sẵn trong SQL v9
    [Table("KhuyenMaiMonAn")]
    public class KhuyenMaiMonAn { public int MaKhuyenMai { get; set; } public int MaMonAn { get; set; } }

    [Table("KhuyenMaiDanhMuc")]
    public class KhuyenMaiDanhMuc { public int MaKhuyenMai { get; set; } public int MaDanhMuc { get; set; } }

    [Table("KhuyenMaiGianHang")]
    public class KhuyenMaiGianHang { public int MaKhuyenMai { get; set; } public int MaGianHang { get; set; } }

    [Table("KhuyenMaiKhachDung")]
    public class KhuyenMaiKhachDung
    {
        public int MaKhuyenMai { get; set; }
        public int MaKH { get; set; }
        public int MaDonHang { get; set; }
        public DateTime NgaySuDung { get; set; } = DateTime.Now;
    }
}
