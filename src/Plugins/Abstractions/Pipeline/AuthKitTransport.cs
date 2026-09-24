namespace AuthKit.Plugins.Abstractions.Pipeline;

/// <summary>
/// Selects the transport a <see cref="PluginMiddleware"/> entry targets.
/// The host never guesses transport by reflection; it follows this declaration.
/// </summary>
public enum AuthKitTransport
{
    /// <summary>ASP.NET Core HTTP middleware pipeline (default).</summary>
    Http = 0,

    /// <summary>gRPC interceptor chain (concrete <c>Interceptor</c> subclass).</summary>
    Grpc = 1
}
