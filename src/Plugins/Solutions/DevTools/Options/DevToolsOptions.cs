namespace DevTools.Options;

/// <summary>
/// Configuration root for the DevTools plugin.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="GrpcUiPathBase"/> and <see cref="Swagger"/> sections
/// control which URL prefixes the gRPC UI and the Swagger UI are served on.
/// </para>
/// <para>
/// The <see cref="GrpcTarget"/> URL and the gRPC port can be overridden
/// through the GRPC_UI_TARGET and DEV_CERT_PORT_GRPC
/// environment variables.
/// </para>
/// </remarks>
public sealed class DevToolsOptions
{
    /// <summary>
    /// Gets or sets the URL prefix under which the gRPC UI is served.
    /// </summary>
    public string GrpcUiPathBase { get; set; } = "/grpc-ui";

    /// <summary>
    /// Gets or sets the URL of the gRPC endpoint invoked when executing a
    /// request.
    /// </summary>
    /// <remarks>
    /// Defaults to the host gRPC port unless <c>DEV_CERT_PORT_GRPC</c> is set.
    /// </remarks>
    public string? GrpcTarget { get; set; }

    /// <summary>
    /// Gets or sets the URL prefix under which a landing page with links to
    /// both tools is served.
    /// </summary>
    public string PathBase { get; set; } = "/devtools";

    /// <summary>
    /// Gets or sets the Swagger UI serving settings.
    /// </summary>
    public SwaggerUiOptions Swagger { get; set; } = new();

    /// <summary>
    /// Resolves the effective gRPC target, honoring the environment variable
    /// overrides.
    /// </summary>
    /// <returns>
    /// The resolved target URL, for example <c>https://localhost:5001</c>.
    /// </returns>
    public string ResolveGrpcTarget() =>
        GrpcTarget ??
        (Environment.GetEnvironmentVariable("GRPC_UI_TARGET")
            ?? $"https://localhost:{GrpcPortFromEnvironment()}");

    private static int GrpcPortFromEnvironment() =>
        int.TryParse(Environment.GetEnvironmentVariable("DEV_CERT_PORT_GRPC"), out var port)
            ? port
            : 5001;
}
