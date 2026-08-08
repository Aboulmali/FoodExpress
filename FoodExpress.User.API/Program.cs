using FoodExpress.Common.Auth;
using FoodExpress.Common.HealthChecks;
using FoodExpress.User.API.Data;
using FoodExpress.User.API.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Serilog;
using Serilog.Sinks.Elasticsearch;

var builder = WebApplication.CreateBuilder(args);

// ==================== Serilog + Elasticsearch ====================
Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithEnvironmentName()
    .Enrich.WithProperty("Service", "UserService")
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

// ==================== Services ====================

// PostgreSQL + Entity Framework
builder.Services.AddDbContext<UserDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("UserDb")));

// HttpClient pour Keycloak
builder.Services.AddHttpClient<IKeycloakService, KeycloakService>();

// Service métier
builder.Services.AddScoped<IUserService, UserService>();

// ==================== Health Checks ====================
builder.Services.AddHealthChecks()
    .AddCheck("PostgreSQL", new PostgresHealthCheck(builder.Configuration.GetConnectionString("UserDb")!), tags: new[] { "database" })
    .AddCheck("Elasticsearch", new ElasticsearchHealthCheck("http://localhost:9200"), tags: new[] { "logs" });

// ==================== Authentification JWT (Keycloak) + RBAC ====================
builder.Services.AddFoodExpressKeycloakAuth(builder.Configuration);

// ==================== CORS ====================
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

// ==================== Controllers + Swagger ====================
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "FoodExpress - User Service",
        Version = "v1",
        Description = "API de gestion des utilisateurs"
    });

    var securityScheme = new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header. Exemple: 'Bearer {token}'",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT"
    };

    c.AddSecurityDefinition("Bearer", securityScheme);

    c.AddSecurityRequirement(doc => new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecuritySchemeReference("Bearer", doc, null!),
            new List<string>()
        }
    });
});

var app = builder.Build();

// ==================== Migration automatique BDD ====================
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<UserDbContext>();
    db.Database.Migrate();
}

// ==================== Pipeline HTTP ====================
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "User Service v1");
    });
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