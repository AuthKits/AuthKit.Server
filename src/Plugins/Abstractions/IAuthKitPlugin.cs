using AuthKit.Plugins.Abstractions.Contracts.Plugins;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace AuthKit.Plugins.Abstractions;

/// <summary>
/// Defines the contract implemented by an AuthKit plugin.
/// </summary>
/// <remarks>
/// <para>
/// AuthKit plugins are discovered and loaded dynamically by the host at startup.
/// A plugin does not need to be directly referenced by the host project.
/// </para>
/// <para>
/// The plugin contract allows an extension to contribute services, middleware,
/// health checks, and OpenAPI security scheme metadata to the host application.
/// </para>
/// <para>
/// Plugin implementations should keep their integration with the host limited
/// to the abstractions exposed by this contract and should register any
/// plugin-specific dependencies through <see cref="ConfigureServices"/>.
/// </para>
/// </remarks>
public interface IAuthKitPlugin
{
    /// <summary>
    /// Gets the unique name of the plugin.
    /// </summary>
    /// <remarks>
    /// The name is used to identify the plugin in host diagnostics,
    /// startup output, and other plugin-related metadata.
    /// </remarks>
    /// <remarks>
    /// By default the name is read from the <see cref="PluginMetadataAttribute"/>.
    /// Plugin classes may override this member, but the attribute is the
    /// preferred place to declare the plugin identity.
    /// </remarks>
    string Name
    {
        get
        {
            var metadata = GetPluginMetadata();
            return metadata is null ? GetType().Name : metadata.Name;
        }
    }

    /// <summary>
    /// Gets the version of the plugin.
    /// </summary>
    /// <remarks>
    /// The version is exposed as plugin metadata and may be used by the host
    /// for diagnostics, compatibility checks, or administrative surfaces.
    /// </remarks>
    /// <remarks>
    /// By default the version is read from the <see cref="PluginMetadataAttribute"/>.
    /// </remarks>
    string Version
    {
        get
        {
            var metadata = GetPluginMetadata();
            return metadata is null ? "0.0.0" : metadata.Version;
        }
    }

    /// <summary>
    /// Gets an optional human-readable description of the plugin.
    /// </summary>
    /// <remarks>
    /// The description may be displayed by the host in startup output,
    /// diagnostics, administrative interfaces, or other status surfaces.
    /// </remarks>
    /// <remarks>
    /// By default the description is read from the <see cref="PluginMetadataAttribute"/>.
    /// </remarks>
    string? Description => GetPluginMetadata()?.Description;

    /// <summary>
    /// Registers the plugin's services in the host dependency injection container.
    /// </summary>
    /// <param name="services"> The host's dependency injection service collection.</param>
    /// <param name="configuration">The host application configuration.</param>
    /// <remarks>
    /// <para>
    /// This method is called while the host application is being configured,
    /// before the application is built.
    /// </para>
    /// <para>
    /// Plugins should register all services required by their functionality
    /// through this method rather than creating their own dependency injection
    /// container.
    /// </para>
    /// </remarks>
    void ConfigureServices(
        IServiceCollection services,
        IConfiguration configuration);

    /// <summary>
    /// Performs an optional health check for the plugin.
    /// </summary>
    /// <param name="services">The root service provider of the host application.</param>
    /// <returns>
    /// <c>true</c> when the plugin is currently able to serve requests;
    /// otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The host may invoke this method as part of its health endpoint.
    /// Plugins can resolve the services they require from
    /// <paramref name="services"/> to verify the availability of their
    /// dependencies.
    /// </para>
    /// <para>
    /// A plugin should return <c>false</c> when a required dependency is
    /// unavailable, such as when its database or external service cannot
    /// currently be reached.
    /// </para>
    /// <para>
    /// The default implementation reports the plugin as healthy. Plugins
    /// that do not require custom health validation therefore do not need
    /// to implement this member.
    /// </para>
    /// </remarks>
    Task<bool> CheckHealthAsync(IServiceProvider services) =>
        Task.FromResult(true);

    /// <summary>
    /// Gets the optional ASP.NET Core middleware type contributed by the plugin.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When specified, the host inserts the middleware into its request
    /// processing pipeline at the plugin middleware slot.
    /// </para>
    /// <para>
    /// The middleware type must follow the conventional ASP.NET Core middleware
    /// pattern, including a constructor accepting <see cref="RequestDelegate"/>
    /// and an <c>InvokeAsync</c> method accepting <see cref="HttpContext"/>.
    /// Additional dependencies may be supplied through dependency injection.
    /// </para>
    /// <para>
    /// The default value is <c>null</c>, indicating that the plugin does not
    /// contribute middleware.
    /// </para>
    /// </remarks>
    Type? MiddlewareType => null;

    /// <summary>
    /// Gets the OpenAPI security schemes contributed by the plugin.
    /// </summary>
    /// <returns>
    /// A read-only dictionary keyed by security scheme name.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Security schemes returned by this method are exposed by the host as
    /// part of its Swagger/OpenAPI security metadata.
    /// </para>
    /// <para>
    /// Plugins that do not contribute security schemes can rely on the default
    /// empty collection.
    /// </para>
    /// </remarks>
    IReadOnlyDictionary<string, AuthKitSecuritySchemeDescriptor> GetSecuritySchemes() =>
        new Dictionary<string, AuthKitSecuritySchemeDescriptor>();

    /// <summary>
    /// Reads the <see cref="PluginMetadataAttribute"/> declared on the plugin
    /// implementation class.
    /// </summary>
    private PluginMetadataAttribute? GetPluginMetadata() =>
        ((object)this).GetType().GetCustomAttribute<PluginMetadataAttribute>();
}
