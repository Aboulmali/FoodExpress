using FoodExpress.Restaurant.API.Data;
using FoodExpress.Restaurant.API.DTOs;
using FoodExpress.Restaurant.API.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace FoodExpress.Restaurant.API.Services;

public class DishService : IDishService
{
    private readonly RestaurantDbContext _db;
    private readonly IFileStorageService _fileStorage;
    private readonly ICacheService _cache;
    private readonly ILogger<DishService> _logger;

    public DishService(
        RestaurantDbContext db,
        IFileStorageService fileStorage,
        ICacheService cache,
        ILogger<DishService> logger)
    {
        _db = db;
        _fileStorage = fileStorage;
        _cache = cache;
        _logger = logger;
    }

    public async Task<List<DishDto>> GetByRestaurantAsync(Guid restaurantId)
    {
        var dishes = await _db.Dishes
            .Include(d => d.Restaurant)
            .Include(d => d.Category)
            .Where(d => d.RestaurantId == restaurantId && d.IsAvailable)
            .ToListAsync();

        return dishes.Select(MapToDto).ToList();
    }

    public async Task<DishDto?> GetByIdAsync(Guid id)
    {
        var dish = await _db.Dishes
            .Include(d => d.Restaurant)
            .Include(d => d.Category)
            .FirstOrDefaultAsync(d => d.Id == id);

        return dish == null ? null : MapToDto(dish);
    }

    public async Task<DishDto> CreateAsync(CreateDishDto dto, Guid callerId, bool isAdmin)
    {
        // Vérifier que le restaurant et la catégorie existent
        var restaurant = await _db.Restaurants.FindAsync(dto.RestaurantId);
        if (restaurant == null)
            throw new KeyNotFoundException("Restaurant introuvable");

        // Sécurité : un RestaurantOwner ne crée un plat que dans SES restaurants
        if (!isAdmin && restaurant.OwnerId != callerId)
            throw new UnauthorizedAccessException("Ce restaurant ne vous appartient pas");

        var categoryExists = await _db.Categories.AnyAsync(c => c.Id == dto.CategoryId);
        if (!categoryExists)
            throw new KeyNotFoundException("Catégorie introuvable");

        var dish = new Dish
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Description = dto.Description,
            Price = dto.Price,
            Stock = dto.Stock,
            IsVegetarian = dto.IsVegetarian,
            IsSpicy = dto.IsSpicy,
            PreparationTimeMinutes = dto.PreparationTimeMinutes,
            RestaurantId = dto.RestaurantId,
            CategoryId = dto.CategoryId
        };

        _db.Dishes.Add(dish);
        await _db.SaveChangesAsync();

        // Invalider le cache du restaurant
        await _cache.RemoveAsync($"restaurants:id:{dto.RestaurantId}");

        return await GetByIdAsync(dish.Id) ?? throw new Exception("Erreur");
    }

    public async Task<DishDto?> UpdateAsync(Guid id, UpdateDishDto dto, Guid callerId, bool isAdmin)
    {
        var dish = await _db.Dishes.FindAsync(id);
        if (dish == null) return null;

        await EnsureOwnerAsync(dish.RestaurantId, callerId, isAdmin);

        dish.Name = dto.Name;
        dish.Description = dto.Description;
        dish.Price = dto.Price;
        dish.Stock = dto.Stock;
        dish.IsAvailable = dto.IsAvailable;
        dish.IsVegetarian = dto.IsVegetarian;
        dish.IsSpicy = dto.IsSpicy;
        dish.PreparationTimeMinutes = dto.PreparationTimeMinutes;
        dish.CategoryId = dto.CategoryId;
        dish.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        await _cache.RemoveAsync($"restaurants:id:{dish.RestaurantId}");

        return await GetByIdAsync(id);
    }

    public async Task<bool> DeleteAsync(Guid id, Guid callerId, bool isAdmin)
    {
        var dish = await _db.Dishes.FindAsync(id);
        if (dish == null) return false;

        await EnsureOwnerAsync(dish.RestaurantId, callerId, isAdmin);

        if (!string.IsNullOrEmpty(dish.ImageUrl))
            await _fileStorage.DeleteFileAsync(dish.ImageUrl);

        _db.Dishes.Remove(dish);
        await _db.SaveChangesAsync();

        await _cache.RemoveAsync($"restaurants:id:{dish.RestaurantId}");
        return true;
    }

    public async Task<string?> UploadImageAsync(Guid id, IFormFile file, Guid callerId, bool isAdmin)
    {
        var dish = await _db.Dishes.FindAsync(id);
        if (dish == null) return null;

        await EnsureOwnerAsync(dish.RestaurantId, callerId, isAdmin);

        if (!string.IsNullOrEmpty(dish.ImageUrl))
            await _fileStorage.DeleteFileAsync(dish.ImageUrl);

        var url = await _fileStorage.UploadFileAsync(file, "dishes");
        dish.ImageUrl = url;
        dish.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        await _cache.RemoveAsync($"restaurants:id:{dish.RestaurantId}");

        return url;
    }

    // Sécurité : le plat appartient à un restaurant dont le caller doit être le propriétaire
    private async Task EnsureOwnerAsync(Guid restaurantId, Guid callerId, bool isAdmin)
    {
        if (isAdmin) return;

        var restaurant = await _db.Restaurants.FindAsync(restaurantId);
        if (restaurant == null)
            throw new KeyNotFoundException("Restaurant introuvable");

        if (restaurant.OwnerId != callerId)
            throw new UnauthorizedAccessException("Ce restaurant ne vous appartient pas");
    }

    private static DishDto MapToDto(Dish d) => new()
    {
        Id = d.Id,
        Name = d.Name,
        Description = d.Description,
        Price = d.Price,
        ImageUrl = d.ImageUrl,
        Stock = d.Stock,
        IsAvailable = d.IsAvailable,
        IsVegetarian = d.IsVegetarian,
        IsSpicy = d.IsSpicy,
        PreparationTimeMinutes = d.PreparationTimeMinutes,
        RestaurantId = d.RestaurantId,
        RestaurantName = d.Restaurant?.Name ?? "",
        CategoryId = d.CategoryId,
        CategoryName = d.Category?.Name ?? ""
    };
}