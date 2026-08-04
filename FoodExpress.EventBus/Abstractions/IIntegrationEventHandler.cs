using FoodExpress.EventBus.Events;

namespace FoodExpress.EventBus.Abstractions;

public interface IIntegrationEventHandler<in TEvent> where TEvent : IntegrationEvent
{
    Task HandleAsync(TEvent @event);
}
