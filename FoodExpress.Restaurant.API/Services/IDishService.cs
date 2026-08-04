using FoodExpress.Restaurant.API.DTOs;

namespace FoodExpress.Restaurant.API.Services;

public interface IDishService
{
    Task<List<DishDto>> GetByRestaurantAsync(Guid restaurantId);
    Task<DishDto?> GetByIdAsync(Guid id);
    Task<DishDto> CreateAsync(CreateDishDto dto);
    Task<DishDto?> UpdateAsync(Guid id, UpdateDishDto dto);
    Task<bool> DeleteAsync(Guid id);
    Task<string?> UploadImageAsync(Guid id, IFormFile file);
}