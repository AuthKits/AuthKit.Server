using Microsoft.Extensions.Configuration;

namespace AuthKit.Plugins.Abstractions.Contracts;

/// <summary>
/// Provides stable host and plugin information during plugin service configuration.
/// </summary>
public sealed record AuthKitPluginContext
{
    /// <summary>
    /// Initializes a new plugin configuration context.
    /// </summary>
    /// <param name="pluginId">The stable ID of the plugin being configured.</param>
    /// <param name="pluginName">The display name of the plugin being configured.</param>
    /// <param name="configuration">The configuration section scoped to the plugin.</param>
    /// <param name="applicationConfiguration">The full application configuration.</param>
    public AuthKitPluginContext(
        string pluginId,
        string pluginName,
        IConfiguration configuration,
        IConfiguration applicationConfiguration)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pluginId);
        ArgumentException.ThrowIfNullOrWhiteSpace(pluginName);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(applicationConfiguration);

        PluginId = pluginId;
        PluginName = pluginName;
        Configuration = configuration;
        ApplicationConfiguration = applicationConfiguration;
    }

    /// <summary>
    /// Gets the stable ID of the plugin being configured.
    /// </summary>
    public string PluginId { get; }

    /// <summary>
    /// Gets the display name of the plugin being configured.
    /// </summary>
    public string PluginName { get; }

    /// <summary>
    /// Gets the configuration section scoped to the plugin ID, or plugin name when no ID section exists.
    /// </summary>
    public IConfiguration Configuration { get; }

    /// <summary>
    /// Gets the full application configuration when host-level settings are required.
    /// </summary>
    public IConfiguration ApplicationConfiguration { get; }
}