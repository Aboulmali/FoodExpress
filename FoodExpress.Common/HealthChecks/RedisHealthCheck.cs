using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace FoodExpress.Common.HealthChecks;

/// <summary>
/// Vérifie Redis via un PING réel.
/// </summary>
public class RedisHealthCheck : IHealthCheck
{
    private readonly IConnectionMultiplexer _redis;

    public RedisHealthCheck(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_redis.IsConnected)
                return HealthCheckResult.Unhealthy("Redis non connecté");

            var ping = await _redis.GetDatabase().PingAsync();
            return HealthCheckResult.Healthy($"PING {ping.TotalMilliseconds:F0} ms");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Ping Redis impossible", ex);
        }
    }
}