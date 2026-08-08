using FoodExpress.Restaurant.API.DTOs;

namespace FoodExpress.Restaurant.API.Services;

public interface IDishService
{
    Task<List<DishDto>> GetByRestaurantAsync(Guid restaurantId);
    Task<DishDto?> GetByIdAsync(Guid id);
    Task<DishDto> CreateAsync(CreateDishDto dto, Guid callerId, bool isAdmin);
    Task<DishDto?> UpdateAsync(Guid id, UpdateDishDto dto, Guid callerId, bool isAdmin);
    Task<bool> DeleteAsync(Guid id, Guid callerId, bool isAdmin);
    Task<string?> UploadImageAsync(Guid id, IFormFile file, Guid callerId, bool isAdmin);
}