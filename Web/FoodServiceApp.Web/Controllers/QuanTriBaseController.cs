using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodServiceApp.Web.Controllers
{
    [Authorize(Roles = "QuanTri")]
    public abstract class QuanTriBaseController : Controller
    {
        protected int CurrentQuanTriId =>
            int.Parse(User.FindFirst("MaQuanTri")!.Value);
    }
}
