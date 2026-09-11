using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AuthKit.PluginContractValidator.Core;
using AuthKit.Plugins.Abstractions.Contracts;
using AuthKit.Plugins.Abstractions.Contracts.SecuritySchemes;

namespace AuthKit.PluginContractValidator.Rules;

/// <summary>
/// Ensures contributed OpenAPI security schemes are present and consistently keyed.
/// </summary>
/// <remarks>
/// The rule verifies that each key in the dictionary returned by
/// <see cref="IAuthKitPlugin.GetSecuritySchemes"/> matches the <c>Name</c> of its
/// <see cref="AuthKitSecuritySchemeDescriptor"/>.
/// </remarks>
public sealed class SecuritySchemesRule : IPluginContractRule
{
    /// <summary>
    /// Gets the rule name ("SecuritySchemes").
    /// </summary>
    public string Name => "SecuritySchemes";

    /// <summary>
    /// Validates the security schemes contributed by the plugin.
    /// </summary>
    /// <param name="plugin">The loaded plugin to validate.</param>
    /// <param name="cancellationToken">
    /// A token that can be used to cancel the validation.
    /// </param>
    /// <returns>
    /// The list of security-scheme violations; empty when the schemes are valid.
    /// </returns>
    public Task<IReadOnlyList<string>> ValidateAsync(
        LoadedPlugin plugin,
        CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();

        try
        {
            var schemes = plugin.Instance.GetSecuritySchemes();

            foreach (var pair in schemes)
            {
                if (pair.Key != pair.Value.Name)
                {
                    errors.Add($"security schemes: key '{pair.Key}' does not match descriptor Name '{pair.Value.Name}'");
                }
            }
        }
        catch (Exception ex)
        {
            errors.Add($"security schemes: GetSecuritySchemes threw: {ex.Message}");
        }

        return Task.FromResult<IReadOnlyList<string>>(errors);
    }
}