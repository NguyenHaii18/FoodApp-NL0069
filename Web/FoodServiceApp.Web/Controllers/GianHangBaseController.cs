
using FoodServiceApp.Web.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FoodServiceApp.Web.Controllers
{
    [Authorize(Roles = "GianHang")]
    public abstract class GianHangBaseController : Controller
    {
        protected int CurrentGianHangId =>
            int.Parse(User.FindFirst("MaGianHang")!.Value);

        public override async Task OnActionExecutionAsync(
            ActionExecutingContext context,
            ActionExecutionDelegate next)
        {
            if (User.Identity?.IsAuthenticated != true)
            {
                await next();
                return;
            }

            var maTkClaim = User.FindFirst(
                System.Security.Claims.ClaimTypes.NameIdentifier);

            if (!int.TryParse(maTkClaim?.Value, out var maTK))
            {
                context.Result = new ForbidResult();
                return;
            }

            var db = context.HttpContext.RequestServices
                .GetRequiredService<AppDbContext>();

            var gianHang = await db.GianHangs
                .AsNoTracking()
                .Where(g => g.MaTK == maTK)
                .Select(g => new
                {
                    g.MaGianHang,
                    g.TrangThaiDuyet,
                    TaiKhoanHoatDong =
                        g.TaiKhoan != null &&
                        g.TaiKhoan.TrangThai
                })
                .FirstOrDefaultAsync();

            if (gianHang == null ||
                !gianHang.TaiKhoanHoatDong)
            {
                context.Result = new ForbidResult();
                return;
            }

            var controllerTen = context.RouteData.Values[
                "controller"]?.ToString();

            var actionTen = context.RouteData.Values[
                "action"]?.ToString();

            bool duocPhepKhiChuaDuyet =
                controllerTen == "HoSo" ||
                (controllerTen == "Dashboard" &&
                 (actionTen == "ChoDuyet" ||
                  actionTen == "GuiHoSoXetDuyet"));

            if (gianHang.TrangThaiDuyet != "DaDuyet" &&
                !duocPhepKhiChuaDuyet)
            {
                context.Result = new RedirectToActionResult(
                    "ChoDuyet",
                    "Dashboard",
                    null
                );
                return;
            }

            await next();
        }
    }
}
