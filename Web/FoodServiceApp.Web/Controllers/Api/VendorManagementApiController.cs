using FoodServiceApp.Web.Data;
using FoodServiceApp.Web.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FoodServiceApp.Web.Controllers.Api;

[ApiController]
[Route("api/mobile/gian-hang/{id:int}")]
public class VendorManagementApiController : VendorApiBaseController
{
    private readonly AppDbContext _db;
    public VendorManagementApiController(AppDbContext db) => _db = db;

    private async Task<GianHang?> OwnStore(int id) => await _db.GianHangs.Include(g => g.TaiKhoan)
        .FirstOrDefaultAsync(g => g.MaGianHang == id && g.MaTK == CurrentMaTK && g.TaiKhoan != null && g.TaiKhoan.TrangThai);
    private async Task<GianHang?> OwnApprovedStore(int id) => await _db.GianHangs.Include(g => g.TaiKhoan)
        .FirstOrDefaultAsync(g => g.MaGianHang == id && g.MaTK == CurrentMaTK && g.TrangThaiDuyet == "DaDuyet" && g.TaiKhoan != null && g.TaiKhoan.TrangThai);
    private static IActionResult Error(string message, int status = 400) => new ObjectResult(new { thongDiep = message }) { StatusCode = status };

    public record ProfileRequest(string? TenGianHang, string? DiaChi, string? SoDienThoai, string? Email, string? MoTa, string? HinhAnh);
    public record CategoryRequest(string? TenDanhMuc);
    public record MenuOptionRequest(string? Loai, string? Ten, decimal GiaThem);
    public record MenuRequest(string? TenMon, string? DanhMuc, decimal Gia, decimal GiaVon, string? MoTa, string? HinhAnh, string? TinhTrang, bool ChoGhiChu, List<MenuOptionRequest>? TuyChon);
    public record StatusRequest(string? TinhTrang);

    [HttpGet("ho-so")]
    public async Task<IActionResult> Profile(int id)
    {
        var g = await OwnStore(id);
        if (g == null) return Error("Không tìm thấy gian hàng.", 404);
        return Ok(new { id = g.MaGianHang, tenGianHang = g.TenCuaHang, diaChi = g.DiaChi, soDienThoai = g.SDT,
            email = g.Email ?? "", moTa = g.MoTa ?? "", hinhAnh = g.HinhAnh ?? "", gioMoCua = $"{g.GioMoCua:hh\\:mm}",
            gioDongCua = $"{g.GioDongCua:hh\\:mm}", dangNhanDon = g.DangNhanDon, dangMoCua = g.TrangThaiKinhDoanh == "DangMo",
            trangThaiDangKy = g.TrangThaiDuyet });
    }

    [HttpPut("ho-so")]
    public async Task<IActionResult> UpdateProfile(int id, ProfileRequest request)
    {
        var g = await OwnStore(id);
        if (g == null) return Error("Không tìm thấy gian hàng.", 404);
        var name = request.TenGianHang?.Trim() ?? "";
        var address = request.DiaChi?.Trim() ?? "";
        var phone = request.SoDienThoai?.Trim() ?? "";
        if (name.Length is < 2 or > 150 || address.Length is < 5 or > 300 || phone.Length is < 8 or > 20)
            return Error("Tên, địa chỉ hoặc số điện thoại không hợp lệ.");
        if (!string.IsNullOrWhiteSpace(request.Email) && !new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(request.Email))
            return Error("Email không hợp lệ.");
        g.TenCuaHang = name; g.DiaChi = address; g.SDT = phone; g.Email = request.Email?.Trim();
        g.MoTa = request.MoTa?.Trim();
        if (!string.IsNullOrWhiteSpace(request.HinhAnh)) g.HinhAnh = request.HinhAnh.Trim();
        await _db.SaveChangesAsync();
        return Ok(new { message = "Đã cập nhật hồ sơ gian hàng." });
    }

    [HttpGet("danh-muc")]
    public async Task<IActionResult> Categories(int id)
    {
        if (await OwnApprovedStore(id) == null) return Error("Gian hàng chưa được duyệt hoặc không tồn tại.", 403);
        return Ok(await _db.DanhMucs.Where(d => d.MaGianHang == id).OrderBy(d => d.TenDanhMuc)
            .Select(d => new { id = d.MaDanhMuc, tenDanhMuc = d.TenDanhMuc, soMon = d.MonAns.Count }).ToListAsync());
    }

