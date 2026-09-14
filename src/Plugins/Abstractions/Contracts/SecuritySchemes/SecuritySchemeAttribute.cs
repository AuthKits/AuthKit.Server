using AuthKit.Plugins.Abstractions.Contracts.PluginContract;

namespace AuthKit.Plugins.Abstractions.Contracts.SecuritySchemes;

/// <summary>
/// Declares the security scheme that protects an endpoint.
/// </summary>
/// <remarks>
/// <para>
/// The host applies this attribute to endpoint metadata so that request-aware
/// security resolution can select the matching
/// <see cref="AuthKitSecuritySchemeDescriptor"/> for the current request,
/// instead of relying on a single ambient registered descriptor.
/// </para>
/// <para>
/// The scheme name must match a key returned by an enabled plugin's
/// <see cref="PluginContract.IAuthKitPlugin.GetSecuritySchemes"/>; otherwise the host rejects
/// requests to the endpoint as a configuration error.
/// </para>
/// </remarks>
/// <param name="schemeName">The unique name of the security scheme protecting the endpoint.</param>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class SecuritySchemeAttribute(string schemeName) : Attribute
{
    /// <summary>
    /// Gets the unique name of the security scheme protecting the endpoint.
    /// </summary>
    public string SchemeName { get; } = schemeName;
}