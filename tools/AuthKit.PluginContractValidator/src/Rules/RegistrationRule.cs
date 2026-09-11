using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AuthKit.PluginContractValidator.Core;
using AuthKit.Plugins.Abstractions.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AuthKit.PluginContractValidator.Rules;

/// <summary>
/// Ensures <c>ConfigureServices</c> registers dependencies without throwing and that the
/// resulting service container can be built (dependency wiring is valid).
/// </summary>
public sealed class RegistrationRule : IPluginContractRule
{
    /// <summary>Gets the rule name ("Registration").</summary>
    public string Name => "Registration";

    /// <summary>
    /// Invokes <see cref="IAuthKitPlugin.ConfigureServices"/> against a fresh service collection
    /// and then builds the <see cref="plugin"/> to confirm the dependency graph is
    /// constructible.
    /// </summary>
    /// <param name="plugin"></param>
    /// <param name="cancellationToken">
    ///     The loaded plugin to validate.
    ///     A token that can be used to cancel the validation.
    /// </param>
    /// <returns>The list of registration violations; empty when registration succeeds.</returns>
    public Task<IReadOnlyList<string>> ValidateAsync(
        LoadedPlugin plugin,
        CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        try
        {
            plugin.Instance.ConfigureServices(services, configuration);
        }
        catch (Exception ex)
        {
            errors.Add($"registration: ConfigureServices threw: {ex.Message}");
            return Task.FromResult<IReadOnlyList<string>>(errors);
        }

        try
        {
            services.BuildServiceProvider();
        }
        catch (Exception ex)
        {
            errors.Add($"registration: service provider build failed (bad dependency wiring): {ex.Message}");
        }

        return Task.FromResult<IReadOnlyList<string>>(errors);
    }
}
