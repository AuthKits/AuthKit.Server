using AuthKit.Plugins.Abstractions.Contracts.Plugins;
using AuthKit.Plugins.Abstractions.Contracts.SecuritySchemes;
using AuthKit.Plugins.Abstractions.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using System.Reflection;

namespace AuthKit.Plugins.Abstractions.Contracts;

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
    /// Gets the stable, host-unique identifier of the plugin.
    /// </summary>
    /// <remarks>
    /// The ID is an author-declared identifier (e.g. "authkit.devtokens"),
    /// is expected to be non-empty and stable across restarts. The host is
    /// responsible for validating format and uniqueness before activation.
    /// </remarks>
    string Id => Metadata.Id;

    /// <summary>
    /// Gets the unique name of the plugin.
    /// </summary>
    /// <remarks>
    /// The name is used to identify the plugin in host diagnostics,
    /// startup output, and other plugin-related metadata.
    /// </remarks>
    string Name => Metadata.Name;

    /// <summary>
    /// Gets an optional human-readable display name for UIs.
    /// </summary>
    /// <remarks>
    /// The host UI should display <c>DisplayName ?? Name</c> when presenting
    /// the plugin to users.
    /// </remarks>
    string? DisplayName => Metadata.DisplayName;

    /// <summary>
    /// Gets an optional human-readable description of the plugin.
    /// </summary>
    /// <remarks>
    /// The host may display the description in startup output,
    /// diagnostics, administrative interfaces, or other status surfaces.
    /// </remarks>
    string Description => Metadata.Description;

    /// <summary>
    /// Gets the version of the plugin as a semantic version (SemVer 2.0.0).
    /// </summary>
    /// <remarks>
    /// The Version replaces the previous string-based version and exposes
    /// full semantic version semantics (parsing, equality, precedence).
    /// </remarks>
    SemanticVersion Version => SemanticVersion.Parse(Metadata.Version);

    /// <summary>
    /// Optional author metadata, visible in catalogs and diagnostics.
    /// </summary>
    string? Author => Metadata.Author;

    /// <summary>
    /// Optional SPDX-style license string (no validation performed by host).
    /// </summary>
    string? License => Metadata.License;

    /// <summary>
    /// Optional absolute URI pointing to the license text.
    /// </summary>
    string? LicenseUrl => Metadata.LicenseUrl;

    /// <summary>
    /// Optional absolute HTTP/HTTPS URI pointing to a plugin homepage.
    /// </summary>
    string? Homepage => Metadata.Homepage;

    /// <summary>
    /// Optional absolute HTTP/HTTPS URI pointing to the plugin repository.
    /// </summary>
    string? RepositoryUrl => Metadata.RepositoryUrl;

    /// <summary>
    /// Optional classification tags for UI filtering. Defaults to empty.
    /// Null or whitespace elements are invalid and should be rejected during validation.
    /// Used for filtering plugins in UIs and catalogs.
    /// </summary>
    /// <remarks>
    /// Tags are case-sensitive strings without controlled vocabulary.
    /// Example: ["security", "auth", "audit"].
    /// </remarks>
    IReadOnlyList<string> Tags => Metadata.Tags;

    /// <summary>
    /// Priority used for activation ordering among dependency-ready plugins.
    /// Lower values are activated earlier, higher values later.
    /// Defaults to 0.
    /// </summary>
    /// <remarks>
    /// The ordering algorithm is: topological sort where, among the set of currently
    /// dependency-ready plugins, the next plugin is chosen by Priority ascending.
    /// Dependency order (G7) wins over Priority.
    /// Example: A (p. 100) → B (p-100, DependsOn A), C (p. 0) ⇒ order: A, C, B.
    /// </remarks>
    int Priority => Metadata.Priority;

    /// <summary>
    /// Indicates whether the plugin is enabled. Defaults to true.
    /// </summary>
    /// <remarks>
    /// If <c>false</c>, the plugin is skipped before loading (no consistency check runs for it).
    /// For plugins accepted by the preload gate and subsequently loaded,
    /// <c>manifest.IsEnabled == instance.IsEnabled</c> is part of consistency validation.
    /// </remarks>
    bool IsEnabled => Metadata.IsEnabled;

    /// <summary>
    /// Features/capabilities exposed by the plugin. Contract
    /// requires Case-insensitive comparison. Defaults to an immutable empty set.
    /// </summary>
    /// <remarks>
    /// Used for pre-activation capability checks (via <see cref="PluginManifest"/>) and
    /// post-load consistency validation. Host checks capabilities using the
    /// <see cref="PluginExtensions.Supports(AuthKit.Plugins.Abstractions.Contracts.IAuthKitPlugin,string)"/> extension method.
    /// Example: <c>plugin.Supports("auth")</c>.
    /// </remarks>
    IReadOnlySet<string> Capabilities =>
        System.Collections.Immutable.ImmutableHashSet.CreateRange(StringComparer.OrdinalIgnoreCase, Metadata.Capabilities);

    /// <summary>
    /// Gets the metadata associated with the plugin.
    /// </summary>
    PluginMetadataAttribute Metadata => GetType().GetCustomAttribute<PluginMetadataAttribute>()
        ?? throw new InvalidOperationException($"Plugin {GetType().Name} is missing [PluginMetadata] attribute.");

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
        IConfiguration configuration) =>
        throw new NotSupportedException(
            $"Plugin '{GetType().Name}' must implement a supported ConfigureServices overload.");

    /// <summary>
    /// Configures plugin services using the host application builder.
    /// </summary>
    /// <param name="builder">The host application builder used by AuthKit.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <remarks>
    /// This overload is optional. Its default implementation delegates to the
    /// legacy service collection overload for existing plugins.
    /// </remarks>
    void ConfigureServices(
        IHostApplicationBuilder builder,
        IConfiguration configuration) =>
        ConfigureServices(builder.Services, configuration);

    /// <summary>
    /// Configures plugin services with stable plugin context information.
    /// </summary>
    /// <param name="services">The service collection used by the host.</param>
    /// <param name="context">The context for the plugin being configured.</param>
    /// <remarks>
    /// This overload is optional. Its default implementation delegates to the
    /// legacy service collection overload for existing plugins.
    /// </remarks>
    void ConfigureServices(IServiceCollection services, AuthKitPluginContext context) =>
        ConfigureServices(services, context.Configuration);

    /// <summary>
    /// Performs an optional structured health check for the plugin.
    /// </summary>
    /// <param name="services">The root service provider of the host application.</param>
    /// <param name="cancellationToken">A token that can cancel the health check.</param>
    /// <returns>
    /// One or more structured health results reported by the plugin.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The host may invoke this method as part of its health endpoint.
    /// Plugins can resolve the services they require from
    /// <paramref name="services"/> to verify the availability of their
    /// dependencies.
    /// </para>
    /// <para>
    /// A plugin may return separate results for independent dependencies or
    /// capabilities. Cancellation must be propagated to cancellable operations
    /// and is not converted into a fabricated health result.
    /// </para>
    /// <para>
    /// The default implementation reports the plugin as healthy. Existing
    /// plugins that do not require custom health validation therefore do not
    /// need to implement this member.
    /// </para>
    /// </remarks>
    Task<IReadOnlyList<PluginHealthResult>> CheckHealthAsync(
        IServiceProvider services,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<PluginHealthResult>>(
        [new PluginHealthResult(PluginHealthStatus.Healthy)]);

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
    /// Registers plugin-owned endpoints during host endpoint configuration.
    /// </summary>
    /// <param name="endpoints">The application's endpoint route builder.</param>
    /// <remarks>
    /// This optional hook runs after host services are configured and before
    /// the application starts processing requests. Exceptions are propagated.
    /// </remarks>
    void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
    }

    /// <summary>
    /// Configures plugin application middleware on the actual host application.
    /// </summary>
    /// <param name="application">The application's a live builder.</param>
    /// <remarks>
    /// When implemented, this hook takes precedence over <see cref="MiddlewareType"/>
    /// to prevent accidental duplicate middleware registration.
    /// </remarks>
    void ConfigureApplication(IApplicationBuilder application)
    {
    }

    /// <summary>
    /// Gets the explicit pipeline position used by <see cref="ConfigurePipeline"/>.
    /// </summary>
    PluginPipelinePosition PipelinePosition => PluginPipelinePosition.BeforeAuthentication;

    /// <summary>
    /// Configures plugin middleware at the declared pipeline position.
    /// </summary>
    /// <param name="application">The application's a live builder.</param>
    /// <param name="position">The position currently being configured.</param>
    /// <remarks>
    /// The host invokes this hook once at <see cref="PipelinePosition"/>.
    /// Plugins at the same position are ordered by stable plugin ID.
    /// </remarks>
    void ConfigurePipeline(IApplicationBuilder application, PluginPipelinePosition position)
    {
    }

    /// <summary>
    /// Gets the minimum host version required to load this plugin.
    /// </summary>
    /// <remarks>
    /// If the host version is lower than <see cref="MinHostVersion"/>, the plugin is rejected.
    /// </remarks>
    SemanticVersion? MinHostVersion => string.IsNullOrEmpty(Metadata.MinHostVersion) ? null : SemanticVersion.Parse(Metadata.MinHostVersion);

    /// <summary>
    /// Gets the list of plugin IDs this plugin depends on.
    /// </summary>
    /// <remarks>
    /// Each entry must be a valid plugin ID. The host validates that:
    /// - Dependencies exist among discovered plugins.
    /// - There are no self-dependencies.
    /// - There are no duplicate dependencies.
    /// - There are no dependency cycles.
    /// </remarks>
    IReadOnlyList<string> DependsOn => Metadata.DependsOn;

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
    /// processed in ascending <see cref="Id"/> order, so invocation order is
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
    /// processed in ascending <see cref="Id"/> order, so invocation order is
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

    /// <summary>
    /// Binds strongly typed plugin options from the plugin configuration section using
    /// the standard options DI infrastructure.
    /// </summary>
    /// <typeparam name="TOptions">The plugin options type.</typeparam>
    /// <param name="services">The host service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <remarks>
    /// <para>
    /// Typically called from <c>ConfigureServices</c>. The section is resolved through
    /// <see cref="Plugins.PluginExtensions.GetPluginConfiguration(AuthKit.Plugins.Abstractions.Contracts.IAuthKitPlugin,IConfiguration)"/>:
    /// <c>Plugins:{Id}</c> wins when it exists, otherwise <c>Plugins:{Name}</c> is used,
    /// matching the scoping of <see cref="AuthKitPluginContext"/>.
    /// </para>
    /// <para>
    /// Registered options become readable through <c>IOptions&lt;TOptions&gt;</c> once
    /// the service provider is built.
    /// </para>
    /// </remarks>
    void BindConfiguration<TOptions>(IServiceCollection services, IConfiguration configuration)
        where TOptions : class =>
        services.Configure<TOptions>(this.GetPluginConfiguration(configuration));

    /// <summary>
    /// Initializes plugin runtime resources before the host is considered started.
    /// </summary>
    /// <param name="cancellationToken">The host startup cancellation token.</param>
    /// <returns>A task that completes when initialization is complete.</returns>
    Task OnStartingAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>
    /// Notifies the plugin after the host has started successfully.
    /// </summary>
    /// <param name="cancellationToken">The host lifecycle cancellation token.</param>
    /// <returns>A task that completes when post-start work is complete.</returns>
    Task OnStartedAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>
    /// Releases plugin runtime resources during a graceful host shutdown.
    /// </summary>
    /// <param name="cancellationToken">The host shutdown cancellation token.</param>
    /// <returns>A task that completes when shutdown preparation is complete.</returns>
    Task OnStoppingAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>
    /// Gets hosted services owned by this plugin.
    /// </summary>
    /// <returns>A non-null collection of services registered in the host DI container.</returns>
    IReadOnlyList<IHostedService> GetHostedServices() => [];
}
