using System.Text.Json;
using AuthKit.Plugins.Abstractions;
using AuthKit.Plugins.Abstractions.Contracts;
using AuthKit.Plugins.Abstractions.Models;
using Core.KeyManagement.DTO;
using Core.KeyManagement.Interfaces;
using Host.Configuration.Pipeline;
using Host.Plugins.Loading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using PluginMetadataAttribute = AuthKit.Plugins.Abstractions.Contracts.Plugins.PluginMetadataAttribute;
using Xunit;
using Host.Plugins.Health;
using IAuthKitPlugin = AuthKit.Plugins.Abstractions.Contracts.PluginContract.IAuthKitPlugin;

namespace AuthKit.Host.Tests;

/// <summary>
/// Verifies the health endpoint aggregation semantics implemented by
/// <see cref="Host.Configuration.Pipeline.EndpointConfiguration"/>: collection of multiple
/// structured results, maximum-status aggregation, default behavior for plugins
/// without custom health checks, and key store integration.
/// </summary>
public sealed class PluginHealthEndpointTests
{
    [Fact]
    public async Task HealthEndpoint_AggregatesMultipleResultsByMaximumStatus()
    {
        var healthy = new SingleResultPlugin(
            new PluginHealthResult(PluginHealthStatus.Healthy, "accepted connections."));
        var degraded = new MultiResultPlugin(
            new PluginHealthResult(PluginHealthStatus.Healthy, "database is available."),
            new PluginHealthResult(
                PluginHealthStatus.Degraded,
                "cache is responding slowly.",
                new Dictionary<string, object> { ["cache-latency-ms"] = "3200" }));

        await using var app = await BuildHostAsync(
            [Load(healthy), Load(degraded)],
            keyStoreHealthy: true);

        using var response = await app.Client.GetAsync("/health");

        Assert.Equal(StatusCodes.Status503ServiceUnavailable, (int)response.StatusCode);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = body.RootElement;

        Assert.Equal("Degraded", root.GetProperty("status").GetString());
        Assert.Equal("Healthy", root.GetProperty("jwtKeyStore").GetString());

        var plugins = root.GetProperty("plugins");

        var singleEntry = plugins.GetProperty("healthy-single");
        Assert.Equal(1, singleEntry.GetArrayLength());
        Assert.Equal((int)PluginHealthStatus.Healthy, singleEntry[0].GetProperty("status").GetInt32());
        Assert.Equal("accepted connections.", singleEntry[0].GetProperty("reason").GetString());

        var multiEntry = plugins.GetProperty("healthy-degraded");
        Assert.Equal(2, multiEntry.GetArrayLength());
        Assert.Equal((int)PluginHealthStatus.Healthy, multiEntry[0].GetProperty("status").GetInt32());
        Assert.Equal("database is available.", multiEntry[0].GetProperty("reason").GetString());
        Assert.Equal((int)PluginHealthStatus.Degraded, multiEntry[1].GetProperty("status").GetInt32());
        Assert.Equal("3200", multiEntry[1].GetProperty("data").GetProperty("cache-latency-ms").GetString());
    }

    [Fact]
    public async Task HealthEndpoint_ReturnsHealthyWhenAllPluginsReportHealthy()
    {
        await using var app = await BuildHostAsync(
            [Load(new SingleResultPlugin(new PluginHealthResult(PluginHealthStatus.Healthy, "ok")))],
            keyStoreHealthy: true);

        using var response = await app.Client.GetAsync("/health");

        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Healthy", body.RootElement.GetProperty("status").GetString());
    }

    [Fact]
    public async Task HealthEndpoint_TreatsPluginsWithoutCustomHealthChecksAsHealthy()
    {
        await using var app = await BuildHostAsync(
            [Load(new DefaultHealthPlugin())],
            keyStoreHealthy: true);

        using var response = await app.Client.GetAsync("/health");

        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var entry = body.RootElement.GetProperty("plugins").GetProperty("default-behavior");
        Assert.Equal(1, entry.GetArrayLength());
        Assert.Equal((int)PluginHealthStatus.Healthy, entry[0].GetProperty("status").GetInt32());
    }

