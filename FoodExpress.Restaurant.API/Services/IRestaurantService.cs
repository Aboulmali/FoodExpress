using FoodExpress.Restaurant.API.DTOs;

namespace FoodExpress.Restaurant.API.Services;

public interface IRestaurantService
{
    Task<List<RestaurantDto>> GetAllAsync();
    Task<RestaurantDto?> GetByIdAsync(Guid id);
    Task<RestaurantDto> CreateAsync(CreateRestaurantDto dto);
    Task<RestaurantDto?> UpdateAsync(Guid id, UpdateRestaurantDto dto);
    Task<bool> DeleteAsync(Guid id);
    Task<string?> UploadLogoAsync(Guid id, IFormFile file);
}