using System.ComponentModel.DataAnnotations;
using FoodServiceApp.Web.Models.Entities;

namespace FoodServiceApp.Web.Models.ViewModels
{
    public class CustomerRegisterViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập họ tên")]
        public string HoTen { get; set; } = "";

        public string SDT { get; set; } = "";   // cần SĐT hoặc email (kiểm tra ở controller)

        [System.ComponentModel.DataAnnotations.EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string? Email { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập")]
        public string TenDangNhap { get; set; } = "";

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
        [DataType(DataType.Password)]
        public string MatKhau { get; set; } = "";

        [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu")]
        [DataType(DataType.Password)]
        public string XacNhanMatKhau { get; set; } = "";

        public bool DongYDieuKhoan { get; set; }

        public string? LoiThongBao { get; set; }
    }

    public class CustomerHomeViewModel
    {
        public string? TuKhoa { get; set; }
        public string? DanhMuc { get; set; }
        public string? KhuVuc { get; set; }
        public bool ChiDangMo { get; set; } = true;
        public decimal? GiaTu { get; set; }
        public decimal? GiaDen { get; set; }
        public int? SaoTu { get; set; }
        public string SapXep { get; set; } = "";   // "" | danhGia | giaThap | giaCao | banChay
        public List<GianHangCardViewModel> DanhSachGianHang { get; set; } = new();
        public List<string> LichSuTimKiem { get; set; } = new();
        public List<string> MonPhoBien { get; set; } = new();
        public List<string> DanhMucPho { get; set; } = new();
        public bool CoLoc => !string.IsNullOrWhiteSpace(TuKhoa) || !string.IsNullOrWhiteSpace(DanhMuc) || !string.IsNullOrWhiteSpace(KhuVuc)
                             || GiaTu != null || GiaDen != null || SaoTu != null || !ChiDangMo;
    }

    public class GianHangCardViewModel
    {
        public int MaGianHang { get; set; }
        public string TenCuaHang { get; set; } = "";
        public string DiaChi { get; set; } = "";
        public string? HinhAnh { get; set; }
        public string TrangThaiKinhDoanh { get; set; } = "";
        public double DiemTrungBinh { get; set; }
        public int SoDanhGia { get; set; }
        public bool DangMo { get; set; }
        public decimal? TuGia { get; set; }
        public int DaBan { get; set; }
        public bool YeuThich { get; set; }
    }

    public class CartItem
    {
        public int MaMonAn { get; set; }
        public string TenMon { get; set; } = "";
        public decimal Gia { get; set; }
        public int SoLuong { get; set; }
        public string? HinhAnh { get; set; }
        public string? GhiChu { get; set; }
        /// <summary>Id các TuyChonMon đã chọn (giá thêm đã cộng vào Gia).</summary>
        public List<int> TuyChonIds { get; set; } = new();
        public string? TuyChonText { get; set; }
        /// <summary>Khóa phân biệt cùng một món nhưng khác tùy chọn / ghi chú.</summary>
        public string Khoa => $"{MaMonAn}|{string.Join(",", TuyChonIds.OrderBy(x => x))}|{(GhiChu ?? "").Trim().ToLowerInvariant()}";
    }

    public class Cart
    {
        public int MaGianHang { get; set; }
        public string TenGianHang { get; set; } = "";
        public List<CartItem> Items { get; set; } = new();

        public decimal TongTien => Items.Sum(i => i.Gia * i.SoLuong);
        public int TongSoMon => Items.Sum(i => i.SoLuong);
    }

    public class CheckoutViewModel
    {
        public Cart GioHang { get; set; } = new();

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ giao hàng")]
        public string DiaChiGiaoHang { get; set; } = "";

        public string? GhiChu { get; set; }
        public string? MaKhuyenMai { get; set; }
        /// <summary>Token chống tạo đơn trùng khi bấm đặt hàng nhiều lần.</summary>
        public string? MaYeuCauDat { get; set; }

        [Required]
        public string HinhThucThanhToan { get; set; } = "TienMat"; // TienMat / VNPay / Momo

        public decimal SoTienGiam { get; set; }
        /// <summary>Phí giao hàng ước tính hiển thị cho khách trước khi đặt — server tính lại chính xác lúc DatHang.</summary>
        public decimal PhiGiaoHangUocTinh { get; set; }
        public decimal ThanhTien => Math.Max(0, GioHang.TongTien + PhiGiaoHangUocTinh - SoTienGiam);
        public string? LoiThongBao { get; set; }
    }

    public class CustomerHoSoViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập họ tên")]
        public string HoTen { get; set; } = "";
        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        public string SDT { get; set; } = "";
        public string? Email { get; set; }
        public string? DiaChiGiaoHang { get; set; }
        public string? MatKhauMoi { get; set; }
        public string? XacNhanMatKhauMoi { get; set; }
    }

    public class DanhGiaFormViewModel
    {
        public int MaDonHang { get; set; }
        public int MaGianHang { get; set; }
        public string TenGianHang { get; set; } = "";

        [Range(1, 5, ErrorMessage = "Vui lòng chọn số sao từ 1 đến 5")]
        public int SoSao { get; set; } = 5;

        public string? NoiDung { get; set; }

        /// <summary>Số sao cho tài xế (0 = không đánh giá).</summary>
        [Range(0, 5)]
        public int SoSaoGiaoHang { get; set; }

        /// <summary>Đánh giá riêng từng món trong đơn (SoSao = 0 nghĩa là bỏ qua món đó).</summary>
        public List<MonDanhGiaItem> Mons { get; set; } = new();
    }

    public class MonDanhGiaItem
    {
        public int MaMonAn { get; set; }
        public string TenMon { get; set; } = "";
        public int SoSao { get; set; } // 0 = không đánh giá
        public string? NoiDung { get; set; }
    }

    public class DanhGiaMonHienThi
    {
        public string TenKhach { get; set; } = "";
        public int SoSao { get; set; }
        public string? NoiDung { get; set; }
        public DateTime Ngay { get; set; }
    }

    public class MonChiTietViewModel
    {
        public MonAn Mon { get; set; } = null!;
        public GianHang GianHang { get; set; } = null!;
        public double DiemTrungBinh { get; set; }
        public int SoDanhGia { get; set; }
        public List<DanhGiaMonHienThi> DanhGias { get; set; } = new();
        public List<TuyChonMon> TuyChons { get; set; } = new();
        public bool YeuThich { get; set; }
    }

    public class DiaChiFormViewModel
    {
        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Vui lòng nhập nhãn địa chỉ (vd: Nhà, Cơ quan)")]
        public string Nhan { get; set; } = "";
        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Vui lòng nhập địa chỉ")]
        public string DiaChi { get; set; } = "";   // số nhà + tên đường
        public string? Phuong { get; set; }
        public string? Quan { get; set; }
        public string? Tinh { get; set; }
        public string? HuongDan { get; set; }
        public decimal? Lat { get; set; }
        public decimal? Lng { get; set; }
        public bool MacDinh { get; set; }
        public int? MaDiaChi { get; set; }          // có giá trị khi sửa
    }

    public class PhanHoiFormViewModel
    {
        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Vui lòng chọn loại phản hồi")]
        public string Loai { get; set; } = "GopY";
        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Vui lòng nhập tiêu đề")]
        [System.ComponentModel.DataAnnotations.StringLength(150)]
        public string TieuDe { get; set; } = "";
        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Vui lòng nhập nội dung")]
        [System.ComponentModel.DataAnnotations.StringLength(1000)]
        public string NoiDung { get; set; } = "";
        public int? MaDonHang { get; set; }
    }
}
