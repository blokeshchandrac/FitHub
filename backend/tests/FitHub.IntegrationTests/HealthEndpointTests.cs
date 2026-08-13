using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;

namespace FitHub.IntegrationTests;

public sealed class HealthEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public HealthEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.WithWebHostBuilder(builder => builder.UseEnvironment("Testing")).CreateClient();
    }

    [Fact]
    public async Task HealthEndpointReturnsHealthyStatus()
    {
        using var response = await _client.GetAsync("/health");
        var payload = await response.Content.ReadFromJsonAsync<HealthResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", payload?.Status);
    }

    private sealed record HealthResponse(string Status);
}
