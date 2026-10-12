using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodServiceApp.Web.Controllers
{
    [Authorize(Roles = "TaiXe")]
    public abstract class TaiXeBaseController : Controller
    {
        protected int CurrentTaiXeId =>
            int.Parse(User.FindFirst("MaTaiXe")!.Value);
    }
}
