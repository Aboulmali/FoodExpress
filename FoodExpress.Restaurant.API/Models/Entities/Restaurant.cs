namespace FoodExpress.Restaurant.API.Models.Entities;

public class Restaurant
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    public string? LogoUrl { get; set; }
    public string? CoverImageUrl { get; set; }

    public double Latitude { get; set; }
    public double Longitude { get; set; }

    public string OpeningTime { get; set; } = "09:00";
    public string ClosingTime { get; set; } = "23:00";

    public double Rating { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsOpen { get; set; } = true;

    // ID de l'utilisateur propriétaire (dans Keycloak/User Service)
    public Guid OwnerId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Relations
    public List<Dish> Dishes { get; set; } = new();
}