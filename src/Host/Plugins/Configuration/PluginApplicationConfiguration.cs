using System.Reflection;
using AuthKit.Plugins.Abstractions.Contracts;
using AuthKit.Plugins.Abstractions.Contracts.PluginContract;
using AuthKit.Plugins.Abstractions.Pipeline;
using Host.Plugins.Loading;
using IAuthKitPlugin = AuthKit.Plugins.Abstractions.Contracts.PluginContract.IAuthKitPlugin;

namespace Host.Plugins.Configuration;

/// <summary>
/// Applies plugin endpoint and application pipeline hooks deterministically.
/// </summary>
/// <remarks>
/// <para>
/// Plugins are invoked in stable order regardless of the order they were
/// discovered: first by <see cref="PluginPipelinePosition"/> and then by
/// plugin identifier using an ordinal comparison.
/// </para>
/// <para>
/// Plugins that do not implement given hook are skipped. The newer
/// <c>ConfigureApplication</c> and <c>ConfigurePipeline</c> hooks take
/// precedence over the legacy <c>MiddlewareType</c> entry point, which is
/// applied only as compatibility fallback.
/// </para>
/// </remarks>
internal static class PluginApplicationConfiguration
{
    /// <summary>
    /// Invokes the <see cref="IAuthKitPlugin.ConfigureApplication(IApplicationBuilder)"/>
    /// hook of every plugin that implements it, after the application pipeline
    /// has been fully assembled.
    /// </summary>
    /// <param name="application">The <see cref="IApplicationBuilder"/> exposing the
    /// application pipeline.</param>
    /// <param name="plugins">The plugins loaded during application startup.</param>
    public static void ConfigureApplications(IApplicationBuilder application, IReadOnlyList<LoadedPlugin> plugins)
    {
        foreach (var loadedPlugin in Ordered(plugins))
        {
            var plugin = loadedPlugin.Plugin;
            if (HasImplementation(plugin, nameof(IAuthKitPlugin.ConfigureApplication), typeof(IApplicationBuilder)))
                plugin.ConfigureApplication(application);
        }
    }

    /// <summary>
    /// Invokes the <see cref="IAuthKitPlugin.ConfigurePipeline(IApplicationBuilder, PluginPipelinePosition)"/>
    /// hook of every plugin that implements it for one explicit pipeline position.
    /// </summary>
    /// <remarks>
    /// Only plugins whose <c>PipelinePosition</c> matches the requested position are
    /// invoked. All plugins are validated first so that a declarative unsupported
    /// position fails before any hook runs.
    /// </remarks>
    /// <param name="application">The <see cref="IApplicationBuilder"/> exposing the
    /// application pipeline.</param>
    /// <param name="plugins">The plugins loaded during application startup.</param>
    /// <param name="position">The pipeline position to run hooks for.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="position"/> is not defined
    /// <see cref="PluginPipelinePosition"/> value.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when plugin that implements the pipeline hook declares a
    /// <c>PipelinePosition</c> that is not defined enum value.
    /// </exception>
    public static void ConfigurePipeline(
        IApplicationBuilder application,
        IReadOnlyList<LoadedPlugin> plugins,
        PluginPipelinePosition position)
    {
        if (!Enum.IsDefined(position))
            throw new ArgumentOutOfRangeException(nameof(position), position, "Unsupported plugin pipeline position.");

        ValidatePluginPositions(plugins);

        foreach (var loadedPlugin in Ordered(plugins))
        {
            var plugin = loadedPlugin.Plugin;
            if (!HasImplementation(plugin, nameof(IAuthKitPlugin.ConfigurePipeline),
                    typeof(IApplicationBuilder), typeof(PluginPipelinePosition))
                || plugin.PipelinePosition != position)
                continue;

            plugin.ConfigurePipeline(application, position);
        }
    }

    /// <summary>
    /// Maps the <see cref="IAuthKitPlugin.MapEndpoints(IEndpointRouteBuilder)"/> hook
    /// of every plugin that implements it onto the application's route builder.
    /// </summary>
    /// <param name="endpoints">The <see cref="IEndpointRouteBuilder"/> used to map
    /// plugin endpoints.</param>
    /// <param name="plugins">The plugins loaded during application startup.</param>
    public static void MapEndpoints(IEndpointRouteBuilder endpoints, IReadOnlyList<LoadedPlugin> plugins)
    {
        foreach (var loadedPlugin in Ordered(plugins))
        {
            var plugin = loadedPlugin.Plugin;
            if (HasImplementation(plugin, nameof(IAuthKitPlugin.MapEndpoints), typeof(IEndpointRouteBuilder)))
                plugin.MapEndpoints(endpoints);
        }
    }

    /// <summary>
    /// Applies the legacy <see cref="IAuthKitPlugin.MiddlewareType"/> for plugins that
    /// do not opt into the newer application or pipeline hooks.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Middleware is applied only when the plugin declares it and does not implement
    /// either <see cref="IAuthKitPlugin.ConfigureApplication(IApplicationBuilder)"/>
    /// or <see cref="IAuthKitPlugin.ConfigurePipeline(IApplicationBuilder, PluginPipelinePosition)"/>.
    /// </para>
    /// <para>
    /// This preserves the original middleware-based integration for existing plugins
    /// while routing new plugins through the deterministic hook model.
    /// </para>
    /// </remarks>
    /// <param name="application">The <see cref="WebApplication"/> configured by the host.</param>
    /// <param name="plugins">The plugins loaded during application startup.</param>
    public static void ConfigureLegacyMiddleware(WebApplication application, IReadOnlyList<LoadedPlugin> plugins)
    {
        foreach (var loadedPlugin in Ordered(plugins))
        {
            var plugin = loadedPlugin.Plugin;
            if (plugin.MiddlewareType is null
                || HasImplementation(plugin, nameof(IAuthKitPlugin.ConfigureApplication), typeof(IApplicationBuilder))
                || HasImplementation(plugin, nameof(IAuthKitPlugin.ConfigurePipeline),
                    typeof(IApplicationBuilder), typeof(PluginPipelinePosition)))
                continue;

            application.UseMiddleware(plugin.MiddlewareType);
        }
    }

