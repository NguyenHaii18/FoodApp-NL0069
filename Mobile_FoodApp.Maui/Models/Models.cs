using System.Collections.ObjectModel;

namespace FoodApp.Maui.Models;

public class GianHang
{
    public string Id { get; set; } = "";
    public string TenGianHang { get; set; } = "";
    public string DiaChi { get; set; } = "";
    public string HinhAnh { get; set; } = "🍽️";
    /// <summary>Đường dẫn ảnh do gian hàng tự tải lên (lưu trong AppDataDirectory). Rỗng = dùng emoji.</summary>
    public string AnhPath { get; set; } = "";
    public string PhanLoai { get; set; } = "";           // com, ga, bun, tra, pizza, banh, nhat, cafe, che — dùng cho bộ lọc trang chủ
    [System.Text.Json.Serialization.JsonIgnore] public bool CoAnh => !string.IsNullOrEmpty(AnhPath) && File.Exists(AnhPath);
    [System.Text.Json.Serialization.JsonIgnore] public bool CoEmoji => !CoAnh;
    public double DanhGia { get; set; }
    public int ThoiGianGiaoPhut { get; set; }

    /// <summary>Nhãn giảm giá riêng của quán (mã do chính quán tạo, còn hiệu lực). Rỗng = không có.</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public string NhanKhuyenMai
    {
        get
        {
            var km = MockData.DanhSachKhuyenMai.FirstOrDefault(k => k.GianHangId == Id && k.DangHoatDong && k.ConHan && !k.ChuaBatDau && k.ConLuot);
            return km is null ? "" : "🏷 " + km.GiaTriGiamHienThi;
        }
    }
    [System.Text.Json.Serialization.JsonIgnore] public bool CoKhuyenMai => NhanKhuyenMai.Length > 0;

    // --- bổ sung cho luồng quản lý gian hàng (đối tác) ---
    public string SoDienThoai { get; set; } = "";
    public string Email { get; set; } = "";
    public string MatKhau { get; set; } = "";           // demo only — sau này KHÔNG lưu plaintext, xác thực qua API
    public string MoTa { get; set; } = "";
    public bool DangMoCua { get; set; } = true;          // tình trạng kinh doanh: đang mở bán / tạm đóng (do gian hàng tự bật/tắt)
    public bool TaiKhoanBiKhoa { get; set; } = false;     // do quản trị viên khóa khi có vi phạm/khiếu nại
    public DateTime NgayDangKy { get; set; } = DateTime.Now;
    public TrangThaiDangKyGianHang TrangThaiDangKy { get; set; } = TrangThaiDangKyGianHang.DaDuyet;

    // --- giờ hoạt động & trạng thái nhận đơn ---
    /// <summary>Giờ mở cửa "HH:mm". Mặc định 00:00–23:59 = hoạt động cả ngày (không chặn đơn theo giờ).</summary>
    public string GioMoCua { get; set; } = "00:00";
    public string GioDongCua { get; set; } = "23:59";
    /// <summary>Công tắc "nhận đơn": quán vẫn mở cửa nhưng có thể tạm dừng nhận đơn mới khi quá tải.</summary>
    public bool DangNhanDon { get; set; } = true;
    /// <summary>Tỉ lệ chiết khấu nền tảng thỏa thuận lúc đăng ký (0.15 = 15%). Null = dùng mức mặc định của nền tảng.</summary>
    public decimal? TyLeChietKhau { get; set; }

    // --- giấy tờ pháp lý gửi kèm hồ sơ xét duyệt ---
    public string SoGiayPhepKinhDoanh { get; set; } = "";
    public string MaSoThue { get; set; } = "";
    public string CccdChuQuan { get; set; } = "";
    /// <summary>Số giấy chứng nhận An toàn thực phẩm (ATTP) — Web lưu ở cột GiayChungNhanATTP.</summary>
    public string SoGiayChungNhanAttp { get; set; } = "";
    /// <summary>Ảnh giấy phép kinh doanh / giấy chứng nhận an toàn thực phẩm do chủ quán tải lên.</summary>
    public string AnhGiayPhepPath { get; set; } = "";
    public string LyDoTuChoi { get; set; } = "";
    [System.Text.Json.Serialization.JsonIgnore] public bool CoAnhGiayPhep => !string.IsNullOrEmpty(AnhGiayPhepPath) && File.Exists(AnhGiayPhepPath);
    [System.Text.Json.Serialization.JsonIgnore] public bool ThieuGiayTo => !DaCoGiayTo;
    [System.Text.Json.Serialization.JsonIgnore] public bool DaCoGiayTo => !string.IsNullOrWhiteSpace(SoGiayPhepKinhDoanh) && !string.IsNullOrWhiteSpace(CccdChuQuan);
    [System.Text.Json.Serialization.JsonIgnore] public string TomTatGiayTo =>
        $"GPKD: {(string.IsNullOrWhiteSpace(SoGiayPhepKinhDoanh) ? "chưa có" : SoGiayPhepKinhDoanh)}  ·  MST: {(string.IsNullOrWhiteSpace(MaSoThue) ? "—" : MaSoThue)}  ·  CCCD: {(string.IsNullOrWhiteSpace(CccdChuQuan) ? "chưa có" : CccdChuQuan)}  ·  ATTP: {(string.IsNullOrWhiteSpace(SoGiayChungNhanAttp) ? "chưa có" : SoGiayChungNhanAttp)}";

    [System.Text.Json.Serialization.JsonIgnore]
    public bool TrongGioHoatDong
    {
        get
        {
            if (!TimeSpan.TryParse(GioMoCua, out var mo) || !TimeSpan.TryParse(GioDongCua, out var dong)) return true;
            var bay = DateTime.Now.TimeOfDay;
            return mo <= dong ? bay >= mo && bay <= dong : bay >= mo || bay <= dong;   // hỗ trợ quán qua nửa đêm
        }
    }

    /// <summary>Khách có đặt được đơn mới ở quán này lúc này không.</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public bool CoTheNhanDon => !TaiKhoanBiKhoa && TrangThaiDangKy == TrangThaiDangKyGianHang.DaDuyet && DangMoCua && DangNhanDon && TrongGioHoatDong;

