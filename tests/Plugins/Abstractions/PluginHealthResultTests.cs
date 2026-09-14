using System.Text.Json;
using AuthKit.Plugins.Abstractions.Contracts;
using AuthKit.Plugins.Abstractions.Contracts.Plugins;
using AuthKit.Plugins.Abstractions.Models;
using Xunit;
using IAuthKitPlugin = AuthKit.Plugins.Abstractions.Contracts.PluginContract.IAuthKitPlugin;

namespace AuthKit.Plugins.Abstractions.Tests;

public sealed class PluginHealthResultTests
{
    [Fact]
    public void Statuses_AreExplicitlyDistinct()
    {
        Assert.NotEqual(PluginHealthStatus.Healthy, PluginHealthStatus.Degraded);
        Assert.NotEqual(PluginHealthStatus.Degraded, PluginHealthStatus.Unhealthy);
        Assert.NotEqual(PluginHealthStatus.Healthy, PluginHealthStatus.Unhealthy);
    }

    [Fact]
    public void HealthyResult_AllowsOptionalReasonAndData()
    {
        var result = new PluginHealthResult(PluginHealthStatus.Healthy);

        Assert.Equal(PluginHealthStatus.Healthy, result.Status);
        Assert.Null(result.Reason);
        Assert.Null(result.Data);
        Assert.Null(result.Tags);
    }

    [Fact]
    public void Result_PreservesReasonAndDiagnosticDataThroughSerialization()
    {
        var result = new PluginHealthResult(
            PluginHealthStatus.Degraded,
            "Cache is unavailable",
            new Dictionary<string, object>
            {
                ["dependency"] = "cache",
                ["retry_count"] = 2
            },
            ["cache", "readiness"]);

        var json = JsonSerializer.Serialize(result);
        var restored = JsonSerializer.Deserialize<PluginHealthResult>(json);

        Assert.NotNull(restored);
        Assert.Equal(PluginHealthStatus.Degraded, restored.Status);
        Assert.Equal("Cache is unavailable", restored.Reason);
        Assert.Equal(["cache", "readiness"], restored.Tags);
        Assert.NotNull(restored.Data);
        Assert.Equal("cache", restored.Data["dependency"].ToString());
        Assert.Equal("2", restored.Data["retry_count"].ToString());
    }

    [Fact]
    public void Tags_RemainDistinctFromStatusAndDiagnosticData()
    {
        var result = new PluginHealthResult(
            PluginHealthStatus.Unhealthy,
            "Database is unavailable.",
            new Dictionary<string, object> { ["status"] = "healthy" },
            ["database", "critical"]);

        Assert.Equal(PluginHealthStatus.Unhealthy, result.Status);
        Assert.Equal(["database", "critical"], result.Tags);
        Assert.Equal("healthy", result.Data!["status"]);
    }

    [Fact]
    public async Task PluginHealthContract_PreservesMultipleResultsAndCancellationToken()
    {
        var plugin = new StructuredHealthPlugin();
        using var cancellation = new CancellationTokenSource();

        var results = await plugin.CheckHealthAsync(
            new ServiceProviderStub(),
            cancellation.Token);

        Assert.Equal(2, results.Count);
        Assert.Equal(PluginHealthStatus.Healthy, results[0].Status);
        Assert.Equal(PluginHealthStatus.Degraded, results[1].Status);
        Assert.Equal(cancellation.Token, plugin.ReceivedToken);
    }

    [Fact]
    public async Task PluginHealthContract_DefaultImplementationReturnsHealthyResult()
    {
        IAuthKitPlugin plugin = new DefaultHealthPlugin();

        var results = await plugin.CheckHealthAsync(new ServiceProviderStub());

        var result = Assert.Single(results);
        Assert.Equal(PluginHealthStatus.Healthy, result.Status);
    }

    [PluginMetadata("structured-health", "1.0.0", [], [], [], description: "Structured health test")]
    private sealed class StructuredHealthPlugin : IAuthKitPlugin
    {
        public CancellationToken ReceivedToken { get; private set; }

        public Task<IReadOnlyList<PluginHealthResult>> CheckHealthAsync(
            IServiceProvider services,
            CancellationToken cancellationToken = default)
        {
            ReceivedToken = cancellationToken;
            return Task.FromResult<IReadOnlyList<PluginHealthResult>>(
            [
                new(PluginHealthStatus.Healthy, "Database is available."),
                new(PluginHealthStatus.Degraded, "Cache is responding slowly.")
            ]);
        }
    }

    [PluginMetadata("default-health", "1.0.0", [], [], [], description: "Default health test")]
    private sealed class DefaultHealthPlugin : IAuthKitPlugin;

    private sealed class ServiceProviderStub : IServiceProvider
    {
        public object? GetService(Type serviceType) => null;
    }
}