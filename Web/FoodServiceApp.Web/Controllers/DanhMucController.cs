using FoodServiceApp.Web.Data;
using FoodServiceApp.Web.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FoodServiceApp.Web.Controllers;

public class DanhMucController : GianHangBaseController
{
    private readonly AppDbContext _db;
    public DanhMucController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index() => View(await _db.DanhMucs
        .Include(d => d.MonAns)
        .Where(d => d.MaGianHang == CurrentGianHangId)
        .OrderBy(d => d.TenDanhMuc)
        .ToListAsync());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string tenDanhMuc)
    {
        var ten = tenDanhMuc?.Trim() ?? "";
        if (ten.Length == 0 || ten.Length > 100)
            TempData["LoiThongBao"] = "Tên danh mục phải có từ 1 đến 100 ký tự.";
        else if (await _db.DanhMucs.AnyAsync(d => d.MaGianHang == CurrentGianHangId && d.TenDanhMuc.ToLower() == ten.ToLower()))
            TempData["LoiThongBao"] = "Danh mục này đã tồn tại.";
        else
        {
            _db.DanhMucs.Add(new DanhMuc { MaGianHang = CurrentGianHangId, TenDanhMuc = ten });
            await _db.SaveChangesAsync();
            TempData["ThongBao"] = $"Đã thêm danh mục “{ten}”.";
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Rename(int id, string tenDanhMuc)
    {
        var danhMuc = await _db.DanhMucs.FirstOrDefaultAsync(d => d.MaDanhMuc == id && d.MaGianHang == CurrentGianHangId);
        if (danhMuc == null) return NotFound();
        var ten = tenDanhMuc?.Trim() ?? "";
        if (ten.Length == 0 || ten.Length > 100)
            TempData["LoiThongBao"] = "Tên danh mục phải có từ 1 đến 100 ký tự.";
        else if (await _db.DanhMucs.AnyAsync(d => d.MaGianHang == CurrentGianHangId && d.MaDanhMuc != id && d.TenDanhMuc.ToLower() == ten.ToLower()))
            TempData["LoiThongBao"] = "Danh mục này đã tồn tại.";
        else
        {
            danhMuc.TenDanhMuc = ten;
            await _db.SaveChangesAsync();
            TempData["ThongBao"] = "Đã đổi tên danh mục.";
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var danhMuc = await _db.DanhMucs.Include(d => d.MonAns)
            .FirstOrDefaultAsync(d => d.MaDanhMuc == id && d.MaGianHang == CurrentGianHangId);
        if (danhMuc == null) return NotFound();
        if (danhMuc.MonAns.Count != 0)
            TempData["LoiThongBao"] = "Chỉ có thể xóa danh mục trống. Hãy chuyển hoặc xóa các món trước.";
        else
        {
            _db.DanhMucs.Remove(danhMuc);
            await _db.SaveChangesAsync();
            TempData["ThongBao"] = "Đã xóa danh mục.";
        }
        return RedirectToAction(nameof(Index));
    }
}
