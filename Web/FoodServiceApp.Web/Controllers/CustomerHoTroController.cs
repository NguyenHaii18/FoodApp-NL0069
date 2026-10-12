using Microsoft.AspNetCore.Mvc;

namespace FoodServiceApp.Web.Controllers
{
    // Trung tâm hỗ trợ: câu hỏi thường gặp + liên hệ + lối vào gửi yêu cầu hỗ trợ (CustomerPhanHoi) và xem lịch sử.
    [Route("MuaHang/HoTro")]
    public class CustomerHoTroController : CustomerBaseController
    {
        private readonly IConfiguration _config;
        public CustomerHoTroController(IConfiguration config) { _config = config; }

        [HttpGet("")]
        public IActionResult Index()
        {
            ViewBag.Hotline = _config["HoTro:Hotline"] ?? "1900 0000";
            ViewBag.Email = _config["HoTro:Email"] ?? "hotro@example.com";
            return View();
        }
    }
}
