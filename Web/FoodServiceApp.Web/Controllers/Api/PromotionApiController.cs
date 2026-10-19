using System.Security.Claims;
using FoodServiceApp.Web.Data;
using FoodServiceApp.Web.Models.Entities;
using FoodServiceApp.Web.Models.ViewModels;
using FoodServiceApp.Web.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FoodServiceApp.Web.Controllers.Api;

[ApiController]
[Route("api/mobile/promotions/{role}")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "GianHang,QuanTri")]
public class PromotionApiController(AppDbContext db) : ControllerBase
{
    private async Task<(bool allowed, int? owner)> AccessAsync(string role, bool write)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var account)) return (false, null);
        if (!await db.TaiKhoans.AnyAsync(x => x.MaTK == account && x.TrangThai)) return (false, null);
        if (role == "admin" && User.IsInRole("QuanTri"))
            return (await db.QuanTris.AnyAsync(x => x.MaTK == account && (!write || x.CapQuyenHan == "SuperAdmin")), null);
        if (role != "vendor" || !User.IsInRole("GianHang")) return (false, null);
        var owner = await db.GianHangs.Where(x => x.MaTK == account && x.TrangThaiDuyet == "DaDuyet").Select(x => (int?)x.MaGianHang).SingleOrDefaultAsync();
        return (owner.HasValue, owner);
    }

    [HttpGet]
    public async Task<IActionResult> List(string role)
    {
        var access = await AccessAsync(role, false); if (!access.allowed) return Forbid();
        var rows = await db.KhuyenMais.Where(x => x.MaGianHang == access.owner).OrderByDescending(x => x.NgayBatDau).ToListAsync();
        var foods = await db.KhuyenMaiMonAns.ToListAsync(); var categories = await db.KhuyenMaiDanhMucs.ToListAsync(); var stores = await db.KhuyenMaiGianHangs.ToListAsync();
        return Ok(rows.Select(x => new {
            id = x.MaKhuyenMai, x.MaCode, x.TenChuongTrinh, x.MoTa, x.LoaiGiam,
            giaTriGiam = x.PhanTramGiam ?? x.SoTienGiam ?? 0, x.GiamToiDa, x.GiaTriDonToiThieu,
            soLuongToiDa = x.SoLuongToiDa ?? int.MaxValue, x.SoLuongDaDung, x.MoiKhachMotLan,
            x.NgayBatDau, x.NgayKetThuc, dangHoatDong = x.TrangThai, gianHangId = x.MaGianHang,
            monApDung = foods.Where(y => y.MaKhuyenMai == x.MaKhuyenMai).Select(y => y.MaMonAn.ToString()),
            danhMucApDung = categories.Where(y => y.MaKhuyenMai == x.MaKhuyenMai).Select(y => y.MaDanhMuc.ToString()),
            gianHangApDung = stores.Where(y => y.MaKhuyenMai == x.MaKhuyenMai).Select(y => y.MaGianHang.ToString())
        }));
    }

    [HttpGet("options")]
    public async Task<IActionResult> Options(string role)
    {
        var access = await AccessAsync(role, false); if (!access.allowed) return Forbid();
        var categories = await db.DanhMucs.Where(x => access.owner == null || x.MaGianHang == access.owner).Select(x => new { id = x.MaDanhMuc, name = x.TenDanhMuc + " (" + x.MaGianHang + ")" }).ToListAsync();
        var foods = await db.MonAns.Where(x => access.owner == null || x.DanhMuc!.MaGianHang == access.owner).Select(x => new { id = x.MaMonAn, name = x.TenMon + " — " + x.DanhMuc!.TenDanhMuc }).ToListAsync();
        var stores = await db.GianHangs.Where(x => access.owner == null).Select(x => new { id = x.MaGianHang, name = x.TenCuaHang }).ToListAsync();
        return Ok(new { categories, foods, stores });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(string role, int id)
    {
        var access = await AccessAsync(role, false); if (!access.allowed) return Forbid();
        var row = await db.KhuyenMais.SingleOrDefaultAsync(x => x.MaKhuyenMai == id && x.MaGianHang == access.owner);
        return row == null ? NotFound() : Ok(await new PromotionManagementService(db).FormAsync(row));
    }

    [HttpPost]
    public Task<IActionResult> Create(string role, KhuyenMaiFormViewModel form) => Save(role, null, form);
    [HttpPut("{id:int}")]
    public Task<IActionResult> Edit(string role, int id, KhuyenMaiFormViewModel form) => Save(role, id, form);

    private async Task<IActionResult> Save(string role, int? id, KhuyenMaiFormViewModel form)
    {
        var access = await AccessAsync(role, true); if (!access.allowed) return Forbid();
        await using var transaction = await db.Database.BeginTransactionAsync();
        KhuyenMai? row = id.HasValue
            ? await db.KhuyenMais.FromSqlInterpolated($"SELECT * FROM dbo.KhuyenMai WITH (UPDLOCK,HOLDLOCK) WHERE MaKhuyenMai = {id.Value}").SingleOrDefaultAsync()
            : new KhuyenMai { MaGianHang = access.owner };
        if (row == null || row.MaGianHang != access.owner) return NotFound();
        var service = new PromotionManagementService(db);
        var error = await service.ValidateAsync(form, access.owner, id);
        if (error != null) return BadRequest(new { thongDiep = error });
        if (!id.HasValue) db.KhuyenMais.Add(row);
        try { await service.SaveAsync(row, form); await transaction.CommitAsync(); }
        catch (DbUpdateException) { return Conflict(new { thongDiep = "Mã voucher đã tồn tại hoặc dữ liệu phạm vi đã thay đổi. Vui lòng tải lại." }); }
        return Ok(new { id = row.MaKhuyenMai });
    }

    public record StatusRequest(bool DangHoatDong);
    [HttpPut("{id:int}/status")]
    public async Task<IActionResult> Status(string role, int id, StatusRequest request)
    {
        var access = await AccessAsync(role, true); if (!access.allowed) return Forbid();
        var row = await db.KhuyenMais.SingleOrDefaultAsync(x => x.MaKhuyenMai == id && x.MaGianHang == access.owner);
        if (row == null) return NotFound();
        row.TrangThai = request.DangHoatDong;
        await db.SaveChangesAsync(); return Ok(new { row.TrangThai });
    }
}
