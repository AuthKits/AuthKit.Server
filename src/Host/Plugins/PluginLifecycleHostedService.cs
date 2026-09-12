using AuthKit.Plugins.Abstractions.Contracts;

namespace Host.Plugins;

/// <summary>
/// Bridges plugin lifecycle hooks to the standard .NET host lifecycle.
/// </summary>
/// <remarks>
/// <para>
/// Plugins are ordered by stable identifier. Startup and started hooks run in
/// ascending order stopping hooks run in reverse order.
/// </para>
/// <para>
/// OnStarting runs when the host starts, while OnStarted and
/// OnStopping are registered against <see cref="IHostApplicationLifetime"/>
/// callbacks so they reflect the same ordering guarantees as the rest of the host.
/// </para>
/// <para>
/// Lifecycle exceptions are wrapped with plugin and stage information and
/// rethrown to the host so that startup or shutdown failures surface loudly.
/// </para>
/// </remarks>
internal sealed class PluginLifecycleHostedService(
    IReadOnlyList<LoadedPlugin> plugins,
    IHostApplicationLifetime lifetime,
    ILogger<PluginLifecycleHostedService> logger) : IHostedService
{
    private readonly IReadOnlyList<LoadedPlugin> _plugins = plugins
        .OrderBy(plugin => plugin.Plugin.Id, StringComparer.Ordinal)
        .ToArray();

    /// <summary>
    /// Invokes plugin lifecycle hooks as the host begins and registers the
    /// remaining hooks against the host application lifetime.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Plugin OnStarting hooks are invoked synchronously, in ascending
    /// plugin identifier order, before the host is considered started.
    /// </para>
    /// <para>
    /// OnStarted hooks are invoked when <see cref="IHostApplicationLifetime.ApplicationStarted"/>
    /// fires, and OnStopping hooks when <see cref="IHostApplicationLifetime.ApplicationStopping"/>
    /// fires, in reverse plugin order.
    /// </para>
    /// </remarks>
    /// <param name="cancellationToken">Token that can be used to signal cancellation of the startup operation.</param>
    /// <exception cref="InvalidOperationException">Thrown when any plugin hook fails during a lifecycle stage. </exception>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        foreach (var loadedPlugin in _plugins)
        {
            var plugin = loadedPlugin.Plugin;
            logger.LogDebug("Starting plugin '{PluginId}'.", plugin.Id);
            Invoke(plugin, "OnStarting", () => plugin.OnStartingAsync(cancellationToken));
        }

        lifetime.ApplicationStarted.Register(() =>
        {
            foreach (var loadedPlugin in _plugins)
            {
                var plugin = loadedPlugin.Plugin;
                logger.LogDebug("Plugin '{PluginId}' started.", plugin.Id);
                Invoke(plugin, "OnStarted", () => plugin.OnStartedAsync(lifetime.ApplicationStopping));
            }
        });

        lifetime.ApplicationStopping.Register(() =>
        {
            foreach (var loadedPlugin in _plugins.Reverse())
            {
                var plugin = loadedPlugin.Plugin;
                logger.LogDebug("Stopping plugin '{PluginId}'.", plugin.Id);
                Invoke(plugin, "OnStopping", () => plugin.OnStoppingAsync(lifetime.ApplicationStopping));
            }
        });

        return Task.CompletedTask;
    }

    /// <summary>
    /// Stops the lifecycle orchestration service. Hook-based shutdown is handled
    /// through <see cref="IHostApplicationLifetime.ApplicationStopping"></see>.
    /// </summary>
    /// <param name="cancellationToken">Token that can be used to signal cancellation of the shutdown operation.</param>
    /// <returns>
    /// A <see cref="Task"/> representing the shutdown operation.
    /// </returns>
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>
    /// Invokes a single lifecycle hook, wrapping any failure with plugin and stage
    /// information.
    /// </summary>
    /// <param name="plugin">The plugin whose hook is being invoked.</param>
    /// <param name="stage">The lifecycle stage, used in the error message.</param>
    /// <param name="operation">The hook operation to execute.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <paramref name="operation"/> throws, wrapping the original
    /// exception.
    /// </exception>
    private static void Invoke(IAuthKitPlugin plugin, string stage, Func<Task> operation)
    {
        try
        {
            operation().GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Plugin '{plugin.Id}' failed during {stage}.", ex);
        }
    }
}