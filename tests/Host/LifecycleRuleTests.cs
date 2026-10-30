using AuthKit.PluginContractValidator.Rules;
using AuthKit.Plugins.Abstractions.Contracts;
using AuthKit.Plugins.Abstractions.Contracts.Plugins;
using AuthKit.Plugins.Abstractions.Models;
using AuthKit.Plugins.Abstractions.Pipeline;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;
using IAuthKitPlugin = AuthKit.Plugins.Abstractions.Contracts.PluginContract.IAuthKitPlugin;
using ValidatorLoadedPlugin = AuthKit.PluginContractValidator.Core.LoadedPlugin;

namespace AuthKit.Host.Tests;

public sealed class LifecycleRuleTests
{
    private readonly LifecycleRule _rule = new();

    [Fact]
    public void RuleName_IsLifecycle() => Assert.Equal("Lifecycle", _rule.Name);

    [Fact]
    public async Task ValidPlugin_IsAccepted()
    {
        var errors = await ValidateAsync(new HealthyPlugin());

        Assert.Empty(errors);
    }

    [Fact]
    public async Task NullHostedServices_AreRejected()
    {
        var errors = await ValidateAsync(new NullServicesPlugin());

        Assert.Contains(errors, error =>
            error.Contains("GetHostedServices", StringComparison.Ordinal)
            && error.Contains("null", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task NullServiceEntry_IsRejected()
    {
        var errors = await ValidateAsync(new NullEntryPlugin());

        Assert.Contains(errors, error =>
            error.Contains("GetHostedServices", StringComparison.Ordinal)
            && error.Contains("null", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task DuplicateServices_AreRejected()
    {
        var errors = await ValidateAsync(new DuplicateServicesPlugin());

        Assert.Contains(errors, error =>
            error.Contains("GetHostedServices", StringComparison.Ordinal)
            && error.Contains("duplicate", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ThrowingHostedServices_AreReported()
    {
        var errors = await ValidateAsync(new ThrowingServicesPlugin());

        Assert.Contains(errors, error =>
            error.Contains("GetHostedServices", StringComparison.Ordinal)
            && error.Contains(nameof(InvalidOperationException), StringComparison.Ordinal));
    }

    [Fact]
    public async Task UndefinedPipelinePosition_IsRejected()
    {
        var errors = await ValidateAsync(new BadPositionPlugin());

        Assert.Contains(errors, error =>
            error.Contains("PipelinePosition", StringComparison.Ordinal));
    }

    private async Task<IReadOnlyList<string>> ValidateAsync(IAuthKitPlugin plugin)
    {
        var loaded = new ValidatorLoadedPlugin(plugin, typeof(LifecycleRuleTests).Assembly);
        return await _rule.ValidateAsync(loaded);
    }

    [PluginMetadata("healthy-plugin", "1.0.0", [], [], [], name: "Healthy", description: "Test plugin")]
    private sealed class HealthyPlugin : IAuthKitPlugin
    {
        public void ConfigureServices(IServiceCollection services, AuthKitPluginContext context) { }
    }

    [PluginMetadata("null-services-plugin", "1.0.0", [], [], [], name: "NullServices", description: "Test plugin")]
    private sealed class NullServicesPlugin : IAuthKitPlugin
    {
        public void ConfigureServices(IServiceCollection services, AuthKitPluginContext context) { }

        public IReadOnlyList<IHostedService> GetHostedServices() => null!;
    }

    [PluginMetadata("null-entry-plugin", "1.0.0", [], [], [], name: "NullEntry", description: "Test plugin")]
    private sealed class NullEntryPlugin : IAuthKitPlugin
    {
        public void ConfigureServices(IServiceCollection services, AuthKitPluginContext context) { }

        public IReadOnlyList<IHostedService> GetHostedServices() => [null!];
    }

    [PluginMetadata("duplicate-services-plugin", "1.0.0", [], [], [], name: "Duplicates", description: "Test plugin")]
    private sealed class DuplicateServicesPlugin : IAuthKitPlugin
    {
        public void ConfigureServices(IServiceCollection services, AuthKitPluginContext context) { }

        public IReadOnlyList<IHostedService> GetHostedServices() =>
            [new NoopService(), new NoopService()];

        private sealed class NoopService : IHostedService
        {
            public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;
            public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        }
    }

    [PluginMetadata("throwing-services-plugin", "1.0.0", [], [], [], name: "Throwing", description: "Test plugin")]
    private sealed class ThrowingServicesPlugin : IAuthKitPlugin
    {
        public void ConfigureServices(IServiceCollection services, AuthKitPluginContext context) { }

        public IReadOnlyList<IHostedService> GetHostedServices() =>
            throw new InvalidOperationException("boom");
    }

    [PluginMetadata("bad-position-plugin", "1.0.0", [], [], [], name: "BadPosition", description: "Test plugin")]
    private sealed class BadPositionPlugin : IAuthKitPlugin
    {
        public void ConfigureServices(IServiceCollection services, AuthKitPluginContext context) { }

        public PluginPipelinePosition PipelinePosition => (PluginPipelinePosition)999;
    }
}
