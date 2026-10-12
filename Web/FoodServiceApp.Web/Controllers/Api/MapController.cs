using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FoodServiceApp.Web.Services.Maps;

namespace FoodServiceApp.Web.Controllers.Api
{
    public record DeliveryFeeRequest(string DiaChiGianHang, string DiaChiKhachHang);

    [ApiController]
    [Route("api/map")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class MapController : ControllerBase
    {
        private readonly IGoongMapsService _mapsService;
        private const decimal PhiMoiKm = 5000m; // VNĐ/km, có thể đưa vào appsettings

        public MapController(IGoongMapsService mapsService)
        {
            _mapsService = mapsService;
        }

        // POST /api/map/phi-giao-hang — dùng chung cho mobile Khách hàng (ước tính phí trước khi đặt)
        [HttpPost("phi-giao-hang")]
        public async Task<IActionResult> TinhPhiGiaoHang(DeliveryFeeRequest request)
        {
            var diemGian = await _mapsService.GeocodeAsync(request.DiaChiGianHang);
            var diemKhach = await _mapsService.GeocodeAsync(request.DiaChiKhachHang);

            if (diemGian == null || diemKhach == null)
                return BadRequest(new { message = "Không tìm thấy tọa độ cho địa chỉ đã nhập." });

            var ketQua = await _mapsService.GetDistanceAsync(diemGian, diemKhach);
            if (ketQua == null)
                return BadRequest(new { message = "Không tính được khoảng cách giữa hai địa điểm." });

            var phiGiaoHang = Math.Ceiling((decimal)ketQua.DistanceKm) * PhiMoiKm;

            return Ok(new
            {
                khoangCachKm = ketQua.DistanceKm,
                thoiGianPhutDuKien = ketQua.DurationMinutes,
                phiGiaoHang
            });
        }
    }
}
