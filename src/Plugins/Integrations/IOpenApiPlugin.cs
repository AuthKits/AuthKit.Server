using Swashbuckle.AspNetCore.SwaggerGen;

namespace AuthKit.Plugins.Integrations;

/// <summary>
/// Allows a plugin to contribute configuration to the host OpenAPI generator.
/// </summary>
public interface IOpenApiPlugin
{
    /// <summary>Configures the actual Swagger generator options used by the host.</summary>
    /// <param name="options">The host-owned Swagger generator options.</param>
    void ConfigureOpenApi(SwaggerGenOptions options);
}
