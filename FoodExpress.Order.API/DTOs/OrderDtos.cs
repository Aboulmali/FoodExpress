using FoodExpress.Order.API.Models.Entities;

namespace FoodExpress.Order.API.DTOs;

public class OrderDto
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public Guid RestaurantId { get; set; }
    public string RestaurantName { get; set; } = string.Empty;
    public string DeliveryAddress { get; set; } = string.Empty;
    public decimal Subtotal { get; set; }
    public decimal DeliveryFee { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DeliveryPersonDto? Delivery { get; set; }
    public List<OrderItemDto> Items { get; set; } = new();
}

public class DeliveryPersonDto
{
    public Guid DeliveryPersonId { get; set; }
    public string DeliveryPersonName { get; set; } = string.Empty;
    public string DeliveryPersonPhone { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? AssignedAt { get; set; }
}

public class OrderItemDto
{
    public Guid Id { get; set; }
    public Guid DishId { get; set; }
    public string DishName { get; set; } = string.Empty;
    public string? DishImageUrl { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Subtotal { get; set; }
    public string? SpecialInstructions { get; set; }
}

public class CreateOrderDto
{
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public Guid RestaurantId { get; set; }
    public string DeliveryAddress { get; set; } = string.Empty;
    public double DeliveryLatitude { get; set; }
    public double DeliveryLongitude { get; set; }
    public string? Notes { get; set; }
    public List<CreateOrderItemDto> Items { get; set; } = new();
}

public class CreateOrderItemDto
{
    public Guid DishId { get; set; }
    public int Quantity { get; set; }
    public string? SpecialInstructions { get; set; }
}

public class UpdateOrderStatusDto
{
    public OrderStatus NewStatus { get; set; }
    public string? Reason { get; set; }
}

public class AssignDeliveryDto
{
    public Guid DeliveryPersonId { get; set; }
    public string DeliveryPersonName { get; set; } = string.Empty;
    public string DeliveryPersonPhone { get; set; } = string.Empty;
}
