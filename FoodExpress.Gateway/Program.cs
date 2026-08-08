using FoodExpress.Common.HealthChecks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Sinks.Elasticsearch;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// ==================== Serilog + Elasticsearch ====================
Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithEnvironmentName()
    .Enrich.WithProperty("Service", "Gateway")
    .WriteTo.Console()
    .WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri("http://localhost:9200"))
    {
        AutoRegisterTemplate = true,
        IndexFormat = $"foodexpress-logs-{DateTime.UtcNow:yyyy-MM}",
        NumberOfShards = 1,
        NumberOfReplicas = 0,
        TypeName = null
    })
    .CreateLogger();

builder.Host.UseSerilog();

// ==================== YARP Reverse Proxy ====================
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// ==================== Authentification JWT (Keycloak) ====================
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["Keycloak:Authority"];
        options.RequireHttpsMetadata = false;
        options.Audience = builder.Configuration["Keycloak:Audience"];

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Keycloak:Authority"]
        };
    });

builder.Services.AddAuthorization();

// ==================== CORS ====================
var allowedOrigins = (builder.Configuration["Cors:Origins"] ?? "http://localhost:5173")
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// ==================== Rate Limiting (100 requêtes/minute par IP) ====================
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 10
            }));

    options.RejectionStatusCode = 429; // Too Many Requests
});

// ==================== Health Checks (agrégation) ====================
builder.Services.AddHealthChecks()
    .AddCheck("RestaurantService", new HttpTargetHealthCheck("http://localhost:5001/health"), tags: new[] { "backend" })
    .AddCheck("OrderService", new HttpTargetHealthCheck("http://localhost:5002/health"), tags: new[] { "backend" })
    .AddCheck("UserService", new HttpTargetHealthCheck("http://localhost:5003/health"), tags: new[] { "backend" })
    .AddCheck("Keycloak", new HttpTargetHealthCheck("http://localhost:8080/realms/foodexpress/.well-known/openid-configuration"), tags: new[] { "identity" })
    .AddCheck("Elasticsearch", new ElasticsearchHealthCheck("http://localhost:9200"), tags: new[] { "logs" });

var app = builder.Build();

// ==================== Pipeline HTTP ====================
app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

// Endpoint racine (page d'accueil du Gateway)
app.MapGet("/", () => Results.Ok(new
{
    service = "FoodExpress API Gateway",
    version = "1.0.0",
    status = "running",
    services = new
    {
        users = "/api/users, /api/auth",
        restaurants = "/api/restaurants, /api/dishes, /api/categories",
        orders = "/api/orders"
    }
}));

// Endpoint de santé (liveness) : vit resp. /health = readiness agrégé
app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    service = "FoodExpress API Gateway",
    checks = new[] { "RestaurantService", "OrderService", "UserService", "Keycloak", "Elasticsearch" },
    details = "/health/ready"
}));

// Endpoint de readiness : exécute les health checks du Gateway
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    ResponseWriter = HealthCheckJsonWriter.WriteAsync
});

// Routes YARP (proxy vers les microservices)
app.MapReverseProxy();
app.UseSerilogRequestLogging();

app.Run();
