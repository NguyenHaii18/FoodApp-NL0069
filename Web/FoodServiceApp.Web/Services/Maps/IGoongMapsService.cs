namespace FoodServiceApp.Web.Services.Maps
{
    public record GeoPoint(double Lat, double Lng);

    public record DistanceResult(double DistanceKm, int DurationMinutes);

    public interface IGoongMapsService
    {
        /// <summary>Chuyển địa chỉ dạng text thành tọa độ (lat, lng).</summary>
        Task<GeoPoint?> GeocodeAsync(string address);

        /// <summary>Tính khoảng cách + thời gian di chuyển giữa 2 điểm (dùng cho phí giao hàng).</summary>
        Task<DistanceResult?> GetDistanceAsync(GeoPoint origin, GeoPoint destination);
    }
}
