namespace FoodExpress.User.API.Models.Entities;

public class Address
{
    public Guid Id { get; set; }

    public string Label { get; set; } = string.Empty;
    public string Street { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string Country { get; set; } = "Maroc";

    public double Latitude { get; set; }
    public double Longitude { get; set; }

    public bool IsDefault { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Foreign key vers AppUser
    public Guid UserId { get; set; }
    public AppUser User { get; set; } = null!;
}