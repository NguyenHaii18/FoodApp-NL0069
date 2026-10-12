using FoodApp.Maui.Models;

namespace FoodApp.Maui.Services;

public enum Quyen
{
    XemTaiKhoan,        // xem danh sách khách / gian hàng / tài xế
    KhoaMoTaiKhoan,     // khóa, mở khóa tài khoản
    DuyetHoSo,          // duyệt / từ chối hồ sơ gian hàng, tài xế
    XuLyVanDe,          // xử lý khiếu nại, sự cố
    HoanTien,           // hủy đơn và hoàn tiền theo khiếu nại
    QuanLyKhuyenMai,    // chương trình khuyến mãi toàn hệ thống
    XemThongKe,         // thống kê & báo cáo
    XemNhatKy,          // nhật ký thao tác
    QuanLyNhanVien,     // tạo / khóa / đổi vai trò tài khoản quản trị
}

/// <summary>Ma trận quyền theo vai trò quản trị. Admin tổng có mọi quyền.</summary>
public static class PhanQuyenService
{
    private static readonly HashSet<Quyen> QuyenNhanVienHoTro = new()
    {
        Quyen.XemTaiKhoan, Quyen.XuLyVanDe, Quyen.HoanTien, Quyen.XemThongKe,
    };

    public static bool Co(Quyen q) =>
        MockData.QuanTriVienHienTai.VaiTro == VaiTroQuanTri.AdminTong || QuyenNhanVienHoTro.Contains(q);

    public static string Ten(Quyen q) => q switch
    {
        Quyen.XemTaiKhoan => "xem tài khoản",
        Quyen.KhoaMoTaiKhoan => "khóa / mở khóa tài khoản",
        Quyen.DuyetHoSo => "duyệt / từ chối hồ sơ",
        Quyen.XuLyVanDe => "xử lý khiếu nại",
        Quyen.HoanTien => "hủy đơn và hoàn tiền",
        Quyen.QuanLyKhuyenMai => "quản lý khuyến mãi",
        Quyen.XemThongKe => "xem thống kê",
        Quyen.XemNhatKy => "xem nhật ký thao tác",
        Quyen.QuanLyNhanVien => "quản lý tài khoản quản trị",
        _ => q.ToString(),
    };

    /// <summary>Kiểm tra quyền; nếu thiếu thì báo cho người dùng và ghi vào nhật ký.</summary>
    public static async Task<bool> KiemTraAsync(Page trang, Quyen q)
    {
        if (Co(q)) return true;
        NhatKyService.GhiQuanTri("Bị từ chối do thiếu quyền", Ten(q), thanhCong: false);
        await trang.DisplayAlert("Không đủ quyền",
            $"Tài khoản \"{MockData.QuanTriVienHienTai.VaiTro.Nhan()}\" không có quyền {Ten(q)}. Hãy liên hệ Admin tổng.", "Đóng");
        return false;
    }
}
