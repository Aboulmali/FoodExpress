namespace FoodExpress.Order.API.Models.Entities;

public class Order
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    
    // Client
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    
    // Restaurant
    public Guid RestaurantId { get; set; }
    public string RestaurantName { get; set; } = string.Empty;
    
    // Livraison
    public string DeliveryAddress { get; set; } = string.Empty;
    public double DeliveryLatitude { get; set; }
    public double DeliveryLongitude { get; set; }
    public decimal DeliveryFee { get; set; } = 15.00m;
    
    // Montants
    public decimal Subtotal { get; set; }
    public decimal TotalAmount { get; set; }
    
    // Statut
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public string? Notes { get; set; }
    
    // Dates
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? AcceptedAt { get; set; }
    public DateTime? PreparingAt { get; set; }
    public DateTime? ReadyAt { get; set; }
    public DateTime? OnDeliveryAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancellationReason { get; set; }
    
    // Relations
    public List<OrderItem> Items { get; set; } = new();
    public Delivery? Delivery { get; set; }
}

public enum OrderStatus
{
    Pending = 0,       // En attente d'acceptation par le restaurant
    Accepted = 1,      // Accepté par le restaurant
    Preparing = 2,     // En préparation
    Ready = 3,         // Prêt à être récupéré
    OnDelivery = 4,    // En livraison
    Delivered = 5,     // Livré
    Cancelled = 6      // Annulé
}
