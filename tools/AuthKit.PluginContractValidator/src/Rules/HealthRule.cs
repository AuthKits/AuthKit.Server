using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AuthKit.PluginContractValidator.Core;
using AuthKit.Plugins.Abstractions.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AuthKit.PluginContractValidator.Rules;

/// <summary>
/// Ensures the plugin can configure its services and that
/// <c>CheckHealthAsync</c> completes without throwing.
/// </summary>
/// <remarks>
/// The rule builds an isolated service provider from the services registered
/// by the plugin and invokes <see cref="Plugins.Abstractions.Contracts.PluginContract.IAuthKitPlugin.CheckHealthAsync"/>.
/// Basic framework services the host would provide, such as logging, are
/// registered so plugins can rely on them. Any exception during service
/// configuration or health checking is reported as a contract violation.
/// </remarks>
public sealed class HealthRule : IPluginContractRule
{
    /// <summary>Gets the rule name ("Health").</summary>
    public string Name => "Health";

    /// <summary>
    /// Validates plugin service configuration and health checking.
    /// </summary>
    /// <param name="plugin">The loaded plugin to validate.</param>
    /// <param name="cancellationToken">
    /// A token that can be used to cancel the validation.
    /// </param>
    /// <returns>
    /// The list of health-related violations; empty when service configuration
    /// and the health check complete without throwing.
    /// </returns>
    public async Task<IReadOnlyList<string>> ValidateAsync(
        LoadedPlugin plugin,
        CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();
        var services = new ServiceCollection();
        services.AddLogging();
        var configuration = new ConfigurationBuilder().Build();

        try
        {
            PluginConfigurationInvoker.Configure(plugin.Instance, services, configuration);
        }
        catch (Exception ex)
        {
            errors.Add($"health: ConfigureServices threw: {ex.Message}");
            return errors;
        }

        try
        {
            await using var provider = services.BuildServiceProvider();
            var results = await plugin.Instance.CheckHealthAsync(provider, cancellationToken);
            if (results is null || results.Count == 0)
                errors.Add("health: CheckHealthAsync returned no health results.");
        }
        catch (Exception ex)
        {
            errors.Add($"health: CheckHealthAsync threw: {ex.Message}");
        }

        return errors;
    }
}