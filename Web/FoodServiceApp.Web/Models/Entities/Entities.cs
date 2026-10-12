using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FoodServiceApp.Web.Models.Entities
{
    [Table("TaiKhoan")]
    public class TaiKhoan
    {
        [Key] public int MaTK { get; set; }
        public string TenDangNhap { get; set; } = "";
        public string MatKhau { get; set; } = "";
        public string VaiTro { get; set; } = ""; // KhachHang / GianHang / QuanTri / TaiXe
        public DateTime NgayTao { get; set; } = DateTime.Now;
        public bool TrangThai { get; set; } = true;
        public int SoLanDangNhapSai { get; set; }
        public DateTime? KhoaDen { get; set; }
        public DateTime? LanDangNhapCuoi { get; set; }
        public bool EmailDaXacThuc { get; set; }
        public bool SdtDaXacThuc { get; set; }
        public DateTime? DongYDieuKhoanLuc { get; set; }
    }

    [Table("GianHang")]
    public class GianHang
    {
        public string? YeuCauBoSung { get; set; }
        [Key] public int MaGianHang { get; set; }
        public int MaTK { get; set; }
        public TaiKhoan? TaiKhoan { get; set; }
        public string TenCuaHang { get; set; } = "";
        public string DiaChi { get; set; } = "";
        public string SDT { get; set; } = "";
        public string? MoTa { get; set; }
        public string? HinhAnh { get; set; }
        public string? Email { get; set; }
        public string? GiayPhepKinhDoanh { get; set; } // đường dẫn ảnh/tệp giấy phép KD
        public string? GiayChungNhanATTP { get; set; } // đường dẫn giấy chứng nhận ATTP
        public string? LyDoTuChoi { get; set; }        // lý do Quản trị từ chối hồ sơ
        public string TrangThaiKinhDoanh { get; set; } = "DangMo"; // DangMo / DongCua / TamNgung
        public string TrangThaiDuyet { get; set; } = "ChoDuyet"; // ChoDuyet / DaDuyet / TuChoi
        public DateTime NgayDangKy { get; set; } = DateTime.Now;
        // Bổ sung Đối tác (SQL/04_BoSung_DoiTac.sql)
        public TimeSpan GioMoCua { get; set; } = TimeSpan.Zero;                          // 00:00–23:59 = hoạt động cả ngày
        public TimeSpan GioDongCua { get; set; } = new TimeSpan(23, 59, 0);
        public bool DangNhanDon { get; set; } = true;                                    // tạm dừng nhận đơn nhưng vẫn mở cửa
        public decimal TyLeChietKhau { get; set; } = 0.15m;                              // phí nền tảng (NV09), 0.15 = 15%

        public ICollection<DanhMuc> DanhMucs { get; set; } = new List<DanhMuc>();
        public ICollection<DonHang> DonHangs { get; set; } = new List<DonHang>();
        public ICollection<KhuyenMai> KhuyenMais { get; set; } = new List<KhuyenMai>();
    }

    [Table("QuanTri")]
    public class QuanTri
    {
        [Key] public int MaQuanTri { get; set; }
        public int MaTK { get; set; }
        public string HoTen { get; set; } = "";
        public string SDT { get; set; } = "";
        public string? Email { get; set; }
        public string CapQuyenHan { get; set; } = "NhanVien";
        public string? PhongBanPhuTrach { get; set; }
        /// <summary>Quyền chi tiết theo chức năng, cách nhau dấu phẩy. Null = dùng mặc định theo CapQuyenHan.</summary>
        public string? QuyenChiTiet { get; set; }
    }

    [Table("KhachHang")]
    public class KhachHang
    {
        [Key] public int MaKH { get; set; }
        public int MaTK { get; set; }
        public TaiKhoan? TaiKhoan { get; set; }
        public string HoTen { get; set; } = "";
        public string SDT { get; set; } = "";
        public string? Email { get; set; }
        public string? DiaChiGiaoHang { get; set; }
        public string? AnhDaiDien { get; set; }
        public bool NhanTbDonHang { get; set; } = true;
        public bool NhanTbThanhToan { get; set; } = true;
        public bool NhanTbKhuyenMai { get; set; } = true;
    }

    [Table("TaiXe")]
    public class TaiXe
    {
        public string? YeuCauBoSung { get; set; }
        [Key] public int MaTaiXe { get; set; }
        public int MaTK { get; set; }
        public string HoTen { get; set; } = "";
        public string SDT { get; set; } = "";
        public string? PhuongTien { get; set; }
        public string? BienSo { get; set; }
        public string? KhuVucHoatDong { get; set; }
        public string TrangThai { get; set; } = "SanSang";
        public string TrangThaiDuyet { get; set; } = "DaDuyet"; // ChoDuyet / DaDuyet / TuChoi
        public DateTime NgayDangKy { get; set; } = DateTime.Now;
        public string? SoCCCD { get; set; }
        public string? SoGPLX { get; set; }
        public string? AnhGPLX { get; set; }
        public string? LyDoTuChoi { get; set; }
        public string? AnhDaiDien { get; set; }

        public ICollection<GiaoHang> GiaoHangs { get; set; } = new List<GiaoHang>();
    }

    [Table("DanhMuc")]
    public class DanhMuc
    {
        [Key] public int MaDanhMuc { get; set; }
        public int MaGianHang { get; set; }
        public GianHang? GianHang { get; set; }
        public string TenDanhMuc { get; set; } = "";

        public ICollection<MonAn> MonAns { get; set; } = new List<MonAn>();
    }

    [Table("MonAn")]
    public class MonAn
    {
        public string? LyDoAn { get; set; }
        [Key] public int MaMonAn { get; set; }
        public int MaDanhMuc { get; set; }
        public DanhMuc? DanhMuc { get; set; }
        public string TenMon { get; set; } = "";
        public decimal Gia { get; set; }
        public string? MoTa { get; set; }
        public string? HinhAnh { get; set; }
        public string TinhTrang { get; set; } = "ConHang"; // ConHang / HetHang / TamNgung
        public decimal GiaVon { get; set; }                // giá vốn/phần, dùng tính lợi nhuận (0 = chưa nhập)
        public bool ChoGhiChu { get; set; } = true;
    }

    [Table("DonHang")]
    public class DonHang
    {
        public decimal TienGiam { get; set; }
        [Key] public int MaDonHang { get; set; }
        public int MaKH { get; set; }
        public KhachHang? KhachHang { get; set; }
        public int MaGianHang { get; set; }
        public GianHang? GianHang { get; set; }
        public int? MaTaiXe { get; set; }
        public DateTime NgayDat { get; set; } = DateTime.Now;
        // ChoXacNhan / DaXacNhan / DangGiao / DaGiao / DaHuy
        public string TrangThai { get; set; } = "ChoXacNhan";
        public decimal TongTien { get; set; }
        /// <summary>Phí giao hàng (tính theo khoảng cách qua Goong Maps, xem CustomerCartController) — toàn bộ khoản này là thu nhập của tài xế khi giao thành công.</summary>
        public decimal PhiGiaoHang { get; set; } = 0;
        public string DiaChiGiaoHang { get; set; } = "";
        public string? GhiChu { get; set; }
        public int? MaKhuyenMaiApDung { get; set; }
        public KhuyenMai? KhuyenMaiApDung { get; set; }
        public string? MaYeuCauDat { get; set; }           // token chống tạo đơn trùng
        public string? LyDoTuChoi { get; set; }            // gian hàng từ chối / tự hủy quá hạn: bắt buộc có lý do (NV04)
        /// <summary>Mã 4 số khách đọc cho tài xế khi nhận hàng. KHÔNG bao giờ trả cho tài xế/gian hàng.</summary>
        public string? MaXacNhanGiao { get; set; }
        public int SoLanSaiMa { get; set; }                // nhập sai quá 5 lần -> khóa, cần Quản trị mở

        public ICollection<ChiTietDonHang> ChiTiets { get; set; } = new List<ChiTietDonHang>();
        public ThanhToan? ThanhToan { get; set; }
        public GiaoHang? GiaoHang { get; set; }
    }

    [Table("ChiTietDonHang")]
    public class ChiTietDonHang
    {
        [Key] public int MaCTDH { get; set; }
        public int MaDonHang { get; set; }
        public int MaMonAn { get; set; }
        public MonAn? MonAn { get; set; }
        public int SoLuong { get; set; }
        public decimal DonGia { get; set; }
        public string? GhiChu { get; set; }
        public string? TuyChon { get; set; }      // "Lớn; Trân châu"

        [NotMapped] public decimal ThanhTien => SoLuong * DonGia;
    }

    [Table("ThanhToan")]
    public class ThanhToan
    {
        public bool DaDoiSoat { get; set; }
        public DateTime? NgayDoiSoat { get; set; }
        [Key] public int MaThanhToan { get; set; }
        public int MaDonHang { get; set; }
        public string HinhThuc { get; set; } = "TienMat"; // TienMat / VNPay / Momo
        public string TrangThai { get; set; } = "ChoThanhToan";
        public string? MaGiaoDich { get; set; }
        public DateTime? NgayThanhToan { get; set; }
        // Hoàn tiền qua cổng (v13)
        public string? NgayGiaoDichCong { get; set; }   // vnp_PayDate (yyyyMMddHHmmss) — cần cho API hoàn tiền VNPay
        public string? MaGiaoDichHoan { get; set; }     // mã giao dịch hoàn do cổng trả về (hoặc mã nhập tay)
        public DateTime? NgayHoan { get; set; }
        public string? KetQuaHoan { get; set; }         // thông điệp cổng trả về / ghi chú thủ công
    }

    [Table("KhuyenMai")]
    public class KhuyenMai
    {
        // Cột đã có từ SQL v9 — nay dùng thật
        public string? TenChuongTrinh { get; set; }
        public string LoaiGiam { get; set; } = "PhanTram";   // PhanTram / SoTien / GiamPhiVanChuyen
        public decimal? GiamToiDa { get; set; }
        public decimal GiaTriDonToiThieu { get; set; }
        public int? SoLuongToiDa { get; set; }
        public int SoLuongDaDung { get; set; }
        public bool MoiKhachMotLan { get; set; }
        [Key] public int MaKhuyenMai { get; set; }
        public int? MaGianHang { get; set; } // null = áp dụng toàn hệ thống
        public string MaCode { get; set; } = "";
        public string? MoTa { get; set; }
        public decimal? PhanTramGiam { get; set; }
        public decimal? SoTienGiam { get; set; }
        public string? DieuKienApDung { get; set; }
        public DateTime NgayBatDau { get; set; }
        public DateTime NgayKetThuc { get; set; }
        public bool TrangThai { get; set; } = true;

        [NotMapped] public bool ApDungToanHeThong => MaGianHang == null;
        [NotMapped] public bool ConHan => DateTime.Now <= NgayKetThuc;
        [NotMapped] public bool ChuaBatDau => DateTime.Now < NgayBatDau;
    }

    [Table("DanhGia")]
    public class DanhGia
    {
        public bool DaAn { get; set; }
        public string? LyDoAn { get; set; }
        [Key] public int MaDanhGia { get; set; }
        public int MaKH { get; set; }
        public int MaGianHang { get; set; }
        public int MaDonHang { get; set; }
        public int SoSao { get; set; }
        public string? NoiDung { get; set; }
        public DateTime NgayDanhGia { get; set; } = DateTime.Now;
        public int? SoSaoGiaoHang { get; set; }
        public string? TheNhanXet { get; set; }
        public string? PhanHoi { get; set; }               // gian hàng phản hồi đánh giá
        public DateTime? NgayPhanHoi { get; set; }
    }

    [Table("GiaoHang")]
    public class GiaoHang
    {
        [Key] public int MaGiaoHang { get; set; }
        public int MaDonHang { get; set; }
        public DonHang? DonHang { get; set; }
        public int MaTaiXe { get; set; }
        public TaiXe? TaiXe { get; set; }
        // DaNhan / DangGiao / GiaoThanhCong / GiaoThatBai
        public string TrangThai { get; set; } = "DaNhan";
        public DateTime? ThoiGianNhan { get; set; }
        public DateTime? ThoiGianGiao { get; set; }
        // Bằng chứng giao hàng & giao thất bại (v14)
        public string? AnhBangChung { get; set; }          // ảnh tài xế chụp lúc giao/giao thất bại
        public double? ViDoGiao { get; set; }              // vị trí tài xế lúc bấm hoàn thành/thất bại
        public double? KinhDoGiao { get; set; }
        public string? LyDoThatBai { get; set; }
        public bool TinhPhiThatBai { get; set; }           // lỗi do khách -> tài xế vẫn nhận phí giao
        public string? NguoiGayLoi { get; set; }          // KhachHang | GianHang | TaiXe | Khac
        public decimal? TienThuCOD { get; set; }           // tiền mặt thực thu khi giao thành công
    }

    /// <summary>Vị trí mới nhất của tài xế theo đơn (lưu bền để không mất khi server khởi động lại).</summary>
    [Table("TaiXeViTri")]
    public class TaiXeViTri
    {
        [Key] public int MaDonHang { get; set; }
        public int MaTaiXe { get; set; }
        public double ViDo { get; set; }
        public double KinhDo { get; set; }
        public DateTime CapNhatLuc { get; set; } = DateTime.Now;
    }

    [Table("DanhGiaMon")]
    public class DanhGiaMon
    {
        [Key] public int MaDanhGiaMon { get; set; }
        public int MaKH { get; set; }
        public int MaMonAn { get; set; }
        public MonAn? MonAn { get; set; }
        public int MaDonHang { get; set; }
        public int SoSao { get; set; }
        public string? NoiDung { get; set; }
        public DateTime NgayDanhGia { get; set; } = DateTime.Now;
    }

    [Table("DiaChiKhachHang")]
    public class DiaChiKhachHang
    {
        [Key] public int MaDiaChi { get; set; }
        public int MaKH { get; set; }
        public string Nhan { get; set; } = "Nhà"; // Nhà / Cơ quan / ...
        public string DiaChi { get; set; } = "";
        public bool MacDinh { get; set; }
        public string? Tinh { get; set; }
        public string? Quan { get; set; }
        public string? Phuong { get; set; }
        public string? HuongDan { get; set; }
        public decimal? Lat { get; set; }
        public decimal? Lng { get; set; }
    }

    [Table("PhanHoi")]
    public class PhanHoi
    {
        public int? MaTKPhuTrach { get; set; }
        public string? YeuCauBoSung { get; set; }
        public string? PhanLoai { get; set; }
        [Key] public int MaPhanHoi { get; set; }
        public int MaKH { get; set; }
        public KhachHang? KhachHang { get; set; }
        public int? MaDonHang { get; set; }
        public int? MaGianHang { get; set; }
        public string Loai { get; set; } = "GopY"; // GopY / KhieuNai / Khac
        public string TieuDe { get; set; } = "";
        public string NoiDung { get; set; } = "";
        public string TrangThai { get; set; } = "ChuaXuLy"; // ChuaXuLy / DaXuLy
        public string? TraLoi { get; set; }
        public DateTime NgayGui { get; set; } = DateTime.Now;
        public DateTime? NgayXuLy { get; set; }
    }

}
