namespace AuthKit.Plugins.Abstractions;

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
public sealed record PluginMiddleware(
    Type MiddlewareType,
    PipelinePosition Position,
    int Order = 0,
    bool IsMiddlewareEnabled = true,
    string? Name = null);
