using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using FoodExpress.User.API.DTOs;
using FoodExpress.User.API.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace FoodExpress.Tests.User;

public class KeycloakServiceTests
{
    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;
        public List<HttpRequestMessage> Requests { get; } = new();

        public StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) => _responder = responder;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            var response = _responder(request);
            response.RequestMessage = request;
            return Task.FromResult(response);
        }
    }

    private static KeycloakService CreateService(HttpMessageHandler handler)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Keycloak:ClientId"] = "foodexpress-api",
                ["Keycloak:ClientSecret"] = "client-secret",
                ["Keycloak:TokenUrl"] = "http://kc.local/realms/foodexpress/protocol/openid-connect/token",
                ["Keycloak:Authority"] = "http://kc.local/realms/foodexpress"
            })
            .Build();

        var http = new HttpClient(handler);
        var logger = new Mock<ILogger<KeycloakService>>().Object;
        return new KeycloakService(http, config, logger);
    }

    private static HttpResponseMessage TokenResponse(string access, string refresh)
    {
        var json = JsonSerializer.Serialize(new
        {
            access_token = access,
            refresh_token = refresh,
            expires_in = 300,
            token_type = "Bearer"
        });
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }

    [Fact]
    public async Task Refresh_SendsRefreshGrant_AndMapsToken()
    {
        var handler = new StubHandler(_ => TokenResponse("new-access", "new-refresh"));
        var service = CreateService(handler);

        var result = await service.RefreshAsync("old-refresh");

        Assert.Equal("new-access", result.AccessToken);
        Assert.Equal("new-refresh", result.RefreshToken);
        Assert.Equal(300, result.ExpiresIn);

        var request = handler.Requests.Single();
        Assert.Equal(HttpMethod.Post, request.Method);
        var body = await request.Content!.ReadAsStringAsync();
        Assert.Contains("grant_type=refresh_token", body);
        Assert.Contains("refresh_token=old-refresh", body);
        Assert.Contains("client_id=foodexpress-api", body);
    }

    [Fact]
    public async Task Refresh_KeycloakError_ThrowsUnauthorized()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("Token expired")
        });
        var service = CreateService(handler);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.RefreshAsync("dead-refresh"));
    }

    [Fact]
    public async Task Logout_RevokesRefreshToken_AtLogoutEndpoint()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.NoContent));
        var service = CreateService(handler);

        await service.LogoutAsync("refresh-to-revoke");

        var request = handler.Requests.Single();
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("http://kc.local/realms/foodexpress/protocol/openid-connect/logout", request.RequestUri!.ToString());
        var body = await request.Content!.ReadAsStringAsync();
        Assert.Contains("refresh_token=refresh-to-revoke", body);
        Assert.Contains("client_secret=client-secret", body);
    }

    [Fact]
    public async Task Logout_KeycloakError_DoesNotThrow()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));
        var service = CreateService(handler);

        await service.LogoutAsync("refresh-token"); // ne doit pas exploser
    }
}