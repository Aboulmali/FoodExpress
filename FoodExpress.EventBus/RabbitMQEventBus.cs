using System.Text;
using System.Text.Json;
using FoodExpress.EventBus.Abstractions;
using FoodExpress.EventBus.Events;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace FoodExpress.EventBus;

public class RabbitMQEventBus : IEventBus, IDisposable
{
    private readonly IConnection _connection;
    private readonly IChannel _channel;
    private readonly ILogger<RabbitMQEventBus> _logger;
    private const string ExchangeName = "foodexpress_events";

    public RabbitMQEventBus(string hostName, string userName, string password, ILogger<RabbitMQEventBus> logger)
    {
        _logger = logger;

        var factory = new ConnectionFactory
        {
            HostName = hostName,
            UserName = userName,
            Password = password
        };

        _connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
        _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();

        // Créer l'exchange (type "fanout" pour broadcast)
        _channel.ExchangeDeclareAsync(
            exchange: ExchangeName,
            type: ExchangeType.Fanout,
            durable: true,
            autoDelete: false).GetAwaiter().GetResult();
    }

    public async Task PublishAsync<T>(T @event) where T : IntegrationEvent
    {
        var eventName = typeof(T).Name;
        var json = JsonSerializer.Serialize(@event, @event.GetType());
        var body = Encoding.UTF8.GetBytes(json);

        var properties = new BasicProperties
        {
            Persistent = true,
            ContentType = "application/json",
            Type = eventName
        };

        await _channel.BasicPublishAsync(
            exchange: ExchangeName,
            routingKey: eventName,
            mandatory: false,
            basicProperties: properties,
            body: body);

        _logger.LogInformation("📤 Event published: {EventName}", eventName);
    }

    public void Subscribe<TEvent, THandler>()
        where TEvent : IntegrationEvent
        where THandler : IIntegrationEventHandler<TEvent>
    {
        // Implémentation basique — sera enrichie plus tard
        throw new NotImplementedException("À implémenter pour les consumers");
    }

    public void Dispose()
    {
        _channel?.CloseAsync().GetAwaiter().GetResult();
        _connection?.CloseAsync().GetAwaiter().GetResult();
    }
}
