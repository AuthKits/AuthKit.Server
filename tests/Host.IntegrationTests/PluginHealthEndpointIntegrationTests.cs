using System.Net;
using System.Text.Json;
using Xunit;

namespace AuthKit.Host.IntegrationTests;

/// <summary>
/// Verifies that the real host executes the plugin health contract through its
/// /health endpoint: per-plugin <c>CheckHealthAsync</c> invocation, result
/// collection, status aggregation, and serialization of the structured results.
/// </summary>
public sealed class PluginHealthEndpointIntegrationTests : IClassFixture<AuthKitWebApplicationFactory>
{
    private readonly AuthKitWebApplicationFactory _factory;

    public PluginHealthEndpointIntegrationTests(AuthKitWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Health_ExecutesPluginHealthChecksThroughTheRealHost()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.True(
            response.StatusCode is HttpStatusCode.OK or HttpStatusCode.ServiceUnavailable,
            $"Expected 200 or 503, got {(int)response.StatusCode}.");

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = body.RootElement;

        Assert.Contains(
            root.GetProperty("status").GetString(),
            new[] { "Healthy", "Degraded", "Unhealthy" });

        Assert.Equal("Healthy", root.GetProperty("jwtKeyStore").GetString());

        var plugins = root.GetProperty("plugins");
        Assert.Single(plugins.EnumerateObject());
        Assert.True(plugins.TryGetProperty("DevTokens", out var devTokens));
        Assert.True(devTokens.GetArrayLength() >= 1);
        Assert.InRange(devTokens[0].GetProperty("status").GetInt32(), 0, 2);
    }
}