    [System.Text.Json.Serialization.JsonIgnore]
    public string LyDoKhongNhanDon =>
        TaiKhoanBiKhoa ? "Gian hàng đang bị khóa" :
        TrangThaiDangKy != TrangThaiDangKyGianHang.DaDuyet ? "Gian hàng chưa được duyệt" :
        !DangMoCua ? "Gian hàng đang tạm đóng cửa" :
        !TrongGioHoatDong ? $"Gian hàng ngoài giờ hoạt động ({GioMoCua} – {GioDongCua})" :
        !DangNhanDon ? "Gian hàng đang tạm dừng nhận đơn" : "";

    [System.Text.Json.Serialization.JsonIgnore]
    public string GioHoatDongHienThi => GioMoCua == "00:00" && GioDongCua == "23:59" ? "Cả ngày" : $"{GioMoCua} – {GioDongCua}";
}

/// <summary>Trạng thái duyệt hồ sơ khi gian hàng đăng ký tham gia nền tảng.</summary>
public enum TrangThaiDangKyGianHang
{
    ChoDuyet,
    DaDuyet,
    TuChoi
}

public static class TrangThaiDangKyGianHangExtensions
{
    public static string Nhan(this TrangThaiDangKyGianHang t) => t switch
    {
        TrangThaiDangKyGianHang.ChoDuyet => "Chờ duyệt",
        TrangThaiDangKyGianHang.DaDuyet => "Đã duyệt",
        TrangThaiDangKyGianHang.TuChoi => "Bị từ chối",
        _ => ""
    };
}

public class MonAn
{
    public string Id { get; set; } = "";
    public string TenMon { get; set; } = "";
    public decimal Gia { get; set; }
    public string HinhAnh { get; set; } = "🍲";
    public string DanhMuc { get; set; } = "";
    public string GianHangId { get; set; } = "";
    public bool ConHang { get; set; } = true;            // tình trạng kinh doanh của món: còn bán / tạm hết

    // --- bổ sung cho quản lý món (đối tác) ---
    public string MoTa { get; set; } = "";
    public string AnhPath { get; set; } = "";             // ảnh món do gian hàng tải lên (rỗng = dùng emoji)
    /// <summary>Tạm ngừng bán (chủ động ngưng, khác với "hết hàng"). Khi true thì ConHang luôn false để phía khách tự ẩn món.</summary>
    public bool TamNgungBan { get; set; } = false;
    public decimal GiaVon { get; set; }                   // giá vốn/phần — dùng tính lợi nhuận (0 = chưa nhập)
    public bool ChoGhiChu { get; set; } = true;           // cho khách ghi chú riêng cho món này
    public List<TuyChonMon> TuyChon { get; set; } = new();

    [System.Text.Json.Serialization.JsonIgnore] public bool CoAnh => !string.IsNullOrEmpty(AnhPath) && File.Exists(AnhPath);
    [System.Text.Json.Serialization.JsonIgnore] public bool CoEmoji => !CoAnh;
    [System.Text.Json.Serialization.JsonIgnore]
    public string TinhTrangText => TamNgungBan ? "⏸ Tạm ngừng bán" : ConHang ? "Còn hàng" : "Hết hàng";
    [System.Text.Json.Serialization.JsonIgnore]
    public string TomTatTuyChon
    {
        get
        {
            var phan = new List<string>();
            foreach (var nhom in TuyChon.GroupBy(t => t.Loai))
                phan.Add($"{TuyChonMon.NhanLoai(nhom.Key)}: {string.Join(", ", nhom.Select(t => t.Ten))}");
            if (ChoGhiChu) phan.Add("Cho ghi chú");
            return string.Join("  •  ", phan);
        }
    }
    [System.Text.Json.Serialization.JsonIgnore] public bool CoTuyChon => TuyChon.Count > 0 || ChoGhiChu;
}

/// <summary>Tài khoản khách hàng — dùng cho phía quản trị để theo dõi/khóa tài khoản.</summary>
public class KhachHang
{
    public string Id { get; set; } = "";
    public string HoTen { get; set; } = "";
    public string Email { get; set; } = "";
    public string MatKhau { get; set; } = "";
    public string SoDienThoai { get; set; } = "";
    public string DiaChi { get; set; } = "";   // địa chỉ khách nhập khi tạo tài khoản
    public DateTime NgayDangKy { get; set; } = DateTime.Now;
    public int TongSoDon { get; set; }
    public bool TaiKhoanBiKhoa { get; set; } = false;

    /// <summary>Ảnh đại diện khách chọn từ máy (rỗng = hiện chữ cái đầu của tên).</summary>
    public string AnhPath { get; set; } = "";
    [System.Text.Json.Serialization.JsonIgnore] public bool CoAnh => !string.IsNullOrEmpty(AnhPath) && File.Exists(AnhPath);
    /// <summary>Email đã được xác thực bằng mã OTP.</summary>
    public bool EmailDaXacThuc { get; set; }
}

/// <summary>Vai trò trong cổng quản trị. Quyền của từng vai trò nằm ở <see cref="Services.PhanQuyenService"/>.</summary>
public enum VaiTroQuanTri
{
    AdminTong = 0,        // toàn quyền, quản lý nhân viên, xem nhật ký thao tác
    NhanVienHoTro = 1,    // xem tài khoản, xử lý khiếu nại / hoàn tiền, xem thống kê
}

public static class VaiTroQuanTriExtensions
{
    public static string Nhan(this VaiTroQuanTri v) => v switch
    {
        VaiTroQuanTri.AdminTong => "Admin tổng",
        VaiTroQuanTri.NhanVienHoTro => "Nhân viên hỗ trợ",
        _ => ""
    };
}

/// <summary>Tài khoản quản trị viên hệ thống (mật khẩu lưu dạng băm PBKDF2).</summary>
public class QuanTriVien
{
    public string Id { get; set; } = "";
    public string HoTen { get; set; } = "";
    public string Email { get; set; } = "";
    public VaiTroQuanTri VaiTro { get; set; } = VaiTroQuanTri.NhanVienHoTro;
    public bool TaiKhoanBiKhoa { get; set; }
    public DateTime NgayTao { get; set; } = DateTime.Now;
    public string Muoi { get; set; } = "";
    public string MatKhauBam { get; set; } = "";

