using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AuthKit.PluginContractValidator.Core;
using AuthKit.Plugins.Abstractions.Contracts.PluginContract;
using AuthKit.Plugins.Abstractions.Pipeline;

namespace AuthKit.PluginContractValidator.Rules;

/// <summary>
/// Ensures plugin lifecycle hooks are structurally sound: hosted services are
/// valid and the pipeline position is a defined stage.
/// </summary>
/// <remarks>
/// The rule invokes <see cref="IAuthKitPlugin.GetHostedServices"/> and verifies
/// the result contains no null entries and no duplicates, since the host
/// registers each entry as a singleton <c>IHostedService</c>. It also verifies
/// <see cref="IAuthKitPlugin.PipelinePosition"/> names a defined
/// <see cref="PluginPipelinePosition"/> value. Every violation names the member.
/// </remarks>
public sealed class LifecycleRule : IPluginContractRule
{
    /// <summary>Gets the rule name ("Lifecycle").</summary>
    public string Name => "Lifecycle";

    /// <summary>
    /// Validates lifecycle hooks of the loaded plugin.
    /// </summary>
    /// <param name="plugin">The loaded plugin to validate.</param>
    /// <param name="cancellationToken">A token that can cancel validation.</param>
    /// <returns>Lifecycle violations; empty when hooks are sound.</returns>
    public Task<IReadOnlyList<string>> ValidateAsync(
        LoadedPlugin plugin,
        CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();
        var instance = plugin.Instance;
        var pluginName = instance.Name;

        List<Microsoft.Extensions.Hosting.IHostedService>? services = null;
        try
        {
            services = instance.GetHostedServices()?.ToList();
        }
        catch (Exception ex)
        {
            errors.Add($"lifecycle: Plugin '{pluginName}' GetHostedServices threw {ex.GetType().Name}: {ex.Message}");
        }

        if (services is null)
        {
            errors.Add($"lifecycle: Plugin '{pluginName}' GetHostedServices returned null; return an empty list when no hosted services are owned.");
        }
        else
        {
            if (services.Any(service => service is null))
                errors.Add($"lifecycle: Plugin '{pluginName}' GetHostedServices contains null entries.");

            var duplicates = services
                .Where(service => service is not null)
                .GroupBy(service => service!.GetType())
                .Where(group => group.Count() > 1)
                .Select(group => group.Key.Name)
                .ToArray();

            if (duplicates.Length > 0)
                errors.Add($"lifecycle: Plugin '{pluginName}' GetHostedServices registers duplicate services: {string.Join(", ", duplicates)}.");
        }

        if (!Enum.IsDefined(instance.PipelinePosition))
            errors.Add($"lifecycle: Plugin '{pluginName}' PipelinePosition '{(int)instance.PipelinePosition}' is not a defined PluginPipelinePosition value.");

        return Task.FromResult<IReadOnlyList<string>>(errors);
    }
}
