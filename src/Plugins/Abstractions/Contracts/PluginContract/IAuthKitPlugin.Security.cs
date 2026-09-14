using AuthKit.Plugins.Abstractions.Contracts.SecuritySchemes;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;

namespace AuthKit.Plugins.Abstractions.Contracts.PluginContract;

public partial interface IAuthKitPlugin
{
    /// <summary>
    /// Gets the OpenAPI security schemes contributed by the plugin.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The host exposes security schemes returned by this method as
    /// part of its Swagger/OpenAPI security metadata.
    /// </para>
    /// <para>
    /// Plugins that do not contribute to security schemes can rely on the default
    /// empty collection.
    /// </para>
    /// </remarks>
    /// <returns>readonly dictionary keyed by the security scheme name. </returns>
    IReadOnlyDictionary<string, AuthKitSecuritySchemeDescriptor> GetSecuritySchemes() =>
        new Dictionary<string, AuthKitSecuritySchemeDescriptor>();

    /// <summary>
    /// Configures authentication schemes on the host-owned authentication builder.
    /// </summary>
    /// <param name="builder">The actual host authentication builder.</param>
    /// <remarks>
    /// <para>
    /// This optional hook is invoked once per plugin while the host configures its
    /// security infrastructure, before the service provider is built. Plugins are
    /// processed in ascending <see cref="IAuthKitPlugin.Id"/> order, so invocation order is
    /// deterministic and does not depend on discovery order.
    /// </para>
    /// <para>
    /// The hook receives the same <see cref="AuthenticationBuilder"/> used by the host,
    /// so schemes registered here participate in the application authentication
    /// infrastructure. Plugin services, including handler dependencies, may be
    /// registered later through <c>ConfigureServices</c>.
    /// </para>
    /// <para>
    /// This optional hook does not change the host default scheme. Scheme names are
    /// globally significant; registering a name already owned by the host or by
    /// another plugin fails explicitly when the authentication options are built.
    /// </para>
    /// </remarks>
    void ConfigureAuthentication(AuthenticationBuilder builder)
    {
    }

    /// <summary>
    /// Configures authorization policies on the host-owned authorization options.
    /// </summary>
    /// <param name="options">The actual host authorization options.</param>
    /// <remarks>
    /// <para>
    /// This optional hook is invoked once per plugin while the host configures its
    /// security infrastructure, before the service provider is built. Plugins are
    /// processed in ascending <see cref="IAuthKitPlugin.Id"/> order, so invocation order is
    /// deterministic and does not depend on discovery order.
    /// </para>
    /// <para>
    /// Policies registered here are available through the standard ASP.NET Core
    /// authorization infrastructure and can protect endpoints mapped by any plugin.
    /// </para>
    /// <para>
    /// Policy names are globally significant. The host rejects duplicate ownership:
    /// a policy name already registered by another plugin fails to start up with an
    /// exception identifying both plugins. Plugins should use globally unique,
    /// preferably namespaced policy names.
    /// </para>
    /// <para>
    /// Existing default and fallback policies are not replaced by the host, and this
    /// hook must not assume ownership of such global defaults.
    /// </para>
    /// </remarks>
    void ConfigureAuthorization(AuthorizationOptions options)
    {
    }
}