    [System.Text.Json.Serialization.JsonIgnore] public string VaiTroNhan => VaiTro.Nhan();
    [System.Text.Json.Serialization.JsonIgnore] public string TrangThaiNhan => TaiKhoanBiKhoa ? "Đang bị khóa" : "Đang hoạt động";
    [System.Text.Json.Serialization.JsonIgnore] public string NutKhoa => TaiKhoanBiKhoa ? "Mở khóa" : "Khóa";
    [System.Text.Json.Serialization.JsonIgnore] public string NutVaiTro => VaiTro == VaiTroQuanTri.AdminTong ? "Hạ xuống Nhân viên" : "Nâng lên Admin tổng";
}

/// <summary>Một dòng nhật ký: thao tác quản trị, đăng nhập/đăng xuất, đổi mật khẩu, xóa tài khoản...</summary>
public class NhatKyThaoTac
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public DateTime ThoiGian { get; set; } = DateTime.Now;
    public string VaiTro { get; set; } = "";      // KhachHang | GianHang | TaiXe | QuanTri:AdminTong | QuanTri:NhanVienHoTro
    public string TaiKhoan { get; set; } = "";    // email hoặc số điện thoại của người thực hiện
    public string HanhDong { get; set; } = "";
    public string DoiTuong { get; set; } = "";    // đối tượng bị tác động (tên gian hàng, mã đơn...)
    public string ChiTiet { get; set; } = "";
    public bool ThanhCong { get; set; } = true;

    [System.Text.Json.Serialization.JsonIgnore] public string ThoiGianHienThi => ThoiGian.ToString("dd/MM/yyyy HH:mm:ss");
    [System.Text.Json.Serialization.JsonIgnore] public string NguoiThucHien => $"{VaiTroNhan} · {TaiKhoan}";
    [System.Text.Json.Serialization.JsonIgnore] public string VaiTroNhan => VaiTro switch
    {
        "KhachHang" => "Khách hàng",
        "GianHang" => "Gian hàng",
        "TaiXe" => "Tài xế",
        "QuanTri:AdminTong" => "Admin tổng",
        "QuanTri:NhanVienHoTro" => "Nhân viên hỗ trợ",
        _ => VaiTro
    };
    [System.Text.Json.Serialization.JsonIgnore] public string TieuDe => string.IsNullOrWhiteSpace(DoiTuong) ? HanhDong : $"{HanhDong} — {DoiTuong}";
    [System.Text.Json.Serialization.JsonIgnore] public string BieuTuong => !ThanhCong ? "⛔" : HanhDong.StartsWith("Đăng nhập") ? "🔑" : VaiTro.StartsWith("QuanTri") ? "🛡️" : "👤";
}

public enum TrangThaiVanDe { MoiPhatSinh, DangXuLy, DaGiaiQuyet }

public static class TrangThaiVanDeExtensions
{
    public static string Nhan(this TrangThaiVanDe t) => t switch
    {
        TrangThaiVanDe.MoiPhatSinh => "Mới phát sinh",
        TrangThaiVanDe.DangXuLy => "Đang xử lý",
        TrangThaiVanDe.DaGiaiQuyet => "Đã giải quyết",
        _ => ""
    };
}

/// <summary>Vấn đề/khiếu nại phát sinh trong quá trình vận hành, do quản trị viên tiếp nhận và xử lý.</summary>
public class VanDeHeThong
{
    public string Id { get; set; } = "";
    public string TieuDe { get; set; } = "";
    public string MoTa { get; set; } = "";
    public string NguonGoc { get; set; } = "";           // VD: tên gian hàng hoặc khách hàng liên quan
    public DateTime NgayBaoCao { get; set; } = DateTime.Now;
    public TrangThaiVanDe TrangThai { get; set; } = TrangThaiVanDe.MoiPhatSinh;
}

public enum LoaiGiamGia { PhanTram, SoTien, GiamPhiVanChuyen }

public static class LoaiGiamGiaExtensions
{
    public static string Nhan(this LoaiGiamGia t) => t switch
    {
        LoaiGiamGia.PhanTram => "Giảm theo %",
        LoaiGiamGia.SoTien => "Giảm số tiền cố định",
        LoaiGiamGia.GiamPhiVanChuyen => "Giảm phí giao hàng",
        _ => ""
    };
}

/// <summary>
/// Chương trình khuyến mãi / mã giảm giá / voucher. Có thể do một gian hàng tự tạo
/// (<see cref="GianHangId"/> khác rỗng) hoặc do quản trị viên tạo áp dụng toàn hệ thống
/// (<see cref="GianHangId"/> rỗng/null — xem <see cref="ApDungToanHeThong"/>).
/// </summary>
public class KhuyenMai
{
    public string Id { get; set; } = "";
    public string MaCode { get; set; } = "";
    public string TenChuongTrinh { get; set; } = "";
    public string MoTa { get; set; } = "";
    public LoaiGiamGia LoaiGiam { get; set; } = LoaiGiamGia.PhanTram;
    public decimal GiaTriGiam { get; set; }              // % (0-100) nếu LoaiGiam=PhanTram, hoặc số tiền cố định nếu SoTien
    public decimal GiaTriDonToiThieu { get; set; }        // đơn hàng tối thiểu để áp dụng được mã
    public decimal? GiamToiDa { get; set; }               // trần số tiền giảm — chỉ áp dụng khi LoaiGiam=PhanTram
    public DateTime NgayBatDau { get; set; } = DateTime.Now;
    public DateTime NgayKetThuc { get; set; } = DateTime.Now.AddMonths(1);
    public string? GianHangId { get; set; }                // null/"" = áp dụng cho TOÀN HỆ THỐNG
    public int SoLuongToiDa { get; set; } = 100;           // giới hạn tổng lượt sử dụng
    public int SoLuongDaDung { get; set; } = 0;
    public bool MoiKhachMotLan { get; set; }                   // mỗi khách chỉ được dùng mã 1 lần
    public List<string> KhachDaDung { get; set; } = new();     // email các khách đã dùng (để chặn dùng lại)
    public List<string> MonApDung { get; set; } = new();       // Id món được áp dụng; cùng với danh mục: rỗng cả hai = mọi món
    public List<string> DanhMucApDung { get; set; } = new();   // tên danh mục được áp dụng
    public List<string> GianHangApDung { get; set; } = new();  // voucher hệ thống chỉ cho một số gian hàng; rỗng = mọi gian hàng
    [System.Text.Json.Serialization.JsonIgnore] public bool CoGioiHanMon => MonApDung.Count > 0 || DanhMucApDung.Count > 0;
    public bool DangHoatDong { get; set; } = true;         // do người tạo bật/tắt thủ công

