using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace MiniShop.Api.Tests;

public sealed class SmokeTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    public SmokeTests(WebApplicationFactory<Program> factory) => _client = factory.CreateClient();

    [Fact]
    public async Task HealthEndpointResponds()
    {
        var response = await _client.GetAsync("/api/health");
        response.EnsureSuccessStatusCode();
    }
}
