using System.Reflection;
using AuthKit.Plugins.Abstractions.Contracts;
using AuthKit.Plugins.Abstractions.Contracts.Plugins;

namespace Host.Plugins;

/// <summary>
/// Selects and invokes one compatible plugin configuration overload.
/// </summary>
/// <remarks>
/// <para>
/// Plugins may implement any single supported <c>ConfigureServices</c> overload.
/// The invoker picks the most specific overload implemented by the plugin instead
/// of requiring all plugins to adopt a single signature.
/// </para>
/// <para>
/// The default interface implementations of <c>IAuthKitPlugin.ConfigureServices</c>
/// forward to one another, so only the most specific overload actually overridden
/// by the plugin is invoked.
/// </para>
/// </remarks>
internal static class PluginConfigurationInvoker
{
    /// <summary>
    /// Invokes the most specific configuration overload implemented by a plugin.
    /// </summary>
    /// <param name="plugin">The plugin being configured.</param>
    /// <param name="builder">The host builder used by AuthKit.</param>
    /// <param name="configuration">The application configuration.</param>
    public static void Configure(
        IAuthKitPlugin plugin,
        IHostApplicationBuilder builder,
        IConfiguration configuration)
    {
        var pluginType = plugin.GetType();

        if (HasImplementation(pluginType, typeof(IServiceCollection), typeof(AuthKitPluginContext)))
        {
            plugin.ConfigureServices(
                builder.Services,
                new AuthKitPluginContext(
                    plugin.Id,
                    plugin.Name,
                    plugin.GetPluginConfiguration(configuration),
                    configuration));
            return;
        }

        if (HasImplementation(pluginType, typeof(IHostApplicationBuilder), typeof(IConfiguration)))
        {
            plugin.ConfigureServices(builder, configuration);
            return;
        }

        plugin.ConfigureServices(builder.Services, configuration);
    }

    /// <summary>
    /// Determines whether a plugin provides a concrete implementation of the
    /// ConfigureServices overload identified by the given parameter types.
    /// </summary>
    /// <param name="pluginType">The plugin type to inspect.</param>
    /// <param name="parameterTypes">The parameter types that identify the overload.</param>
    /// <returns>
    /// true when the plugin overrides the overload; otherwise, false.
    /// </returns>
    private static bool HasImplementation(Type pluginType, params Type[] parameterTypes)
    {
        var method = pluginType.GetMethod(
            nameof(IAuthKitPlugin.ConfigureServices),
            BindingFlags.Instance | BindingFlags.Public,
            binder: null,
            types: parameterTypes,
            modifiers: null);

        return method is not null && method.DeclaringType != typeof(IAuthKitPlugin);
    }
}