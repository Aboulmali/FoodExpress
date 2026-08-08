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
    private static readonly Guid OwnerId = Guid.Parse("dddddddd-0000-0000-0000-000000000001");
    private static readonly Guid OtherOwnerId = Guid.Parse("dddddddd-0000-0000-0000-000000000099");
    private static readonly Guid CustomerId = Guid.Parse("eeeeeeee-0000-0000-0000-000000000001");
    private static readonly Guid OtherCustomerId = Guid.Parse("eeeeeeee-0000-0000-0000-000000000099");
    private static readonly Guid CourierId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000007");

    // Parcours complet de la machine à états : Pending → Accepted → Preparing → Ready
    // puis OnDelivery → Delivered (par le livreur), avec assignation au passage.
    private static async Task DriveOrderToDeliveredAsync(OrderService service, Guid orderId, Guid ownerId, Guid courierId)
    {
        await service.UpdateStatusAsync(orderId,
            new UpdateOrderStatusDto { NewStatus = OrderStatus.Accepted }, ownerId, isAdmin: false);
        await service.UpdateStatusAsync(orderId,
            new UpdateOrderStatusDto { NewStatus = OrderStatus.Preparing }, ownerId, isAdmin: false);
        await service.UpdateStatusAsync(orderId,
            new UpdateOrderStatusDto { NewStatus = OrderStatus.Ready }, ownerId, isAdmin: false);

        await service.AssignDeliveryAsync(orderId, new AssignDeliveryDto
        {
            DeliveryPersonId = courierId,
            DeliveryPersonName = "Livreur Test",
            DeliveryPersonPhone = "0611111111"
        }, ownerId, isAdmin: false);

        await service.UpdateDeliveryStatusAsync(orderId,
            new UpdateOrderStatusDto { NewStatus = OrderStatus.OnDelivery }, courierId, isAdmin: false);
        await service.UpdateDeliveryStatusAsync(orderId,
            new UpdateOrderStatusDto { NewStatus = OrderStatus.Delivered }, courierId, isAdmin: false);
    }

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

    private void SetupRestaurant(bool isOpen = true, Guid? ownerId = null) =>
        _client.Setup(c => c.GetRestaurantAsync(RestaurantId))
               .ReturnsAsync(new RestaurantInfo
               {
                   Id = RestaurantId,
                   Name = "Pizza Roma",
                   IsOpen = isOpen,
                   OwnerId = ownerId ?? OwnerId
               });

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
        CustomerId = CustomerId,
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
        var result = await service.CreateAsync(SampleDto(quantity: 2), CustomerId);

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

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.CreateAsync(SampleDto(), CustomerId));
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

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(SampleDto(), CustomerId));
        _eventBus.Verify(b => b.PublishAsync(It.IsAny<IntegrationEvent>()), Times.Never);
    }

    [Fact]
    public async Task Create_UnavailableDish_Throws()
    {
        SetupRestaurant();
        SetupDish(available: false);
        var db = CreateDb(nameof(Create_UnavailableDish_Throws));

        var service = new OrderService(db, _client.Object, _eventBus.Object, _logger.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(SampleDto(), CustomerId));
        Assert.Empty(db.Orders);
    }

    [Fact]
    public async Task Create_DishFromAnotherRestaurant_Throws()
    {
        SetupRestaurant();
        SetupDish(restaurantId: Guid.NewGuid());
        var db = CreateDb(nameof(Create_DishFromAnotherRestaurant_Throws));

        var service = new OrderService(db, _client.Object, _eventBus.Object, _logger.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(SampleDto(), CustomerId));
        Assert.Empty(db.Orders);
    }

    [Fact]
    public async Task Create_IgnoresCustomerIdFromBody_UsesCallerFromJwt()
    {
        SetupRestaurant();
        SetupDish();
        var db = CreateDb(nameof(Create_IgnoresCustomerIdFromBody_UsesCallerFromJwt));
        var service = new OrderService(db, _client.Object, _eventBus.Object, _logger.Object);

        var dto = SampleDto();
        dto.CustomerId = Guid.NewGuid(); // tentative d'usurpation via le body
        var result = await service.CreateAsync(dto, CustomerId);

        Assert.Equal(CustomerId, result.CustomerId);
        Assert.Equal(CustomerId, db.Orders.Single().CustomerId);
    }

    [Fact]
    public async Task Cancel_UnknownOrder_ReturnsFalse()
    {
        var db = CreateDb(nameof(Cancel_UnknownOrder_ReturnsFalse));
        var service = new OrderService(db, _client.Object, _eventBus.Object, _logger.Object);

        var result = await service.CancelAsync(Guid.NewGuid(), "raison", CustomerId, isAdmin: false);

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
        var created = await service.CreateAsync(SampleDto(), CustomerId);

        var cancelled = await service.CancelAsync(created.Id, "Fini faim", CustomerId, isAdmin: false);

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
        var created = await service.CreateAsync(SampleDto(), CustomerId);

        await DriveOrderToDeliveredAsync(service, created.Id, OwnerId, CourierId);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CancelAsync(created.Id, "raison", CustomerId, isAdmin: false));
    }

    [Fact]
    public async Task UpdateStatus_Delivered_PublishesBothEvents()
    {
        SetupRestaurant();
        SetupDish();
        var db = CreateDb(nameof(UpdateStatus_Delivered_PublishesBothEvents));
        var service = new OrderService(db, _client.Object, _eventBus.Object, _logger.Object);
        var created = await service.CreateAsync(SampleDto(), CustomerId);

        await DriveOrderToDeliveredAsync(service, created.Id, OwnerId, CourierId);

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

        await service.CreateAsync(SampleDto(), CustomerId);
        await service.CreateAsync(SampleDto(), OtherCustomerId);

        var result = await service.GetByCustomerAsync(CustomerId);

        Assert.Single(result);
        Assert.Equal(CustomerId, result[0].CustomerId);
    }

    [Fact]
    public async Task Add_Customer_Cannot_Update_Another_Owners_Order()
    {
        SetupRestaurant(ownerId: OwnerId);
        SetupDish();
        var db = CreateDb(nameof(Add_Customer_Cannot_Update_Another_Owners_Order));
        var service = new OrderService(db, _client.Object, _eventBus.Object, _logger.Object);
        var created = await service.CreateAsync(SampleDto(), CustomerId);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.UpdateStatusAsync(created.Id,
                new UpdateOrderStatusDto { NewStatus = OrderStatus.Accepted },
                OtherOwnerId, isAdmin: false));
    }

    [Fact]
    public async Task Add_Owner_Can_Update_Own_Order_Status()
    {
        SetupRestaurant(ownerId: OwnerId);
        SetupDish();
        var db = CreateDb(nameof(Add_Owner_Can_Update_Own_Order_Status));
        var service = new OrderService(db, _client.Object, _eventBus.Object, _logger.Object);
        var created = await service.CreateAsync(SampleDto(), CustomerId);

        var updated = await service.UpdateStatusAsync(created.Id,
            new UpdateOrderStatusDto { NewStatus = OrderStatus.Accepted },
            OwnerId, isAdmin: false);

        Assert.Equal("Accepted", updated!.Status);
    }

    [Fact]
    public async Task Add_GetByRestaurant_NotOwner_Throws()
    {
        SetupRestaurant(ownerId: OwnerId);
        SetupDish();
        var db = CreateDb(nameof(Add_GetByRestaurant_NotOwner_Throws));
        var service = new OrderService(db, _client.Object, _eventBus.Object, _logger.Object);
        await service.CreateAsync(SampleDto(), CustomerId);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.GetByRestaurantAsync(RestaurantId, OtherOwnerId, isAdmin: false));
    }

    [Fact]
    public async Task Add_GetByRestaurant_Owner_Succeeds()
    {
        SetupRestaurant(ownerId: OwnerId);
        SetupDish();
        var db = CreateDb(nameof(Add_GetByRestaurant_Owner_Succeeds));
        var service = new OrderService(db, _client.Object, _eventBus.Object, _logger.Object);
        await service.CreateAsync(SampleDto(), CustomerId);

        var result = await service.GetByRestaurantAsync(RestaurantId, OwnerId, isAdmin: false);

        Assert.Single(result);
    }

    [Fact]
    public async Task Add_GetById_NotOwner_Throws()
    {
        SetupRestaurant();
        SetupDish();
        var db = CreateDb(nameof(Add_GetById_NotOwner_Throws));
        var service = new OrderService(db, _client.Object, _eventBus.Object, _logger.Object);
        var created = await service.CreateAsync(SampleDto(), CustomerId);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.GetByIdAsync(created.Id, OtherCustomerId, isAdmin: false));
    }

    [Fact]
    public async Task Add_Cancel_NotOwner_Throws()
    {
        SetupRestaurant();
        SetupDish();
        var db = CreateDb(nameof(Add_Cancel_NotOwner_Throws));
        var service = new OrderService(db, _client.Object, _eventBus.Object, _logger.Object);
        var created = await service.CreateAsync(SampleDto(), CustomerId);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.CancelAsync(created.Id, "raison", OtherCustomerId, isAdmin: false));
    }

    [Fact]
    public async Task UpdateStatus_SkipsStep_Throws()
    {
        SetupRestaurant();
        SetupDish();
        var db = CreateDb(nameof(UpdateStatus_SkipsStep_Throws));
        var service = new OrderService(db, _client.Object, _eventBus.Object, _logger.Object);
        var created = await service.CreateAsync(SampleDto(), CustomerId);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UpdateStatusAsync(created.Id,
                new UpdateOrderStatusDto { NewStatus = OrderStatus.Ready }, OwnerId, isAdmin: false));
    }

    [Fact]
    public async Task UpdateStatus_Backwards_Throws()
    {
        SetupRestaurant();
        SetupDish();
        var db = CreateDb(nameof(UpdateStatus_Backwards_Throws));
        var service = new OrderService(db, _client.Object, _eventBus.Object, _logger.Object);
        var created = await service.CreateAsync(SampleDto(), CustomerId);

        await service.UpdateStatusAsync(created.Id,
            new UpdateOrderStatusDto { NewStatus = OrderStatus.Accepted }, OwnerId, isAdmin: false);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UpdateStatusAsync(created.Id,
                new UpdateOrderStatusDto { NewStatus = OrderStatus.Pending }, OwnerId, isAdmin: false));
    }

    [Fact]
    public async Task UpdateStatus_Cancelled_Throws_EndpointDedicated()
    {
        SetupRestaurant();
        SetupDish();
        var db = CreateDb(nameof(UpdateStatus_Cancelled_Throws_EndpointDedicated));
        var service = new OrderService(db, _client.Object, _eventBus.Object, _logger.Object);
        var created = await service.CreateAsync(SampleDto(), CustomerId);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UpdateStatusAsync(created.Id,
                new UpdateOrderStatusDto { NewStatus = OrderStatus.Cancelled }, OwnerId, isAdmin: false));
    }

    [Fact]
    public async Task UpdateStatus_OwnerCannotSetDelivered_Throws()
    {
        SetupRestaurant();
        SetupDish();
        var db = CreateDb(nameof(UpdateStatus_OwnerCannotSetDelivered_Throws));
        var service = new OrderService(db, _client.Object, _eventBus.Object, _logger.Object);
        var created = await service.CreateAsync(SampleDto(), CustomerId);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UpdateStatusAsync(created.Id,
                new UpdateOrderStatusDto { NewStatus = OrderStatus.OnDelivery }, OwnerId, isAdmin: false));
    }

    [Fact]
    public async Task AssignDelivery_NotReady_Throws()
    {
        SetupRestaurant();
        SetupDish();
        var db = CreateDb(nameof(AssignDelivery_NotReady_Throws));
        var service = new OrderService(db, _client.Object, _eventBus.Object, _logger.Object);
        var created = await service.CreateAsync(SampleDto(), CustomerId);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.AssignDeliveryAsync(created.Id, new AssignDeliveryDto
            {
                DeliveryPersonId = CourierId,
                DeliveryPersonName = "Livreur Test",
                DeliveryPersonPhone = "0611111111"
            }, OwnerId, isAdmin: false));
    }

    [Fact]
    public async Task AssignDelivery_OtherOwner_Throws()
    {
        SetupRestaurant();
        SetupDish();
        var db = CreateDb(nameof(AssignDelivery_OtherOwner_Throws));
        var service = new OrderService(db, _client.Object, _eventBus.Object, _logger.Object);
        var created = await service.CreateAsync(SampleDto(), CustomerId);

        await service.UpdateStatusAsync(created.Id,
            new UpdateOrderStatusDto { NewStatus = OrderStatus.Accepted }, OwnerId, isAdmin: false);
        await service.UpdateStatusAsync(created.Id,
            new UpdateOrderStatusDto { NewStatus = OrderStatus.Preparing }, OwnerId, isAdmin: false);
        await service.UpdateStatusAsync(created.Id,
            new UpdateOrderStatusDto { NewStatus = OrderStatus.Ready }, OwnerId, isAdmin: false);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.AssignDeliveryAsync(created.Id, new AssignDeliveryDto
            {
                DeliveryPersonId = CourierId,
                DeliveryPersonName = "Livreur Test",
                DeliveryPersonPhone = "0611111111"
            }, OtherOwnerId, isAdmin: false));
    }

    [Fact]
    public async Task AssignDelivery_Success_SetsDeliveryInfo()
    {
        SetupRestaurant();
        SetupDish();
        var db = CreateDb(nameof(AssignDelivery_Success_SetsDeliveryInfo));
        var service = new OrderService(db, _client.Object, _eventBus.Object, _logger.Object);
        var created = await service.CreateAsync(SampleDto(), CustomerId);

        await service.UpdateStatusAsync(created.Id,
            new UpdateOrderStatusDto { NewStatus = OrderStatus.Accepted }, OwnerId, isAdmin: false);
        await service.UpdateStatusAsync(created.Id,
            new UpdateOrderStatusDto { NewStatus = OrderStatus.Preparing }, OwnerId, isAdmin: false);
        await service.UpdateStatusAsync(created.Id,
            new UpdateOrderStatusDto { NewStatus = OrderStatus.Ready }, OwnerId, isAdmin: false);

        var assigned = await service.AssignDeliveryAsync(created.Id, new AssignDeliveryDto
        {
            DeliveryPersonId = CourierId,
            DeliveryPersonName = "Livreur Test",
            DeliveryPersonPhone = "0611111111"
        }, OwnerId, isAdmin: false);

        Assert.Equal(CourierId, assigned!.Delivery!.DeliveryPersonId);
    }

    [Fact]
    public async Task UpdateDelivery_UnassignedCourier_Throws()
    {
        SetupRestaurant();
        SetupDish();
        var db = CreateDb(nameof(UpdateDelivery_UnassignedCourier_Throws));
        var service = new OrderService(db, _client.Object, _eventBus.Object, _logger.Object);
        var created = await service.CreateAsync(SampleDto(), CustomerId);

        await service.UpdateStatusAsync(created.Id,
            new UpdateOrderStatusDto { NewStatus = OrderStatus.Accepted }, OwnerId, isAdmin: false);
        await service.UpdateStatusAsync(created.Id,
            new UpdateOrderStatusDto { NewStatus = OrderStatus.Preparing }, OwnerId, isAdmin: false);
        await service.UpdateStatusAsync(created.Id,
            new UpdateOrderStatusDto { NewStatus = OrderStatus.Ready }, OwnerId, isAdmin: false);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.UpdateDeliveryStatusAsync(created.Id,
                new UpdateOrderStatusDto { NewStatus = OrderStatus.OnDelivery }, CourierId, isAdmin: false));
    }

    [Fact]
    public async Task Cancel_Owner_OwnRestaurant_Succeeds()
    {
        SetupRestaurant(ownerId: OwnerId);
        SetupDish();
        var db = CreateDb(nameof(Cancel_Owner_OwnRestaurant_Succeeds));
        var service = new OrderService(db, _client.Object, _eventBus.Object, _logger.Object);
        var created = await service.CreateAsync(SampleDto(), CustomerId);

        var cancelled = await service.CancelAsync(created.Id, "Plus de stock", OwnerId, isAdmin: false);

        Assert.True(cancelled);
        Assert.Equal(OrderStatus.Cancelled, db.Orders.Single().Status);
    }

    [Fact]
    public async Task Cancel_Owner_OtherRestaurant_Throws()
    {
        SetupRestaurant(ownerId: OwnerId);
        SetupDish();
        var db = CreateDb(nameof(Cancel_Owner_OtherRestaurant_Throws));
        var service = new OrderService(db, _client.Object, _eventBus.Object, _logger.Object);
        var created = await service.CreateAsync(SampleDto(), CustomerId);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.CancelAsync(created.Id, "raison", OtherOwnerId, isAdmin: false));
    }

    [Fact]
    public async Task Cancel_Client_AfterPreparation_Throws()
    {
        SetupRestaurant();
        SetupDish();
        var db = CreateDb(nameof(Cancel_Client_AfterPreparation_Throws));
        var service = new OrderService(db, _client.Object, _eventBus.Object, _logger.Object);
        var created = await service.CreateAsync(SampleDto(), CustomerId);

        await service.UpdateStatusAsync(created.Id,
            new UpdateOrderStatusDto { NewStatus = OrderStatus.Accepted }, OwnerId, isAdmin: false);
        await service.UpdateStatusAsync(created.Id,
            new UpdateOrderStatusDto { NewStatus = OrderStatus.Preparing }, OwnerId, isAdmin: false);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CancelAsync(created.Id, "raison", CustomerId, isAdmin: false));
    }

    [Fact]
    public async Task Cancel_Admin_MidProcess_Succeeds()
    {
        SetupRestaurant();
        SetupDish();
        var db = CreateDb(nameof(Cancel_Admin_MidProcess_Succeeds));
        var service = new OrderService(db, _client.Object, _eventBus.Object, _logger.Object);
        var created = await service.CreateAsync(SampleDto(), CustomerId);

        await service.UpdateStatusAsync(created.Id,
            new UpdateOrderStatusDto { NewStatus = OrderStatus.Accepted }, OwnerId, isAdmin: false);
        await service.UpdateStatusAsync(created.Id,
            new UpdateOrderStatusDto { NewStatus = OrderStatus.Preparing }, OwnerId, isAdmin: false);
        await service.UpdateStatusAsync(created.Id,
            new UpdateOrderStatusDto { NewStatus = OrderStatus.Ready }, OwnerId, isAdmin: false);

        var cancelled = await service.CancelAsync(created.Id, "raison admin", Guid.Empty, isAdmin: true);

        Assert.True(cancelled);
        Assert.Equal(OrderStatus.Cancelled, db.Orders.Single().Status);
    }
}