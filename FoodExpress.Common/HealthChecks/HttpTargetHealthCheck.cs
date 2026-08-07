using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FoodExpress.Common.HealthChecks;

/// <summary>
/// Vérifie qu'une URL répond en HTTP 2xx (utile au Gateway pour sonder
/// les microservices, ou pour Keycloak).
/// </summary>
public class HttpTargetHealthCheck : IHealthCheck
{
    private readonly string _url;
    private readonly HttpClient _http;

    public HttpTargetHealthCheck(string url)
    {
        _url = url;
        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _http.GetAsync(_url, cancellationToken);
            return response.IsSuccessStatusCode
                ? HealthCheckResult.Healthy($"HTTP {(int)response.StatusCode}")
                : HealthCheckResult.Degraded($"HTTP {(int)response.StatusCode}");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Service injoignable", ex);
        }
    }
}