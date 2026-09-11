namespace DevTools.Options;

/// <summary>
/// Swagger UI serving configuration.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="RoutePrefix"/> determines the URL path for the Swagger JSON
/// document and the Swagger UI page. With the default prefix swagger,
/// the document is served at /swagger/v1/swagger.json.
/// </para>
/// <para>
/// The spec version can be pinned with <see cref="SpecVersion"/> when omitted
/// the OpenApi:SpecVersion host configuration is used.
/// </para>
/// </remarks>
public sealed class SwaggerUiOptions
{
    /// <summary>
    /// Gets or sets the route prefix under which the Swagger document and UI
    /// are served.
    /// </summary>
    /// <remarks>
    /// For example, value of <c>swagger</c> yields /swagger/v1/swagger.json</remarks>
    public string RoutePrefix { get; set; } = "swagger";

    /// <summary>
    /// Gets or sets the name of the OpenAPI document served by Swagger UI.
    /// </summary>
    public string DocumentName { get; set; } = "v1";

    /// <summary>
    /// Gets or sets the title shown in the Swagger UI browser tab.
    /// </summary>
    public string DocumentTitle { get; set; } = "AuthKit API";

    /// <summary>
    /// Gets or sets the OpenAPI spec version to pin when serializing the
    /// document.
    /// </summary>
    /// <remarks>Overrides the OpenApi:SpecVersion host setting when provided.</remarks>
    public string? SpecVersion { get; set; }

    /// <summary>
    /// Gets or sets value indicating whether Swagger UI is enabled.
    /// </summary>
    /// <remarks>When null the UI is served only in the development environment.</remarks>
    public bool? Enabled { get; set; }
}