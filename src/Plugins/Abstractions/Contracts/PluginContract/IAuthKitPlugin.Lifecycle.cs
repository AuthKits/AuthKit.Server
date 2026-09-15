using Microsoft.Extensions.Hosting;

namespace AuthKit.Plugins.Abstractions.Contracts.PluginContract;

public partial interface IAuthKitPlugin
{
    /// <summary>
    /// Initializes plugin runtime resources before the host is considered started.
    /// </summary>
    /// <param name="cancellationToken">The host startup cancellation token.</param>
    /// <returns>A task that completes when initialization is complete.</returns>
    Task OnStartingAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>
    /// Notifies the plugin after the host has started successfully.
    /// </summary>
    /// <param name="cancellationToken">The host lifecycle cancellation token.</param>
    /// <returns>A task that completes when post-start work is complete.</returns>
    Task OnStartedAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>
    /// Releases plugin runtime resources during a graceful host shutdown.
    /// </summary>
    /// <param name="cancellationToken">The host shutdown cancellation token.</param>
    /// <returns>A task that completes when shutdown preparation is complete.</returns>
    Task OnStoppingAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>
    /// Gets hosted services owned by this plugin.
    /// </summary>
    /// <returns>A non-null collection of services registered in the host DI container.</returns>
    IReadOnlyList<IHostedService> GetHostedServices() => [];
}