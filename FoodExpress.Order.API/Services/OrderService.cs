using FoodExpress.EventBus.Abstractions;
using FoodExpress.EventBus.Events;
using FoodExpress.Order.API.Data;
using FoodExpress.Order.API.DTOs;
using FoodExpress.Order.API.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace FoodExpress.Order.API.Services;

public class OrderService : IOrderService
{
    private readonly OrderDbContext _db;
    private readonly IRestaurantApiClient _restaurantApi;
    private readonly IEventBus _eventBus;
    private readonly ILogger<OrderService> _logger;

    public OrderService(
        OrderDbContext db,
        IRestaurantApiClient restaurantApi,
        IEventBus eventBus,
        ILogger<OrderService> logger)
    {
        _db = db;
        _restaurantApi = restaurantApi;
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task<OrderDto> CreateAsync(CreateOrderDto dto, Guid customerId)
    {
        // 1. Vérifier que le restaurant existe et est ouvert
        var restaurant = await _restaurantApi.GetRestaurantAsync(dto.RestaurantId)
            ?? throw new KeyNotFoundException("Restaurant introuvable");

        if (!restaurant.IsOpen)
            throw new InvalidOperationException("Le restaurant est fermé");

        // 2. Récupérer les infos des plats depuis Restaurant Service
        var orderItems = new List<OrderItem>();
        decimal subtotal = 0;

        foreach (var item in dto.Items)
        {
            var dish = await _restaurantApi.GetDishAsync(item.DishId)
                ?? throw new KeyNotFoundException($"Plat {item.DishId} introuvable");

            if (!dish.IsAvailable)
                throw new InvalidOperationException($"Le plat '{dish.Name}' n'est pas disponible");

            if (dish.RestaurantId != dto.RestaurantId)
                throw new InvalidOperationException($"Le plat '{dish.Name}' n'appartient pas à ce restaurant");

            var orderItem = new OrderItem
            {
                Id = Guid.NewGuid(),
                DishId = dish.Id,
                DishName = dish.Name,
                DishImageUrl = dish.ImageUrl,
                Quantity = item.Quantity,
                UnitPrice = dish.Price,
                SpecialInstructions = item.SpecialInstructions
            };

            subtotal += orderItem.UnitPrice * orderItem.Quantity;
            orderItems.Add(orderItem);
        }

        // 3. Créer la commande
        var order = new Models.Entities.Order
        {
            Id = Guid.NewGuid(),
            OrderNumber = GenerateOrderNumber(),
            // Sécurité : le client est TOUJOURS celui du token JWT (sub),
            // jamais celui envoyé dans le corps de la requête.
            CustomerId = customerId,
            CustomerName = dto.CustomerName,
            CustomerPhone = dto.CustomerPhone,
            RestaurantId = dto.RestaurantId,
            RestaurantName = restaurant.Name,
            DeliveryAddress = dto.DeliveryAddress,
            DeliveryLatitude = dto.DeliveryLatitude,
            DeliveryLongitude = dto.DeliveryLongitude,
            DeliveryFee = 15.00m,
            Subtotal = subtotal,
            TotalAmount = subtotal + 15.00m,
            Notes = dto.Notes,
            Status = OrderStatus.Pending,
            Items = orderItems
        };

        _db.Orders.Add(order);
        await _db.SaveChangesAsync();

        _logger.LogInformation("✅ Commande créée: {OrderNumber} - Total: {Total} MAD",
            order.OrderNumber, order.TotalAmount);

        // 4. Publier l'événement OrderCreated (RabbitMQ)
        await _eventBus.PublishAsync(new OrderCreatedEvent
        {
            OrderId = order.Id,
            CustomerId = order.CustomerId,
            RestaurantId = order.RestaurantId,
            RestaurantName = order.RestaurantName,
            TotalAmount = order.TotalAmount,
            DeliveryAddress = order.DeliveryAddress,
            Items = order.Items.Select(i => new OrderItemInfo
            {
                DishId = i.DishId,
                DishName = i.DishName,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice
            }).ToList()
        });

        return MapToDto(order);
    }

    public async Task<OrderDto?> GetByIdAsync(Guid id, Guid callerId, bool isAdmin)
    {
        var order = await _db.Orders
            .Include(o => o.Items)
            .Include(o => o.Delivery)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null) return null;

        // Un client ne peut consulter QUE ses propres commandes.
        if (!isAdmin && order.CustomerId != callerId)
            throw new UnauthorizedAccessException("Cette commande ne vous appartient pas");

        return MapToDto(order);
    }

