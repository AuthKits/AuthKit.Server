using AuthKit.Plugins.Abstractions;
using Host.Plugins;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi;

namespace Host.Configuration;

/// <summary>
/// Provides extension methods for configuring AuthKit RESTful services and
/// OpenAPI/Swagger documentation.
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item>Registers API explorer services required for endpoint metadata discovery.</item>
/// <item>Registers Swagger generation and configures the application's OpenAPI document.</item>
/// <item>Registers the built-in JWT bearer security scheme.</item>
/// <item>Registers additional security schemes contributed by loaded AuthKit plugins.</item>
/// <item>Converts AuthKit security scheme descriptors into OpenAPI security scheme definitions.</item>
/// </list>
/// </remarks>
public static class RestfulConfiguration
{
    /// <summary>
    /// Registers RESTful API services and configures Swagger/OpenAPI generation.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to configure.</param>
    /// <param name="plugins">
    /// The plugins loaded by the Host. Each plugin may contribute one or more
    /// OpenAPI security scheme definitions.
    /// </param>
    /// <param name="configuration">The host application configuration.</param>
    /// <param name="logger">The logger used to report skipped security schemes.</param>
    /// <returns>The configured <see cref="IServiceCollection"/> instance.</returns>
    /// <remarks>
    /// <para>The method registers API explorer support and creates Swagger document named <c>v1</c>.</para>
    /// <para>
    /// A built-in HTTP Bearer authentication scheme is registered for JWT
    /// authentication. Every security scheme exposed by a loaded plugin is also
    /// added to the generated OpenAPI document together with a corresponding
    /// security requirement.
    /// </para>
    /// <para>
    /// Security schemes without a semantically correct OpenAPI representation are
    /// explicitly rejected by <see cref="AuthKitOpenApiSecuritySchemeMapper"/> and
    /// skipped (with a logged warning) rather than being silently mapped to a
    /// generic scheme.
    /// </para>
    /// <exception cref="InvalidOperationException">
    /// The <c>OpenApi:SpecVersion</c> configuration value is not supported by
    /// this host. Supported values are <c>3.0</c> (default) and <c>3.1</c>.
    /// </exception>
    /// </remarks>
    public static IServiceCollection AddRestfulServices(
        this IServiceCollection services,
        IReadOnlyList<LoadedPlugin> plugins,
        IConfiguration configuration,
        ILogger logger)
    {
        var specVersion = ResolveOpenApiSpecVersion(configuration);

        services.AddEndpointsApiExplorer();

        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "API", Version = "v1" });
            c.EnableAnnotations();

            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "Bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Insert JWT token in the format: Bearer {token}"
            });

            c.AddSecurityRequirement(document => new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecuritySchemeReference("Bearer", document),
                    new List<string>()
                }
            });

            foreach (var lp in plugins)
            {
                foreach (var (name, descriptor) in lp.Plugin.GetSecuritySchemes())
                {
                    OpenApiSecurityScheme openApiScheme;
                    try
                    {
                        openApiScheme = AuthKitOpenApiSecuritySchemeMapper.Map(descriptor, specVersion);
                    }
                    catch (NotSupportedException ex)
                    {
                        logger.LogWarning(
                            "Skipping security scheme '{Scheme}' contributed by plugin '{Plugin}': {Reason}",
                            name, lp.Plugin.Name, ex.Message);
                        continue;
                    }
                    catch (ArgumentOutOfRangeException ex)
                    {
                        logger.LogWarning(
                            "Skipping security scheme '{Scheme}' contributed by plugin '{Plugin}' because it declares an unknown contract value: {Reason}",
                            name, lp.Plugin.Name, ex.Message);
                        continue;
                    }

                    c.AddSecurityDefinition(name, openApiScheme);
                    c.AddSecurityRequirement(document => new OpenApiSecurityRequirement
                    {
                        {
                            new OpenApiSecuritySchemeReference(name, document),
                            new List<string>()
                        }
                    });
                }
            }
        });

        return services;
    }

    internal static OpenApiSpecVersion ResolveOpenApiSpecVersion(IConfiguration configuration)
    {
        var configured = configuration["OpenApi:SpecVersion"]?.Trim() ?? "3.0";

        if (configured.Equals("3.0", StringComparison.OrdinalIgnoreCase)
            || configured.StartsWith("3.0.", StringComparison.OrdinalIgnoreCase))
        {
            return OpenApiSpecVersion.OpenApi3_0;
        }

        if (configured.Equals("3.1", StringComparison.OrdinalIgnoreCase)
            || configured.StartsWith("3.1.", StringComparison.OrdinalIgnoreCase))
        {
            return OpenApiSpecVersion.OpenApi3_1;
        }

        throw new InvalidOperationException(
            $"The configured OpenAPI spec version '{configured}' (OpenApi:SpecVersion) is not supported by this host. " +
            $"Set 'OpenApi:SpecVersion' to '3.0' (default) or '3.1'.");
    }
}