using FoodExpress.Restaurant.API.Data;
using FoodExpress.Restaurant.API.DTOs;
using FoodExpress.Restaurant.API.Models.Entities;
using FoodExpress.Restaurant.API.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using RestaurantEntity = FoodExpress.Restaurant.API.Models.Entities.Restaurant;

namespace FoodExpress.Tests.Restaurant;

public class RestaurantServiceTests
{
    private readonly Mock<ICacheService> _cache;
    private readonly Mock<IFileStorageService> _fileStorage;
    private readonly Mock<ILogger<RestaurantService>> _logger;

    public RestaurantServiceTests()
    {
        _cache = new Mock<ICacheService>();
        _fileStorage = new Mock<IFileStorageService>();
        _logger = new Mock<ILogger<RestaurantService>>();
    }

    private static RestaurantDbContext CreateDb(string name)
    {
        var options = new DbContextOptionsBuilder<RestaurantDbContext>()
            .UseInMemoryDatabase(name)
            .Options;
        return new RestaurantDbContext(options);
    }

    [Fact]
    public async Task GetAll_CacheMiss_FetchesOnlyActiveFromDbAndCaches()
    {
        var db = CreateDb(nameof(GetAll_CacheMiss_FetchesOnlyActiveFromDbAndCaches));
        var active = Guid.NewGuid();
        var inactive = Guid.NewGuid();
        db.Restaurants.Add(new RestaurantEntity { Id = active, Name = "Active", Address = "1", City = "Casablanca", IsActive = true });
        db.Restaurants.Add(new RestaurantEntity { Id = inactive, Name = "Inactive", Address = "2", City = "Rabat", IsActive = false });
        await db.SaveChangesAsync();

        _cache.Setup(c => c.GetAsync<List<RestaurantDto>>("restaurants:all")).ReturnsAsync(() => (List<RestaurantDto>?)null);

        var service = new RestaurantService(db, _cache.Object, _fileStorage.Object, _logger.Object);
        var result = await service.GetAllAsync();

        Assert.Single(result);
        Assert.Equal("Active", result[0].Name);
        _cache.Verify(c => c.SetAsync("restaurants:all", It.IsAny<List<RestaurantDto>>(), It.IsAny<TimeSpan?>()), Times.Once);
    }

