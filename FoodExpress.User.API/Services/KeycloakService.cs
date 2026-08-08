using System.Text;
using System.Text.Json;
using FoodExpress.User.API.DTOs;

namespace FoodExpress.User.API.Services;

public class KeycloakService : IKeycloakService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;
    private readonly ILogger<KeycloakService> _logger;

    public KeycloakService(HttpClient httpClient, IConfiguration config, ILogger<KeycloakService> logger)
    {
        _httpClient = httpClient;
        _config = config;
        _logger = logger;
    }

    // Récupérer le token admin pour appeler l'API Keycloak
    private async Task<string> GetAdminTokenAsync()
    {
        var body = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "password"),
            new KeyValuePair<string, string>("client_id", "admin-cli"),
            new KeyValuePair<string, string>("username", "admin"),
            new KeyValuePair<string, string>("password", "admin123")
        });

        var response = await _httpClient.PostAsync(
            "http://localhost:8080/realms/master/protocol/openid-connect/token", body);

        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("access_token").GetString()!;
    }

    // Créer un utilisateur dans Keycloak
    public async Task<string> CreateUserAsync(RegisterUserDto dto)
    {
        var adminToken = await GetAdminTokenAsync();
        _httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminToken);

        var userPayload = new
        {
            username = dto.Email,
            email = dto.Email,
            firstName = dto.FirstName,
            lastName = dto.LastName,
            enabled = true,
            emailVerified = true,
            credentials = new[]
            {
                new { type = "password", value = dto.Password, temporary = false }
            }
        };

        var jsonContent = new StringContent(
            JsonSerializer.Serialize(userPayload),
            Encoding.UTF8,
            "application/json");

        var createResponse = await _httpClient.PostAsync(
            "http://localhost:8080/admin/realms/foodexpress/users", jsonContent);

        if (!createResponse.IsSuccessStatusCode)
        {
            var error = await createResponse.Content.ReadAsStringAsync();
            _logger.LogError("Keycloak creation failed: {Error}", error);
            throw new Exception($"Erreur création Keycloak: {error}");
        }

        // Récupérer l'ID Keycloak de l'utilisateur créé
        var location = createResponse.Headers.Location?.ToString();
        var keycloakId = location?.Split('/').Last() ?? throw new Exception("Impossible de récupérer l'ID Keycloak");

        // Assigner le rôle
        await AssignRoleAsync(keycloakId, dto.Role.ToString());

        return keycloakId;
    }

    // Assigner un rôle à un utilisateur
    public async Task AssignRoleAsync(string userId, string roleName)
    {
        var adminToken = await GetAdminTokenAsync();
        _httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminToken);

        // Récupérer le rôle
        var roleResponse = await _httpClient.GetAsync(
            $"http://localhost:8080/admin/realms/foodexpress/roles/{roleName}");
        roleResponse.EnsureSuccessStatusCode();
        var roleJson = await roleResponse.Content.ReadAsStringAsync();

        // Assigner le rôle
        var payload = $"[{roleJson}]";
        var content = new StringContent(payload, Encoding.UTF8, "application/json");
        var assignResponse = await _httpClient.PostAsync(
            $"http://localhost:8080/admin/realms/foodexpress/users/{userId}/role-mappings/realm",
            content);

        assignResponse.EnsureSuccessStatusCode();
    }

    // Login : obtenir un token
    public async Task<TokenResponseDto> LoginAsync(string email, string password)
    {
        var clientId = _config["Keycloak:ClientId"]!;
        var clientSecret = _config["Keycloak:ClientSecret"]!;
        var tokenUrl = _config["Keycloak:TokenUrl"]!;

        var body = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "password"),
            new KeyValuePair<string, string>("client_id", clientId),
            new KeyValuePair<string, string>("client_secret", clientSecret),
            new KeyValuePair<string, string>("username", email),
            new KeyValuePair<string, string>("password", password)
        });

        var response = await _httpClient.PostAsync(tokenUrl, body);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new UnauthorizedAccessException($"Login échoué: {error}");
        }

        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        return new TokenResponseDto
        {
            AccessToken = root.GetProperty("access_token").GetString()!,
            RefreshToken = root.GetProperty("refresh_token").GetString()!,
            ExpiresIn = root.GetProperty("expires_in").GetInt32(),
            TokenType = root.GetProperty("token_type").GetString()!
        };
    }

    // Renouveler le token avec le refresh token
    public async Task<TokenResponseDto> RefreshAsync(string refreshToken)
    {
        var clientId = _config["Keycloak:ClientId"]!;
        var clientSecret = _config["Keycloak:ClientSecret"]!;
        var tokenUrl = _config["Keycloak:TokenUrl"]!;

        var body = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "refresh_token"),
            new KeyValuePair<string, string>("client_id", clientId),
            new KeyValuePair<string, string>("client_secret", clientSecret),
            new KeyValuePair<string, string>("refresh_token", refreshToken)
        });

        var response = await _httpClient.PostAsync(tokenUrl, body);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("Keycloak refresh failed: {Error}", error);
            throw new UnauthorizedAccessException($"Refresh token invalide ou expiré: {error}");
        }

        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        return new TokenResponseDto
        {
            AccessToken = root.GetProperty("access_token").GetString()!,
            RefreshToken = root.GetProperty("refresh_token").GetString()!,
            ExpiresIn = root.GetProperty("expires_in").GetInt32(),
            TokenType = root.GetProperty("token_type").GetString()!
        };
    }

    // Révoquer la session : invalide le refresh token côté Keycloak
    public async Task LogoutAsync(string refreshToken)
    {
        var clientId = _config["Keycloak:ClientId"]!;
        var clientSecret = _config["Keycloak:ClientSecret"]!;
        var authority = _config["Keycloak:Authority"]!;
        var logoutUrl = $"{authority}/protocol/openid-connect/logout";

        var body = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("client_id", clientId),
            new KeyValuePair<string, string>("client_secret", clientSecret),
            new KeyValuePair<string, string>("refresh_token", refreshToken)
        });

        var response = await _httpClient.PostAsync(logoutUrl, body);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("Keycloak logout failed: {Error}", error);
        }
    }
}