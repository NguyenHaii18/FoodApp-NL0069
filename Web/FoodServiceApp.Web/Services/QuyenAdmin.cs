using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using FoodServiceApp.Web.Data;

namespace FoodServiceApp.Web.Services
{
    /// <summary>
    /// Phân quyền chi tiết theo chức năng. Mỗi quản trị viên có CapQuyenHan (mặc định) và có thể được Admin tổng
    /// ghi đè bằng QuanTri.QuyenChiTiet (danh sách khóa cách nhau dấu phẩy). SuperAdmin luôn có mọi quyền.
    /// </summary>
    public static class QuyenAdmin
    {
        public static readonly (string Khoa, string Ten)[] TatCa =
        {
            ("DonHang", "Đơn hàng & điều phối"), ("ThanhToan", "Giao dịch & đối soát"), ("HoanTien", "Duyệt hoàn tiền"),
            ("DanhGia", "Kiểm duyệt đánh giá"), ("ThongBao", "Thông báo & món ăn"),
        };

        public static readonly string[] MacDinhQuanLy = { "DonHang", "ThanhToan", "HoanTien", "DanhGia", "ThongBao" };
        public static readonly string[] MacDinhNhanVien = Array.Empty<string>();   // chỉ xem + xử lý phản hồi/sự cố

        public static HashSet<string> Hieu(string cap, string? chiTiet)
        {
            if (cap == "SuperAdmin") return TatCa.Select(t => t.Khoa).ToHashSet();
            if (!string.IsNullOrWhiteSpace(chiTiet))
                return chiTiet.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToHashSet();
            return (cap == "QuanLy" ? MacDinhQuanLy : MacDinhNhanVien).ToHashSet();
        }

        public static async Task<bool> CoAsync(AppDbContext db, int maTK, string khoa)
        {
            var q = await db.QuanTris.AsNoTracking().Where(x => x.MaTK == maTK).Select(x => new { x.CapQuyenHan, x.QuyenChiTiet }).FirstOrDefaultAsync();
            return q != null && Hieu(q.CapQuyenHan, q.QuyenChiTiet).Contains(khoa);
        }

        public static Task<bool> CoAsync(AppDbContext db, ClaimsPrincipal user, string khoa) =>
            int.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? CoAsync(db, id, khoa) : Task.FromResult(false);
    }
}
