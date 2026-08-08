using FoodExpress.Restaurant.API.DTOs;

namespace FoodExpress.Restaurant.API.Services;

public interface IRestaurantService
{
    Task<List<RestaurantDto>> GetAllAsync();
    Task<RestaurantDto?> GetByIdAsync(Guid id);
    Task<RestaurantDto> CreateAsync(CreateRestaurantDto dto, Guid ownerId);
    Task<RestaurantDto?> UpdateAsync(Guid id, UpdateRestaurantDto dto, Guid ownerId);
    Task<bool> DeleteAsync(Guid id, Guid ownerId);
    Task<string?> UploadLogoAsync(Guid id, IFormFile file);
}