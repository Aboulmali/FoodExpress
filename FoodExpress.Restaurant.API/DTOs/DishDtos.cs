namespace FoodExpress.Restaurant.API.DTOs;

public class DishDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsAvailable { get; set; }
    public bool IsVegetarian { get; set; }
    public bool IsSpicy { get; set; }
    public int PreparationTimeMinutes { get; set; }
    public Guid RestaurantId { get; set; }
    public string RestaurantName { get; set; } = string.Empty;
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
}

public class CreateDishDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public bool IsVegetarian { get; set; }
    public bool IsSpicy { get; set; }
    public int PreparationTimeMinutes { get; set; } = 20;
    public Guid RestaurantId { get; set; }
    public Guid CategoryId { get; set; }
}

public class UpdateDishDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public bool IsAvailable { get; set; }
    public bool IsVegetarian { get; set; }
    public bool IsSpicy { get; set; }
    public int PreparationTimeMinutes { get; set; }
    public Guid CategoryId { get; set; }
}