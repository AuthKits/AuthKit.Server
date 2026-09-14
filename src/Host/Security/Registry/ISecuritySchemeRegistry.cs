using AuthKit.Plugins.Abstractions.Contracts.SecuritySchemes;

namespace Host.Security.Registry;

/// <summary>
/// Resolves the <see cref="AuthKitSecuritySchemeDescriptor"/> that protects the
/// current request.
/// </summary>
/// <remarks>
/// <para>
/// The registry is built from the security schemes contributed by every enabled
/// plugin at startup. It provides request-aware lookup by scheme name, replacing
/// any reliance on a single ambient descriptor registered in the container.
/// </para>
/// <para>
/// Lookups are case-insensitive because scheme names are identifiers referenced
/// by endpoint metadata and are expected to be treated consistently across the
/// host and plugins.
/// </para>
/// </remarks>
public interface ISecuritySchemeRegistry
{
    /// <summary>
    /// Looks up the descriptor for the named security scheme.
    /// </summary>
    /// <param name="schemeName">The scheme name declared on the endpoint.</param>
    /// <param name="result">
    /// Receives the matching descriptor when found; otherwise the null reference.
    /// </param>
    /// <returns><c>true</c> when the scheme is registered; otherwise, <c>false</c>.</returns>
    bool TryGet(string schemeName, out AuthKitSecuritySchemeDescriptor result);
}