    public bool ApDungToanHeThong => string.IsNullOrEmpty(GianHangId);
    public bool ConHan => DateTime.Now <= NgayKetThuc;
    public bool ChuaBatDau => DateTime.Now < NgayBatDau;
    public bool ConLuot => SoLuongDaDung < SoLuongToiDa;

    /// <summary>Trạng thái hiển thị tổng hợp cho danh sách quản lý.</summary>
    public string TrangThaiHienThi
    {
        get
        {
            if (!DangHoatDong) return "Đã tắt";
            if (ChuaBatDau) return "Chưa bắt đầu";
            if (!ConHan) return "Đã hết hạn";
            if (!ConLuot) return "Hết lượt dùng";
            return "Đang hoạt động";
        }
    }

    /// <summary>Mô tả ngắn gọn mức giảm, VD "Giảm 20% (tối đa 30.000đ)" hoặc "Giảm 15.000đ".</summary>
    public string GiaTriGiamHienThi => LoaiGiam switch
    {
        LoaiGiamGia.PhanTram => $"Giảm {GiaTriGiam:0.#}%" + (GiamToiDa is decimal tran ? $" (tối đa {tran:N0}đ)" : ""),
        LoaiGiamGia.GiamPhiVanChuyen => GiaTriGiam > 0 ? $"Giảm {GiaTriGiam:N0}đ phí giao" : "Miễn phí giao hàng",
        _ => $"Giảm {GiaTriGiam:N0}đ",
    };
    /// <summary>Phạm vi áp dụng để hiển thị, VD "Toàn hệ thống" hoặc tên gian hàng cụ thể.</summary>
    public string PhamViHienThi => ApDungToanHeThong
        ? "Toàn hệ thống"
        : MockData.DanhSachGianHang.FirstOrDefault(g => g.Id == GianHangId)?.TenGianHang ?? "Gian hàng";
}

public class MucGioHang
{
    public MonAn MonAn { get; set; } = null!;
    public int SoLuong { get; set; } = 1;

    public decimal ThanhTien => MonAn.Gia * SoLuong;
}

/// <summary>Tài xế giao hàng.</summary>
public class TaiXe
{
    public string Id { get; set; } = "";
    public string HoTen { get; set; } = "";
    public string Anh { get; set; } = "🛵";
    public string AnhPath { get; set; } = "";        // ảnh đại diện tài xế chọn từ máy (rỗng = dùng emoji)
    [System.Text.Json.Serialization.JsonIgnore] public bool CoAnh => !string.IsNullOrEmpty(AnhPath) && File.Exists(AnhPath);
    [System.Text.Json.Serialization.JsonIgnore] public bool CoEmoji => !CoAnh;
    public string LoaiXe { get; set; } = "Xe máy";
    public string SoDienThoai { get; set; } = "";
    public string BienSoXe { get; set; } = "";
    public double DanhGia { get; set; }
    /// <summary>Sẵn sàng nhận đơn (tài xế đang trực tuyến nhưng có thể tạm không nhận đơn).</summary>
    public bool DangHoatDong { get; set; } = true;
    /// <summary>Trực tuyến (online) = đã đăng nhập và đang mở ca; offline khi đăng xuất hoặc tự tắt.</summary>
    public bool DangOnline { get; set; }
    [System.Text.Json.Serialization.JsonIgnore] public bool NhanDuocDon => DangOnline && DangHoatDong && !TaiKhoanBiKhoa;
    [System.Text.Json.Serialization.JsonIgnore] public string TrangThaiLamViec =>
        !DangOnline ? "⚪ Offline" : DangHoatDong ? "🟢 Online · Sẵn sàng nhận đơn" : "🟡 Online · Tạm không nhận đơn";
    public decimal TongThuNhapHomNay { get; set; }
    public int SoDonHomNay { get; set; }
    public DateTime NgayThongKe { get; set; } = DateTime.Today;

    // --- xét duyệt tài xế ---
    public TrangThaiDangKyGianHang TrangThaiDangKy { get; set; } = TrangThaiDangKyGianHang.DaDuyet; // hồ sơ đăng ký mới sẽ đặt ChoDuyet
    public string LyDoTuChoi { get; set; } = "";
    public DateTime NgayDangKy { get; set; } = DateTime.Now;
    public bool TaiKhoanBiKhoa { get; set; }
    public string SoCccd { get; set; } = "";
    public string SoGplx { get; set; } = "";
    public string AnhGplxPath { get; set; } = "";    // ảnh giấy phép lái xe để quản trị viên đối chiếu
    public string Muoi { get; set; } = "";           // salt ngẫu nhiên
    public string MatKhauBam { get; set; } = "";     // PBKDF2 (không lưu mật khẩu gốc)

    [System.Text.Json.Serialization.JsonIgnore] public bool CoAnhGplx => !string.IsNullOrEmpty(AnhGplxPath) && File.Exists(AnhGplxPath);
    [System.Text.Json.Serialization.JsonIgnore] public bool ChoDuyet => TrangThaiDangKy == TrangThaiDangKyGianHang.ChoDuyet;
    [System.Text.Json.Serialization.JsonIgnore] public bool DaDuyet => TrangThaiDangKy == TrangThaiDangKyGianHang.DaDuyet;
    [System.Text.Json.Serialization.JsonIgnore] public string TrangThaiNhan =>
        TaiKhoanBiKhoa ? "Đang bị khóa" : TrangThaiDangKy.Nhan();
    [System.Text.Json.Serialization.JsonIgnore] public string NutKhoa => TaiKhoanBiKhoa ? "Mở khóa" : "Khóa tài khoản";
    [System.Text.Json.Serialization.JsonIgnore] public string ThongTinXe => $"{LoaiXe} · {(string.IsNullOrWhiteSpace(BienSoXe) ? "chưa có biển số" : BienSoXe)}";
}

