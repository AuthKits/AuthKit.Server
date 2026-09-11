using System;
using Microsoft.Extensions.DependencyInjection;
using AuthKit.ManifestGenerator.Core.Generators;
using AuthKit.ManifestGenerator.Core.Providers;

namespace AuthKit.ManifestGenerator.Core;

/// <summary>
/// Provides extension methods for registering manifest generator services
/// with an <see cref="IServiceCollection"/>.
/// </summary>
/// <remarks>
/// <para>
/// The registered services provide the core functionality required to discover
/// plugin types, retrieve plugin metadata, and generate plugin manifests.
/// </para>
/// <para>
/// All manifest generator services are registered with a transient lifetime,
/// allowing each operation to receive fresh service instances from the
/// dependency injection container.
/// </para>
/// </remarks>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the services required for plugin manifest generation.
    /// </summary>
    /// <param name="services">
    /// The <see cref="IServiceCollection"/> to which the manifest generator
    /// services are registered.
    /// </param>
    /// <returns>
    /// The same <see cref="IServiceCollection"/> instance so that additional
    /// service registrations can be chained.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="services"/> is <see langword="null"/>.
    /// </exception>
    public static IServiceCollection AddManifestGeneratorServices(
        this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddTransient<IPluginTypeResolver, PluginTypeResolver>();
        services.AddTransient<IPluginMetadataProvider, PluginMetadataProvider>();
        services.AddTransient<IManifestGenerator, AuthKit.ManifestGenerator.Core.Generators.ManifestGenerator>();

        return services;
    }
}