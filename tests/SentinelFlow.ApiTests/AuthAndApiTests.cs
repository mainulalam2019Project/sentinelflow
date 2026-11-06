using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace SentinelFlow.ApiTests;

public class AuthAndApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public AuthAndApiTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Unauthenticated_Request_ToServices_IsRejected()
    {
        var res = await _client.GetAsync("/api/services");
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Login_WithSeededAdmin_ReturnsToken()
    {
        var res = await _client.PostAsJsonAsync("/api/auth/login", new { email = "admin@sentinelflow.dev", password = "Admin@123" });
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var body = await res.Content.ReadAsStringAsync();
        Assert.Contains("accessToken", body);
    }

    [Fact]
    public async Task ServicesEndpoint_ReturnsSeededServices()
    {
        var login = await _client.PostAsJsonAsync("/api/auth/login", new { email = "admin@sentinelflow.dev", password = "Admin@123" });
        var token = System.Text.Json.JsonDocument.Parse(await login.Content.ReadAsStringAsync()).RootElement.GetProperty("accessToken").GetString();
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var res = await _client.GetAsync("/api/services");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var body = await res.Content.ReadAsStringAsync();
        Assert.Contains("Payment API", body);
    }

    [Fact]
    public async Task Register_Validation_FailsForShortPassword()
    {
        var res = await _client.PostAsJsonAsync("/api/auth/register", new { fullName = "X", email = "x@example.com", password = "123" });
        Assert.NotEqual(HttpStatusCode.OK, res.StatusCode);
    }
}
