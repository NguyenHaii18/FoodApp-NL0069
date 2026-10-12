using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using FoodServiceApp.Web.Data;
using FoodServiceApp.Web.Services;

namespace FoodServiceApp.Web.Filters
{
    /// <summary>
    /// Phân quyền quản trị: tài khoản "NhanVien" (nhân viên hỗ trợ) chỉ được XEM; mọi thao tác ghi
    /// (POST/PUT/DELETE: duyệt, khóa, sửa...) chỉ "SuperAdmin" (admin tổng) mới được làm.
    /// Dùng được cho cả Website (cookie) lẫn API (JWT). Đặt ở class hoặc action.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class ChiSuperAdminGhiAttribute : Attribute, IAsyncAuthorizationFilter
    {
        public async Task OnAuthorizationAsync(AuthorizationFilterContext ctx)
        {
            var m = ctx.HttpContext.Request.Method;
            if (HttpMethods.IsGet(m) || HttpMethods.IsHead(m) || HttpMethods.IsOptions(m)) return;
            if (!await LaSuperAdminAsync(ctx.HttpContext))
                ctx.Result = ctx.HttpContext.Request.Path.StartsWithSegments("/api")
                    ? new ObjectResult(new { thongDiep = "Chỉ Admin tổng mới được thực hiện thao tác này." }) { StatusCode = 403 }
                    : new ContentResult { StatusCode = 403, ContentType = "text/html; charset=utf-8",
                        Content = "<h3>403 - Chỉ Admin tổng mới được thực hiện thao tác này.</h3><a href=\"javascript:history.back()\">Quay lại</a>" };
        }

        public static async Task<bool> LaSuperAdminAsync(HttpContext http)
        {
            if (!int.TryParse(http.User.FindFirstValue(ClaimTypes.NameIdentifier), out var maTK)) return false;
            var db = http.RequestServices.GetRequiredService<AppDbContext>();
            return await db.QuanTris.AnyAsync(q => q.MaTK == maTK && q.CapQuyenHan == "SuperAdmin");
        }
    }

    /// <summary>Thao tác ghi cho phép "SuperAdmin" và "QuanLy" (vận hành: đơn, điều phối, thanh toán...). "NhanVien" chỉ xem.</summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class ChiQuanLyTroLenGhiAttribute : Attribute, IAsyncAuthorizationFilter
    {
        public async Task OnAuthorizationAsync(AuthorizationFilterContext ctx)
        {
            var m = ctx.HttpContext.Request.Method;
            if (HttpMethods.IsGet(m) || HttpMethods.IsHead(m) || HttpMethods.IsOptions(m)) return;
            if (!int.TryParse(ctx.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier), out var maTK)) { ctx.Result = new ForbidResult(); return; }
            var db = ctx.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
            if (!await db.QuanTris.AnyAsync(q => q.MaTK == maTK && (q.CapQuyenHan == "SuperAdmin" || q.CapQuyenHan == "QuanLy")))
                ctx.Result = new ContentResult { StatusCode = 403, ContentType = "text/html; charset=utf-8",
                    Content = "<h3>403 - Chỉ Admin tổng hoặc Quản lý mới được thực hiện thao tác này.</h3><a href=\"javascript:history.back()\">Quay lại</a>" };
        }
    }

    /// <summary>Thao tác ghi cần quyền chi tiết theo chức năng (xem QuyenAdmin). GET luôn được phép.</summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class CoQuyenGhiAttribute : Attribute, IAsyncAuthorizationFilter
    {
        private readonly string _khoa;
        public CoQuyenGhiAttribute(string khoa) { _khoa = khoa; }
        public async Task OnAuthorizationAsync(AuthorizationFilterContext ctx)
        {
            var m = ctx.HttpContext.Request.Method;
            if (HttpMethods.IsGet(m) || HttpMethods.IsHead(m) || HttpMethods.IsOptions(m)) return;
            var db = ctx.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
            if (!await QuyenAdmin.CoAsync(db, ctx.HttpContext.User, _khoa))
                ctx.Result = new ContentResult { StatusCode = 403, ContentType = "text/html; charset=utf-8",
                    Content = "<h3>403 - Tài khoản của bạn chưa được cấp quyền cho chức năng này.</h3><a href=\"javascript:history.back()\">Quay lại</a>" };
        }
    }

    /// <summary>Tự ghi nhật ký mọi thao tác ghi (POST/PUT/DELETE) của người dùng đã đăng nhập vào NhatKyHeThong.</summary>
    public class NhatKyThaoTacFilter : IAsyncActionFilter
    {
        private readonly AppDbContext _db;
        public NhatKyThaoTacFilter(AppDbContext db) { _db = db; }

        public async Task OnActionExecutionAsync(ActionExecutingContext ctx, ActionExecutionDelegate next)
        {
            var http = ctx.HttpContext;
            var m = http.Request.Method;
            var ghi = !(HttpMethods.IsGet(m) || HttpMethods.IsHead(m) || HttpMethods.IsOptions(m))
                      && http.User.Identity?.IsAuthenticated == true;
            var sw = Stopwatch.StartNew();
            var done = await next();
            if (!ghi) return;
            sw.Stop();
            int? maTK = int.TryParse(http.User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
            var vaiTro = http.User.FindFirstValue(ClaimTypes.Role) ?? "?";
            var ten = ctx.ActionDescriptor.RouteValues;
            ten.TryGetValue("controller", out var c); ten.TryGetValue("action", out var a);
            var status = done.Exception != null && !done.ExceptionHandled ? 500 : http.Response.StatusCode;
            await NhatKy.GhiAsync(_db, http, "ThaoTac", $"[{vaiTro}] {c}/{a}", maTK,
                done.Exception != null ? "Error" : "Info", status, (int)sw.ElapsedMilliseconds);
        }
    }
}
