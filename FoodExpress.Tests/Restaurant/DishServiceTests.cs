using FoodExpress.Restaurant.API.Data;
using FoodExpress.Restaurant.API.DTOs;
using FoodExpress.Restaurant.API.Models.Entities;
using FoodExpress.Restaurant.API.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using RestaurantEntity = FoodExpress.Restaurant.API.Models.Entities.Restaurant;

namespace FoodExpress.Tests.Restaurant;

public class DishServiceTests
{
    private readonly Mock<IFileStorageService> _fileStorage;
    private readonly Mock<ICacheService> _cache;
    private readonly Mock<ILogger<DishService>> _logger;

    private static readonly Guid RestaurantId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
    private static readonly Guid CategoryId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid OwnerId = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000001");
    private static readonly Guid OtherOwnerId = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000099");

    private static RestaurantEntity TestRestaurant() =>
        new() { Id = RestaurantId, Name = "R", Address = "a", City = "c", OwnerId = OwnerId };

    public DishServiceTests()
    {
        _fileStorage = new Mock<IFileStorageService>();
        _cache = new Mock<ICacheService>();
        _logger = new Mock<ILogger<DishService>>();
    }

    private static RestaurantDbContext CreateDb(string name)
    {
        var options = new DbContextOptionsBuilder<RestaurantDbContext>()
            .UseInMemoryDatabase(name)
            .Options;
        return new RestaurantDbContext(options);
    }

    [Fact]
    public async Task Create_ValidRestaurantAndCategory_CreatesDishWithStock()
    {
        var db = CreateDb(nameof(Create_ValidRestaurantAndCategory_CreatesDishWithStock));
        db.Restaurants.Add(TestRestaurant());
        db.Categories.Add(new Category { Id = CategoryId, Name = "Pizza" });
        await db.SaveChangesAsync();

        var service = new DishService(db, _fileStorage.Object, _cache.Object, _logger.Object);
        var result = await service.CreateAsync(new CreateDishDto
        {
            Name = "Margherita",
            Price = 60,
            Stock = 50,
            RestaurantId = RestaurantId,
            CategoryId = CategoryId
        }, OwnerId, isAdmin: false);

        Assert.Equal("Margherita", result.Name);
        Assert.Equal(50, result.Stock);
        Assert.Equal(60, result.Price);
        _cache.Verify(c => c.RemoveAsync($"restaurants:id:{RestaurantId}"), Times.Once);
    }

