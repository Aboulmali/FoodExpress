using FoodExpress.Order.API.DTOs;

namespace FoodExpress.Order.API.Services;

public interface IOrderService
{
    Task<OrderDto> CreateAsync(CreateOrderDto dto, Guid customerId);
    Task<OrderDto?> GetByIdAsync(Guid id, Guid callerId, bool isAdmin);
    Task<List<OrderDto>> GetByCustomerAsync(Guid customerId);
    Task<List<OrderDto>> GetByRestaurantAsync(Guid restaurantId, Guid callerId, bool isAdmin);
    Task<List<OrderDto>> GetByDeliveryPersonAsync(Guid deliveryPersonId);
    Task<List<OrderDto>> GetAllAsync();
    Task<OrderDto?> UpdateStatusAsync(Guid id, UpdateOrderStatusDto dto, Guid callerId, bool isAdmin);
    Task<OrderDto?> UpdateDeliveryStatusAsync(Guid id, UpdateOrderStatusDto dto, Guid? callerId, bool isAdmin);
    Task<OrderDto?> AssignDeliveryAsync(Guid orderId, AssignDeliveryDto dto);
    Task<bool> CancelAsync(Guid id, string reason, Guid callerId, bool isAdmin);
}
