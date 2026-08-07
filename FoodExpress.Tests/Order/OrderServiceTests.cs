using FoodExpress.EventBus.Abstractions;
using FoodExpress.EventBus.Events;
using FoodExpress.Order.API.Data;
using FoodExpress.Order.API.DTOs;
using FoodExpress.Order.API.Models.Entities;
using FoodExpress.Order.API.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace FoodExpress.Tests.Order;

public class OrderServiceTests
{
    private readonly Mock<IRestaurantApiClient> _client;
    private readonly Mock<IEventBus> _eventBus;
    private readonly Mock<ILogger<OrderService>> _logger;

    private static readonly Guid RestaurantId = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000001");
    private static readonly Guid DishId = Guid.Parse("cccccccc-0000-0000-0000-000000000002");

    public OrderServiceTests()
    {
        _client = new Mock<IRestaurantApiClient>();
        _eventBus = new Mock<IEventBus>();
        _logger = new Mock<ILogger<OrderService>>();
    }

    private OrderDbContext CreateDb(string name)
    {
        var options = new DbContextOptionsBuilder<OrderDbContext>()
            .UseInMemoryDatabase(name)
            .Options;
        return new OrderDbContext(options);
    }

    private void SetupRestaurant(bool isOpen = true) =>
        _client.Setup(c => c.GetRestaurantAsync(RestaurantId))
               .ReturnsAsync(new RestaurantInfo { Id = RestaurantId, Name = "Pizza Roma", IsOpen = isOpen });

    private void SetupDish(decimal price = 60m, bool available = true, Guid? restaurantId = null) =>
        _client.Setup(c => c.GetDishAsync(DishId))
               .ReturnsAsync(new DishInfo
               {
                   Id = DishId,
                   Name = "Margherita",
                   Price = price,
                   IsAvailable = available,
                   RestaurantId = restaurantId ?? RestaurantId
               });

    private static CreateOrderDto SampleDto(int quantity = 2) => new()
    {
        CustomerId = Guid.Parse("eeeeeeee-0000-0000-0000-000000000001"),
        CustomerName = "Client Test",
        CustomerPhone = "0600000000",
        RestaurantId = RestaurantId,
        DeliveryAddress = "12 rue des Fleurs, Casablanca",
        Items = new List<CreateOrderItemDto>
        {
            new() { DishId = DishId, Quantity = quantity }
        }
    };

    [Fact]
    public async Task Create_Success_ComputesTotalsAndPublishesEvent()
    {
        SetupRestaurant();
        SetupDish(price: 60m);
        var db = CreateDb(nameof(Create_Success_ComputesTotalsAndPublishesEvent));

        var service = new OrderService(db, _client.Object, _eventBus.Object, _logger.Object);
        var result = await service.CreateAsync(SampleDto(quantity: 2));

        Assert.Equal("Pending", result.Status);
        Assert.Equal(120m, result.Subtotal);
        Assert.Equal(15m, result.DeliveryFee);
        Assert.Equal(135m, result.TotalAmount);
        Assert.StartsWith("ORD-", result.OrderNumber);
        Assert.Single(db.Orders);
        _eventBus.Verify(b => b.PublishAsync(It.IsAny<OrderCreatedEvent>()), Times.Once);
    }