/// <summary>Một tin nhắn giữa khách hàng và tài xế trong phạm vi một đơn hàng.</summary>
public class TinNhan
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string DonHangId { get; set; } = "";
    public string NguoiGui { get; set; } = "";      // "KhachHang" | "TaiXe"
    public string TenNguoiGui { get; set; } = "";
    public string NoiDung { get; set; } = "";
    public DateTime ThoiGian { get; set; } = DateTime.Now;
}

public enum TrangThaiDonHang
{
    ChoXacNhan,
    DangLam,
    DangGiao,
    HoanThanh,
    DaHuy
}

public static class TrangThaiDonHangExtensions
{
    public static string Nhan(this TrangThaiDonHang t) => t switch
    {
        TrangThaiDonHang.ChoXacNhan => "Chờ xác nhận",
        TrangThaiDonHang.DangLam => "Đang chuẩn bị",
        TrangThaiDonHang.DangGiao => "Đang giao",
        TrangThaiDonHang.HoanThanh => "Hoàn thành",
        TrangThaiDonHang.DaHuy => "Đã hủy",
        _ => ""
    };

    // Bước tương ứng trên thanh tiến trình 4 bước (0-3)
    public static int Buoc(this TrangThaiDonHang t) => t switch
    {
        TrangThaiDonHang.ChoXacNhan => 0,
        TrangThaiDonHang.DangLam => 1,
        TrangThaiDonHang.DangGiao => 2,
        TrangThaiDonHang.HoanThanh => 3,
        TrangThaiDonHang.DaHuy => 0,
        _ => 0
    };
}

public class DonHang
{
    public string Id { get; set; } = "";
    public string GianHangId { get; set; } = "";         // dùng để lọc đơn hàng theo từng gian hàng (đối tác)
    public string TenGianHang { get; set; } = "";
    public List<MucGioHang> ChiTiet { get; set; } = new();
    public TrangThaiDonHang TrangThai { get; set; }
    public string DiaChiGiao { get; set; } = "";
    public DateTime NgayDat { get; set; }
    public bool SanSangGiao { get; set; }            // quán đã chuẩn bị xong, đang chờ tài xế đến lấy
    [System.Text.Json.Serialization.JsonIgnore] public bool CoTheBanGiao => TrangThai == TrangThaiDonHang.DangLam && !SanSangGiao;
    [System.Text.Json.Serialization.JsonIgnore] public bool DangChoTaiXe => TrangThai == TrangThaiDonHang.DangLam && SanSangGiao && string.IsNullOrEmpty(TaiXeId);
    /// <summary>Nhãn theo đúng luồng: Chờ xác nhận → Đang chuẩn bị → Chờ tài xế → Tài xế đang đến lấy → Đang giao → Hoàn thành.</summary>
    [System.Text.Json.Serialization.JsonIgnore] public string NhanHienThi => TrangThai switch
    {
        TrangThaiDonHang.DangLam when !SanSangGiao => "Đang chuẩn bị",
        TrangThaiDonHang.DangLam when string.IsNullOrEmpty(TaiXeId) => "Chờ tài xế nhận",
        TrangThaiDonHang.DangLam => "Tài xế đang đến lấy",
        _ => TrangThai.Nhan(),
    };
    public string TaiXeTen { get; set; } = "";       // hiển thị cho khách để nhắn tin / gọi tài xế
    public string TaiXeSdt { get; set; } = "";
    public string KhachTen { get; set; } = "";       // tên + SĐT khách: tài xế dùng để liên hệ khi giao hàng
    public string KhachSdt { get; set; } = "";
    public string KhachEmail { get; set; } = "";     // email khách đặt đơn (lọc "Đơn của tôi" khi nhiều tài khoản dùng chung máy)

    // --- bổ sung cho luồng tài xế giao hàng ---
    public string DiaChiLayHang { get; set; } = ""; // địa chỉ gian hàng, nơi tài xế đến lấy món
    public string? TaiXeId { get; set; }             // null = chưa có tài xế nhận
    public decimal PhiGiaoHang { get; set; } = 15000; // tiền tài xế nhận cho đơn này
    public string GioHenGiao { get; set; } = "";     // rỗng = giao ngay; ví dụ "11:30 – 12:00" = đơn hẹn giờ
    public bool LaDonHenGio => !string.IsNullOrEmpty(GioHenGiao);
    public DateTime? HenGiaoLuc { get; set; }         // thời điểm bắt đầu khung giờ hẹn
    public string NhanHenGio => LaDonHenGio ? $"🕒 Hẹn giao {GioHenGiao}" : "";
    public string TieuDe => LaDonHenGio ? $"Đơn #{Id} · 🕒 {GioHenGiao}" : $"Đơn #{Id}";
    public bool DaHoanTien { get; set; }              // đơn bị hủy và đã hoàn tiền cho khách
    public int BuocTaiXe { get; set; }                // 0 = đang đến quán, 1 = đang lấy hàng tại quán (trạng thái DangLam)
    public DateTime? GiaoLuc { get; set; }            // lúc giao xong / giao thất bại
    public string LyDoThatBai { get; set; } = "";     // có giá trị = giao không thành công
    public string AnhGiaoPath { get; set; } = "";     // ảnh bằng chứng giao hàng
    public decimal PhuCap { get; set; }               // phụ cấp của đơn (giao đêm...)
    public int DiemTaiXe { get; set; }                // khách chấm tài xế 1–5 (0 = chưa chấm)
    public string NhanXetTaiXe { get; set; } = "";
    public int DiemMonAn { get; set; }                // khách chấm món ăn 1–5 (0 = chưa chấm)
    public string TheDanhGia { get; set; } = "";      // các thẻ nhận xét khách chọn, ngăn cách bằng "|"
    public string NoiDungDanhGia { get; set; } = "";  // nhận xét tự do của khách
    [System.Text.Json.Serialization.JsonIgnore] public bool DaDanhGia => DiemMonAn > 0;
    [System.Text.Json.Serialization.JsonIgnore] public bool GiaoThatBai => !string.IsNullOrEmpty(LyDoThatBai);
    public string HinhThucThanhToan { get; set; } = "Tiền mặt";
    public string MaKhuyenMai { get; set; } = "";
    public decimal GiamGia { get; set; }