    [Fact]
    public async Task Create_MissingRestaurant_ThrowsKeyNotFound()
    {
        var db = CreateDb(nameof(Create_MissingRestaurant_ThrowsKeyNotFound));
        db.Categories.Add(new Category { Id = CategoryId, Name = "Pizza" });
        await db.SaveChangesAsync();

        var service = new DishService(db, _fileStorage.Object, _cache.Object, _logger.Object);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.CreateAsync(new CreateDishDto { Name = "X", RestaurantId = Guid.NewGuid(), CategoryId = CategoryId },
                OwnerId, isAdmin: false));
    }

    [Fact]
    public async Task Create_AnotherOwnersRestaurant_ThrowsUnauthorized()
    {
        var db = CreateDb(nameof(Create_AnotherOwnersRestaurant_ThrowsUnauthorized));
        db.Restaurants.Add(TestRestaurant());
        db.Categories.Add(new Category { Id = CategoryId, Name = "Pizza" });
        await db.SaveChangesAsync();

        var service = new DishService(db, _fileStorage.Object, _cache.Object, _logger.Object);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.CreateAsync(new CreateDishDto
            {
                Name = "X",
                RestaurantId = RestaurantId,
                CategoryId = CategoryId
            }, OtherOwnerId, isAdmin: false));
    }

    [Fact]
    public async Task Update_UnknownDish_ReturnsNull()
    {
        var db = CreateDb(nameof(Update_UnknownDish_ReturnsNull));
        var service = new DishService(db, _fileStorage.Object, _cache.Object, _logger.Object);

        var result = await service.UpdateAsync(Guid.NewGuid(), new UpdateDishDto(), OwnerId, isAdmin: false);

        Assert.Null(result);
    }

    [Fact]
    public async Task Update_AppliesStockAndAvailability()
    {
        var db = CreateDb(nameof(Update_AppliesStockAndAvailability));
        var id = Guid.NewGuid();
        db.Restaurants.Add(TestRestaurant());
        db.Categories.Add(new Category { Id = CategoryId, Name = "Pizza" });
        db.Dishes.Add(new Dish { Id = id, Name = "Old", Price = 10, Stock = 100, RestaurantId = RestaurantId, CategoryId = CategoryId });
        await db.SaveChangesAsync();

        var service = new DishService(db, _fileStorage.Object, _cache.Object, _logger.Object);
        var result = await service.UpdateAsync(id, new UpdateDishDto
        {
            Name = "New",
            Price = 25,
            Stock = 3,
            IsAvailable = false,
            CategoryId = CategoryId
        }, OwnerId, isAdmin: false);

        Assert.NotNull(result);
        Assert.Equal("New", result.Name);
        Assert.Equal(3, result.Stock);
        Assert.False(result.IsAvailable);
    }

    [Fact]
    public async Task Delete_UnknownDish_ReturnsFalse()
    {
        var db = CreateDb(nameof(Delete_UnknownDish_ReturnsFalse));
        var service = new DishService(db, _fileStorage.Object, _cache.Object, _logger.Object);

        var result = await service.DeleteAsync(Guid.NewGuid(), OwnerId, isAdmin: false);

        Assert.False(result);
    }

    [Fact]
    public async Task GetByRestaurant_ReturnsOnlyAvailableDishes()
    {
        var db = CreateDb(nameof(GetByRestaurant_ReturnsOnlyAvailableDishes));
        db.Restaurants.Add(TestRestaurant());
        db.Categories.Add(new Category { Id = CategoryId, Name = "Pizza" });
        db.Dishes.Add(new Dish { Id = Guid.NewGuid(), Name = "Visible", Price = 20, Stock = 5, IsAvailable = true, RestaurantId = RestaurantId, CategoryId = CategoryId });
        db.Dishes.Add(new Dish { Id = Guid.NewGuid(), Name = "Caché", Price = 20, Stock = 0, IsAvailable = false, RestaurantId = RestaurantId, CategoryId = CategoryId });
        await db.SaveChangesAsync();

        var service = new DishService(db, _fileStorage.Object, _cache.Object, _logger.Object);
        var result = await service.GetByRestaurantAsync(RestaurantId);

        Assert.Single(result);
        Assert.Equal("Visible", result[0].Name);
    }

    [Fact]
    public async Task GetById_ReturnsNullForUnknown()
    {
        var db = CreateDb(nameof(GetById_ReturnsNullForUnknown));
        var service = new DishService(db, _fileStorage.Object, _cache.Object, _logger.Object);

        var result = await service.GetByIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task Update_AnotherOwnersDish_ThrowsUnauthorized()
    {
        var db = CreateDb(nameof(Update_AnotherOwnersDish_ThrowsUnauthorized));
        var id = Guid.NewGuid();
        db.Restaurants.Add(TestRestaurant());
        db.Categories.Add(new Category { Id = CategoryId, Name = "Pizza" });
        db.Dishes.Add(new Dish { Id = id, Name = "Old", Price = 10, Stock = 100, RestaurantId = RestaurantId, CategoryId = CategoryId });
        await db.SaveChangesAsync();

        var service = new DishService(db, _fileStorage.Object, _cache.Object, _logger.Object);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.UpdateAsync(id, new UpdateDishDto { Name = "Hack", Price = 1, CategoryId = CategoryId }, OtherOwnerId, isAdmin: false));
    }

    [Fact]
    public async Task Delete_AnotherOwnersDish_ThrowsUnauthorized()
    {
        var db = CreateDb(nameof(Delete_AnotherOwnersDish_ThrowsUnauthorized));
        var id = Guid.NewGuid();
        db.Restaurants.Add(TestRestaurant());
        db.Categories.Add(new Category { Id = CategoryId, Name = "Pizza" });
        db.Dishes.Add(new Dish { Id = id, Name = "Old", Price = 10, Stock = 100, RestaurantId = RestaurantId, CategoryId = CategoryId });
        await db.SaveChangesAsync();

        var service = new DishService(db, _fileStorage.Object, _cache.Object, _logger.Object);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.DeleteAsync(id, OtherOwnerId, isAdmin: false));
    }

    [Fact]
    public async Task Admin_CanModifyDish_OfAnyRestaurant()
    {
        var db = CreateDb(nameof(Admin_CanModifyDish_OfAnyRestaurant));
        var id = Guid.NewGuid();
        db.Restaurants.Add(TestRestaurant());
        db.Categories.Add(new Category { Id = CategoryId, Name = "Pizza" });
        db.Dishes.Add(new Dish { Id = id, Name = "Old", Price = 10, Stock = 100, RestaurantId = RestaurantId, CategoryId = CategoryId });
        await db.SaveChangesAsync();

        var service = new DishService(db, _fileStorage.Object, _cache.Object, _logger.Object);

        var result = await service.UpdateAsync(id,
            new UpdateDishDto { Name = "ByAdmin", Price = 9, CategoryId = CategoryId }, Guid.Empty, isAdmin: true);

        Assert.Equal("ByAdmin", result!.Name);
    }
}