using FoodExpress.EventBus.Events;
using FoodExpress.Restaurant.API.Data;
using FoodExpress.Restaurant.API.Handlers;
using FoodExpress.Restaurant.API.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace FoodExpress.Tests.Restaurant;

public class StockHandlersTests
{
    private readonly Guid _dishId;
    private readonly Dish _dish;

    public StockHandlersTests()
    {
        _dishId = Guid.NewGuid();
        _dish = new Dish { Id = _dishId, Name = "Pizza", Price = 60, Stock = 50, IsAvailable = true };
    }

    private static RestaurantDbContext CreateDb(string name)
    {
        var options = new DbContextOptionsBuilder<RestaurantDbContext>()
            .UseInMemoryDatabase(name)
            .Options;
        return new RestaurantDbContext(options);
    }

    private static void SeedDish(RestaurantDbContext db, Dish dish)
    {
        db.Dishes.Add(dish);
        db.SaveChanges();
    }

    private static OrderCreatedEvent CreatedEvent(Guid eventId, Guid dishId) => new()
    {
        Id = eventId,
        OrderId = Guid.NewGuid(),
        CustomerId = Guid.NewGuid(),
        RestaurantId = Guid.NewGuid(),
        Items = new List<OrderItemInfo> { new() { DishId = dishId, DishName = "Pizza", Quantity = 2 } }
    };

    [Fact]
    public async Task OrderCreated_DecrementsStock_AndMarksProcessed()
    {
        var db = CreateDb(nameof(OrderCreated_DecrementsStock_AndMarksProcessed));
        SeedDish(db, _dish);
        var handler = new OrderCreatedStockHandler(db, new Mock<ILogger<OrderCreatedStockHandler>>().Object);

        await handler.HandleAsync(CreatedEvent(Guid.NewGuid(), _dishId));

        Assert.Equal(48, db.Dishes.Single().Stock);
        Assert.True(db.Dishes.Single().IsAvailable);
        Assert.Single(db.ProcessedMessages);
    }

    [Fact]
    public async Task OrderCreated_StockReachesZero_DisablesAvailability()
    {
        var db = CreateDb(nameof(OrderCreated_StockReachesZero_DisablesAvailability));
        _dish.Stock = 1;
        SeedDish(db, _dish);
        var handler = new OrderCreatedStockHandler(db, new Mock<ILogger<OrderCreatedStockHandler>>().Object);

        await handler.HandleAsync(CreatedEvent(Guid.NewGuid(), _dishId));

        var updated = db.Dishes.Single();
        Assert.Equal(0, updated.Stock);
        Assert.False(updated.IsAvailable);
    }

    [Fact]
    public async Task OrderCreated_AlreadyProcessed_UsesSingleDecrement()
    {
        var db = CreateDb(nameof(OrderCreated_AlreadyProcessed_UsesSingleDecrement));
        SeedDish(db, _dish);
        var handler = new OrderCreatedStockHandler(db, new Mock<ILogger<OrderCreatedStockHandler>>().Object);
        var evt = CreatedEvent(Guid.NewGuid(), _dishId);

        await handler.HandleAsync(evt); // traitement initial
        await handler.HandleAsync(evt); // redélivrance -> doit être ignorée

        Assert.Equal(48, db.Dishes.Single().Stock);
        Assert.Single(db.ProcessedMessages);
    }

    [Fact]
    public async Task OrderCancelled_NonCancelledStatus_DoesNothing()
    {
        var db = CreateDb(nameof(OrderCancelled_NonCancelledStatus_DoesNothing));
        SeedDish(db, _dish);
        var handler = new OrderCancelledStockHandler(db, new Mock<ILogger<OrderCancelledStockHandler>>().Object);

        await handler.HandleAsync(new OrderStatusChangedEvent
        {
            NewStatus = "Delivered",
            Items = new List<OrderItemInfo> { new() { DishId = _dishId, Quantity = 2 } }
        });

        Assert.Equal(50, db.Dishes.Single().Stock);
        Assert.Empty(db.ProcessedMessages);
    }

    [Fact]
    public async Task OrderCancelled_RestoresStock()
    {
        var db = CreateDb(nameof(OrderCancelled_RestoresStock));
        _dish.Stock = 48;
        SeedDish(db, _dish);
        var handler = new OrderCancelledStockHandler(db, new Mock<ILogger<OrderCancelledStockHandler>>().Object);

        await handler.HandleAsync(new OrderStatusChangedEvent
        {
            Id = Guid.NewGuid(),
            NewStatus = "Cancelled",
            Items = new List<OrderItemInfo> { new() { DishId = _dishId, Quantity = 2 } }
        });

        Assert.Equal(50, db.Dishes.Single().Stock);
        Assert.True(db.Dishes.Single().IsAvailable);
    }

    [Fact]
    public async Task OrderCancelled_AlreadyProcessed_IsIgnored()
    {
        var db = CreateDb(nameof(OrderCancelled_AlreadyProcessed_IsIgnored));
        _dish.Stock = 48;
        SeedDish(db, _dish);
        var handler = new OrderCancelledStockHandler(db, new Mock<ILogger<OrderCancelledStockHandler>>().Object);
        var evt = new OrderStatusChangedEvent
        {
            Id = Guid.NewGuid(),
            OrderId = Guid.NewGuid(),
            NewStatus = "Cancelled",
            Items = new List<OrderItemInfo> { new() { DishId = _dishId, Quantity = 2 } }
        };

        await handler.HandleAsync(evt);
        await handler.HandleAsync(evt);

        Assert.Equal(50, db.Dishes.Single().Stock);
        Assert.Single(db.ProcessedMessages);
    }
}