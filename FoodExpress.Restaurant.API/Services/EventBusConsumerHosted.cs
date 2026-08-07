using FoodExpress.EventBus.Abstractions;
using FoodExpress.EventBus.Events;
using FoodExpress.Restaurant.API.Handlers;

namespace FoodExpress.Restaurant.API.Services;

/// <summary>
/// Active les abonnements RabbitMQ au démarrage du service.
/// </summary>
public class EventBusConsumerHosted : IHostedService
{
    private readonly IEventBus _eventBus;

    public EventBusConsumerHosted(IEventBus eventBus)
    {
        _eventBus = eventBus;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _eventBus.Subscribe<OrderCreatedEvent, OrderCreatedStockHandler>();
        _eventBus.Subscribe<OrderStatusChangedEvent, OrderCancelledStockHandler>();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}