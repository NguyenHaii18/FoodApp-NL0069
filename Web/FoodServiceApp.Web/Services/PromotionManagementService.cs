using FoodServiceApp.Web.Data;
using FoodServiceApp.Web.Models.Entities;
using FoodServiceApp.Web.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace FoodServiceApp.Web.Services;

public sealed class PromotionManagementService(AppDbContext db)
{
    public static List<int> Ids(string? value) => (value ?? "").Split([',', ';', ' '], StringSplitOptions.RemoveEmptyEntries)
        .Select(x => int.TryParse(x, out var id) ? id : -1).Distinct().ToList();

    public async Task<string?> ValidateAsync(KhuyenMaiFormViewModel vm, int? owner, int? id = null)
    {
        vm.MaCode = (vm.MaCode ?? "").Trim().ToUpperInvariant();
        if (vm.MaCode.Length is < 1 or > 50 || vm.MaCode.Any(char.IsWhiteSpace)) return "Mã voucher dài 1–50 ký tự, không chứa khoảng trắng.";
        if (string.IsNullOrWhiteSpace(vm.TenChuongTrinh) || vm.TenChuongTrinh.Length > 150) return "Tên chương trình cần có 1–150 ký tự.";
        if (vm.NgayKetThuc <= vm.NgayBatDau) return "Ngày kết thúc phải sau ngày bắt đầu.";
        if (vm.LoaiGiam is not ("PhanTram" or "SoTien" or "GiamPhiVanChuyen")) return "Loại giảm không hợp lệ.";
        if (vm.GiaTriGiam < 0 || (vm.LoaiGiam == "PhanTram" && (vm.GiaTriGiam < 1 || vm.GiaTriGiam > 100)) || (vm.LoaiGiam == "SoTien" && vm.GiaTriGiam == 0)) return "Giá trị giảm không hợp lệ (phí giao: 0 = miễn phí).";
        if (vm.GiaTriDonToiThieu < 0 || vm.GiamToiDa is <= 0 || vm.SoLuongToiDa is <= 0) return "Điều kiện tối thiểu hoặc giới hạn không hợp lệ.";
        if (await db.KhuyenMais.AnyAsync(x => x.MaCode == vm.MaCode && x.MaKhuyenMai != id)) return "Mã voucher đã tồn tại.";
        if (id.HasValue && vm.SoLuongToiDa.HasValue && await db.KhuyenMais.AnyAsync(x => x.MaKhuyenMai == id && x.SoLuongDaDung > vm.SoLuongToiDa)) return "Giới hạn không được nhỏ hơn số lượt đã dùng.";
        var stores = Ids(vm.DsGianHang); var categories = Ids(vm.DsDanhMuc); var foods = Ids(vm.DsMonAn);
        if (owner.HasValue && stores.Count > 0) return "Gian hàng không được gán phạm vi gian hàng khác.";
        if (await db.GianHangs.CountAsync(x => stores.Contains(x.MaGianHang)) != stores.Count) return "Gian hàng áp dụng không hợp lệ.";
        if (await db.DanhMucs.CountAsync(x => categories.Contains(x.MaDanhMuc) && (owner == null || x.MaGianHang == owner)) != categories.Count) return "Danh mục không tồn tại hoặc không thuộc gian hàng của bạn.";
        if (await db.MonAns.CountAsync(x => foods.Contains(x.MaMonAn) && (owner == null || x.DanhMuc!.MaGianHang == owner)) != foods.Count) return "Món ăn không tồn tại hoặc không thuộc gian hàng của bạn.";
        return null;
    }

    public async Task SaveAsync(KhuyenMai entity, KhuyenMaiFormViewModel vm)
    {
        entity.MaCode = vm.MaCode.Trim().ToUpperInvariant();
        entity.TenChuongTrinh = vm.TenChuongTrinh?.Trim(); entity.MoTa = vm.MoTa;
        entity.DieuKienApDung = vm.DieuKienApDung; entity.LoaiGiam = vm.LoaiGiam;
        entity.PhanTramGiam = vm.LoaiGiam == "PhanTram" ? vm.GiaTriGiam : null;
        entity.SoTienGiam = vm.LoaiGiam == "PhanTram" ? null : vm.GiaTriGiam;
        entity.GiamToiDa = vm.LoaiGiam == "PhanTram" ? vm.GiamToiDa : null;
        entity.GiaTriDonToiThieu = vm.GiaTriDonToiThieu; entity.SoLuongToiDa = vm.SoLuongToiDa;
        entity.MoiKhachMotLan = vm.MoiKhachMotLan; entity.NgayBatDau = vm.NgayBatDau;
        entity.NgayKetThuc = vm.NgayKetThuc; entity.TrangThai = vm.TrangThai;
        await db.SaveChangesAsync();
        // Save removals first so unchanged composite keys can be reinserted safely in this transaction.
        db.KhuyenMaiMonAns.RemoveRange(await db.KhuyenMaiMonAns.Where(x => x.MaKhuyenMai == entity.MaKhuyenMai).ToListAsync());
        db.KhuyenMaiDanhMucs.RemoveRange(await db.KhuyenMaiDanhMucs.Where(x => x.MaKhuyenMai == entity.MaKhuyenMai).ToListAsync());
        db.KhuyenMaiGianHangs.RemoveRange(await db.KhuyenMaiGianHangs.Where(x => x.MaKhuyenMai == entity.MaKhuyenMai).ToListAsync());
        await db.SaveChangesAsync();
        foreach (var id in Ids(vm.DsMonAn)) db.KhuyenMaiMonAns.Add(new() { MaKhuyenMai = entity.MaKhuyenMai, MaMonAn = id });
        foreach (var id in Ids(vm.DsDanhMuc)) db.KhuyenMaiDanhMucs.Add(new() { MaKhuyenMai = entity.MaKhuyenMai, MaDanhMuc = id });
        foreach (var id in Ids(vm.DsGianHang)) db.KhuyenMaiGianHangs.Add(new() { MaKhuyenMai = entity.MaKhuyenMai, MaGianHang = id });
        await db.SaveChangesAsync();
    }

    public async Task<KhuyenMaiFormViewModel> FormAsync(KhuyenMai k) => new()
    {
        MaKhuyenMai = k.MaKhuyenMai, MaCode = k.MaCode, TenChuongTrinh = k.TenChuongTrinh, MoTa = k.MoTa,
        DieuKienApDung = k.DieuKienApDung, LoaiGiam = k.LoaiGiam, GiaTriGiam = k.PhanTramGiam ?? k.SoTienGiam ?? 0,
        GiamToiDa = k.GiamToiDa, GiaTriDonToiThieu = k.GiaTriDonToiThieu, SoLuongToiDa = k.SoLuongToiDa,
        MoiKhachMotLan = k.MoiKhachMotLan, NgayBatDau = k.NgayBatDau, NgayKetThuc = k.NgayKetThuc, TrangThai = k.TrangThai,
        DsMonAn = string.Join(",", await db.KhuyenMaiMonAns.Where(x => x.MaKhuyenMai == k.MaKhuyenMai).Select(x => x.MaMonAn).ToListAsync()),
        DsDanhMuc = string.Join(",", await db.KhuyenMaiDanhMucs.Where(x => x.MaKhuyenMai == k.MaKhuyenMai).Select(x => x.MaDanhMuc).ToListAsync()),
        DsGianHang = string.Join(",", await db.KhuyenMaiGianHangs.Where(x => x.MaKhuyenMai == k.MaKhuyenMai).Select(x => x.MaGianHang).ToListAsync())
    };
}
