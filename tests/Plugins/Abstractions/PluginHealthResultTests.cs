using System.Text.Json;
using AuthKit.Plugins.Abstractions.Models;
using Xunit;

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
            });

        var json = JsonSerializer.Serialize(result);
        var restored = JsonSerializer.Deserialize<PluginHealthResult>(json);

        Assert.NotNull(restored);
        Assert.Equal(PluginHealthStatus.Degraded, restored.Status);
        Assert.Equal("Cache is unavailable", restored.Reason);
        Assert.NotNull(restored.Data);
        Assert.Equal("cache", restored.Data["dependency"].ToString());
        Assert.Equal("2", restored.Data["retry_count"].ToString());
    }
}