    public async Task<List<OrderDto>> GetByCustomerAsync(Guid customerId)
    {
        var orders = await _db.Orders
            .Include(o => o.Items)
            .Where(o => o.CustomerId == customerId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        return orders.Select(MapToDto).ToList();
    }

    public async Task<List<OrderDto>> GetByRestaurantAsync(Guid restaurantId, Guid callerId, bool isAdmin)
    {
        // Sécurité : un RestaurantOwner ne voit QUE les commandes de SES restaurants.
        if (!isAdmin)
        {
            var restaurant = await _restaurantApi.GetRestaurantAsync(restaurantId)
                ?? throw new KeyNotFoundException("Restaurant introuvable");

            if (restaurant.OwnerId != callerId)
                throw new UnauthorizedAccessException("Ce restaurant ne vous appartient pas");
        }

        var orders = await _db.Orders
            .Include(o => o.Items)
            .Where(o => o.RestaurantId == restaurantId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        return orders.Select(MapToDto).ToList();
    }

    public async Task<List<OrderDto>> GetAllAsync()
    {
        var orders = await _db.Orders
            .Include(o => o.Items)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        return orders.Select(MapToDto).ToList();
    }

    public async Task<OrderDto?> UpdateStatusAsync(Guid id, UpdateOrderStatusDto dto, Guid callerId, bool isAdmin)
    {
        var order = await _db.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null) return null;

        // Sécurité : un RestaurantOwner ne modifie que les commandes de SES restaurants.
        if (!isAdmin)
        {
            var restaurant = await _restaurantApi.GetRestaurantAsync(order.RestaurantId)
                ?? throw new KeyNotFoundException("Restaurant introuvable");

            if (restaurant.OwnerId != callerId)
                throw new UnauthorizedAccessException("Ce restaurant ne vous appartient pas");
        }

        var previousStatus = order.Status;
        order.Status = dto.NewStatus;

        // Mettre à jour les dates selon le statut
        var now = DateTime.UtcNow;
        switch (dto.NewStatus)
        {
            case OrderStatus.Accepted: order.AcceptedAt = now; break;
            case OrderStatus.Preparing: order.PreparingAt = now; break;
            case OrderStatus.Ready: order.ReadyAt = now; break;
            case OrderStatus.OnDelivery: order.OnDeliveryAt = now; break;
            case OrderStatus.Delivered: order.DeliveredAt = now; break;
            case OrderStatus.Cancelled:
                order.CancelledAt = now;
                order.CancellationReason = dto.Reason;
                break;
        }

        await _db.SaveChangesAsync();

        // Publier l'événement de changement de statut
        await _eventBus.PublishAsync(new OrderStatusChangedEvent
        {
            OrderId = order.Id,
            CustomerId = order.CustomerId,
            PreviousStatus = previousStatus.ToString(),
            NewStatus = dto.NewStatus.ToString(),
            Items = order.Items.Select(i => new OrderItemInfo
            {
                DishId = i.DishId,
                DishName = i.DishName,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice
            }).ToList()
        });

        // Si livré, publier OrderDelivered
        if (dto.NewStatus == OrderStatus.Delivered)
        {
            await _eventBus.PublishAsync(new OrderDeliveredEvent
            {
                OrderId = order.Id,
                CustomerId = order.CustomerId,
                DeliveryPersonId = order.Delivery?.DeliveryPersonId ?? Guid.Empty,
                TotalAmount = order.TotalAmount,
                DeliveredAt = now
            });
        }

        _logger.LogInformation("🔄 Statut mis à jour: {OrderNumber} → {Status}",
            order.OrderNumber, dto.NewStatus);

        return MapToDto(order);
    }

    public async Task<List<OrderDto>> GetByDeliveryPersonAsync(Guid deliveryPersonId)
    {
        var orders = await _db.Orders
            .Include(o => o.Items)
            .Where(o => o.Delivery != null && o.Delivery.DeliveryPersonId == deliveryPersonId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        return orders.Select(MapToDto).ToList();
    }

    public async Task<OrderDto?> UpdateDeliveryStatusAsync(Guid id, UpdateOrderStatusDto dto, Guid? callerId, bool isAdmin)
    {
        var order = await _db.Orders
            .Include(o => o.Items)
            .Include(o => o.Delivery)
            .FirstOrDefaultAsync(o => o.Id == id);
        if (order == null) return null;

        if (!isAdmin && (order.Delivery == null || order.Delivery.DeliveryPersonId != callerId))
            throw new UnauthorizedAccessException("Cette commande ne vous est pas assignée");

        if (dto.NewStatus is not (OrderStatus.OnDelivery or OrderStatus.Delivered))
            throw new InvalidOperationException("Un livreur ne peut passer une commande qu'en OnDelivery ou Delivered");

        var previousStatus = order.Status;
        order.Status = dto.NewStatus;
        var now = DateTime.UtcNow;
        switch (dto.NewStatus)
        {
            case OrderStatus.OnDelivery: order.OnDeliveryAt = now; break;
            case OrderStatus.Delivered: order.DeliveredAt = now; break;
        }

        if (order.Delivery != null)
        {
            if (dto.NewStatus == OrderStatus.Delivered)
            {
                order.Delivery.Status = DeliveryStatus.Delivered;
                order.Delivery.DeliveredAt = now;
            }
        }

        await _db.SaveChangesAsync();

        await _eventBus.PublishAsync(new OrderStatusChangedEvent
        {
            OrderId = order.Id,
            CustomerId = order.CustomerId,
            PreviousStatus = previousStatus.ToString(),
            NewStatus = dto.NewStatus.ToString(),
            Items = order.Items.Select(i => new OrderItemInfo
            {
                DishId = i.DishId,
                DishName = i.DishName,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice
            }).ToList()
        });

        if (dto.NewStatus == OrderStatus.Delivered)
        {
            await _eventBus.PublishAsync(new OrderDeliveredEvent
            {
                OrderId = order.Id,
                CustomerId = order.CustomerId,
                DeliveryPersonId = order.Delivery?.DeliveryPersonId ?? Guid.Empty,
                TotalAmount = order.TotalAmount,
                DeliveredAt = now
            });
        }

        _logger.LogInformation("🚚 Livraison mise à jour: {OrderNumber} → {Status}", order.OrderNumber, dto.NewStatus);

        return MapToDto(order);
    }

    public async Task<OrderDto?> AssignDeliveryAsync(Guid orderId, AssignDeliveryDto dto)
    {
        var order = await _db.Orders
            .Include(o => o.Items)
            .Include(o => o.Delivery)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null) return null;

        var delivery = order.Delivery;
        if (delivery == null || _db.Entry(delivery).State == EntityState.Detached)
        {
            delivery = new Delivery
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id
            };
            _db.Deliveries.Add(delivery);
            order.Delivery = delivery;
        }

        delivery.DeliveryPersonId = dto.DeliveryPersonId;
        delivery.DeliveryPersonName = dto.DeliveryPersonName;
        delivery.DeliveryPersonPhone = dto.DeliveryPersonPhone;
        delivery.Status = DeliveryStatus.Assigned;
        delivery.AssignedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        _logger.LogInformation("🛵 Livreur assigné à la commande {OrderNumber}", order.OrderNumber);

        return MapToDto(order);
    }

