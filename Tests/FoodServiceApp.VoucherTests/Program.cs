using FoodServiceApp.Web.Models.Entities;
using FoodServiceApp.Web.Services;

var now = new DateTime(2026, 10, 20, 12, 0, 0);
var lines = new[] { new PromotionLine(1, 10, 2, 50000m), new PromotionLine(2, 20, 1, 100000m) };
KhuyenMai Voucher() => new() { MaCode = "TEST", LoaiGiam = "PhanTram", PhanTramGiam = 10, NgayBatDau = now.AddDays(-1), NgayKetThuc = now.AddDays(1), TrangThai = true };
PromotionResult Calculate(KhuyenMai? v, bool used = false, int[]? stores = null, int[]? categories = null, int[]? dishes = null) =>
    PromotionService.Evaluate(v, lines, 7, 15000m, now, used, stores ?? [], categories ?? [], dishes ?? []);
var passed = 0;
void Check(string name, bool condition) { if (!condition) throw new Exception("FAIL: " + name); Console.WriteLine("PASS: " + name); passed++; }
void Error(string name, Action<KhuyenMai> mutate, string code) { var v = Voucher(); mutate(v); Check(name, Calculate(v).ErrorCode == code); }
Check("10 percent", Calculate(Voucher()).Discount == 20000m);
var fixedVoucher = Voucher(); fixedVoucher.LoaiGiam = "SoTien"; fixedVoucher.PhanTramGiam = null; fixedVoucher.SoTienGiam = 30000;
Check("fixed amount", Calculate(fixedVoucher).Discount == 30000m);
var shipping = Voucher(); shipping.LoaiGiam = "GiamPhiVanChuyen"; shipping.PhanTramGiam = null; shipping.SoTienGiam = 10000;
Check("shipping only", Calculate(shipping).Discount == 10000m && Calculate(shipping).Total == 205000m);
shipping.SoTienGiam = 0; Check("free shipping zero", Calculate(shipping).Discount == 15000m);
shipping.SoTienGiam = null; Check("legacy free shipping null", Calculate(shipping).Discount == 15000m);
shipping.SoTienGiam = 50000; Check("shipping cannot spill into food", Calculate(shipping).Discount == 15000m);
Error("minimum excludes shipping", v => v.GiaTriDonToiThieu = 200001, "MINIMUM");
Error("future voucher", v => v.NgayBatDau = now.AddMinutes(1), "NOT_STARTED");
Error("expired voucher", v => v.NgayKetThuc = now.AddMinutes(-1), "EXPIRED");
Error("disabled voucher", v => v.TrangThai = false, "DISABLED");
Error("exhausted voucher", v => { v.SoLuongToiDa = 1; v.SoLuongDaDung = 1; }, "EXHAUSTED");
var once = Voucher(); once.MoiKhachMotLan = true;
Check("once per customer", Calculate(once, used: true).ErrorCode == "ALREADY_USED");
Error("wrong owning store", v => v.MaGianHang = 8, "WRONG_STORE");
Check("one dish in mixed cart", Calculate(Voucher(), dishes: [1]).Discount == 10000m);
Check("category scope", Calculate(Voucher(), categories: [20]).Discount == 10000m);
Check("dish OR category without double count", Calculate(Voucher(), dishes: [1], categories: [10]).Discount == 10000m);
Check("system store restriction", Calculate(Voucher(), stores: [8]).ErrorCode == "WRONG_STORE");
Check("system allowed store", Calculate(Voucher(), stores: [7, 8]).Discount == 20000m);
Check("missing eligible dishes", Calculate(Voucher(), dishes: [99]).ErrorCode == "NO_ELIGIBLE_ITEMS");
fixedVoucher.SoTienGiam = 999999;
Check("fixed capped by eligible value", Calculate(fixedVoucher, dishes: [1]).Discount == 100000m);
Check("discount cannot exceed order", Calculate(fixedVoucher).Total == 15000m);
Check("unknown code", Calculate(null).ErrorCode == "NOT_FOUND");
Error("invalid percent", v => v.PhanTramGiam = 101, "INVALID_CONFIG");
var capped = Voucher(); capped.GiamToiDa = 5000;
Check("percent maximum", Calculate(capped).Discount == 5000m);
var boundary = Voucher(); boundary.NgayBatDau = boundary.NgayKetThuc = now;
Check("inclusive effective boundaries", Calculate(boundary).Valid);
Check("no code", PromotionService.Evaluate(null, lines, 7, 15000, now, false, [], [], [], false).Total == 215000);
Console.WriteLine($"{passed} business-rule assertions passed.");
await IntegrationTests.RunAsync();
