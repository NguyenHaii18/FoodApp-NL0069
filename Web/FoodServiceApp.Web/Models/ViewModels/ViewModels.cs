using System.ComponentModel.DataAnnotations;
using FoodServiceApp.Web.Models.Entities;

namespace FoodServiceApp.Web.Models.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập hoặc email")]
        public string TenDangNhap { get; set; } = "";

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
        [DataType(DataType.Password)]
        public string MatKhau { get; set; } = "";

        public string? LoiThongBao { get; set; }
    }


    public class VendorRegisterViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập tên gian hàng")]
        [StringLength(150, MinimumLength = 2, ErrorMessage = "Tên gian hàng phải có từ 2 đến 150 ký tự")]
        public string TenCuaHang { get; set; } = "";

        [Required(ErrorMessage = "Vui lòng nhập email")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        [RegularExpression(@"^[0-9+ .()-]{9,20}$", ErrorMessage = "Số điện thoại không hợp lệ")]
        public string SDT { get; set; } = "";

        [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập")]
        [RegularExpression(@"^[a-zA-Z0-9._-]{4,50}$", ErrorMessage = "Tên đăng nhập gồm 4-50 ký tự chữ, số, dấu chấm, gạch dưới hoặc gạch ngang")]
        public string TenDangNhap { get; set; } = "";

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
        [MinLength(6, ErrorMessage = "Mật khẩu tối thiểu 6 ký tự")]
        [DataType(DataType.Password)]
        public string MatKhau { get; set; } = "";

        [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu")]
        [Compare(nameof(MatKhau), ErrorMessage = "Mật khẩu xác nhận không khớp")]
        [DataType(DataType.Password)]
        public string XacNhanMatKhau { get; set; } = "";
    }


    public class DriverRegisterViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập họ tên")]
        public string HoTen { get; set; } = "";

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        public string SDT { get; set; } = "";

        public string? PhuongTien { get; set; }
        public string? BienSo { get; set; }
        public string? KhuVucHoatDong { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập")]
        public string TenDangNhap { get; set; } = "";

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
        [DataType(DataType.Password)]
        public string MatKhau { get; set; } = "";

        [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu")]
        [DataType(DataType.Password)]
        public string XacNhanMatKhau { get; set; } = "";
    }

    public class AdminCreateDriverViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập họ tên")]
        public string HoTen { get; set; } = "";

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        public string SDT { get; set; } = "";

        public string? PhuongTien { get; set; }
        public string? BienSo { get; set; }
        public string? KhuVucHoatDong { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập")]
        public string TenDangNhap { get; set; } = "";

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
        [DataType(DataType.Password)]
        public string MatKhau { get; set; } = "";

        [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu")]
        [DataType(DataType.Password)]
        public string XacNhanMatKhau { get; set; } = "";
    }

    public class DashboardViewModel
    {
        public GianHang GianHang { get; set; } = null!;
        public decimal DoanhThuHomNay { get; set; }
        public int SoDonHomNay { get; set; }
        public int SoDonChoXacNhan { get; set; }
        public int SoMonAn { get; set; }
        public List<DonHang> DonMoiNhat { get; set; } = new();
    }

    public class MonAnFormViewModel
    {
        public int MaMonAn { get; set; }
        public int MaDanhMuc { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên món")]
        public string TenMon { get; set; } = "";

        [Required(ErrorMessage = "Vui lòng nhập giá")]
        [Range(1, 100000000, ErrorMessage = "Giá bán phải lớn hơn 0")]
        public decimal Gia { get; set; }

        [Range(0, 100000000, ErrorMessage = "Giá vốn không hợp lệ")]
        public decimal GiaVon { get; set; }

        public string? MoTa { get; set; }
        public string? HinhAnhHienTai { get; set; }
        public IFormFile? AnhMoi { get; set; }

        public string? Email { get; set; }
        [RegularExpression(@"^([01]\d|2[0-3]):[0-5]\d$", ErrorMessage = "Giờ mở cửa dạng HH:mm")]
        public string GioMoCua { get; set; } = "00:00";
        [RegularExpression(@"^([01]\d|2[0-3]):[0-5]\d$", ErrorMessage = "Giờ đóng cửa dạng HH:mm")]
        public string GioDongCua { get; set; } = "23:59";
        public bool DangNhanDon { get; set; } = true;
        public string? GiayPhepHienTai { get; set; }
        public string? GiayAttpHienTai { get; set; }
        public IFormFile? GiayPhepMoi { get; set; }
        public IFormFile? GiayAttpMoi { get; set; }
        [RegularExpression("^(ConHang|HetHang|TamNgung)$", ErrorMessage = "Tình trạng món không hợp lệ")]
        public string TinhTrang { get; set; } = "ConHang";
        public bool ChoGhiChu { get; set; } = true;

        public string TenDanhMucMoi { get; set; } = "";
        public List<DanhMuc> DanhSachDanhMuc { get; set; } = new();
    }

    public class KhuyenMaiFormViewModel
    {
        public int MaKhuyenMai { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mã khuyến mãi")]
        public string MaCode { get; set; } = "";

        public string? MoTa { get; set; }

        [Required] public string LoaiGiam { get; set; } = "PhanTram"; // PhanTram / SoTien
        public decimal GiaTriGiam { get; set; }
        public string? DieuKienApDung { get; set; }

        // Nâng cao (Quản trị): đơn tối thiểu, giảm tối đa, giới hạn lượt, phạm vi áp dụng
        public string? TenChuongTrinh { get; set; }
        public decimal? GiamToiDa { get; set; }
        public decimal GiaTriDonToiThieu { get; set; }
        public int? SoLuongToiDa { get; set; }
        public bool MoiKhachMotLan { get; set; }
        /// <summary>Danh sách mã (cách nhau dấu phẩy); để trống = không giới hạn.</summary>
        public string? DsGianHang { get; set; }
        public string? DsDanhMuc { get; set; }
        public string? DsMonAn { get; set; }

        [Required] public DateTime NgayBatDau { get; set; } = DateTime.Today;
        [Required] public DateTime NgayKetThuc { get; set; } = DateTime.Today.AddMonths(1);
        public bool TrangThai { get; set; } = true;
    }


    public class HoSoViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập tên cửa hàng")]
        public string TenCuaHang { get; set; } = "";

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ")]
        public string DiaChi { get; set; } = "";

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        public string SDT { get; set; } = "";

        public string? MoTa { get; set; }

        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string? Email { get; set; }

        // Hình ảnh gian hàng
        public string? HinhAnhHienTai { get; set; }
        public IFormFile? AnhMoi { get; set; }

        // Thời gian hoạt động
        [RegularExpression(
            @"^([01]\d|2[0-3]):[0-5]\d$",
            ErrorMessage = "Giờ mở cửa phải có định dạng HH:mm")]
        public string GioMoCua { get; set; } = "00:00";

        [RegularExpression(
            @"^([01]\d|2[0-3]):[0-5]\d$",
            ErrorMessage = "Giờ đóng cửa phải có định dạng HH:mm")]
        public string GioDongCua { get; set; } = "23:59";

        public bool DangNhanDon { get; set; }

        // Giấy tờ kinh doanh
        public string? GiayPhepHienTai { get; set; }
        public IFormFile? GiayPhepMoi { get; set; }

        public string? GiayAttpHienTai { get; set; }
        public IFormFile? GiayAttpMoi { get; set; }

        // Đổi mật khẩu
        [DataType(DataType.Password)]
        public string? MatKhauMoi { get; set; }

        [DataType(DataType.Password)]
        public string? XacNhanMatKhauMoi { get; set; }
    }


    public class DoanhThuViewModel
    {
        public DateTime TuNgay { get; set; }
        public DateTime DenNgay { get; set; }
        public decimal TongDoanhThu { get; set; }
        public decimal PhiNenTang { get; set; }
        public decimal ThucNhan { get; set; }
        public decimal GiaVon { get; set; }
        public decimal LoiNhuanUocTinh { get; set; }
        public decimal GiaTriDonTrungBinh { get; set; }
        public int TongDonHoanThanh { get; set; }
        public int TongDonHuy { get; set; }
        public decimal TyLeHuy { get; set; }
        public List<(DateTime Ngay, decimal DoanhThu)> DoanhThuTheoNgay { get; set; } = new();
        public List<(string TenMon, int SoLuongBan, decimal DoanhThu)> MonBanChay { get; set; } = new();
        public List<(string TrangThai, int SoLuong)> DonTheoTrangThai { get; set; } = new();
    }

    public class DriverThuNhapViewModel
    {
        public decimal TongThuNhap30Ngay { get; set; }
        public int TongDonGiaoThanhCong30Ngay { get; set; }
        public List<(DateTime Ngay, decimal ThuNhap)> ThuNhapTheoNgay { get; set; } = new();
        public List<GiaoHang> LichSuGanDay { get; set; } = new();
    }
}
