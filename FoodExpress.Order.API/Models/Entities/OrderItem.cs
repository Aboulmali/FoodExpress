namespace FoodExpress.Order.API.Models.Entities;

public class OrderItem
{
    public Guid Id { get; set; }
    
    public Guid DishId { get; set; }
    public string DishName { get; set; } = string.Empty;
    public string? DishImageUrl { get; set; }
    
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Subtotal => UnitPrice * Quantity;
    
    public string? SpecialInstructions { get; set; }
    
    // Foreign Key
    public Guid OrderId { get; set; }
    public Order Order { get; set; } = null!;
}
