using FoodExpress.EventBus;
using FoodExpress.EventBus.Abstractions;
using FoodExpress.Order.API.Data;
using FoodExpress.Order.API.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// ==================== PostgreSQL + EF Core ====================
builder.Services.AddDbContext<OrderDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("OrderDb")));

// ==================== HTTP Client vers Restaurant Service ====================
builder.Services.AddHttpClient<IRestaurantApiClient, RestaurantApiClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:RestaurantApi"]!);
});

// ==================== RabbitMQ EventBus ====================
builder.Services.AddSingleton<IEventBus>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<RabbitMQEventBus>>();
    return new RabbitMQEventBus(
        builder.Configuration["RabbitMQ:HostName"]!,
        builder.Configuration["RabbitMQ:UserName"]!,
        builder.Configuration["RabbitMQ:Password"]!,
        logger);
});

// ==================== Services métier ====================
builder.Services.AddScoped<IOrderService, OrderService>();

// ==================== JWT (Keycloak) ====================
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
builder.Services.AddCors(options =>
    options.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

// ==================== Controllers + Swagger ====================
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "FoodExpress - Order Service",
        Version = "v1",
        Description = "API de gestion des commandes"
    });

    var scheme = new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header. Exemple: 'Bearer {token}'",
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

var app = builder.Build();

// ==================== Migration BDD automatique ====================
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
    db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Order Service v1"));
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
