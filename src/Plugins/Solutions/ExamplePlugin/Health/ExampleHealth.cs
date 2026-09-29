using AuthKit.Plugins.Abstractions.Models;
using Microsoft.Extensions.DependencyInjection;
using IAuthKitPlugin = AuthKit.Plugins.Abstractions.Contracts.PluginContract.IAuthKitPlugin;

namespace ExamplePlugin;

public sealed partial class ExamplePlugin : IAuthKitPlugin
{
    /// <summary>
    /// Performs a structured health check of the plugin's dependencies.
    /// </summary>
    /// <param name="services">The root service provider of the host application.</param>
    /// <param name="cancellationToken">A token that can cancel the health check.</param>
    /// <returns>Structured health results for each checked capability.</returns>
    /// <remarks>
    /// The reference implementation always reports healthy to keep the example self-contained;
    /// real plugins resolve their dependencies from <paramref name="services"/> and report
    /// degraded or unhealthy states with matching reasons, data, and tags.
    /// </remarks>
    public Task<IReadOnlyList<PluginHealthResult>> CheckHealthAsync(
        IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var timeProvider = services.GetService<TimeProvider>() ?? TimeProvider.System;

        return Task.FromResult<IReadOnlyList<PluginHealthResult>>(
        [
            new(
                PluginHealthStatus.Healthy,
                "ExamplePlugin is operational.",
                new Dictionary<string, object>
                {
                    ["capability"] = "example",
                    ["utcNow"] = timeProvider.GetUtcNow().ToString("O")
                },
                ["example", "readiness"])
        ]);
    }
}
