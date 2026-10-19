using FoodServiceApp.Web.Data;
using FoodServiceApp.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace FoodServiceApp.Web.Services;

public record PromotionItem(int FoodId, int Quantity, IReadOnlyList<int>? OptionIds = null);
public record PromotionLine(int FoodId, int CategoryId, int Quantity, decimal Price);
public record PromotionResult(KhuyenMai? Voucher, string? ErrorCode, string? Message,
    decimal Subtotal, decimal EligibleAmount, decimal Shipping, decimal Discount)
{
    public IReadOnlyList<PromotionLine> Lines { get; init; } = Array.Empty<PromotionLine>();
    public bool Valid => ErrorCode == null;
    public decimal Total => Math.Max(0, Subtotal + Shipping - Discount);
}

/// <summary>Authoritative pricing for every checkout. Call inside the order transaction when consuming.</summary>
public sealed class PromotionService(AppDbContext db)
{
    public static DateTime VietnamNow => TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, "SE Asia Standard Time");

    public async Task LockAsync(string resource)
    {
        if (db.Database.CurrentTransaction == null) throw new InvalidOperationException("Voucher lock requires a transaction.");
        await db.Database.ExecuteSqlInterpolatedAsync($"DECLARE @r int; EXEC @r = sys.sp_getapplock @Resource={resource}, @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=15000; IF @r < 0 THROW 51000, N'Không thể khóa yêu cầu. Vui lòng thử lại.', 1;");
    }

    public async Task<PromotionResult> CalculateAsync(int customerId, int storeId, IEnumerable<PromotionItem> items,
        decimal shipping, string? code, DateTime? at = null, bool consume = false)
    {
        if (consume && db.Database.CurrentTransaction == null) throw new InvalidOperationException("Consumption requires an order transaction.");
        var input = items.ToList();
        var ids = input.Select(x => x.FoodId).Distinct().ToList();
        var foods = await db.MonAns.AsNoTracking().Include(x => x.DanhMuc).Where(x => ids.Contains(x.MaMonAn)).ToListAsync();
        var options = await db.TuyChonMons.AsNoTracking().Where(x => ids.Contains(x.MaMonAn)).ToListAsync();
        var lines = new List<PromotionLine>();
        foreach (var item in input)
        {
            var food = foods.FirstOrDefault(x => x.MaMonAn == item.FoodId);
            var selected = (item.OptionIds ?? Array.Empty<int>()).Distinct().ToList();
            var validOptions = options.Where(x => x.MaMonAn == item.FoodId && selected.Contains(x.MaTuyChon)).ToList();
            if (item.Quantity <= 0 || food?.DanhMuc?.MaGianHang != storeId || food.TinhTrang != "ConHang" || selected.Count != validOptions.Count)
                return new(null, "INVALID_CART", "Món ăn, số lượng hoặc tùy chọn không còn hợp lệ.", 0, 0, Math.Max(0, shipping), 0);
            lines.Add(new(item.FoodId, food.MaDanhMuc, item.Quantity, food.Gia + validOptions.Sum(x => x.GiaThem)));
        }
        if (lines.Count == 0) return new(null, "EMPTY_CART", "Giỏ hàng đang trống.", 0, 0, Math.Max(0, shipping), 0);
        if (string.IsNullOrWhiteSpace(code)) return Evaluate(null, lines, storeId, shipping, at ?? VietnamNow, false, [], [], [], false) with { Lines = lines };
        var normalized = code.Trim().ToUpperInvariant();
        // SQL update locks also serialize with administrative edits; all counters are read after acquiring the lock.
        var voucher = consume
            ? await db.KhuyenMais.FromSqlInterpolated($"SELECT * FROM dbo.KhuyenMai WITH (UPDLOCK,HOLDLOCK) WHERE MaCode = {normalized}").SingleOrDefaultAsync()
            : await db.KhuyenMais.AsNoTracking().SingleOrDefaultAsync(x => x.MaCode == normalized);
        if (voucher == null) return Evaluate(null, lines, storeId, shipping, at ?? VietnamNow, false, [], [], []);
        var stores = await db.KhuyenMaiGianHangs.Where(x => x.MaKhuyenMai == voucher.MaKhuyenMai).Select(x => x.MaGianHang).ToListAsync();
        var categories = await db.KhuyenMaiDanhMucs.Where(x => x.MaKhuyenMai == voucher.MaKhuyenMai).Select(x => x.MaDanhMuc).ToListAsync();
        var dishes = await db.KhuyenMaiMonAns.Where(x => x.MaKhuyenMai == voucher.MaKhuyenMai).Select(x => x.MaMonAn).ToListAsync();
        var used = voucher.MoiKhachMotLan && await db.KhuyenMaiKhachDungs.AnyAsync(x => x.MaKhuyenMai == voucher.MaKhuyenMai && x.MaKH == customerId);
        return Evaluate(voucher, lines, storeId, shipping, at ?? VietnamNow, used, stores, categories, dishes) with { Lines = lines };
    }

    public static PromotionResult Evaluate(KhuyenMai? voucher, IReadOnlyList<PromotionLine> lines, int storeId,
        decimal shipping, DateTime at, bool used, IReadOnlyCollection<int> stores, IReadOnlyCollection<int> categories,
        IReadOnlyCollection<int> dishes, bool hasCode = true)
    {
        var subtotal = lines.Sum(x => x.Price * x.Quantity);
        shipping = Math.Max(0, shipping);
        PromotionResult Fail(string code, string text) => new(null, code, text, subtotal, 0, shipping, 0);
        if (lines.Any(x => x.Price < 0 || x.Quantity <= 0)) return Fail("INVALID_CART", "Giỏ hàng không hợp lệ.");
        if (!hasCode) return new(null, null, null, subtotal, subtotal, shipping, 0);
        if (voucher == null) return Fail("NOT_FOUND", "Mã khuyến mãi không tồn tại.");
        if (!voucher.TrangThai) return Fail("DISABLED", "Mã khuyến mãi đang bị tắt.");
        if (at < voucher.NgayBatDau) return Fail("NOT_STARTED", "Mã khuyến mãi chưa bắt đầu.");
        if (at > voucher.NgayKetThuc) return Fail("EXPIRED", "Mã khuyến mãi đã hết hạn.");
        if ((voucher.MaGianHang != null && voucher.MaGianHang != storeId) || (stores.Count > 0 && !stores.Contains(storeId)))
            return Fail("WRONG_STORE", "Mã không áp dụng cho gian hàng này.");
        if (subtotal < voucher.GiaTriDonToiThieu) return Fail("MINIMUM", $"Đơn tối thiểu {voucher.GiaTriDonToiThieu:N0}đ mới dùng được mã này.");
        if (voucher.SoLuongToiDa != null && voucher.SoLuongDaDung >= voucher.SoLuongToiDa) return Fail("EXHAUSTED", "Mã khuyến mãi đã hết lượt sử dụng.");
        if (voucher.MoiKhachMotLan && used) return Fail("ALREADY_USED", "Bạn đã dùng mã này rồi (mỗi khách một lần).");
        var eligible = lines.Where(x => (categories.Count == 0 && dishes.Count == 0) || dishes.Contains(x.FoodId) || categories.Contains(x.CategoryId)).Sum(x => x.Price * x.Quantity);
        if (eligible <= 0) return Fail("NO_ELIGIBLE_ITEMS", "Giỏ hàng không có món thuộc phạm vi áp dụng.");
        decimal discount;
        switch (voucher.LoaiGiam)
        {
            case "PhanTram":
                if (voucher.PhanTramGiam is not (>= 1 and <= 100)) return Fail("INVALID_CONFIG", "Tỷ lệ giảm không hợp lệ.");
                discount = Math.Round(eligible * voucher.PhanTramGiam.Value / 100);
                if (voucher.GiamToiDa.HasValue) discount = Math.Min(discount, voucher.GiamToiDa.Value);
                discount = Math.Min(discount, eligible);
                break;
            case "SoTien": discount = Math.Min(voucher.SoTienGiam ?? 0, eligible); break;
            case "GiamPhiVanChuyen": discount = Math.Min(voucher.SoTienGiam is null or 0 ? shipping : voucher.SoTienGiam.Value, shipping); break;
            default: return Fail("INVALID_CONFIG", "Loại khuyến mãi không hợp lệ.");
        }
        if (discount < 0) return Fail("INVALID_CONFIG", "Giá trị giảm không hợp lệ.");
        return new(voucher, null, null, subtotal, eligible, shipping, discount);
    }

    public void Record(PromotionResult result, int customerId, int orderId)
    {
        if (!result.Valid || db.Database.CurrentTransaction == null) throw new InvalidOperationException("Invalid voucher transaction.");
        if (result.Voucher is not { } voucher) return;
        voucher.SoLuongDaDung++;
        db.KhuyenMaiKhachDungs.Add(new() { MaKhuyenMai = voucher.MaKhuyenMai, MaKH = customerId, MaDonHang = orderId });
    }
}
