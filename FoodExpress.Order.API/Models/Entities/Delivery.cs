namespace FoodExpress.Order.API.Models.Entities;

public class Delivery
{
    public Guid Id { get; set; }
    
    public Guid OrderId { get; set; }
    public Order Order { get; set; } = null!;
    
    public Guid? DeliveryPersonId { get; set; }
    public string? DeliveryPersonName { get; set; }
    public string? DeliveryPersonPhone { get; set; }
    
    public DeliveryStatus Status { get; set; } = DeliveryStatus.Pending;
    
    public double? CurrentLatitude { get; set; }
    public double? CurrentLongitude { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? AssignedAt { get; set; }
    public DateTime? PickedUpAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
}

public enum DeliveryStatus
{
    Pending = 0,
    Assigned = 1,
    PickedUp = 2,
    Delivered = 3
}
