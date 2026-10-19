using FoodServiceApp.Web.Controllers.Api;
using FoodServiceApp.Web.Data;
using FoodServiceApp.Web.Models.Entities;
using FoodServiceApp.Web.Models.ViewModels;
using FoodServiceApp.Web.Services;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

internal static class IntegrationTests
{
    public static async Task RunAsync()
    {
        var connection = Environment.GetEnvironmentVariable("VOUCHER_TEST_CONNECTION");
        if (string.IsNullOrWhiteSpace(connection))
        {
            Console.WriteLine("SKIP SQL integration: set VOUCHER_TEST_CONNECTION to the restored MigrationTest database after applying the migrations. Tests retain labelled fixtures; never target the primary database.");
            return;
        }
        if (new SqlConnectionStringBuilder(connection).InitialCatalog != "QuanLyDichVuAnUong_MigrationTest")
            throw new Exception("Integration tests refuse any database except QuanLyDichVuAnUong_MigrationTest.");
        AppDbContext Db() => new(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(connection).Options);
        await using var setup = Db();
        var prerequisites = await setup.Database.SqlQueryRaw<int>("SELECT CASE WHEN OBJECT_ID('dbo.TuyChonMon') IS NULL OR COL_LENGTH('dbo.DonHang','MaXacNhanGiao') IS NULL OR COL_LENGTH('dbo.ThanhToan','DaDoiSoat') IS NULL OR (SELECT COUNT(*) FROM dbo.MonAn)=0 OR (SELECT COUNT(*) FROM dbo.KhachHang)<2 THEN 0 ELSE 1 END AS Value").SingleAsync();
        if (prerequisites == 0)
        {
            Console.WriteLine("SKIP SQL order/concurrency/authorization tests: restored database lacks checkout schema or existing food/two customer fixtures. Voucher migration alone intentionally does not upgrade unrelated modules.");
            return;
        }
        var food = await setup.MonAns.Include(x => x.DanhMuc).FirstAsync(x => x.TinhTrang == "ConHang");
        var customers = await setup.KhachHangs.Take(2).ToListAsync();
        if (customers.Count < 2) throw new Exception("Integration fixture requires two existing customers.");
        var store = food.DanhMuc!.MaGianHang;
        var prefix = "TEST_" + Guid.NewGuid().ToString("N")[..16];
        void Assert(bool test, string name) { if (!test) throw new Exception("FAIL SQL: " + name); Console.WriteLine("PASS SQL: " + name); }
        async Task<KhuyenMai> NewVoucher(string suffix, int? limit = null, bool once = false)
        {
            var v = new KhuyenMai { MaCode = prefix + suffix, TenChuongTrinh = "Automated voucher test", LoaiGiam = "PhanTram", PhanTramGiam = 10,
                NgayBatDau = PromotionService.VietnamNow.AddHours(-1), NgayKetThuc = PromotionService.VietnamNow.AddHours(1), SoLuongToiDa = limit, MoiKhachMotLan = once };
            setup.KhuyenMais.Add(v); await setup.SaveChangesAsync(); return v;
        }
        async Task<(int? id, string? error)> Place(string code, int customer, string token, bool fail = false)
        {
            await using var db = Db(); await using var tx = await db.Database.BeginTransactionAsync();
            var service = new PromotionService(db); await service.LockAsync($"checkout:customer:{customer}");
            var old = await db.DonHangs.SingleOrDefaultAsync(x => x.MaKH == customer && x.MaYeuCauDat == token);
            if (old != null) return (old.MaDonHang, null);
            var result = await service.CalculateAsync(customer, store, [new(food.MaMonAn, 1)], 15000, code, consume: true);
            if (!result.Valid) return (null, result.ErrorCode);
            var order = new DonHang { MaKH = customer, MaGianHang = store, TongTien = result.Total, TienGiam = result.Discount,
                PhiGiaoHang = 15000, MaKhuyenMaiApDung = result.Voucher!.MaKhuyenMai, MaYeuCauDat = token,
                DiaChiGiaoHang = "AUTOMATED TEST — DO NOT DELIVER", GhiChu = prefix, TrangThai = "DaHuy" };
            db.DonHangs.Add(order); await db.SaveChangesAsync();
            db.ChiTietDonHangs.Add(new() { MaDonHang = order.MaDonHang, MaMonAn = food.MaMonAn, SoLuong = 1, DonGia = result.Lines[0].Price });
            db.ThanhToans.Add(new() { MaDonHang = order.MaDonHang, HinhThuc = "TienMat", TrangThai = "ChoThanhToan" });
            service.Record(result, customer, order.MaDonHang); await db.SaveChangesAsync();
            if (fail) throw new InvalidOperationException("INJECTED_FAILURE");
            await tx.CommitAsync(); return (order.MaDonHang, null);
        }
        string Token() => Guid.NewGuid().ToString("N");
        var last = await NewVoucher("_LAST", 1);
        var races = await Task.WhenAll(Place(last.MaCode, customers[0].MaKH, Token()), Place(last.MaCode, customers[1].MaKH, Token()));
        Assert(races.Count(x => x.id.HasValue) == 1 && races.Count(x => x.error == "EXHAUSTED") == 1, "two customers race last slot");
        var one = await NewVoucher("_ONCE", once: true);
        races = await Task.WhenAll(Place(one.MaCode, customers[0].MaKH, Token()), Place(one.MaCode, customers[0].MaKH, Token()));
        Assert(races.Count(x => x.id.HasValue) == 1 && races.Count(x => x.error == "ALREADY_USED") == 1, "same customer concurrent distinct tokens");
        var duplicate = await NewVoucher("_DUP"); var sameToken = Token();
        races = await Task.WhenAll(Place(duplicate.MaCode, customers[0].MaKH, sameToken), Place(duplicate.MaCode, customers[0].MaKH, sameToken));
        Assert(races[0].id == races[1].id && races[0].id.HasValue, "duplicate request returns same order");
        Assert(await setup.KhuyenMaiKhachDungs.CountAsync(x => x.MaKhuyenMai == duplicate.MaKhuyenMai) == 1, "one usage record per order");
        var rollback = await NewVoucher("_ROLLBACK"); var rollbackToken = Token();
        try { await Place(rollback.MaCode, customers[0].MaKH, rollbackToken, true); throw new Exception("Expected failure"); }
        catch (InvalidOperationException ex) when (ex.Message == "INJECTED_FAILURE") { }
        Assert(!await setup.DonHangs.AnyAsync(x => x.MaYeuCauDat == rollbackToken), "order and details roll back on failure");
        Assert(!await setup.KhuyenMaiKhachDungs.AnyAsync(x => x.MaKhuyenMai == rollback.MaKhuyenMai), "usage history rolls back");
        Assert(await setup.KhuyenMais.AsNoTracking().Where(x => x.MaKhuyenMai == rollback.MaKhuyenMai).Select(x => x.SoLuongDaDung).SingleAsync() == 0, "counter rolls back");
        var serviceRead = new PromotionService(setup);
        var web = await serviceRead.CalculateAsync(customers[0].MaKH, store, [new(food.MaMonAn, 2, [])], 15000, rollback.MaCode);
        var mobile = await serviceRead.CalculateAsync(customers[0].MaKH, store, [new(food.MaMonAn, 2)], 15000, rollback.MaCode);
        Assert(web.Discount == mobile.Discount && web.Total == mobile.Total, "Web and Mobile input parity with database prices");
        var vendor = await setup.GianHangs.FirstAsync(x => x.MaGianHang == store);
        PromotionApiController Controller(int account, string role) => new(setup) {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity([
                new Claim(ClaimTypes.NameIdentifier, account.ToString()), new Claim(ClaimTypes.Role, role)], "test")) } }
        };
        Assert(await Controller(vendor.MaTK, "GianHang").Edit("vendor", last.MaKhuyenMai, new()) is NotFoundResult, "vendor cannot edit system/other owner's voucher");
        var lowAdmin = await setup.QuanTris.FirstOrDefaultAsync(x => x.CapQuyenHan != "SuperAdmin");
        if (lowAdmin != null) Assert(await Controller(lowAdmin.MaTK, "QuanTri").Edit("admin", last.MaKhuyenMai, new()) is ForbidResult, "lower admin cannot edit");
        else Console.WriteLine("SKIP lower-admin fixture: none present.");
        // Keep explicitly labelled fixtures rather than deleting any database data.
        await setup.KhuyenMais.Where(x => x.MaCode.StartsWith(prefix)).ExecuteUpdateAsync(x => x.SetProperty(v => v.TrangThai, false));
        Console.WriteLine("SQL fixtures retained (disabled vouchers/cancelled orders): " + prefix);
    }
}
