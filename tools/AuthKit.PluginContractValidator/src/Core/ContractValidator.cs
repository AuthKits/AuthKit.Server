using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AuthKit.PluginContractValidator.Core;

/// <summary>
/// Runs every registered <see cref="IPluginContractRule"/> against a loaded plugin and
/// aggregates the resulting contract violations.
/// </summary>
/// <remarks>
/// Rules are executed sequentially; each rule's violations are collected into a single
/// combined list so the caller sees every problem at once.
/// </remarks>
/// <param name="rules">The contract rules to execute for each plugin.</param>
public sealed class ContractValidator(IEnumerable<IPluginContractRule> rules)
{
    /// <summary>
    /// Validates the supplied plugin against all registered rules.
    /// </summary>
    /// <param name="plugin">The loaded plugin to validate.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the validation.</param>
    /// <returns>The aggregated, read-only list of contract violations.</returns>
    public async Task<IReadOnlyList<string>> ValidateAsync(
        LoadedPlugin plugin,
        CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();

        foreach (var rule in rules)
        {
            cancellationToken.ThrowIfCancellationRequested();
            errors.AddRange(await rule.ValidateAsync(plugin, cancellationToken));
        }

        return errors;
    }
}
