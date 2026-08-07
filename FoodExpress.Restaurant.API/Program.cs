using FoodExpress.Common.HealthChecks;
using FoodExpress.EventBus;
using FoodExpress.EventBus.Abstractions;
using FoodExpress.EventBus.Events;
using FoodExpress.Restaurant.API.Data;
using FoodExpress.Restaurant.API.Handlers;
using FoodExpress.Restaurant.API.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Serilog;
using Serilog.Sinks.Elasticsearch;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// ==================== Serilog + Elasticsearch ====================
Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithEnvironmentName()
    .Enrich.WithProperty("Service", "RestaurantService")
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

// ========== PostgreSQL ==========
builder.Services.AddDbContext<RestaurantDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("RestaurantDb")));

// ========== Redis ==========
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
    ConnectionMultiplexer.Connect(builder.Configuration["Redis:Connection"]!));

builder.Services.AddScoped<ICacheService, RedisCacheService>();

// ========== MinIO ==========
builder.Services.AddScoped<IFileStorageService, MinioFileStorageService>();

// ========== Business Services ==========
builder.Services.AddScoped<IRestaurantService, RestaurantService>();
builder.Services.AddScoped<IDishService, DishService>();

// ========== RabbitMQ EventBus (consumer stock) ==========
builder.Services.AddSingleton<IEventBus>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<RabbitMQEventBus>>();
    return new RabbitMQEventBus(
        builder.Configuration["RabbitMQ:HostName"]!,
        builder.Configuration["RabbitMQ:UserName"]!,
        builder.Configuration["RabbitMQ:Password"]!,
        sp,
        logger);
});

builder.Services.AddScoped<OrderCreatedStockHandler>();
builder.Services.AddScoped<OrderCancelledStockHandler>();
builder.Services.AddHostedService<EventBusConsumerHosted>();

// ========== Health Checks ==========
builder.Services.AddHealthChecks()
    .AddCheck("PostgreSQL", new PostgresHealthCheck(builder.Configuration.GetConnectionString("RestaurantDb")!), tags: new[] { "database" })
    .AddCheck<RedisHealthCheck>("Redis", tags: new[] { "cache" })
    .AddCheck("RabbitMQ", new RabbitMqHealthCheck(
        builder.Configuration["RabbitMQ:HostName"]!,
        builder.Configuration["RabbitMQ:UserName"]!,
        builder.Configuration["RabbitMQ:Password"]!), tags: new[] { "queue" })
    .AddCheck("Elasticsearch", new ElasticsearchHealthCheck("http://localhost:9200"), tags: new[] { "logs" });

// ========== Authentification JWT Keycloak ==========
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

// ========== CORS ==========
builder.Services.AddCors(options =>
    options.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

// ========== Controllers + Swagger ==========
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "FoodExpress - Restaurant Service",
        Version = "v1",
        Description = "API des restaurants, catégories et plats"
    });

    var scheme = new OpenApiSecurityScheme
    {
        Description = "JWT: Bearer {token}",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT"
    };
    c.AddSecurityDefinition("Bearer", scheme);

    c.AddSecurityRequirement(doc => new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecuritySchemeReference("Bearer", doc, null!),
            new List<string>()
        }
    });
});

// Support upload de fichiers volumineux
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 10 * 1024 * 1024; // 10 MB
});

var app = builder.Build();

// ========== Migration automatique ==========
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<RestaurantDbContext>();
    db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Restaurant Service v1"));
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = HealthCheckJsonWriter.WriteAsync
});
app.UseSerilogRequestLogging();
app.Run();