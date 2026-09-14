using Host.Plugins.Loading;
using JasperFx;
using Marten;
using Wolverine;
using Wolverine.FluentValidation;
using Wolverine.Marten;
using AuthKit.Plugins.Integrations;

namespace Host.Configuration.Infrastructure;

/// <summary>
/// Provides extension methods for configuring application infrastructure.
/// </summary>
/// <remarks>
/// <para>
/// Configures Wolverine messaging and Marten document persistence used by the
/// AuthKit host.
/// Wolverine is configured to discover handlers from the Core assembly and
/// dynamically loaded plugin assemblies, while FluentValidation is integrated
/// into message processing.
/// </para>
/// </remarks>
public static class InfrastructureConfiguration
{
    /// <summary>
    /// Configures Wolverine for the application host.
    /// </summary>
    /// <param name="builder">web application builder used to configure Wolverine and logging.</param>
    /// <param name="plugins">
    /// The plugins loaded during application startup whose assemblies may
    /// contain Wolverine message handlers.
    /// </param>
    public static void ConfigureWolverine(
        this WebApplicationBuilder  builder,
        IReadOnlyList<LoadedPlugin> plugins)
    {
        builder.UseWolverine(opts =>
        {
            opts.UseFluentValidation();
            opts.IncludeEventHandlers(plugins);

            if (builder.Configuration.GetValue<bool>("AuthKit:SkipStorageMigrationOnStartup"))
            {
                opts.AutoBuildMessageStorageOnStartup = AutoCreate.None;
                opts.Durability.Mode = DurabilityMode.MediatorOnly;
            }

            opts.Policies.MessageExecutionLogLevel(LogLevel.None);
            opts.Policies.MessageSuccessLogLevel(LogLevel.None);
        });
        builder.Logging.AddFilter("Wolverine", LogLevel.None);
        builder.Logging.AddFilter("Marten", LogLevel.None);
        builder.Logging.AddFilter("Npgsql", LogLevel.None);
    }

    /// <summary>
    /// Configures Marten document store and integrates it with Wolverine.
    /// </summary>
    /// <param name="services">The service collection to add dependencies to.</param>
    /// <param name="configuration">Application configuration containing Marten connection strings.</param>
    public static IServiceCollection ConfigureMarten(
        this IServiceCollection services,
        IConfiguration configuration,
        IReadOnlyList<LoadedPlugin> plugins)
    {
        services.AddMarten(opts =>
            {
                opts.Connection(configuration.GetConnectionString("Marten")!);
                opts.AutoCreateSchemaObjects = AutoCreate.All;

                foreach (var plugin in plugins
                    .OrderBy(plugin => plugin.Plugin.Id, StringComparer.Ordinal)
                    .Select(plugin => plugin.Plugin)
                    .OfType<IMartenPlugin>())
                {
                    plugin.ConfigureMarten(opts);
                }
            })
            .IntegrateWithWolverine()
            .UseLightweightSessions();

        services.AddScoped<IDocumentSession>(sp =>
            sp.GetRequiredService<IDocumentStore>().LightweightSession());

        return services;
    }
}