    // --- bổ sung cho xử lý đơn phía gian hàng ---
    public string LyDoTuChoi { get; set; } = "";       // gian hàng từ chối / hệ thống tự hủy quá hạn: bắt buộc có lý do
    public DateTime? XacNhanLuc { get; set; }
    public DateTime? SanSangGiaoLuc { get; set; }

    /// <summary>Gian hàng phải phản hồi đơn mới trong 5 phút (quy định NV04).</summary>
    public const int PhutPhanHoiToiDa = 5;
    [System.Text.Json.Serialization.JsonIgnore]
    public bool QuaHanPhanHoi => TrangThai == TrangThaiDonHang.ChoXacNhan && (DateTime.Now - NgayDat).TotalMinutes >= PhutPhanHoiToiDa;
    [System.Text.Json.Serialization.JsonIgnore]
    public bool CoHanPhanHoi => TrangThai == TrangThaiDonHang.ChoXacNhan;
    [System.Text.Json.Serialization.JsonIgnore]
    public string HanPhanHoiText
    {
        get
        {
            var con = PhutPhanHoiToiDa - (DateTime.Now - NgayDat).TotalMinutes;
            return con <= 0 ? "⏰ Đã quá hạn phản hồi" : $"⏰ Còn {Math.Ceiling(con):0} phút để phản hồi";
        }
    }
    [System.Text.Json.Serialization.JsonIgnore] public bool CoLyDoTuChoi => TrangThai == TrangThaiDonHang.DaHuy && !string.IsNullOrWhiteSpace(LyDoTuChoi);
    [System.Text.Json.Serialization.JsonIgnore] public string LyDoTuChoiText => $"Lý do hủy: {LyDoTuChoi}";

    /// <summary>Trạng thái tài xế nhìn từ phía gian hàng: chờ tài xế nhận / tài xế đang đến lấy / đang giao.</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public string TrangThaiTaiXeText
    {
        get
        {
            var ten = string.IsNullOrWhiteSpace(TaiXeTen) ? "Tài xế" : TaiXeTen;
            var sdt = string.IsNullOrWhiteSpace(TaiXeSdt) ? "" : $" · {TaiXeSdt}";
            if (TrangThai == TrangThaiDonHang.DangLam && SanSangGiao)
                return string.IsNullOrEmpty(TaiXeId) ? "⏳ Đã bàn giao — đang chờ tài xế nhận đơn" : $"🛵 {ten} đang đến lấy món{sdt}";
            if (TrangThai == TrangThaiDonHang.DangGiao) return $"🛵 {ten} đã lấy hàng, đang giao{sdt}";
            return "";
        }
    }
    [System.Text.Json.Serialization.JsonIgnore] public bool CoTrangThaiTaiXe => TrangThaiTaiXeText.Length > 0;

    // --- giao hàng & bản đồ (ước tính theo khu vực, xem Services/GiaoHangService.cs) ---
    public double KhoangCachKm { get; set; }          // 0 = chưa xác định được khoảng cách
    public int ThoiGianDuKienPhut { get; set; }       // thời gian di chuyển ước tính quán → khách
    public string KhuVucLay { get; set; } = "";       // vd "Quận 5"
    public string KhuVucGiao { get; set; } = "";      // vd "Quận 10", "Củ Chi"
    public bool NgoaiThanh { get; set; }              // giao ra ngoại thành → có phụ phí khu vực
    public decimal PhiKhoangCach { get; set; }        // phí theo khoảng cách (đã gồm phí cơ bản)
    public decimal PhuPhiKhuVuc { get; set; }         // phụ phí khu vực
    public bool PhiDaDieuChinh { get; set; }          // tài xế đã báo khoảng cách thực tế và phí được tính lại
    public double KhoangCachThucTeKm { get; set; }

    // --- tài xế nhận đơn (khách dùng để gọi/theo dõi) ---
    public string TaiXeBienSo { get; set; } = "";
    public bool DaLayHang { get; set; }               // tài xế đã xác nhận nhận hàng từ quán
    public DateTime? LayHangLuc { get; set; }
    public double? TaiXeLat { get; set; }             // vị trí gần nhất tài xế chia sẻ
    public double? TaiXeLng { get; set; }
    public DateTime? ViTriCapNhatLuc { get; set; }
    public int SoLanGoiKhach { get; set; }            // tài xế đã gọi khách bao nhiêu lần (điều kiện báo giao thất bại)

    // --- xác nhận giao / giao thất bại ---
    /// <summary>Mã 4 số hiện trên đơn của khách; tài xế nhập để xác nhận khách đã nhận hàng. Rỗng (đơn cũ) = không yêu cầu mã.</summary>
    public string MaXacNhanGiao { get; set; } = "";
    public DateTime? GiaoThatBaiLuc { get; set; }
    public bool TinhPhiKhiThatBai { get; set; }       // tài xế vẫn nhận phí giao (lỗi do khách)

    [System.Text.Json.Serialization.JsonIgnore]
    public string NhanKetQuaGiao => GiaoThatBai ? "⚠ Giao thất bại" : "✓ Đã giao";
    /// <summary>Tiền tài xế nhận: giao thất bại do tài xế/quán thì không có phí.</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public decimal PhiTaiXeNhan => GiaoThatBai && !TinhPhiKhiThatBai ? 0 : PhiGiaoHang;
    [System.Text.Json.Serialization.JsonIgnore]
    public string NhanLichSuGiao => $"Đơn #{Id} · {NhanKetQuaGiao}";
    [System.Text.Json.Serialization.JsonIgnore]
    public string KhoangCachHienThi => KhoangCachKm > 0 ? $"{KhoangCachKm:0.#} km" : "chưa xác định";

