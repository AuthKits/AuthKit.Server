using AuthKit.Plugins.Abstractions;

namespace AuthKit.Plugins.Abstractions.Contracts.PluginContract;

public partial interface IAuthKitPlugin
{
    /// <summary>
    /// Gets the middleware registrations contributed by the plugin.
    /// The plugin declares the middleware type and position; the host owns
    /// validation, deterministic ordering (Order → stable PluginId → DeclarationIndex),
    /// activation, and connection to the ASP.NET Core pipeline.
    /// Defaults to empty (no middleware).
    /// </summary>
    IReadOnlyList<PluginMiddleware> Middlewares => [];
}
