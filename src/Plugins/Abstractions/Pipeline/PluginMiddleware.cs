namespace AuthKit.Plugins.Abstractions.Pipeline;

/// <summary>
/// Declares middleware type contributed by plugin. The plugin declares WHAT;
/// the host owns activation and connection to the ASP.NET Core pipeline.
/// </summary>
/// <param name="MiddlewareType">The middleware type. Must follow the conventional
/// ASP.NET Core pattern or implement <see cref="Contracts.PluginContract.IAuthKitMiddleware"/>
/// or derive from <see cref="Contracts.PluginContract.AuthKitMiddlewareBase"/>.</param>
/// <param name="Position">Where the middleware should be inserted.</param>
/// <param name="Order">Ordering key within single position (ascending).</param>
/// <param name="IsMiddlewareEnabled">When false, the host skips the entry without side effects.</param>
/// <param name="Name">Optional diagnostic name.</param>
/// <param name="Transport">Target transport. The host never guesses transport;
/// <see cref="AuthKitTransport.Http"/> entries run in the HTTP pipeline,
/// <see cref="AuthKitTransport.Grpc"/> entries must be <c>Interceptor</c>
/// subclasses composed into the gRPC interceptor chain. HTTP only middleware
/// is skipped on the gRPC transport with an explicit warning (no auto-bridge).</param>
public sealed record PluginMiddleware(
    Type MiddlewareType,
    PipelinePosition Position,
    int Order = 0,
    bool IsMiddlewareEnabled = true,
    string? Name = null,
    AuthKitTransport Transport = AuthKitTransport.Http);
