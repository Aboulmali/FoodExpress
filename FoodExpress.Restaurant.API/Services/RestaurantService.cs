using FoodExpress.Restaurant.API.Data;
using FoodExpress.Restaurant.API.DTOs;
using FoodExpress.Restaurant.API.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace FoodExpress.Restaurant.API.Services;

public class RestaurantService : IRestaurantService
{
    private readonly RestaurantDbContext _db;
    private readonly ICacheService _cache;
    private readonly IFileStorageService _fileStorage;
    private readonly ILogger<RestaurantService> _logger;

    private const string CacheKeyAll = "restaurants:all";
    private const string CacheKeyById = "restaurants:id:";

    public RestaurantService(
        RestaurantDbContext db,
        ICacheService cache,
        IFileStorageService fileStorage,
        ILogger<RestaurantService> logger)
    {
        _db = db;
        _cache = cache;
        _fileStorage = fileStorage;
        _logger = logger;
    }

    public async Task<List<RestaurantDto>> GetAllAsync()
    {
        // 1. Chercher dans le cache
        var cached = await _cache.GetAsync<List<RestaurantDto>>(CacheKeyAll);
        if (cached != null)
        {
            _logger.LogInformation("✅ Cache HIT - restaurants:all");
            return cached;
        }

        _logger.LogInformation("❌ Cache MISS - fetching from DB");

        // 2. Sinon, aller en BDD
        var restaurants = await _db.Restaurants
            .Include(r => r.Dishes)
            .Where(r => r.IsActive)
            .ToListAsync();

        var dtos = restaurants.Select(MapToDto).ToList();

        // 3. Sauvegarder dans le cache (5 minutes)
        await _cache.SetAsync(CacheKeyAll, dtos, TimeSpan.FromMinutes(5));

        return dtos;
    }

    public async Task<RestaurantDto?> GetByIdAsync(Guid id)
    {
        var cacheKey = CacheKeyById + id;
        var cached = await _cache.GetAsync<RestaurantDto>(cacheKey);
        if (cached != null) return cached;

        var restaurant = await _db.Restaurants
            .Include(r => r.Dishes)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (restaurant == null) return null;

        var dto = MapToDto(restaurant);
        await _cache.SetAsync(cacheKey, dto, TimeSpan.FromMinutes(10));
        return dto;
    }

    public async Task<RestaurantDto> CreateAsync(CreateRestaurantDto dto, Guid ownerId)
    {
        var restaurant = new Models.Entities.Restaurant
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Description = dto.Description,
            Address = dto.Address,
            City = dto.City,
            PhoneNumber = dto.PhoneNumber,
            Email = dto.Email,
            Latitude = dto.Latitude,
            Longitude = dto.Longitude,
            OpeningTime = dto.OpeningTime,
            ClosingTime = dto.ClosingTime,
            OwnerId = ownerId
        };

        _db.Restaurants.Add(restaurant);
        await _db.SaveChangesAsync();

        // Invalider le cache
        await _cache.RemoveAsync(CacheKeyAll);

        _logger.LogInformation("Restaurant created: {Name}", dto.Name);
        return MapToDto(restaurant);
    }

    public async Task<RestaurantDto?> UpdateAsync(Guid id, UpdateRestaurantDto dto, Guid ownerId)
    {
        var restaurant = await _db.Restaurants.FindAsync(id);
        if (restaurant == null) return null;
        if (restaurant.OwnerId != ownerId)
            throw new UnauthorizedAccessException("Vous ne pouvez modifier que vos propres restaurants.");

        restaurant.Name = dto.Name;
        restaurant.Description = dto.Description;
        restaurant.Address = dto.Address;
        restaurant.City = dto.City;
        restaurant.PhoneNumber = dto.PhoneNumber;
        restaurant.OpeningTime = dto.OpeningTime;
        restaurant.ClosingTime = dto.ClosingTime;
        restaurant.IsOpen = dto.IsOpen;
        restaurant.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        // Invalider le cache
        await _cache.RemoveAsync(CacheKeyAll);
        await _cache.RemoveAsync(CacheKeyById + id);

        return MapToDto(restaurant);
    }

    public async Task<bool> DeleteAsync(Guid id, Guid ownerId)
    {
        var restaurant = await _db.Restaurants.FindAsync(id);
        if (restaurant == null) return false;
        if (restaurant.OwnerId != ownerId)
            throw new UnauthorizedAccessException("Vous ne pouvez supprimer que vos propres restaurants.");

        _db.Restaurants.Remove(restaurant);
        await _db.SaveChangesAsync();

        await _cache.RemoveAsync(CacheKeyAll);
        await _cache.RemoveAsync(CacheKeyById + id);

        return true;
    }

    public async Task<string?> UploadLogoAsync(Guid id, IFormFile file)
    {
        var restaurant = await _db.Restaurants.FindAsync(id);
        if (restaurant == null) return null;

        // Supprimer l'ancien logo s'il existe
        if (!string.IsNullOrEmpty(restaurant.LogoUrl))
            await _fileStorage.DeleteFileAsync(restaurant.LogoUrl);

        // Uploader le nouveau
        var url = await _fileStorage.UploadFileAsync(file, "restaurants");
        restaurant.LogoUrl = url;
        restaurant.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        await _cache.RemoveAsync(CacheKeyAll);
        await _cache.RemoveAsync(CacheKeyById + id);

        return url;
    }

    private static RestaurantDto MapToDto(Models.Entities.Restaurant r) => new()
    {
        Id = r.Id,
        Name = r.Name,
        Description = r.Description,
        Address = r.Address,
        City = r.City,
        PhoneNumber = r.PhoneNumber,
        Email = r.Email,
        LogoUrl = r.LogoUrl,
        CoverImageUrl = r.CoverImageUrl,
        Latitude = r.Latitude,
        Longitude = r.Longitude,
        OpeningTime = r.OpeningTime,
        ClosingTime = r.ClosingTime,
        Rating = r.Rating,
        OwnerId = r.OwnerId,
        IsActive = r.IsActive,
        IsOpen = r.IsOpen,
        DishesCount = r.Dishes.Count
    };
}