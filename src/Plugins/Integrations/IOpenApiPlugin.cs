using Swashbuckle.AspNetCore.SwaggerGen;

namespace AuthKit.Plugins.Integrations;

/// <summary>
/// Allows a plugin to contribute configuration to the host OpenAPI generator.
/// </summary>
public interface IOpenApiPlugin
{
    /// <summary>
    /// Configures the actual Swagger generator options used by the host.
    /// </summary>
    /// <param name="options">The host-owned Swagger generator options.</param>
    /// <remarks>
    /// <para>
    /// This optional hook is invoked once while the host registers its OpenAPI/Swagger
    /// generator, before the service provider is built. Plugins are processed in
    /// ascending plugin ID order, so invocation order is deterministic.
    /// </para>
    /// <para>
    /// The hook configures <c>SwaggerGenOptions</c> only. It runs before the service
    /// provider is built, so it must not resolve services from the container.
    /// </para>
    /// </remarks>
    void ConfigureOpenApi(SwaggerGenOptions options);
}
