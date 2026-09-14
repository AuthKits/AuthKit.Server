using System.Reflection;
using Core.KeyManagement.Services;
using Core.Options;
using Core.KeyManagement.Entity;
using FluentValidation;
using Host.Plugins.Loading;
using Host.KeyManagement.Repositories;
using Host.ServiceDiscovery;
using Host.TokenKeyBindings.Repositories;

namespace Host.Configuration.Bootstrap;

/// <summary>
/// Provides application service registration and dependency injection configuration.
/// </summary>
/// <remarks>
/// <para>
/// Configures logging, options, HTTP context access, MVC controllers, service
/// discovery, FluentValidation validators, and application-specific options.
/// Controllers contributed by dynamically loaded plugins are registered as MVC
/// application parts during application initialization.
/// </para>
/// <para>
/// Service discovery scans the relevant Host and Core assemblies while excluding
/// infrastructure services that require explicit registration or configuration.
/// </para>
/// </remarks>
public static class ApplicationInitialization
{
    /// <summary>
    /// Configures application services and registers dependencies with the
    /// dependency injection container.
    /// </summary>
    /// <param name="services">The service collection used to register application dependencies.</param>
    /// <param name="configuration">The application configuration used to configure registered servicesand options.</param>
    /// <param name="plugins">
    /// The plugins loaded during application startup. Each plugin assembly is
    /// registered as an MVC application part to expose its controllers.
    /// </param>
    /// <returns>The configured <see cref="IServiceCollection"/> instance.</returns>
    public static IServiceCollection ConfigureApp(
        this IServiceCollection services,
        IConfiguration configuration,
        IReadOnlyList<LoadedPlugin> plugins)
    {
        services.AddLogging(logging =>
        {
            logging.AddConsole();
            logging.AddConfiguration(configuration.GetSection("Logging"));
        });

        services.AddOptions();
        services.AddHttpContextAccessor();

        var mvcBuilder = services.AddControllers();
        foreach (var lp in plugins)
            mvcBuilder.AddApplicationPart(lp.Assembly);

        var discoveryLogger = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Debug);
        }).CreateLogger("ServiceDiscovery");

        services.AddDiscoveredServices(GetRelevantAssemblies(), opts =>
        {
            configuration.GetSection("ServiceDiscovery").Bind(opts);

            opts.ExcludedTypes.Add(typeof(AesKeyEncryptor));
            opts.ExcludedTypes.Add(typeof(RsaKeyGenerator));
            opts.ExcludedTypes.Add(typeof(JwtKeyStore));
            opts.ExcludedTypes.Add(typeof(KeyStoreRepository));
            opts.ExcludedTypes.Add(typeof(KeyBindingRepository));
        }, discoveryLogger)
        .AddFluentValidation();

        services.ConfigureAppOptions(configuration);

        return services;
    }

    #region Private Options FluentValidation

    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers application option classes and binds them to their
        /// corresponding configuration sections.
        /// </summary>
        /// <param name="configuration">The application configuration containing option values.</param>
        private void ConfigureAppOptions(IConfiguration configuration)
        {
            services.Configure<ErrorMetadataOptions>(configuration.GetSection("ErrorMetadata"));
        }

        /// <summary>
        /// Scans the relevant application assemblies and registers all
        /// FluentValidation validators with a scoped lifetime.
        /// </summary>
        private void AddFluentValidation()
        {
            services.Scan(scan => scan
                .FromAssemblies(GetRelevantAssemblies())
                .AddClasses(classes => classes.AssignableTo(typeof(AbstractValidator<>)))
                .AsImplementedInterfaces()
                .WithScopedLifetime());
        }
    }

    #endregion

    #region Private Helpers
    private static Assembly[] GetRelevantAssemblies() =>
        [.. new[]
        {
            typeof(ApplicationInitialization).Assembly,  // Host
            typeof(SigningKey).Assembly                  // Core
        }.Distinct()];
    #endregion
}