    [HttpPost("danh-muc")]
    public async Task<IActionResult> CreateCategory(int id, CategoryRequest request)
    {
        if (await OwnApprovedStore(id) == null) return Error("Gian hàng chưa được duyệt hoặc không tồn tại.", 403);
        var name = request.TenDanhMuc?.Trim() ?? "";
        if (name.Length is < 1 or > 100) return Error("Tên danh mục phải có từ 1 đến 100 ký tự.");
        if (await _db.DanhMucs.AnyAsync(d => d.MaGianHang == id && d.TenDanhMuc.ToLower() == name.ToLower())) return Error("Danh mục đã tồn tại.");
        var category = new DanhMuc { MaGianHang = id, TenDanhMuc = name };
        _db.DanhMucs.Add(category); await _db.SaveChangesAsync();
        return Ok(new { id = category.MaDanhMuc, tenDanhMuc = category.TenDanhMuc });
    }

    [HttpPut("danh-muc/{categoryId:int}")]
    public async Task<IActionResult> RenameCategory(int id, int categoryId, CategoryRequest request)
    {
        if (await OwnApprovedStore(id) == null) return Error("Gian hàng chưa được duyệt hoặc không tồn tại.", 403);
        var category = await _db.DanhMucs.FirstOrDefaultAsync(d => d.MaDanhMuc == categoryId && d.MaGianHang == id);
        if (category == null) return Error("Không tìm thấy danh mục.", 404);
        var name = request.TenDanhMuc?.Trim() ?? "";
        if (name.Length is < 1 or > 100) return Error("Tên danh mục không hợp lệ.");
        if (await _db.DanhMucs.AnyAsync(d => d.MaGianHang == id && d.MaDanhMuc != categoryId && d.TenDanhMuc.ToLower() == name.ToLower())) return Error("Danh mục đã tồn tại.");
        category.TenDanhMuc = name; await _db.SaveChangesAsync(); return Ok(new { message = "Đã đổi tên danh mục." });
    }

    [HttpDelete("danh-muc/{categoryId:int}")]
    public async Task<IActionResult> DeleteCategory(int id, int categoryId)
    {
        if (await OwnApprovedStore(id) == null) return Error("Gian hàng chưa được duyệt hoặc không tồn tại.", 403);
        var category = await _db.DanhMucs.Include(d => d.MonAns).FirstOrDefaultAsync(d => d.MaDanhMuc == categoryId && d.MaGianHang == id);
        if (category == null) return Error("Không tìm thấy danh mục.", 404);
        if (category.MonAns.Count != 0) return Error("Chỉ có thể xóa danh mục trống.");
        _db.DanhMucs.Remove(category); await _db.SaveChangesAsync(); return Ok(new { message = "Đã xóa danh mục." });
    }

    [HttpGet("thuc-don")]
    public async Task<IActionResult> Menu(int id)
    {
        if (await OwnApprovedStore(id) == null) return Error("Gian hàng chưa được duyệt hoặc không tồn tại.", 403);
        var foods = await _db.MonAns.Include(m => m.DanhMuc).Where(m => m.DanhMuc!.MaGianHang == id)
            .OrderBy(m => m.DanhMuc!.TenDanhMuc).ThenBy(m => m.TenMon).ToListAsync();
        var foodIds = foods.Select(m => m.MaMonAn).ToList();
        var options = await _db.TuyChonMons.Where(x => foodIds.Contains(x.MaMonAn)).ToListAsync();
        return Ok(foods.Select(m => new { id = m.MaMonAn, tenMon = m.TenMon, gia = m.Gia, giaVon = m.GiaVon, hinhAnh = m.HinhAnh ?? "🍲",
            danhMuc = m.DanhMuc!.TenDanhMuc, gianHangId = id, conHang = m.TinhTrang == "ConHang", tamNgungBan = m.TinhTrang == "TamNgung",
            choGhiChu = m.ChoGhiChu, moTa = m.MoTa ?? "", tuyChon = options.Where(x => x.MaMonAn == m.MaMonAn)
                .Select(x => new { loai = x.Loai, ten = x.TenTuyChon, giaThem = x.GiaThem }) }));
    }

    [HttpPost("thuc-don")]
    public async Task<IActionResult> CreateMenuItem(int id, MenuRequest request)
    {
        if (await OwnApprovedStore(id) == null) return Error("Gian hàng chưa được duyệt hoặc không tồn tại.", 403);
        var validation = await ValidateMenu(id, request); if (validation.Error != null) return validation.Error;
        var m = new MonAn { MaDanhMuc = validation.Category!.MaDanhMuc, TenMon = request.TenMon!.Trim(), Gia = request.Gia,
            GiaVon = request.GiaVon, MoTa = request.MoTa?.Trim(), HinhAnh = request.HinhAnh?.Trim(), TinhTrang = NormalizeStatus(request.TinhTrang), ChoGhiChu = request.ChoGhiChu };
        _db.MonAns.Add(m); await _db.SaveChangesAsync();
        SaveOptions(m.MaMonAn, request.TuyChon); await _db.SaveChangesAsync();
        return Ok(new { id = m.MaMonAn });
    }

