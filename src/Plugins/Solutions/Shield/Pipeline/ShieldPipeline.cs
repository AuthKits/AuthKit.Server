using AuthKit.Plugins.Abstractions.Pipeline;
using Shield.Grpc;
using Shield.Middleware;

namespace Shield.Pipeline;

/// <summary>
/// The pipeline footprint of Shield: HTTP kill-switch first in line plus the
/// gRPC interceptor. The host owns activation and ordering; this only declares
/// <i>what</i> runs <i>where</i>.
/// </summary>
public static class ShieldPipeline
{
    /// <summary>
    /// Declarative middleware entries contributed by the plugin.
    /// </summary>
    public static IReadOnlyList<PluginMiddleware> Declarations =>
    [
        new(typeof(ShieldMiddleware), PipelinePosition.BeforeRouting,
            Order: -1000, Name: "shield-kill-switch"),
        new(typeof(ShieldInterceptor), PipelinePosition.BeforeRouting,
            Order: -1000, Name: "shield-grpc", Transport: AuthKitTransport.Grpc),
    ];
}
