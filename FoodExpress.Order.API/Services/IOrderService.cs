using FoodExpress.Order.API.DTOs;

namespace FoodExpress.Order.API.Services;

public interface IOrderService
{
    Task<OrderDto> CreateAsync(CreateOrderDto dto);
    Task<OrderDto?> GetByIdAsync(Guid id);
    Task<List<OrderDto>> GetByCustomerAsync(Guid customerId);
    Task<List<OrderDto>> GetByRestaurantAsync(Guid restaurantId);
    Task<List<OrderDto>> GetAllAsync();
    Task<OrderDto?> UpdateStatusAsync(Guid id, UpdateOrderStatusDto dto);
    Task<OrderDto?> AssignDeliveryAsync(Guid orderId, AssignDeliveryDto dto);
    Task<bool> CancelAsync(Guid id, string reason);
}
