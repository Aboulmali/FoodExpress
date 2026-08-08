using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace FoodExpress.Order.API.Services;

public class RestaurantApiClient : IRestaurantApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<RestaurantApiClient> _logger;
    private readonly string _internalToken;
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public RestaurantApiClient(HttpClient httpClient, IConfiguration config, ILogger<RestaurantApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _internalToken = config["InternalApi:SharedSecret"] ?? string.Empty;
    }

    public async Task<DishInfo?> GetDishAsync(Guid dishId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/Dishes/{dishId}");
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<DishInfo>(json, JsonOpts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur récupération plat {DishId}", dishId);
            return null;
        }
    }

    public async Task<RestaurantInfo?> GetRestaurantAsync(Guid restaurantId)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"/api/Restaurants/{restaurantId}/internal");
            request.Headers.Add("X-Internal-Token", _internalToken);

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<RestaurantInfo>(json, JsonOpts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur récupération restaurant {RestaurantId}", restaurantId);
            return null;
        }
    }
}
