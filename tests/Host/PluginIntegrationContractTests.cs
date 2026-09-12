using AuthKit.Plugins.Abstractions.Contracts;
using AuthKit.Plugins.Abstractions.Contracts.Plugins;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace AuthKit.Host.Tests;

/// <summary>
/// Verifies plugin option binding uses the same scoped configuration section as the host.
/// </summary>
public sealed class PluginIntegrationContractTests
{
    [Fact]
    public void BindConfiguration_UsesPluginNameSectionWhenNoIdSectionExists()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Plugins:OptionsPlugin:Value"] = "by-name",
                ["Plugins:other-plugin:Value"] = "other-value"
            })
            .Build();
        var services = new ServiceCollection();
        services.AddOptions();

        IAuthKitPlugin plugin = new OptionsPlugin();
        plugin.BindConfiguration<PluginOptions>(services, configuration);

        using var provider = services.BuildServiceProvider();
        Assert.Equal("by-name", provider.GetRequiredService<IOptions<PluginOptions>>().Value.Value);
    }

    [Fact]
    public void BindConfiguration_PrefersPluginIdSectionOverPluginName()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Plugins:authkit.optionsplugin:Value"] = "by-id",
                ["Plugins:OptionsPlugin:Value"] = "by-name"
            })
            .Build();
        var services = new ServiceCollection();
        services.AddOptions();

        IAuthKitPlugin plugin = new OptionsPlugin();
        plugin.BindConfiguration<PluginOptions>(services, configuration);

        using var provider = services.BuildServiceProvider();
        Assert.Equal("by-id", provider.GetRequiredService<IOptions<PluginOptions>>().Value.Value);
    }

    private sealed class PluginOptions
    {
        public string Value { get; set; } = string.Empty;
    }

    [PluginMetadata("authkit.optionsplugin", "1.0.0", [], [], [], name: "OptionsPlugin", description: "Options test")]
    private sealed class OptionsPlugin : IAuthKitPlugin
    {
    }
}