using System.Text;
using FoodServiceApp.Web.Data;
using FoodServiceApp.Web.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FoodServiceApp.Web.Controllers;

public class DoanhThuController : GianHangBaseController
{
    private readonly AppDbContext _db;
    public DoanhThuController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index(DateTime? tu, DateTime? den)
    {
        var (from, to) = ChuanHoaKhoangNgay(tu, den);
        var store = await _db.GianHangs.AsNoTracking().FirstAsync(g => g.MaGianHang == CurrentGianHangId);
        var orders = await _db.DonHangs.Include(d => d.ChiTiets).ThenInclude(c => c.MonAn)
            .Where(d => d.MaGianHang == CurrentGianHangId && d.NgayDat >= from && d.NgayDat < to.AddDays(1)).ToListAsync();
        var completed = orders.Where(d => d.TrangThai == "DaGiao").ToList();
        var revenue = completed.Sum(d => d.ChiTiets.Sum(c => c.DonGia * c.SoLuong));
        var fee = Math.Round(revenue * store.TyLeChietKhau, 0);
        var cost = completed.Sum(d => d.ChiTiets.Sum(c => (c.MonAn?.GiaVon ?? 0) * c.SoLuong));
        var cancelled = orders.Count(d => d.TrangThai == "DaHuy");

        return View(new DoanhThuViewModel
        {
            TuNgay = from, DenNgay = to, TongDoanhThu = revenue, PhiNenTang = fee,
            ThucNhan = revenue - fee, GiaVon = cost, LoiNhuanUocTinh = revenue - fee - cost,
            GiaTriDonTrungBinh = completed.Count == 0 ? 0 : revenue / completed.Count,
            TongDonHoanThanh = completed.Count, TongDonHuy = cancelled,
            TyLeHuy = orders.Count == 0 ? 0 : (decimal)cancelled / orders.Count * 100,
            DoanhThuTheoNgay = completed.GroupBy(d => d.NgayDat.Date).OrderBy(g => g.Key)
                .Select(g => (g.Key, g.Sum(d => d.ChiTiets.Sum(c => c.DonGia * c.SoLuong)))).ToList(),
            MonBanChay = completed.SelectMany(d => d.ChiTiets).GroupBy(c => c.MonAn?.TenMon ?? "Món đã xóa")
                .Select(g => (g.Key, g.Sum(c => c.SoLuong), g.Sum(c => c.DonGia * c.SoLuong)))
                .OrderByDescending(x => x.Item2).Take(10).ToList(),
            DonTheoTrangThai = orders.GroupBy(d => d.TrangThai).Select(g => (g.Key, g.Count()))
                .OrderByDescending(x => x.Item2).ToList()
        });
    }

    public async Task<IActionResult> ExportCsv(DateTime? tu, DateTime? den)
    {
        var (from, to) = ChuanHoaKhoangNgay(tu, den);
        var orders = await _db.DonHangs.Include(d => d.ChiTiets)
            .Where(d => d.MaGianHang == CurrentGianHangId && d.NgayDat >= from && d.NgayDat < to.AddDays(1))
            .OrderByDescending(d => d.NgayDat).ToListAsync();
        var csv = new StringBuilder("MaDon,NgayDat,TrangThai,SoMon,DoanhThuMon,LyDoHuy\r\n");
        foreach (var d in orders)
            csv.AppendLine($"{d.MaDonHang},{d.NgayDat:yyyy-MM-dd HH:mm:ss},{Csv(d.TrangThai)},{d.ChiTiets.Sum(c => c.SoLuong)},{d.ChiTiets.Sum(c => c.DonGia * c.SoLuong)},{Csv(d.LyDoTuChoi)}");
        return File(new UTF8Encoding(true).GetBytes(csv.ToString()), "text/csv; charset=utf-8", $"bao-cao-{from:yyyyMMdd}-{to:yyyyMMdd}.csv");
    }

    private static (DateTime From, DateTime To) ChuanHoaKhoangNgay(DateTime? tu, DateTime? den)
    {
        var to = (den ?? DateTime.Today).Date;
        var from = (tu ?? to.AddDays(-29)).Date;
        if (from > to) (from, to) = (to, from);
        if ((to - from).TotalDays > 366) from = to.AddDays(-366);
        return (from, to);
    }

    private static string Csv(string? value) => $"\"{(value ?? "").Replace("\"", "\"\"")}\"";
}
