namespace FoodExpress.Restaurant.API.Models.Entities;

public class Dish
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string? ImageUrl { get; set; }

    public int Stock { get; set; } = 100;
    public bool IsAvailable { get; set; } = true;
    public bool IsVegetarian { get; set; }
    public bool IsSpicy { get; set; }

    public int PreparationTimeMinutes { get; set; } = 20;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Foreign Keys
    public Guid RestaurantId { get; set; }
    public Restaurant Restaurant { get; set; } = null!;

    public Guid CategoryId { get; set; }
    public Category Category { get; set; } = null!;
}