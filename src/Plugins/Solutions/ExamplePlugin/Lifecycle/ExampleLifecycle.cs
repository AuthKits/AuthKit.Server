using ExamplePlugin.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using IAuthKitPlugin = AuthKit.Plugins.Abstractions.Contracts.PluginContract.IAuthKitPlugin;

namespace ExamplePlugin;

public sealed partial class ExamplePlugin : IAuthKitPlugin
{
    /// <summary>
    /// Initializes plugin runtime resources before the host is considered started.
    /// </summary>
    public Task OnStartingAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>
    /// Notifies the plugin after the host has started successfully.
    /// </summary>
    public Task OnStartedAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>
    /// Releases plugin runtime resources during a graceful host shutdown.
    /// </summary>
    public Task OnStoppingAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>
    /// Returns the plugin-owned hosted services registered in the host DI container.
    /// </summary>
    /// <remarks>
    /// The host registers each service returned here as a singleton <see cref="IHostedService"/>
    /// and starts and stops it with the application.
    /// </remarks>
    public IReadOnlyList<IHostedService> GetHostedServices() => [new ExampleBackgroundService(
        new NullLogger<ExampleBackgroundService>(),
        TimeProvider.System)];
}