    public async Task<bool> CancelAsync(Guid id, string reason, Guid callerId, bool isAdmin)
    {
        var order = await _db.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id);
        if (order == null) return false;

        // Sécurité : un client ne peut annuler QUE ses propres commandes.
        if (!isAdmin && order.CustomerId != callerId)
            throw new UnauthorizedAccessException("Vous ne pouvez annuler que vos propres commandes");

        if (order.Status == OrderStatus.Delivered)
            throw new InvalidOperationException("Impossible d'annuler une commande livrée");

        order.Status = OrderStatus.Cancelled;
        order.CancelledAt = DateTime.UtcNow;
        order.CancellationReason = reason;

        await _db.SaveChangesAsync();

        await _eventBus.PublishAsync(new OrderStatusChangedEvent
        {
            OrderId = order.Id,
            CustomerId = order.CustomerId,
            PreviousStatus = "Any",
            NewStatus = OrderStatus.Cancelled.ToString(),
            Items = order.Items.Select(i => new OrderItemInfo
            {
                DishId = i.DishId,
                DishName = i.DishName,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice
            }).ToList()
        });

        return true;
    }

    // Générer un numéro de commande unique : ORD-YYYYMMDD-XXXX
    private static string GenerateOrderNumber()
    {
        var date = DateTime.UtcNow.ToString("yyyyMMdd");
        var random = new Random().Next(1000, 9999);
        return $"ORD-{date}-{random}";
    }

    private static OrderDto MapToDto(Models.Entities.Order o) => new()
    {
        Id = o.Id,
        OrderNumber = o.OrderNumber,
        CustomerId = o.CustomerId,
        CustomerName = o.CustomerName,
        CustomerPhone = o.CustomerPhone,
        RestaurantId = o.RestaurantId,
        RestaurantName = o.RestaurantName,
        DeliveryAddress = o.DeliveryAddress,
        Subtotal = o.Subtotal,
        DeliveryFee = o.DeliveryFee,
        TotalAmount = o.TotalAmount,
        Status = o.Status.ToString(),
        Notes = o.Notes,
        CreatedAt = o.CreatedAt,
        Items = o.Items.Select(i => new OrderItemDto
        {
            Id = i.Id,
            DishId = i.DishId,
            DishName = i.DishName,
            DishImageUrl = i.DishImageUrl,
            Quantity = i.Quantity,
            UnitPrice = i.UnitPrice,
            Subtotal = i.UnitPrice * i.Quantity,
            SpecialInstructions = i.SpecialInstructions
        }).ToList()
    };
}
