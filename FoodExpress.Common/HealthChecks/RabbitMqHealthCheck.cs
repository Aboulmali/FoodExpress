using Microsoft.Extensions.Diagnostics.HealthChecks;
using RabbitMQ.Client;

namespace FoodExpress.Common.HealthChecks;

/// <summary>
/// Vérifie RabbitMQ en ouvrant une connexion (timeout borné à 3s).
/// </summary>
public class RabbitMqHealthCheck : IHealthCheck
{
    private readonly string _host;
    private readonly string _userName;
    private readonly string _password;

    public RabbitMqHealthCheck(string host, string userName, string password)
    {
        _host = host;
        _userName = userName;
        _password = password;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        IConnection? connection = null;
        try
        {
            var factory = new ConnectionFactory
            {
                HostName = _host,
                UserName = _userName,
                Password = _password,
                RequestedHeartbeat = TimeSpan.FromSeconds(5)
            };

            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(3));

            connection = await factory.CreateConnectionAsync(timeout.Token);
            return HealthCheckResult.Healthy("Connexion RabbitMQ établie");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Connexion RabbitMQ impossible", ex);
        }
        finally
        {
            if (connection is not null)
                await connection.CloseAsync();
        }
    }
}