    [Fact]
    public async Task GetAll_CacheHit_ReturnsCachedWithoutDb()
    {
        var db = CreateDb(nameof(GetAll_CacheHit_ReturnsCachedWithoutDb));
        var cachedList = new List<RestaurantDto> { new() { Id = Guid.NewGuid(), Name = "FromCache" } };

        var cache = new Mock<ICacheService>();
        cache.Setup(c => c.GetAsync<List<RestaurantDto>>("restaurants:all")).ReturnsAsync(cachedList);

        var service = new RestaurantService(db, cache.Object, _fileStorage.Object, _logger.Object);
        var result = await service.GetAllAsync();

        Assert.Single(result);
        Assert.Equal("FromCache", result[0].Name);
        // Le coup de Redis : hit => pas de requête, pas de re-set
        cache.Verify(c => c.SetAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<TimeSpan?>()), Times.Never);
    }

    [Fact]
    public async Task Create_SetsOwnerIdAndInvalidatesCache()
    {
        var db = CreateDb(nameof(Create_SetsOwnerIdAndInvalidatesCache));
        var ownerId = Guid.NewGuid();

        var service = new RestaurantService(db, _cache.Object, _fileStorage.Object, _logger.Object);
        var created = await service.CreateAsync(new CreateRestaurantDto
        {
            Name = "Pizzeria 2000",
            Address = "45 rue X",
            City = "Casablanca"
        }, ownerId);

        Assert.Equal("Pizzeria 2000", created.Name);
        Assert.Equal(ownerId, db.Restaurants.Single().OwnerId);
        Assert.Equal(1, db.Restaurants.Count());
        _cache.Verify(c => c.RemoveAsync("restaurants:all"), Times.Once);
    }

    [Fact]
    public async Task Create_IgnorOwnerIdFromBody()
    {
        var db = CreateDb(nameof(Create_IgnorOwnerIdFromBody));
        var ownerId = Guid.NewGuid();

        var service = new RestaurantService(db, _cache.Object, _fileStorage.Object, _logger.Object);
        var created = await service.CreateAsync(new CreateRestaurantDto
        {
            Name = "Proprio JWT",
            Address = "10 rue Y",
            City = "Casablanca"
        }, ownerId);

        Assert.Equal(ownerId, db.Restaurants.Single().OwnerId);
    }

    [Fact]
    public async Task GetById_NotFound_ReturnsNull()
    {
        var db = CreateDb(nameof(GetById_NotFound_ReturnsNull));
        var service = new RestaurantService(db, _cache.Object, _fileStorage.Object, _logger.Object);

        var result = await service.GetByIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task Update_NotFound_ReturnsNull()
    {
        var db = CreateDb(nameof(Update_NotFound_ReturnsNull));
        var service = new RestaurantService(db, _cache.Object, _fileStorage.Object, _logger.Object);

        var result = await service.UpdateAsync(Guid.NewGuid(), new UpdateRestaurantDto(), Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task Update_NotOwner_Throws()
    {
        var db = CreateDb(nameof(Update_NotOwner_Throws));
        var id = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        db.Restaurants.Add(new RestaurantEntity { Id = id, Name = "Mon resto", Address = "a", City = "c", OwnerId = ownerId });
        await db.SaveChangesAsync();

        var service = new RestaurantService(db, _cache.Object, _fileStorage.Object, _logger.Object);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.UpdateAsync(id, new UpdateRestaurantDto { Name = "Hijacked" }, Guid.NewGuid()));
    }

    [Fact]
    public async Task Update_Existing_AppliesChangesAndInvalidatesCache()
    {
        var db = CreateDb(nameof(Update_Existing_AppliesChangesAndInvalidatesCache));
        var id = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        db.Restaurants.Add(new RestaurantEntity { Id = id, Name = "Old", Address = "a", City = "c", Email = "", OwnerId = ownerId });
        await db.SaveChangesAsync();

        var service = new RestaurantService(db, _cache.Object, _fileStorage.Object, _logger.Object);
        var result = await service.UpdateAsync(id, new UpdateRestaurantDto { Name = "New", Address = "b", City = "c", IsOpen = false }, ownerId);

        Assert.NotNull(result);
        Assert.Equal("New", result.Name);
        Assert.False(result.IsOpen);
        _cache.Verify(c => c.RemoveAsync("restaurants:all"), Times.Once);
        _cache.Verify(c => c.RemoveAsync($"restaurants:id:{id}"), Times.Once);
    }

    [Fact]
    public async Task Delete_NotFound_ReturnsFalse()
    {
        var db = CreateDb(nameof(Delete_NotFound_ReturnsFalse));
        var service = new RestaurantService(db, _cache.Object, _fileStorage.Object, _logger.Object);

        var result = await service.DeleteAsync(Guid.NewGuid(), Guid.NewGuid());

        Assert.False(result);
    }

    [Fact]
    public async Task Delete_NotOwner_Throws()
    {
        var db = CreateDb(nameof(Delete_NotOwner_Throws));
        var id = Guid.NewGuid();
        db.Restaurants.Add(new RestaurantEntity { Id = id, Name = "Mon resto", Address = "a", City = "c", OwnerId = Guid.NewGuid() });
        await db.SaveChangesAsync();

        var service = new RestaurantService(db, _cache.Object, _fileStorage.Object, _logger.Object);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.DeleteAsync(id, Guid.NewGuid()));
    }

    [Fact]
    public async Task Delete_Existing_RemovesAndInvalidates()
    {
        var db = CreateDb(nameof(Delete_Existing_RemovesAndInvalidates));
        var id = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        db.Restaurants.Add(new RestaurantEntity { Id = id, Name = "To Delete", Address = "a", City = "c", OwnerId = ownerId });
        await db.SaveChangesAsync();

        var service = new RestaurantService(db, _cache.Object, _fileStorage.Object, _logger.Object);
        var result = await service.DeleteAsync(id, ownerId);

        Assert.True(result);
        Assert.Empty(db.Restaurants);
        _cache.Verify(c => c.RemoveAsync("restaurants:all"), Times.Once);
    }

    [Fact]
    public async Task GetMine_ReturnsOnlyOwnRestaurants()
    {
        var db = CreateDb(nameof(GetMine_ReturnsOnlyOwnRestaurants));
        var ownerId = Guid.NewGuid();
        var otherId = Guid.NewGuid();
        db.Restaurants.Add(new RestaurantEntity { Id = Guid.NewGuid(), Name = "Mien", Address = "a", City = "c", OwnerId = ownerId });
        db.Restaurants.Add(new RestaurantEntity { Id = Guid.NewGuid(), Name = "A moi aussi", Address = "b", City = "c", OwnerId = ownerId });
        db.Restaurants.Add(new RestaurantEntity { Id = Guid.NewGuid(), Name = "A un autre", Address = "c", City = "c", OwnerId = otherId });
        await db.SaveChangesAsync();

        var service = new RestaurantService(db, _cache.Object, _fileStorage.Object, _logger.Object);
        var result = await service.GetMineAsync(ownerId);

        Assert.Equal(2, result.Count);
        Assert.All(result, r =>
            Assert.Contains(r.Name, new[] { "Mien", "A moi aussi" }));
    }

    [Fact]
    public async Task PublicDto_DoesNotExposeOwnerId()
    {
        var db = CreateDb(nameof(PublicDto_DoesNotExposeOwnerId));
        db.Restaurants.Add(new RestaurantEntity { Id = Guid.NewGuid(), Name = "R", Address = "a", City = "c" });
        await db.SaveChangesAsync();

        _cache.Setup(c => c.GetAsync<List<RestaurantDto>>("restaurants:all")).ReturnsAsync(() => (List<RestaurantDto>?)null);

        var service = new RestaurantService(db, _cache.Object, _fileStorage.Object, _logger.Object);
        var result = await service.GetAllAsync();

        var json = System.Text.Json.JsonSerializer.Serialize(result);
        Assert.DoesNotContain("OwnerId", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UploadLogo_NotOwner_Throws()
    {
        var db = CreateDb(nameof(UploadLogo_NotOwner_Throws));
        var id = Guid.NewGuid();
        db.Restaurants.Add(new RestaurantEntity { Id = id, Name = "R", Address = "a", City = "c", OwnerId = Guid.NewGuid() });
        await db.SaveChangesAsync();

        var service = new RestaurantService(db, _cache.Object, _fileStorage.Object, _logger.Object);

        var file = new FormFile(new MemoryStream(new byte[10]), 0, 10, "logo", "logo.png")
        {
            Headers = new HeaderDictionary { ["Content-Type"] = "image/png" }
        };

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.UploadLogoAsync(id, file, Guid.NewGuid(), isAdmin: false));
    }

    [Fact]
    public async Task UploadLogo_Owner_Succeeds()
    {
        var db = CreateDb(nameof(UploadLogo_Owner_Succeeds));
        var id = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        db.Restaurants.Add(new RestaurantEntity { Id = id, Name = "R", Address = "a", City = "c", OwnerId = ownerId });
        await db.SaveChangesAsync();

        _fileStorage.Setup(f => f.UploadFileAsync(It.IsAny<IFormFile>(), "restaurants")).ReturnsAsync("http://minio/logo.png");

        var service = new RestaurantService(db, _cache.Object, _fileStorage.Object, _logger.Object);
        var file = new FormFile(new MemoryStream(new byte[10]), 0, 10, "logo", "logo.png")
        {
            Headers = new HeaderDictionary { ["Content-Type"] = "image/png" }
        };

        var url = await service.UploadLogoAsync(id, file, ownerId, isAdmin: false);

        Assert.Equal("http://minio/logo.png", url);
    }
}