    /// <summary>
    /// Inserts declarative <see cref="PluginMiddleware"/> entries for one
    /// <see cref="PipelinePosition"/> in deterministic order
    /// (Order -> stable plugin Id -> declaration index).
    /// Disabled entries are skipped without side effects.
    /// <see cref="IAuthKitMiddleware"/> and <see cref="AuthKitMiddlewareBase"/>
    /// implementations are resolved from the request
    /// service provider (single request scope); other types use
    /// <c>UseMiddleware</c> activation.
    /// </summary>
    public static void ConfigurePluginMiddlewares(
        IApplicationBuilder application,
        IReadOnlyList<LoadedPlugin> plugins,
        PipelinePosition position)
    {
        if (!Enum.IsDefined(position))
            throw new ArgumentOutOfRangeException(nameof(position), position, "Unsupported pipeline position.");

        var ordered = plugins
            .SelectMany(lp => (lp.Plugin.Middlewares ?? [])
                .Select((mw, index) => (Plugin: lp.Plugin, Entry: mw, Index: index)))
            .Where(x => x.Entry.Position == position && x.Entry.IsMiddlewareEnabled
                && x.Entry.Transport == AuthKitTransport.Http)
            .OrderBy(x => x.Entry.Order)
            .ThenBy(x => x.Plugin.Id, StringComparer.Ordinal)
            .ThenBy(x => x.Index)
            .ToList();

        foreach (var (plugin, entry, _) in ordered)
        {
            if (entry.MiddlewareType is null)
                throw new InvalidOperationException($"Plugin '{plugin.Id}' declares middleware with null type.");

            try
            {
                RegisterPluginMiddleware(application, entry.MiddlewareType);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Plugin '{plugin.Id}' failed to register middleware '{entry.MiddlewareType?.Name ?? entry.Name}'.", ex);
            }
        }
    }

    private static void RegisterPluginMiddleware(IApplicationBuilder application, Type middlewareType)
    {
        if (typeof(IAuthKitMiddleware).IsAssignableFrom(middlewareType))
        {
            application.Use(async (context, next) =>
            {
                var middleware = ResolvePluginMiddleware<IAuthKitMiddleware>(context, middlewareType);
                await middleware.InvokeAsync(context, next);
            });

            return;
        }

        if (typeof(AuthKitMiddlewareBase).IsAssignableFrom(middlewareType))
        {
            application.Use(async (context, next) =>
            {
                var middleware = ResolvePluginMiddleware<AuthKitMiddlewareBase>(context, middlewareType);
                await middleware.InvokeAsync(context, next);
            });

            return;
        }

        application.UseMiddleware(middlewareType);
    }

    private static TMiddleware ResolvePluginMiddleware<TMiddleware>(HttpContext context, Type middlewareType)
        where TMiddleware : class =>
        context.RequestServices.GetService(middlewareType) as TMiddleware
        ?? ActivatorUtilities.CreateInstance(context.RequestServices, middlewareType) as TMiddleware
        ?? throw new InvalidOperationException(
            $"Middleware type '{middlewareType.FullName}' must be assignable to '{typeof(TMiddleware).FullName}'.");

    /// <summary>
    /// Orders plugins by pipeline position and then by plugin identifier.
    /// </summary>
    private static IEnumerable<LoadedPlugin> Ordered(IReadOnlyList<LoadedPlugin> plugins) =>
        plugins.OrderBy(plugin => plugin.Plugin.PipelinePosition)
            .ThenBy(plugin => plugin.Plugin.Id, StringComparer.Ordinal);

    /// <summary>
    /// Ensures every plugin implementing the pipeline hook declares a valid
    /// <see cref="PluginPipelinePosition"/> before any hook is invoked.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when a plugin implements <c>ConfigurePipeline</c> but declares an
    /// unsupported <c>PipelinePosition</c>.
    /// </exception>
    private static void ValidatePluginPositions(IReadOnlyList<LoadedPlugin> plugins)
    {
        foreach (var loadedPlugin in plugins)
        {
            var plugin = loadedPlugin.Plugin;
            if (HasImplementation(plugin, nameof(IAuthKitPlugin.ConfigurePipeline),
                    typeof(IApplicationBuilder), typeof(PluginPipelinePosition))
                && !Enum.IsDefined(plugin.PipelinePosition))
            {
                throw new InvalidOperationException(
                    $"Plugin '{plugin.Id}' declares unsupported pipeline position '{plugin.PipelinePosition}'.");
            }
        }
    }

    /// <summary>
    /// Determines whether plugin provides concrete implementation of the given
    /// hook rather than inheriting the interface's default implementation.
    /// </summary>
    /// <param name="plugin">The plugin to inspect.</param>
    /// <param name="methodName">The name of the interface method to look up.</param>
    /// <param name="parameterTypes">The parameter types that identify the overload.</param>
    /// <returns>
    /// <c>true</c> when the plugin overrides the hook otherwise, <c>false</c>.
    /// </returns>
    private static bool HasImplementation(IAuthKitPlugin plugin, string methodName, params Type[] parameterTypes)
    {
        var method = plugin.GetType().GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.Public,
            binder: null,
            types: parameterTypes,
            modifiers: null);

        return method is not null && method.DeclaringType != typeof(IAuthKitPlugin);
    }
}
