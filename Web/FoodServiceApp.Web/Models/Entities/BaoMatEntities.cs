using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FoodServiceApp.Web.Models.Entities
{
    /// <summary>Mã OTP xác thực / quên mật khẩu. Chỉ lưu HMAC của mã, không lưu mã gốc.</summary>
    [Table("OtpXacThuc")]
    public class OtpXacThuc
    {
        [Key] public long MaOTP { get; set; }
        public string MucDich { get; set; } = "";   // DangKy | QuenMatKhau | DoiLienHe | XacNhan
        public string Kenh { get; set; } = "email"; // email | sms
        public string NguoiNhan { get; set; } = "";
        public int? MaTK { get; set; }
        public string OtpHash { get; set; } = "";
        public DateTime NgayTao { get; set; } = DateTime.Now;
        public DateTime HetHan { get; set; }
        public int SoLanSai { get; set; }
        public bool DaDung { get; set; }
    }

    /// <summary>Nhật ký hệ thống: đăng nhập, thao tác của người dùng/quản trị, lỗi.</summary>
    [Table("NhatKyHeThong")]
    public class NhatKyHeThong
    {
        [Key] public long MaLog { get; set; }
        public DateTime ThoiGian { get; set; } = DateTime.Now;
        public string MucDo { get; set; } = "Info";     // Info | Warn | Error
        public string Nguon { get; set; } = "";         // Auth | ThaoTac | Otp | TaiKhoan ...
        public string ThongDiep { get; set; } = "";
        public string? ChiTiet { get; set; }
        public int? MaTK { get; set; }
        public string? DiaChiIP { get; set; }
        public string? PhuongThuc { get; set; }
        public string? DuongDan { get; set; }
        public int? MaTrangThai { get; set; }
        public int? ThoiGianMs { get; set; }
    }
}

namespace FoodServiceApp.Web.Models.Entities
{
    /// <summary>Giỏ hàng lưu trên server (05_GioHang.sql): mỗi khách đúng một giỏ, mọi món cùng một gian hàng.</summary>
    [Table("GioHang")]
    public class GioHang
    {
        [Key] public int MaGioHang { get; set; }
        public int MaKH { get; set; }
        public int MaGianHang { get; set; }
        public DateTime NgayCapNhat { get; set; } = DateTime.Now;
        public ICollection<GioHangChiTiet> ChiTiet { get; set; } = new List<GioHangChiTiet>();
    }

    [Table("GioHangChiTiet")]
    public class GioHangChiTiet
    {
        [Key] public int MaChiTiet { get; set; }
        public int MaGioHang { get; set; }
        public int MaMonAn { get; set; }
        public int SoLuong { get; set; }
        public string? GhiChu { get; set; }
        public string? TuyChon { get; set; }   // JSON: ["Lớn","Trân châu"]
    }
}

namespace FoodServiceApp.Web.Models.Entities
{
    [Table("ThongBao")]
    public class ThongBao
    {
        [Key] public long MaThongBao { get; set; }
        public int MaTK { get; set; }
        public string TieuDe { get; set; } = "";
        public string NoiDung { get; set; } = "";
        public string Loai { get; set; } = "HeThong";   // HeThong | DonHang | KhuyenMai | TinNhan (+ ThanhToan lưu vào HeThong)
        public string? MaThamChieu { get; set; }
        public bool DaDoc { get; set; }
        public DateTime NgayTao { get; set; } = DateTime.Now;
    }

    [Table("YeuThich")]
    public class YeuThich
    {
        [Key] public int MaYeuThich { get; set; }
        public int MaKH { get; set; }
        public string Loai { get; set; } = "GianHang";  // GianHang | MonAn
        public int MaThamChieu { get; set; }
        public DateTime NgayTao { get; set; } = DateTime.Now;
    }

    [Table("PhienDangNhap")]
    public class PhienDangNhap
    {
        [Key] public Guid MaPhien { get; set; }
        public int MaTK { get; set; }
        public string Kenh { get; set; } = "web";
        public string? ThietBi { get; set; }
        public string? DiaChiIP { get; set; }
        public DateTime NgayTao { get; set; } = DateTime.Now;
        public DateTime LanCuoi { get; set; } = DateTime.Now;
        public DateTime? ThuHoiLuc { get; set; }
    }

    [Table("BaoCaoDanhGia")]
    public class BaoCaoDanhGia
    {
        public string? KetQua { get; set; }
        public DateTime? NgayXuLy { get; set; }
        [Key] public int MaBaoCao { get; set; }
        public int MaKH { get; set; }
        public int MaDanhGia { get; set; }
        public string LyDo { get; set; } = "";
        public DateTime NgayTao { get; set; } = DateTime.Now;
        public string TrangThai { get; set; } = "ChoXuLy";
    }

    [Table("TuyChonMon")]
    public class TuyChonMon
    {
        [Key] public int MaTuyChon { get; set; }
        public int MaMonAn { get; set; }
        public string Loai { get; set; } = "Size";      // Size | Topping | MucCay
        public string TenTuyChon { get; set; } = "";
        public decimal GiaThem { get; set; }
    }
}
