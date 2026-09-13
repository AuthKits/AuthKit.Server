using AuthKit.Plugins.Abstractions.Contracts;
using AuthKit.Plugins.Abstractions.Contracts.Plugins;
using Host.Plugins;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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

    [Fact]
    public void BindConfiguration_InvalidRequiredConfiguration_FailsExplicitly()
    {
        var builder = Microsoft.Extensions.Hosting.Host.CreateApplicationBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Plugins:OptionsPlugin:Value"] = "bad"
        });
        var plugin = new ValidatingPlugin();

        PluginConfigurationInvoker.Configure(plugin, builder, builder.Configuration);

        using var provider = builder.Services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<PluginOptions>>();
        var ex = Record.Exception(() => options.Value);

        Assert.IsType<OptionsValidationException>(ex);
    }

    private sealed class PluginOptions
    {
        public string Value { get; set; } = string.Empty;
    }

    [PluginMetadata("authkit.optionsplugin", "1.0.0", [], [], [], name: "OptionsPlugin", description: "Options test")]
    private sealed class OptionsPlugin : IAuthKitPlugin
    {
    }

    [PluginMetadata("authkit.optionsplugin", "1.0.0", [], [], [], name: "OptionsPlugin", description: "Options test")]
    private sealed class ValidatingPlugin : IAuthKitPlugin
    {
        public void ConfigureServices(IServiceCollection services, AuthKitPluginContext context)
        {
            services.AddOptions<PluginOptions>()
                .Bind(context.Configuration)
                .Validate(value => value.Value != "bad", "Value must not be 'bad'");
        }
    }
}