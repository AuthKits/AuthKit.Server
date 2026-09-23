using AuthKit.Plugins.Abstractions.Contracts.PluginContract;
using AuthKit.Plugins.Abstractions.Pipeline;
using Grpc.Core.Interceptors;
using Host.Plugins.Loading;
using Microsoft.Extensions.Logging;

namespace Host.Plugins.Configuration;

/// <summary>
/// Composes plugin contributed gRPC interceptors into the host interceptor chain.
/// </summary>
/// <remarks>
/// <para>
/// Only <see cref="PluginMiddleware"/> entries declared with
/// <see cref="AuthKitTransport.Grpc"/> are composed here. The host never guesses
/// transport by reflection: entries targeting <see cref="AuthKitTransport.Http"/>
/// are skipped on the gRPC transport with an explicit warning (no automatic
/// <c>HttpContext</c> -> <c>ServerCallContext</c> bridge).
/// </para>
/// <para>
/// <see cref="PipelinePosition"/> values are AuthKit semantic positions, not native
/// ASP.NET Core gRPC insertion points: the single interceptor chain is ordered
/// BeforeRouting -> … -> BeforeEndpoints, so each interceptor wraps the downstream
/// call in that order; <c>AfterEndpointExecution</c> interceptors perform
/// post-processing after the downstream call (the natural interceptor tail shape).
/// Within one position entries are ordered deterministically by
/// Order -> stable plugin Id -> declaration index.
/// </para>
/// <para>
/// Interceptors are registered scoped and resolved per call from the request
/// service provider (single scope, mirroring). Streaming (unary, client,
/// server and duplex streaming) is supported through the base
/// <see cref="Interceptor"/> overloads no custom streaming pipeline exists.
/// </para>
/// </remarks>
internal static class PluginGrpcConfiguration
{
    /// <summary>
    /// Registers every enabled gRPC transport interceptor and composes the
    /// interceptor chain in semantic position order.
    /// </summary>
    public static IServiceCollection AddPluginGrpcInterceptors(
        this IServiceCollection services,
        IReadOnlyList<LoadedPlugin> plugins,
        ILogger logger)
    {
        WarnForHttpOnlyMiddleware(plugins, logger);

        var ordered = OrderGrpcInterceptors(plugins);

        foreach (var (_, entry, _) in ordered)
        {
            if (entry.MiddlewareType is null)
                throw new InvalidOperationException("Plugin declares PluginMiddleware with a null MiddlewareType.");

            if (!typeof(Interceptor).IsAssignableFrom(entry.MiddlewareType))
                throw new InvalidOperationException(
                    $"Plugin middleware '{entry.MiddlewareType.FullName ?? entry.MiddlewareType.Name}' targets the gRPC transport " +
                    $"but is not an Interceptor subclass.");

            services.AddScoped(entry.MiddlewareType);
        }

        services.Configure<global::Grpc.AspNetCore.Server.GrpcServiceOptions>(options =>
        {
            foreach (var (_, entry, _) in ordered)
                options.Interceptors.Add(entry.MiddlewareType!);
        });

        return services;
    }

    /// <summary>
    /// Orders enabled gRPC transport entries by semantic position, then
    /// Order -> stable plugin Id -> declaration index.
    /// </summary>
    internal static IReadOnlyList<(string PluginId, PluginMiddleware Entry, int Index)> OrderGrpcInterceptors(
        IReadOnlyList<LoadedPlugin> plugins) =>
        plugins
            .SelectMany(lp => (lp.Plugin.Middlewares ?? [])
                .Select((mw, index) => (PluginId: lp.Plugin.Id, Entry: mw, Index: index)))
            .Where(x => x.Entry.IsMiddlewareEnabled && x.Entry.Transport == AuthKitTransport.Grpc)
            .OrderBy(x => x.Entry.Position)
            .ThenBy(x => x.Entry.Order)
            .ThenBy(x => x.PluginId, StringComparer.Ordinal)
            .ThenBy(x => x.Index)
            .ToList();

    private static void WarnForHttpOnlyMiddleware(IReadOnlyList<LoadedPlugin> plugins, ILogger logger)
    {
        foreach (var loadedPlugin in plugins)
        {
            foreach (var entry in loadedPlugin.Plugin.Middlewares ?? [])
            {
                if (!entry.IsMiddlewareEnabled || entry.Transport != AuthKitTransport.Http)
                    continue;

                logger.LogWarning(
                    "Plugin '{PluginId}' middleware '{MiddlewareName}' targets the HTTP transport " +
                    "and is skipped on the gRPC transport. Declare Transport = Grpc with an " +
                    "Interceptor MiddlewareType to run on gRPC; no automatic HttpContext bridge is provided.",
                    loadedPlugin.Plugin.Id,
                    entry.Name ?? entry.MiddlewareType?.FullName ?? entry.MiddlewareType?.Name ?? "<unknown>");
            }
        }
    }
}
