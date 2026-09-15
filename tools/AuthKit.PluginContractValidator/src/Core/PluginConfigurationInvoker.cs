using System;
using System.Reflection;
using AuthKit.Plugins.Abstractions.Contracts;
using AuthKit.Plugins.Abstractions.Contracts.Plugins;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using IAuthKitPlugin = AuthKit.Plugins.Abstractions.Contracts.PluginContract.IAuthKitPlugin;

namespace AuthKit.PluginContractValidator.Core;

/// <summary>
/// Selects and invokes the most specific <c>ConfigureServices</c> overload a plugin
/// implements, mirroring the selection performed by the AuthKit host.
/// </summary>
/// <remarks>
/// <para>
/// Plugins may implement any single supported <c>ConfigureServices</c> overload. The
/// invoker picks the most specific one actually overridden by the plugin instead of
/// requiring all plugins to adopt a single signature.
/// </para>
/// <para>
/// The default interface implementations of <c>IAuthKitPlugin.ConfigureServices</c>
/// forward to one another, but the concrete method a plugin implements is only visible
/// on the concrete type, so the overload is resolved through reflection exactly as the
/// host resolver does.
/// </para>
/// </remarks>
internal static class PluginConfigurationInvoker
{
    /// <summary>
    /// Invokes the plugin's <c>ConfigureServices</c>, returning the service collection the
    /// plugin registered into.
    /// </summary>
    /// <param name="plugin">The loaded plugin to configure.</param>
    /// <param name="services">The service collection the plugin should register into.</param>
    /// <param name="configuration">The application configuration used to build the plugin context.</param>
    /// <returns>The service collection containing the plugin's registrations.</returns>
    public static IServiceCollection Configure(
        IAuthKitPlugin plugin,
        IServiceCollection services,
        IConfiguration configuration)
    {
        var pluginType = plugin.GetType();

        if (HasImplementation(pluginType, typeof(IServiceCollection), typeof(AuthKitPluginContext)))
        {
            plugin.ConfigureServices(
                services,
                new AuthKitPluginContext(
                    plugin.Id,
                    plugin.Name,
                    plugin.GetPluginConfiguration(configuration),
                    configuration));
            return services;
        }

        if (HasImplementation(pluginType, typeof(IHostApplicationBuilder), typeof(IConfiguration)))
        {
            var builder = Host.CreateEmptyApplicationBuilder(new HostApplicationBuilderSettings());
            plugin.ConfigureServices(builder, configuration);
            return builder.Services;
        }

        plugin.ConfigureServices(services, configuration);
        return services;
    }

    /// <summary>
    /// Determines whether a plugin provides a concrete implementation of the
    /// <c>ConfigureServices</c> overload identified by the given parameter types.
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