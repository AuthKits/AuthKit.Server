using AuthKit.Plugins.Abstractions.Contracts;
using AuthKit.Plugins.Abstractions.Contracts.Plugins;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace AuthKit.Host.Tests;

/// <summary>
/// Verifies plugin option binding uses the plugin-specific configuration section.
/// </summary>
public sealed class PluginIntegrationContractTests
{
    [Fact]
    public void BindConfiguration_UsesPluginNameSectionAndNormalOptionsDi()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Plugins:options-plugin:Value"] = "plugin-value",
                ["Plugins:other-plugin:Value"] = "other-value"
            })
            .Build();
        var services = new ServiceCollection();
        services.AddOptions();

        new OptionsPlugin().BindConfiguration<PluginOptions>(services, configuration);

        using var provider = services.BuildServiceProvider();
        Assert.Equal("plugin-value", provider.GetRequiredService<IOptions<PluginOptions>>().Value.Value);
    }

    private sealed class PluginOptions
    {
        public string Value { get; set; } = string.Empty;
    }

    [PluginMetadata("options-plugin", "1.0.0", [], [], [], name: "options-plugin", description: "Options test")]
    private sealed class OptionsPlugin : IAuthKitPlugin
    {
    }
}