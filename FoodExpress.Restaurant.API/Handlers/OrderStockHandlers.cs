using FoodExpress.EventBus.Abstractions;
using FoodExpress.EventBus.Events;
using FoodExpress.Restaurant.API.Data;
using FoodExpress.Restaurant.API.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace FoodExpress.Restaurant.API.Handlers;

/// <summary>
/// @! Garde d'idempotence : marque l'événement comme traité dans le même
/// <c>SaveChanges</c> que la mise à jour du stock (commits atomique).
/// Si l'Id existe déjà (redélivrance), aucune action n'est faite.
/// </summary>
internal static class ProcessedMessageGuard
{
    /// <summary>
    /// Enregistre un marqueur "déjà traité" et renvoie true si l'événement
    /// n'avait jamais été vu (stock à mettre à jour), false sinon.
    /// Le marqueur est validé avec le même SaveChanges que la logique métier.
    /// </summary>
    public static async Task<bool> TryMarkAsync(RestaurantDbContext db, IntegrationEvent @event)
    {
        if (await db.ProcessedMessages.AnyAsync(p => p.Id == @event.Id))
            return false;

        db.ProcessedMessages.Add(new ProcessedMessage
        {
            Id = @event.Id,
            EventType = @event.GetType().Name
        });
        return true;
    }

    /// <summary>
    /// Valide les changements ; absorbe la violation de clé unique
    /// (concurrence : deux modes traitent le même événement simultanément).
    /// </summary>
    public static void SaveChanges(RestaurantDbContext db)
    {
        try
        {
            db.SaveChanges();
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException pg &&
                                           pg.SqlState == "23505")
        {
            db.ChangeTracker.Clear();
        }
    }
}

/// <summary>
/// Met à jour le stock des plats quand une commande est créée.
/// </summary>
public class OrderCreatedStockHandler : IIntegrationEventHandler<OrderCreatedEvent>
{
    private readonly RestaurantDbContext _db;
    private readonly ILogger<OrderCreatedStockHandler> _logger;

    public OrderCreatedStockHandler(RestaurantDbContext db, ILogger<OrderCreatedStockHandler> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task HandleAsync(OrderCreatedEvent @event)
    {
        if (!await ProcessedMessageGuard.TryMarkAsync(_db, @event))
        {
            _logger.LogInformation("↩️ Événement {EventId} déjà traité (ignoré)", @event.Id);
            return;
        }

        foreach (var item in @event.Items)
        {
            var dish = await _db.Dishes.FirstOrDefaultAsync(d => d.Id == item.DishId);
            if (dish == null)
            {
                _logger.LogWarning("⚠️ Plat {DishId} introuvable (commande {OrderId})", item.DishId, @event.OrderId);
                continue;
            }

            dish.Stock = Math.Max(0, dish.Stock - item.Quantity);
            if (dish.Stock <= 0)
                dish.IsAvailable = false;

            dish.UpdatedAt = DateTime.UtcNow;
        }

        ProcessedMessageGuard.SaveChanges(_db);
        _logger.LogInformation("📦 Stock décrémenté pour la commande {OrderId}", @event.OrderId);
    }
}

/// <summary>
/// Restaure le stock des plats si la commande est annulée.
/// </summary>
public class OrderCancelledStockHandler : IIntegrationEventHandler<OrderStatusChangedEvent>
{
    private readonly RestaurantDbContext _db;
    private readonly ILogger<OrderCancelledStockHandler> _logger;

    public OrderCancelledStockHandler(RestaurantDbContext db, ILogger<OrderCancelledStockHandler> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task HandleAsync(OrderStatusChangedEvent @event)
    {
        if (!string.Equals(@event.NewStatus, "Cancelled", StringComparison.OrdinalIgnoreCase))
            return;

        if (!await ProcessedMessageGuard.TryMarkAsync(_db, @event))
        {
            _logger.LogInformation("↩️ Événement {EventId} déjà traité (ignoré)", @event.Id);
            return;
        }

        foreach (var item in @event.Items)
        {
            var dish = await _db.Dishes.FirstOrDefaultAsync(d => d.Id == item.DishId);
            if (dish == null)
            {
                _logger.LogWarning("⚠️ {DishId} introuvable pour restauration (commande {OrderId})", item.DishId, @event.OrderId);
                continue;
            }

            dish.Stock += item.Quantity;
            if (dish.Stock > 0)
                dish.IsAvailable = true;

            dish.UpdatedAt = DateTime.UtcNow;
        }

        ProcessedMessageGuard.SaveChanges(_db);
        _logger.LogInformation("♻️ Stock restauré pour la commande annulée {OrderId}", @event.OrderId);
    }
}