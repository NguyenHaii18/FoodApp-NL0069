using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FoodServiceApp.Web.Services;

namespace FoodServiceApp.Web.Controllers.Api
{
    [ApiController]
    [Route("api/upload")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)] // JWT — không dùng Cookie của web
    public class UploadController : ControllerBase
    {
        private readonly IFileStorageService _storageService;

        public UploadController(IFileStorageService storageService)
        {
            _storageService = storageService;
        }

        [HttpPost("monan")]
        public async Task<IActionResult> UploadMonAnImage(IFormFile file)
        {
            try
            {
                var url = await _storageService.SaveFileAsync(file, "monan");
                return Ok(new { imageUrl = url });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("gianhang")]
        public async Task<IActionResult> UploadGianHangImage(IFormFile file)
        {
            try
            {
                var url = await _storageService.SaveFileAsync(file, "gianhang");
                return Ok(new { imageUrl = url });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