    public decimal TongTien => ChiTiet.Sum(x => x.ThanhTien);
    /// <summary>Số tiền khách phải trả: tiền món + phí giao − giảm giá.</summary>
    public decimal TongThanhToan => Math.Max(0, TongTien + PhiGiaoHang - GiamGia);
}

/// <summary>
/// Kho dữ liệu trong bộ nhớ của app (không còn dữ liệu mẫu — tài khoản/quán/món/đơn do người dùng tạo). Khi nối server thì nạp từ
/// ASP.NET Core Web API thật (xem Services/ApiClient.cs).
/// </summary>
public static class MockData
{
    // Danh mục được lưu riêng theo từng gian hàng, ban đầu suy ra từ các món mẫu hiện có.
    public static Dictionary<string, List<string>> DanhMucTheoGianHang { get; } = new();

    public static List<string> DanhMucCuaGianHang(string gianHangId)
    {
        if (!DanhMucTheoGianHang.TryGetValue(gianHangId, out var danhMuc))
        {
            danhMuc = DanhSachMonAn.Where(m => m.GianHangId == gianHangId)
                .Select(m => m.DanhMuc).Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            DanhMucTheoGianHang[gianHangId] = danhMuc;
        }
        return danhMuc;
    }

    public static List<GianHang> DanhSachGianHang { get; } = new();

    public static List<MonAn> DanhSachMonAn { get; } = new();

    public static ObservableCollection<DonHang> LichSuDonHang { get; } = new();

    /// <summary>Tài khoản tài xế mẫu (đăng nhập demo cho luồng giao hàng).</summary>
    public static TaiXe TaiXeHienTai { get; set; } = new() { Id = "tx", HoTen = "Tài xế" };

    /// <summary>Tất cả hồ sơ tài xế (đã đăng ký) — quản trị viên xét duyệt ở AdminDriverApprovalPage.</summary>
    public static List<TaiXe> DanhSachTaiXe { get; } = new();

    /// <summary>
    /// Đơn hàng đã nấu xong, đang chờ tài xế nhận (TaiXeId == null).
    /// Sau này thay bằng GET /api/tai-xe/don-cho-nhan khi có API thật.
    /// </summary>
    public static ObservableCollection<DonHang> DonHangChoTaiXe { get; } = new();

    /// <summary>Đơn tài xế hiện đang giao (chỉ 1 đơn tại 1 thời điểm), null nếu chưa nhận đơn nào.</summary>
    public static DonHang? DonHangDangGiaoCuaTaiXe { get; set; }

    /// <summary>Đơn tài xế đã giao xong trong ca hôm nay.</summary>
    public static ObservableCollection<DonHang> DonHangDaGiaoCuaTaiXe { get; } = new();

    // ====================== LUỒNG QUẢN LÝ GIAN HÀNG (ĐỐI TÁC) ======================

    /// <summary>Gian hàng đang đăng nhập (tài khoản đối tác demo — mặc định "Cơm Tấm Sài Gòn").</summary>
    public static GianHang GianHangHienTai { get; set; } = new();

    /// <summary>Lấy danh sách món ăn thuộc về một gian hàng.</summary>
    public static List<MonAn> MonAnCuaGianHang(string gianHangId) =>
        DanhSachMonAn.Where(m => m.GianHangId == gianHangId).ToList();

    /// <summary>
    /// Hàng đợi đơn hàng của gian hàng hiện tại — độc lập với luồng tài xế/khách hàng
    /// vì mỗi vai trò có góc nhìn khác nhau trên cùng một đơn (sau này thay bằng
    /// GET /api/gian-hang/don-hang khi có API thật, backend sẽ đồng bộ trạng thái chung).
    /// </summary>
    public static ObservableCollection<DonHang> DonHangCuaGianHang { get; } = new();

    // ====================== LUỒNG QUẢN LÝ QUẢN TRỊ ======================

    /// <summary>Tài khoản quản trị viên demo (đăng nhập cổng quản trị).</summary>
    public static QuanTriVien QuanTriVienHienTai { get; set; } = new()
    {
        Id = "qt0", HoTen = "Quản trị hệ thống", Email = "admin@fooddelivery.vn", VaiTro = VaiTroQuanTri.AdminTong,
    };

    /// <summary>Tất cả tài khoản quản trị (Admin tổng + nhân viên hỗ trợ), lưu bởi <see cref="Services.QuanTriStore"/>.</summary>
    public static List<QuanTriVien> DanhSachQuanTri { get; } = new();

    /// <summary>Danh sách tài khoản khách hàng — sau này thay bằng GET /api/quan-tri/khach-hang.</summary>
    public static ObservableCollection<KhachHang> DanhSachKhachHang { get; } = new();

    /// <summary>Danh sách vấn đề/khiếu nại phát sinh — sau này thay bằng GET /api/quan-tri/van-de.</summary>
    public static ObservableCollection<VanDeHeThong> DanhSachVanDe { get; } = new();

    /// <summary>
    /// Danh sách chương trình khuyến mãi — gồm cả mã do từng gian hàng tự tạo và mã
    /// do quản trị viên tạo áp dụng toàn hệ thống. Sau này thay bằng GET /api/khuyen-mai.
    /// </summary>
    public static ObservableCollection<KhuyenMai> DanhSachKhuyenMai { get; } = new();

    /// <summary>Đánh giá khách gửi cho gian hàng (kèm phản hồi của quán) — sau này thay bằng GET /api/mobile/gian-hang/{id}/danh-gia.</summary>
    public static ObservableCollection<DanhGiaGianHang> DanhSachDanhGia { get; } = new();

    /// <summary>Thông báo gửi tới gian hàng (đơn mới, hủy đơn, đánh giá mới…).</summary>
    public static ObservableCollection<ThongBaoGianHang> DanhSachThongBao { get; } = new();

