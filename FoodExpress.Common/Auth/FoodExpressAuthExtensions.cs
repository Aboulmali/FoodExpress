using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace FoodExpress.Common.Auth;

public static class Roles
{
    public const string Admin = "Admin";
    public const string Customer = "Customer";
    public const string RestaurantOwner = "RestaurantOwner";
    public const string DeliveryPerson = "DeliveryPerson";
}

public static class Policies
{
    public const string AdminOnly = "AdminOnly";
    public const string CustomerOnly = "CustomerOnly";
    public const string CustomerOrAdmin = "CustomerOrAdmin";
    public const string RestaurantAdmin = "RestaurantAdmin";
    public const string DeliveryOrAdmin = "DeliveryOrAdmin";

    // Tout utilisateur authentifié : la logique d'autorisation se fait au niveau du service
    // (ex. annulation : client de la commande, owner du restaurant ou admin)
    public const string AnyAuthenticated = "AnyAuthenticated";
}

public static class FoodExpressAuthExtensions
{
    public static IServiceCollection AddFoodExpressKeycloakAuth(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = configuration["Keycloak:Authority"];
                options.RequireHttpsMetadata = false;
                options.Audience = configuration["Keycloak:Audience"];
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = configuration["Keycloak:Authority"]
                };

                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = MapKeycloakRealmRoles
                };
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy(Policies.AdminOnly, p => p.RequireRole(Roles.Admin));
            options.AddPolicy(Policies.CustomerOnly, p => p.RequireRole(Roles.Customer));
            options.AddPolicy(Policies.CustomerOrAdmin, p => p.RequireRole(Roles.Customer, Roles.Admin));
            options.AddPolicy(Policies.RestaurantAdmin, p => p.RequireRole(Roles.RestaurantOwner, Roles.Admin));
            options.AddPolicy(Policies.DeliveryOrAdmin, p => p.RequireRole(Roles.DeliveryPerson, Roles.Admin));
            options.AddPolicy(Policies.AnyAuthenticated, p => p.RequireAuthenticatedUser());
        });

        return services;
    }

    private static Task MapKeycloakRealmRoles(TokenValidatedContext context)
    {
        if (context.Principal?.Identity is not ClaimsIdentity identity)
            return Task.CompletedTask;

        var realmAccess = identity.FindFirst("realm_access")?.Value;
        if (string.IsNullOrWhiteSpace(realmAccess))
            return Task.CompletedTask;

        using var doc = JsonDocument.Parse(realmAccess);
        if (!doc.RootElement.TryGetProperty("roles", out var roles) || roles.ValueKind != JsonValueKind.Array)
            return Task.CompletedTask;

        var roleClaimType = identity.RoleClaimType ?? ClaimTypes.Role;
        foreach (var role in roles.EnumerateArray()
                     .Select(r => r.GetString())
                     .Where(r => !string.IsNullOrWhiteSpace(r)))
        {
            identity.AddClaim(new Claim(roleClaimType, role!));
        }

        return Task.CompletedTask;
    }
}