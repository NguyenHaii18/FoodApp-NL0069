using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodServiceApp.Web.Controllers
{
    [Authorize(Roles = "KhachHang")]
    public abstract class CustomerBaseController : Controller
    {
        protected int CurrentMaKH =>
            int.Parse(User.FindFirst("MaKH")!.Value);
    }
}