    [Fact]
    public async Task HealthEndpoint_ReportsUnhealthyKeyStoreDespiteHealthyPlugins()
    {
        await using var app = await BuildHostAsync(
            [Load(new SingleResultPlugin(new PluginHealthResult(PluginHealthStatus.Healthy, "ok")))],
            keyStoreHealthy: false);

        using var response = await app.Client.GetAsync("/health");

        Assert.Equal(StatusCodes.Status503ServiceUnavailable, (int)response.StatusCode);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = body.RootElement;
        Assert.Equal("Unhealthy", root.GetProperty("status").GetString());
        Assert.Equal("Unhealthy", root.GetProperty("jwtKeyStore").GetString());
        var entry = root.GetProperty("plugins").GetProperty("healthy-single");
        Assert.Equal((int)PluginHealthStatus.Healthy, entry[0].GetProperty("status").GetInt32());
    }

    private static async Task<AppUnderTest> BuildHostAsync(
        IReadOnlyList<LoadedPlugin> plugins,
        bool keyStoreHealthy)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddControllers();
        builder.Services.AddSingleton<IJwtKeyStore>(new StubKeyStore(keyStoreHealthy));
        builder.Services.AddSingleton(plugins);
        builder.Services.Configure<PluginHealthExecutionOptions>(_ => { });
        builder.Services.AddSingleton<PluginHealthExecutor>();

        var app = builder.Build();
        app.MapAppEndpoints(plugins);
        await app.StartAsync();

        return new AppUnderTest(app, app.GetTestClient());
    }

    private sealed record AppUnderTest(WebApplication App, HttpClient Client) : IAsyncDisposable
    {
        public ValueTask DisposeAsync() => App.DisposeAsync();
    }

    private sealed class StubKeyStore(bool healthy) : IJwtKeyStore
    {
        public IEnumerable<PublicJwkDto> GetPublicJwks() =>
            healthy ? new[] { new PublicJwkDto() } : Array.Empty<PublicJwkDto>();

        public Task InitializeAsync() => Task.CompletedTask;
        public SigningCredentials GetActiveSigningCredentials() => throw new NotSupportedException();
        public SigningCredentials? GetSigningCredentialsByKid(string kid) => throw new NotSupportedException();
        public Task<KeyMetadata> RotateAsync(int rsaBits = 4096) => throw new NotSupportedException();
        public Task<bool> RevokeAsync(string kid) => throw new NotSupportedException();
        public KeyMetadata? GetMetadata(string kid) => throw new NotSupportedException();
    }

    private static LoadedPlugin Load(IAuthKitPlugin plugin) =>
        new(plugin, plugin.GetType().Assembly, "test");

    [PluginMetadataAttribute("healthy-single", "1.0.0", [], [], [], description: "Single result health plugin")]
    private sealed class SingleResultPlugin(PluginHealthResult result) : IAuthKitPlugin
    {
        public Task<IReadOnlyList<PluginHealthResult>> CheckHealthAsync(
            IServiceProvider services,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<PluginHealthResult>>([result]);
    }

    [PluginMetadataAttribute("healthy-degraded", "1.0.0", [], [], [], description: "Multi result health plugin")]
    private sealed class MultiResultPlugin(params PluginHealthResult[] results) : IAuthKitPlugin
    {
        public Task<IReadOnlyList<PluginHealthResult>> CheckHealthAsync(
            IServiceProvider services,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<PluginHealthResult>>(results);
    }

    [PluginMetadataAttribute("default-behavior", "1.0.0", [], [], [], description: "Default health plugin")]
    private sealed class DefaultHealthPlugin : IAuthKitPlugin;
}