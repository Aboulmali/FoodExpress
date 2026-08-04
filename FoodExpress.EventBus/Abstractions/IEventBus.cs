using FoodExpress.EventBus.Events;

namespace FoodExpress.EventBus.Abstractions;

public interface IEventBus
{
    Task PublishAsync<T>(T @event) where T : IntegrationEvent;
    void Subscribe<TEvent, THandler>()
        where TEvent : IntegrationEvent
        where THandler : IIntegrationEventHandler<TEvent>;
}
