using FoodServiceApp.Web.Data;
using FoodServiceApp.Web.Models.Entities;
using FoodServiceApp.Web.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FoodServiceApp.Web.Controllers;

public class ChienDichQuangCaoController(AppDbContext db) : GianHangBaseController
{
    public async Task<IActionResult> Index() => View(await db.ChienDichQuangCaos
        .Where(x => x.MaGianHang == CurrentGianHangId)
        .OrderByDescending(x => x.NgayTao).ToListAsync());

    public IActionResult Create() => View("Form", new AdvertisingCampaignViewModel());

    public async Task<IActionResult> Edit(int id)
    {
        var campaign = await Owned(id);
        if (campaign == null) return NotFound();
        ViewBag.Id = id;
        return View("Form", new AdvertisingCampaignViewModel
        {
            TenChienDich = campaign.TenChienDich,
            LoaiViTri = campaign.LoaiViTri,
            TieuDe = campaign.TieuDe,
            NoiDung = campaign.NoiDung,
            HinhAnh = campaign.HinhAnh,
            NgayBatDau = campaign.NgayBatDau,
            NgayKetThuc = campaign.NgayKetThuc,
            NganSach = campaign.NganSach,
            KichHoat = campaign.TrangThai == "DangChay"
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Create(AdvertisingCampaignViewModel model) => Save(null, model);

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Edit(int id, AdvertisingCampaignViewModel model) => Save(id, model);

    private async Task<IActionResult> Save(int? id, AdvertisingCampaignViewModel model)
    {
        if (!ModelState.IsValid) { ViewBag.Id = id; return View("Form", model); }
        var campaign = id == null
            ? new ChienDichQuangCao { MaGianHang = CurrentGianHangId }
            : await Owned(id.Value);
        if (campaign == null) return NotFound();

        campaign.TenChienDich = model.TenChienDich.Trim();
        campaign.LoaiViTri = model.LoaiViTri;
        campaign.TieuDe = model.TieuDe.Trim();
        campaign.NoiDung = model.NoiDung?.Trim();
        campaign.HinhAnh = model.HinhAnh?.Trim();
        campaign.NgayBatDau = model.NgayBatDau;
        campaign.NgayKetThuc = model.NgayKetThuc;
        campaign.NganSach = model.NganSach;
        campaign.TrangThai = model.KichHoat ? "DangChay" : "BanNhap";
        if (id == null) db.ChienDichQuangCaos.Add(campaign);
        await db.SaveChangesAsync();
        TempData["ThongBao"] = "Đã lưu chiến dịch quảng cáo.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int id)
    {
        var campaign = await Owned(id);
        if (campaign == null) return NotFound();
        if (campaign.NgayKetThuc <= DateTime.Now)
            TempData["LoiThongBao"] = "Chiến dịch đã hết hạn, hãy sửa ngày kết thúc trước khi bật.";
        else
        {
            campaign.TrangThai = campaign.TrangThai == "DangChay" ? "TamDung" : "DangChay";
            await db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var campaign = await Owned(id);
        if (campaign == null) return NotFound();
        db.ChienDichQuangCaos.Remove(campaign);
        await db.SaveChangesAsync();
        TempData["ThongBao"] = "Đã xóa chiến dịch.";
        return RedirectToAction(nameof(Index));
    }

    private Task<ChienDichQuangCao?> Owned(int id) => db.ChienDichQuangCaos
        .SingleOrDefaultAsync(x => x.MaChienDich == id && x.MaGianHang == CurrentGianHangId);
}
