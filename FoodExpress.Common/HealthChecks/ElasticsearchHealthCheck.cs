using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FoodExpress.Common.HealthChecks;

/// <summary>
/// Vérifie Elasticsearch via GET /_cluster/health.
/// </summary>
public class ElasticsearchHealthCheck : IHealthCheck
{
    private readonly string _baseUrl;
    private readonly HttpClient _http;

    public ElasticsearchHealthCheck(string baseUrl)
    {
        _baseUrl = baseUrl.TrimEnd('/');
        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _http.GetAsync($"{_baseUrl}/_cluster/health", cancellationToken);
            return response.IsSuccessStatusCode
                ? HealthCheckResult.Healthy($"HTTP {(int)response.StatusCode}")
                : HealthCheckResult.Degraded($"Elasticsearch répond {(int)response.StatusCode}");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Elasticsearch injoignable", ex);
        }
    }
}