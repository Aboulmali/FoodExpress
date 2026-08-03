namespace FoodExpress.User.API.Models.Entities;

public class AppUser
{
    public Guid Id { get; set; }

    // Lien avec Keycloak
    public string KeycloakId { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;

    public UserRole Role { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public bool IsActive { get; set; } = true;

    // Relation : un user a plusieurs adresses
    public List<Address> Addresses { get; set; } = new();
}

public enum UserRole
{
    Customer = 0,
    RestaurantOwner = 1,
    DeliveryPerson = 2,
    Admin = 3
}