    /// <summary>
    /// Kiểm tra và áp dụng một mã khuyến mãi cho đơn hàng — dùng chung cho trang thanh toán
    /// của khách hàng. Sau này thay bằng POST /api/khuyen-mai/ap-dung khi có API thật
    /// (backend sẽ tăng SoLuongDaDung khi đơn hàng đặt thành công, không phải ngay khi nhập mã).
    /// </summary>
    public static (bool ThanhCong, decimal SoTienGiam, string ThongDiep) ApDungMaKhuyenMai(
        string maCode, string gianHangId, decimal tamTinh)
    {
        var km = DanhSachKhuyenMai.FirstOrDefault(k => string.Equals(k.MaCode, maCode, StringComparison.OrdinalIgnoreCase));
        if (km is null) return (false, 0, "Mã khuyến mãi không tồn tại");
        if (!km.DangHoatDong) return (false, 0, "Mã khuyến mãi hiện đã ngừng hoạt động");
        if (km.ChuaBatDau) return (false, 0, "Mã khuyến mãi chưa đến ngày áp dụng");
        if (!km.ConHan) return (false, 0, "Mã khuyến mãi đã hết hạn sử dụng");
        if (!km.ConLuot) return (false, 0, "Mã khuyến mãi đã hết lượt sử dụng");
        if (!km.ApDungToanHeThong && km.GianHangId != gianHangId)
            return (false, 0, "Mã khuyến mãi không áp dụng cho gian hàng này");
        if (tamTinh < km.GiaTriDonToiThieu)
            return (false, 0, $"Đơn hàng cần tối thiểu {km.GiaTriDonToiThieu:N0}đ để dùng mã này");

        var giam = km.LoaiGiam == LoaiGiamGia.PhanTram
            ? Math.Round(tamTinh * km.GiaTriGiam / 100m)
            : km.GiaTriGiam;
        if (km.LoaiGiam == LoaiGiamGia.PhanTram && km.GiamToiDa is decimal tran && giam > tran)
            giam = tran;
        if (giam > tamTinh) giam = tamTinh;

        return (true, giam, $"Áp dụng thành công: giảm {giam:N0}đ");
    }

    /// <summary>Gộp toàn bộ đơn hàng từ mọi nguồn mock hiện có — dùng cho thống kê & báo cáo toàn hệ thống.</summary>
    public static IEnumerable<DonHang> TatCaDonHangHeThong =>
        LichSuDonHang.Concat(DonHangChoTaiXe).Concat(DonHangCuaGianHang);

    /// <summary>
    /// Ước tính doanh thu (đơn đã hoàn thành) của một gian hàng, gộp từ mọi nguồn dữ liệu mock hiện có
    /// — sau này thay bằng GET /api/quan-tri/doanh-thu?gianHangId=... khi có API + CSDL thật.
    /// </summary>
    public static decimal DoanhThuUocTinh(string gianHangId)
    {
        return TatCaDonHangHeThong
            .Where(d => d.GianHangId == gianHangId && d.TrangThai == TrangThaiDonHang.HoanThanh)
            .Sum(d => d.TongTien);
    }
}

/// <summary>Tin mới / thông báo hiển thị ở trang chủ và trang "Tin mới".</summary>
public class TinTuc
{
    public string Id { get; set; } = "";
    public string TieuDe { get; set; } = "";
    public string TomTat { get; set; } = "";
    public string NoiDung { get; set; } = "";
    public string Loai { get; set; } = "HeThong";   // KhuyenMai | TinQuan | HeThong
    public string Icon { get; set; } = "📰";
    public DateTime Ngay { get; set; } = DateTime.Now;

    public string NhanLoai => Loai switch { "KhuyenMai" => "Khuyến mãi", "TinQuan" => "Quán mới", _ => "Hệ thống" };
    public string NgayHienThi
    {
        get
        {
            var d = (DateTime.Now.Date - Ngay.Date).Days;
            return d <= 0 ? "Hôm nay" : d == 1 ? "Hôm qua" : $"{d} ngày trước";
        }
    }
}

public static class TinTucData
{
    public static ObservableCollection<TinTuc> DanhSach { get; } = new();

    /// <summary>Số tin mới hơn lần xem gần nhất (dùng cho chấm đỏ ở chuông).</summary>
    public static int SoChuaXem
    {
        get
        {
            var ticks = Preferences.Default.Get("tin-xem-luc", 0L);
            var moc = new DateTime(ticks);
            return DanhSach.Count(t => t.Ngay > moc && t.Ngay.Date >= DateTime.Now.Date.AddDays(-3));
        }
    }
    public static void DanhDauDaXem() => Preferences.Default.Set("tin-xem-luc", DateTime.Now.Ticks);

    /// <summary>Dựng lại danh sách tin từ dữ liệu thật: khuyến mãi đang chạy + gian hàng mới tham gia trong 14 ngày.</summary>
    public static void LamMoi()
    {
        var ds = new List<TinTuc>();
        foreach (var k in MockData.DanhSachKhuyenMai.Where(k => k.DangHoatDong && k.ConHan && !k.ChuaBatDau && k.ConLuot))
        {
            ds.Add(new TinTuc
            {
                Id = "tin-km-" + k.Id,
                TieuDe = k.TenChuongTrinh,
                TomTat = k.GiaTriGiamHienThi + (k.GiaTriDonToiThieu > 0 ? $" cho đơn từ {k.GiaTriDonToiThieu:N0}đ" : ""),
                NoiDung = (string.IsNullOrWhiteSpace(k.MoTa) ? "" : k.MoTa + "\n") + $"Mã: {k.MaCode}. Áp dụng: {k.PhamViHienThi}.",
                Loai = "KhuyenMai", Icon = "🎁", Ngay = k.NgayBatDau,
            });
        }
        foreach (var g in MockData.DanhSachGianHang.Where(g => g.TrangThaiDangKy == TrangThaiDangKyGianHang.DaDuyet
                                                              && (DateTime.Now - g.NgayDangKy).TotalDays <= 14))
        {
            ds.Add(new TinTuc
            {
                Id = "tin-gh-" + g.Id,
                TieuDe = $"{g.TenGianHang} vừa tham gia",
                TomTat = "Gian hàng mới trên nền tảng, đặt thử ngay!",
                NoiDung = $"{g.TenGianHang} đã mở bán trên nền tảng.",
                Loai = "TinQuan", Icon = "🏪", Ngay = g.NgayDangKy,
            });
        }
        DanhSach.Clear();
        foreach (var t in ds.OrderByDescending(t => t.Ngay)) DanhSach.Add(t);
    }
}
