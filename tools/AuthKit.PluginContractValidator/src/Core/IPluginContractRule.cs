using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AuthKit.PluginContractValidator.Core;

/// <summary>
/// Validates single contract concern of loaded plugin.
/// </summary>
/// <remarks>
/// Rules are composed by <see cref="ContractValidator"/> and executed independently, so a
/// failure in one concern does not prevent the others from being evaluated.
/// </remarks>
public interface IPluginContractRule
{
    /// <summary>
    /// Gets a name of the validated contract concern (for example, "Metadata").
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Validates the plugin and returns the list of contract violations.
    /// </summary>
    /// <param name="plugin">The loaded plugin to validate.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the validation.</param>
    /// <returns>
    /// A read-only list of violation messages. An empty list means the rule passed.
    /// </returns>
    Task<IReadOnlyList<string>> ValidateAsync(LoadedPlugin plugin, CancellationToken cancellationToken = default);
}
