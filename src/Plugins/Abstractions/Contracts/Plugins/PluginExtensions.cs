using AuthKit.Plugins.Abstractions.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AuthKit.Plugins.Abstractions.Contracts.Plugins;

/// <summary>
/// Extension methods for plugin capabilities and metadata.
/// </summary>
public static class PluginExtensions
{
    /// <param name="plugin">The plugin whose configuration section is used.</param>
    extension(IAuthKitPlugin plugin)
    {
        /// <summary>
        /// Binds strongly typed options from the plugin's configuration section.
        /// </summary>
        /// <typeparam name="TOptions">The plugin options type.</typeparam>
        /// <param name="services">The host service collection.</param>
        /// <param name="configuration">The application configuration.</param>
        /// <remarks>
        /// <para>
        /// The section is resolved through <see cref="GetPluginConfiguration"/>: the
        /// <c>Plugins:{Id}</c> section wins when it exists, otherwise the
        /// <c>Plugins:{Name}</c> section is used. This matches the scoping used by
        /// <see cref="AuthKitPluginContext"/>.
        /// </para>
        /// </remarks>
        public void BindConfiguration<TOptions>(IServiceCollection services,
            IConfiguration configuration)
            where TOptions : class
        {
            ArgumentNullException.ThrowIfNull(plugin);
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(configuration);

            services.Configure<TOptions>(plugin.GetPluginConfiguration(configuration));
        }

        /// <summary>
        /// Resolves the configuration section scoped to a plugin.
        /// </summary>
        /// <param name="configuration">The application configuration.</param>
        /// <returns>
        /// The <c>Plugins:{Id}</c> section when it has children; otherwise, the
        /// <c>Plugins:{Name}</c> section.
        /// </returns>
        /// <remarks>
        /// <para>
        /// Plugin sections live under the Plugins root. The stable plugin ID is
        /// checked first an empty or missing ID section falls back to the plugin name.
        /// This is the single resolution rule used by the host
        /// (<see cref="AuthKitPluginContext"/>) and by options binding.
        /// </para>
        /// </remarks>
        public IConfiguration GetPluginConfiguration(IConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(plugin);
            ArgumentNullException.ThrowIfNull(configuration);

            var plugins = configuration.GetSection("Plugins");
            var byId = plugins.GetSection(plugin.Id);

            return byId.GetChildren().Any()
                ? byId
                : plugins.GetSection(plugin.Name);
        }

        /// <summary>
        /// Checks if the plugin supports the specified capability.
        /// </summary>
        /// <param name="capability">The ability to check.</param>
        /// <returns>true if the plugin supports the capability; otherwise, <c>false</c>.</returns>
        /// <remarks>The comparison is case-insensitive.</remarks>
        public bool Supports(string capability) =>
            plugin == null
                ? throw new ArgumentNullException(nameof(plugin))
                : plugin.Capabilities.Contains(capability, StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Checks if the plugin has the specified dependency.
        /// </summary>
        /// <param name="dependencyId">The dependency ID to check.</param>
        /// <returns><c>true</c> if the plugin depends on the specified dependency; otherwise, <c>false</c>.</returns>
        public bool HasDependency(string dependencyId) =>
            plugin == null
                ? throw new ArgumentNullException(nameof(plugin))
                : plugin.DependsOn.Contains(dependencyId, StringComparer.OrdinalIgnoreCase);
    }

    /// <param name="manifest">The plugin manifest.</param>
    extension(PluginManifest manifest)
    {
        /// <summary>
        /// Checks if the plugin manifest supports the specified capability.
        /// </summary>
        /// <param name="capability">The ability to check.</param>
        /// <returns>true if the manifest supports the capability; otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// The comparison is case-insensitive.
        /// </remarks>
        public bool Supports(string capability) =>
            manifest == null
                ? throw new ArgumentNullException(nameof(manifest))
                : manifest.Capabilities.Contains(capability, StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Checks if the plugin manifest has the specified dependency.
        /// </summary>
        /// <param name="dependencyId">The dependency ID to check.</param>
        /// <returns><c>true</c> if the manifest declares the specified dependency; otherwise, <c>false</c>.</returns>
        public bool HasDependency(string dependencyId) =>
            manifest == null
                ? throw new ArgumentNullException(nameof(manifest))
                : manifest.DependsOn.Contains(dependencyId, StringComparer.OrdinalIgnoreCase);
    }
}