    [Fact]
    public async Task Create_MissingRestaurant_ThrowsAndPublishesNothing()
    {
        _client.Setup(c => c.GetRestaurantAsync(RestaurantId)).ReturnsAsync((RestaurantInfo?)null);
        var db = CreateDb(nameof(Create_MissingRestaurant_ThrowsAndPublishesNothing));

        var service = new OrderService(db, _client.Object, _eventBus.Object, _logger.Object);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.CreateAsync(SampleDto()));
        Assert.Empty(db.Orders);
        _eventBus.Verify(b => b.PublishAsync(It.IsAny<IntegrationEvent>()), Times.Never);
    }

    [Fact]
    public async Task Create_ClosedRestaurant_Throws()
    {
        SetupRestaurant(isOpen: false);
        SetupDish();
        var db = CreateDb(nameof(Create_ClosedRestaurant_Throws));

        var service = new OrderService(db, _client.Object, _eventBus.Object, _logger.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(SampleDto()));
        _eventBus.Verify(b => b.PublishAsync(It.IsAny<IntegrationEvent>()), Times.Never);
    }

    [Fact]
    public async Task Create_UnavailableDish_Throws()
    {
        SetupRestaurant();
        SetupDish(available: false);
        var db = CreateDb(nameof(Create_UnavailableDish_Throws));

        var service = new OrderService(db, _client.Object, _eventBus.Object, _logger.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(SampleDto()));
        Assert.Empty(db.Orders);
    }

    [Fact]
    public async Task Create_DishFromAnotherRestaurant_Throws()
    {
        SetupRestaurant();
        SetupDish(restaurantId: Guid.NewGuid());
        var db = CreateDb(nameof(Create_DishFromAnotherRestaurant_Throws));

        var service = new OrderService(db, _client.Object, _eventBus.Object, _logger.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(SampleDto()));
        Assert.Empty(db.Orders);
    }

    [Fact]
    public async Task Cancel_UnknownOrder_ReturnsFalse()
    {
        var db = CreateDb(nameof(Cancel_UnknownOrder_ReturnsFalse));
        var service = new OrderService(db, _client.Object, _eventBus.Object, _logger.Object);

        var result = await service.CancelAsync(Guid.NewGuid(), "raison");

        Assert.False(result);
        _eventBus.Verify(b => b.PublishAsync(It.IsAny<IntegrationEvent>()), Times.Never);
    }

    [Fact]
    public async Task Cancel_ExistingOrder_SetsStatusAndPublishesEvent()
    {
        SetupRestaurant();
        SetupDish();
        var db = CreateDb(nameof(Cancel_ExistingOrder_SetsStatusAndPublishesEvent));
        var service = new OrderService(db, _client.Object, _eventBus.Object, _logger.Object);
        var created = await service.CreateAsync(SampleDto());

        var cancelled = await service.CancelAsync(created.Id, "Fini faim");

        Assert.True(cancelled);
        var order = db.Orders.Single();
        Assert.Equal(OrderStatus.Cancelled, order.Status);
        Assert.Equal("Fini faim", order.CancellationReason);
        _eventBus.Verify(b => b.PublishAsync(It.Is<OrderStatusChangedEvent>(e => e.NewStatus == "Cancelled")), Times.Once);
    }

    [Fact]
    public async Task Cancel_DeliveredOrder_Throws()
    {
        SetupRestaurant();
        SetupDish();
        var db = CreateDb(nameof(Cancel_DeliveredOrder_Throws));
        var service = new OrderService(db, _client.Object, _eventBus.Object, _logger.Object);
        var created = await service.CreateAsync(SampleDto());

        await service.UpdateStatusAsync(created.Id, new UpdateOrderStatusDto { NewStatus = OrderStatus.Delivered });

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CancelAsync(created.Id, "raison"));
    }

    [Fact]
    public async Task UpdateStatus_Delivered_PublishesBothEvents()
    {
        SetupRestaurant();
        SetupDish();
        var db = CreateDb(nameof(UpdateStatus_Delivered_PublishesBothEvents));
        var service = new OrderService(db, _client.Object, _eventBus.Object, _logger.Object);
        var created = await service.CreateAsync(SampleDto());

        await service.UpdateStatusAsync(created.Id, new UpdateOrderStatusDto { NewStatus = OrderStatus.Delivered });

        _eventBus.Verify(b => b.PublishAsync(It.Is<OrderStatusChangedEvent>(e => e.NewStatus == "Delivered")), Times.AtLeastOnce);
        _eventBus.Verify(b => b.PublishAsync(It.IsAny<OrderDeliveredEvent>()), Times.Once);
    }

    [Fact]
    public async Task GetByCustomer_ReturnsOnlyThatCustomersOrders()
    {
        SetupRestaurant();
        SetupDish();
        var db = CreateDb(nameof(GetByCustomer_ReturnsOnlyThatCustomersOrders));
        var service = new OrderService(db, _client.Object, _eventBus.Object, _logger.Object);
        var customerA = SampleDto();

        await service.CreateAsync(customerA);
        var other = SampleDto();
        other.CustomerId = Guid.NewGuid();
        await service.CreateAsync(other);

        var result = await service.GetByCustomerAsync(customerA.CustomerId);

        Assert.Single(result);
        Assert.Equal(customerA.CustomerId, result[0].CustomerId);
    }
}