using System.Text.Json;
using Microsoft.Extensions.Options;
using FoodServiceApp.Web.Services.Configs;

namespace FoodServiceApp.Web.Services.Maps
{
    public class GoongMapsService : IGoongMapsService
    {
        private readonly HttpClient _httpClient;
        private readonly GoongMapsSettings _settings;

        public GoongMapsService(HttpClient httpClient, IOptions<GoongMapsSettings> options)
        {
            _httpClient = httpClient;
            _settings = options.Value;
        }

        public async Task<GeoPoint?> GeocodeAsync(string address)
        {
            var url = $"{_settings.BaseUrl}/Geocode?address={Uri.EscapeDataString(address)}&api_key={_settings.ApiKey}";
            using var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
                return null;

            using var stream = await response.Content.ReadAsStreamAsync();
            using var doc = await JsonDocument.ParseAsync(stream);

            var results = doc.RootElement.GetProperty("results");
            if (results.GetArrayLength() == 0)
                return null;

            var location = results[0].GetProperty("geometry").GetProperty("location");
            var lat = location.GetProperty("lat").GetDouble();
            var lng = location.GetProperty("lng").GetDouble();
            return new GeoPoint(lat, lng);
        }

        public async Task<DistanceResult?> GetDistanceAsync(GeoPoint origin, GeoPoint destination)
        {
            var origins = $"{origin.Lat},{origin.Lng}";
            var destinations = $"{destination.Lat},{destination.Lng}";
            // vehicle=bike phù hợp với giao hàng bằng xe máy trong đô thị
            var url = $"{_settings.BaseUrl}/DistanceMatrix?origins={origins}&destinations={destinations}" +
                      $"&vehicle=bike&api_key={_settings.ApiKey}";

            using var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
                return null;

            using var stream = await response.Content.ReadAsStreamAsync();
            using var doc = await JsonDocument.ParseAsync(stream);

            var element = doc.RootElement
                .GetProperty("rows")[0]
                .GetProperty("elements")[0];

            if (element.GetProperty("status").GetString() != "OK")
                return null;

            var distanceMeters = element.GetProperty("distance").GetProperty("value").GetDouble();
            var durationSeconds = element.GetProperty("duration").GetProperty("value").GetDouble();

            return new DistanceResult(
                DistanceKm: Math.Round(distanceMeters / 1000.0, 2),
                DurationMinutes: (int)Math.Ceiling(durationSeconds / 60.0)
            );
        }
    }
}
