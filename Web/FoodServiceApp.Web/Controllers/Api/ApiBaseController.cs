using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodServiceApp.Web.Controllers.Api
{
    // Toàn bộ Controllers/Api/* dùng JWT (cho mobile/FoodApp), khác với Controllers/*
    // (MVC, dùng Cookie cho trình duyệt). Chỉ định rõ AuthenticationSchemes ở đây để
    // không bị nhầm sang scheme mặc định (Cookie) đã đặt trong Program.cs.
    public abstract class ApiBaseController : ControllerBase
    {
        protected int CurrentMaTK =>
            int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
    }

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "GianHang")]
    public abstract class VendorApiBaseController : ApiBaseController { }

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "QuanTri")]
    public abstract class AdminApiBaseController : ApiBaseController { }

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "TaiXe")]
    public abstract class DriverApiBaseController : ApiBaseController { }
}
