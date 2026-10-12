using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace FoodServiceApp.Web.Controllers
{
    [Authorize(Roles = "GianHang")]
    public abstract class GianHangBaseController : Controller
    {
        protected int CurrentGianHangId =>
            int.Parse(User.FindFirst("MaGianHang")!.Value);

        // Gian hàng chưa được Quản trị duyệt thì không cho vào các trang nghiệp vụ
        // (trừ chính trang "Chờ duyệt" và Hồ sơ, để họ vẫn xem/sửa được thông tin
        // đã đăng ký trong lúc chờ).
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            base.OnActionExecuting(context);

            var trangThaiDuyet = User.FindFirst("TrangThaiDuyet")?.Value;
            var controllerTen = context.RouteData.Values["controller"]?.ToString();
            var actionTen = context.RouteData.Values["action"]?.ToString();

            var duocPhepKhiChuaDuyet =
                (controllerTen == "Dashboard" && actionTen == "ChoDuyet") ||
                controllerTen == "HoSo";

            if (trangThaiDuyet != "DaDuyet" && !duocPhepKhiChuaDuyet)
            {
                context.Result = new RedirectToActionResult("ChoDuyet", "Dashboard", null);
            }
        }
    }
}
