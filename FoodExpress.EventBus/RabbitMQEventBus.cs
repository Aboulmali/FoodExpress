using System.Text;
using System.Text.Json;
using FoodExpress.EventBus.Abstractions;
using FoodExpress.EventBus.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace FoodExpress.EventBus;

public class RabbitMQEventBus : IEventBus, IDisposable
{
    private readonly IConnection _connection;
    private readonly IChannel _channel;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RabbitMQEventBus> _logger;
    private const string ExchangeName = "foodexpress_events";

    public RabbitMQEventBus(
        string hostName,
        string userName,
        string password,
        IServiceProvider serviceProvider,
        ILogger<RabbitMQEventBus> logger)
    {
        _serviceProvider = serviceProvider;
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
        var eventName = typeof(TEvent).Name;
        var handlerName = typeof(THandler).Name;
        var queueName = $"{ExchangeName}.{handlerName}";

        // File dédiée à ce handler (durable, liée au fanout : reçoit tous les événements)
        _channel.QueueDeclareAsync(
            queue: queueName,
            durable: true,
            exclusive: false,
            autoDelete: false).GetAwaiter().GetResult();

        _channel.QueueBindAsync(
            queue: queueName,
            exchange: ExchangeName,
            routingKey: string.Empty).GetAwaiter().GetResult();

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (model, ea) =>
        {
            try
            {
                // Filtrer : la file reçoit tous les événements du fanout
                var messageType = ea.BasicProperties.Type ?? string.Empty;
                if (!string.Equals(messageType, eventName, StringComparison.OrdinalIgnoreCase))
                {
                    await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
                    return;
                }

                var body = Encoding.UTF8.GetString(ea.Body.ToArray());
                var @event = JsonSerializer.Deserialize<TEvent>(body);
                if (@event == null)
                {
                    await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
                    return;
                }

                // Résoudre le handler dans un scope (services scoped : DbContext, ...)
                await using var scope = _serviceProvider.CreateAsyncScope();
                var handler = scope.ServiceProvider.GetRequiredService<THandler>();
                await handler.HandleAsync(@event);

                await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
                _logger.LogInformation("✅ Event traité: {EventName} → {HandlerName}", eventName, handlerName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur de traitement de l'événement {EventName} ({QueueName})",
                    eventName, queueName);
                try
                {
                    await _channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
                }
                catch
                {
                    // Channel déjà fermé
                }
            }
        };

        _channel.BasicConsumeAsync(
            queue: queueName,
            autoAck: false,
            consumer: consumer).GetAwaiter().GetResult();

        _logger.LogInformation("👂 Abonnement activé: {EventName} → {HandlerName}", eventName, handlerName);
    }

    public void Dispose()
    {
        _channel?.CloseAsync().GetAwaiter().GetResult();
        _connection?.CloseAsync().GetAwaiter().GetResult();
    }
}
