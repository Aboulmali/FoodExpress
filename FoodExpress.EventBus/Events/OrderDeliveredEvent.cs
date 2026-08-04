namespace FoodExpress.EventBus.Events;

public class OrderDeliveredEvent : IntegrationEvent
{
    public Guid OrderId { get; set; }
    public Guid CustomerId { get; set; }
    public Guid DeliveryPersonId { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime DeliveredAt { get; set; }
}