    [HttpPut("thuc-don/{menuId:int}")]
    public async Task<IActionResult> UpdateMenuItem(int id, int menuId, MenuRequest request)
    {
        if (await OwnApprovedStore(id) == null) return Error("Gian hàng chưa được duyệt hoặc không tồn tại.", 403);
        var m = await _db.MonAns.Include(x => x.DanhMuc).FirstOrDefaultAsync(x => x.MaMonAn == menuId && x.DanhMuc!.MaGianHang == id);
        if (m == null) return Error("Không tìm thấy món.", 404);
        var validation = await ValidateMenu(id, request, menuId); if (validation.Error != null) return validation.Error;
        m.MaDanhMuc = validation.Category!.MaDanhMuc; m.TenMon = request.TenMon!.Trim(); m.Gia = request.Gia; m.GiaVon = request.GiaVon;
        m.MoTa = request.MoTa?.Trim(); if (!string.IsNullOrWhiteSpace(request.HinhAnh)) m.HinhAnh = request.HinhAnh.Trim();
        m.TinhTrang = NormalizeStatus(request.TinhTrang); m.ChoGhiChu = request.ChoGhiChu;
        _db.TuyChonMons.RemoveRange(_db.TuyChonMons.Where(x => x.MaMonAn == menuId));
        SaveOptions(menuId, request.TuyChon); await _db.SaveChangesAsync();
        return Ok(new { message = "Đã cập nhật món." });
    }

    [HttpPut("thuc-don/{menuId:int}/trang-thai")]
    public async Task<IActionResult> UpdateStatus(int id, int menuId, StatusRequest request)
    {
        if (await OwnApprovedStore(id) == null) return Error("Gian hàng chưa được duyệt hoặc không tồn tại.", 403);
        var m = await _db.MonAns.Include(x => x.DanhMuc).FirstOrDefaultAsync(x => x.MaMonAn == menuId && x.DanhMuc!.MaGianHang == id);
        if (m == null) return Error("Không tìm thấy món.", 404);
        m.TinhTrang = NormalizeStatus(request.TinhTrang); await _db.SaveChangesAsync(); return Ok(new { tinhTrang = m.TinhTrang });
    }

    [HttpDelete("thuc-don/{menuId:int}")]
    public async Task<IActionResult> DeleteMenuItem(int id, int menuId)
    {
        if (await OwnApprovedStore(id) == null) return Error("Gian hàng chưa được duyệt hoặc không tồn tại.", 403);
        var m = await _db.MonAns.Include(x => x.DanhMuc).FirstOrDefaultAsync(x => x.MaMonAn == menuId && x.DanhMuc!.MaGianHang == id);
        if (m == null) return Error("Không tìm thấy món.", 404);
        if (await _db.ChiTietDonHangs.AnyAsync(c => c.MaMonAn == menuId)) { m.TinhTrang = "TamNgung"; await _db.SaveChangesAsync(); return Ok(new { message = "Món đã có lịch sử đơn nên được chuyển sang tạm ngừng." }); }
        _db.TuyChonMons.RemoveRange(_db.TuyChonMons.Where(x => x.MaMonAn == menuId)); _db.MonAns.Remove(m); await _db.SaveChangesAsync();
        return Ok(new { message = "Đã xóa món." });
    }

    private async Task<(DanhMuc? Category, IActionResult? Error)> ValidateMenu(int id, MenuRequest r, int excludedId = 0)
    {
        var name = r.TenMon?.Trim() ?? ""; var categoryName = r.DanhMuc?.Trim() ?? "";
        if (name.Length is < 1 or > 150 || r.Gia <= 0 || r.Gia > 100000000 || r.GiaVon < 0) return (null, Error("Thông tin tên món, giá bán hoặc giá vốn không hợp lệ."));
        var category = await _db.DanhMucs.FirstOrDefaultAsync(d => d.MaGianHang == id && d.TenDanhMuc.ToLower() == categoryName.ToLower());
        if (category == null) return (null, Error("Danh mục không thuộc gian hàng."));
        if (await _db.MonAns.AnyAsync(m => m.MaMonAn != excludedId && m.MaDanhMuc == category.MaDanhMuc && m.TenMon.ToLower() == name.ToLower())) return (null, Error("Tên món đã tồn tại trong danh mục."));
        return (category, null);
    }

    private static string NormalizeStatus(string? status) => status is "HetHang" or "TamNgung" ? status : "ConHang";

    private void SaveOptions(int menuId, IEnumerable<MenuOptionRequest>? options)
    {
        foreach (var option in (options ?? []).Take(50))
        {
            var type = option.Loai is "Size" or "Topping" or "MucCay" ? option.Loai : "Topping";
            var name = option.Ten?.Trim() ?? "";
            if (name.Length == 0 || name.Length > 100 || option.GiaThem < 0 || option.GiaThem > 100000000) continue;
            _db.TuyChonMons.Add(new TuyChonMon { MaMonAn = menuId, Loai = type, TenTuyChon = name, GiaThem = option.GiaThem });
        }
    }
}
