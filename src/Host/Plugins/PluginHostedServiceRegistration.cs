namespace Host.Plugins;

/// <summary>
/// Registers plugin lifecycle orchestration and plugin owned hosted services.
/// </summary>
/// <remarks>
/// <para>
/// Registers the singleton <see cref="PluginLifecycleHostedService"/> that bridges
/// plugin lifecycle hooks to the standard host lifecycle, followed by every hosted
/// service returned by the loaded plugins.
/// </para>
/// <para>
/// The registration is idempotent by contract: calling Register a second time
/// on the same service collection is rejected so that plugin hosted services are
/// registered exactly once before the host startup.
/// </para>
/// </remarks>
internal static class PluginHostedServiceRegistration
{
    /// <summary>
    /// Registers each plugin hosted service exactly once before the host startup.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Each distinct hosted service instance is deduplicated using reference
    /// equality, and is registered as an <see cref="IHostedService"/> singleton so
    /// the host starts and stops it once.
    /// </para>
    /// <para>
    /// The lifecycle orchestration host is always registered, even when no plugin
    /// returns any hosted services, so lifecycle hooks are invoked consistently.
    /// </para>
    /// </remarks>
    /// <param name="services">The service collection the plugin services are added to.</param>
    /// <param name="plugins">The plugins loaded during application startup.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the registration was already performed, when a plugin returns
    /// <c>null</c> from <c>GetHostedServices</c>, or when a plugin returns the same
    /// hosted service instance more than once.
    /// </exception>
    public static void Register(
        IServiceCollection services,
        IReadOnlyList<LoadedPlugin> plugins)
    {
        if (services.Any(descriptor =>
                descriptor.ImplementationType == typeof(PluginLifecycleHostedService)))
            throw new InvalidOperationException("Plugin hosted services have already been registered.");

        var hostedServices = new HashSet<IHostedService>(ReferenceEqualityComparer.Instance);

        services.AddSingleton<IHostedService, PluginLifecycleHostedService>();

        foreach (var loadedPlugin in plugins.OrderBy(plugin => plugin.Plugin.Id, StringComparer.Ordinal))
        {
            var pluginServices = loadedPlugin.Plugin.GetHostedServices()
                ?? throw new InvalidOperationException(
                    $"Plugin '{loadedPlugin.Plugin.Id}' returned null from GetHostedServices().");

            foreach (var hostedService in pluginServices)
            {
                ArgumentNullException.ThrowIfNull(hostedService);
                if (!hostedServices.Add(hostedService))
                    throw new InvalidOperationException(
                        $"Plugin '{loadedPlugin.Plugin.Id}' returned the same hosted service instance more than once.");

                services.AddSingleton(typeof(IHostedService), hostedService);
            }
        }
    }
}