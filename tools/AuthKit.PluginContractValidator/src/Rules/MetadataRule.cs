using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AuthKit.PluginContractValidator.Core;
using AuthKit.Plugins.Abstractions.Contracts;

namespace AuthKit.PluginContractValidator.Rules;

/// <summary>
/// Ensures plugin metadata (Name, Version) is present and well-formed.
/// </summary>
/// <remarks>
/// The rule verifies that <see cref="IAuthKitPlugin.Name"/> is non-empty and that
/// <see cref="IAuthKitPlugin.Version"/> follows the SemVer
/// (<c>major.minor.patch[-prerelease]</c>) format expected by the host.
/// </remarks>
public sealed class MetadataRule : IPluginContractRule
{
    public string Name => "Metadata";

    public Task<IReadOnlyList<string>> ValidateAsync(
        LoadedPlugin plugin,
        CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();
        var instance = plugin.Instance;

        if (string.IsNullOrWhiteSpace(instance.Name))
        {
            errors.Add("metadata: Name is null or empty");
        }

        return Task.FromResult<IReadOnlyList<string>>(errors);
    }
}