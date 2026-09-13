using AuthKit.Plugins.Abstractions.Contracts;
using AuthKit.Plugins.Abstractions.Contracts.Plugins;
using Xunit;

namespace AuthKit.Plugins.Abstractions.Tests;

public sealed class IAuthKitPluginCapabilitiesTests
{
    [Fact]
    public void Capabilities_ArePerPluginInstance_NotSharedAcrossPlugins()
    {
        IAuthKitPlugin first = new FirstPlugin();
        IAuthKitPlugin second = new SecondPlugin();

        Assert.Contains("auth", first.Capabilities);
        Assert.DoesNotContain("audit", first.Capabilities);
        Assert.Contains("audit", second.Capabilities);
        Assert.DoesNotContain("auth", second.Capabilities);

        Assert.NotSame(first.Capabilities, second.Capabilities);
    }

    [Fact]
    public void Capabilities_Comparison_IsCaseInsensitive()
    {
        IAuthKitPlugin plugin = new FirstPlugin();

        Assert.Contains("AUTH", plugin.Capabilities);
        Assert.True(plugin.Capabilities.Contains("Auth", StringComparer.OrdinalIgnoreCase));
    }

    [PluginMetadata("test.first", "1.0.0", [], null, ["auth", "storage"], description: "First test plugin")]
    private sealed class FirstPlugin : IAuthKitPlugin;

    [PluginMetadata("test.second", "1.0.0", [], null, ["audit", "storage"], description: "Second test plugin")]
    private sealed class SecondPlugin : IAuthKitPlugin;
}