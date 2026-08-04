namespace FoodExpress.Order.API.Services;

public interface IRestaurantApiClient
{
    Task<DishInfo?> GetDishAsync(Guid dishId);
    Task<RestaurantInfo?> GetRestaurantAsync(Guid restaurantId);
}

public class DishInfo
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsAvailable { get; set; }
    public Guid RestaurantId { get; set; }
}

public class RestaurantInfo
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsOpen { get; set; }
}
