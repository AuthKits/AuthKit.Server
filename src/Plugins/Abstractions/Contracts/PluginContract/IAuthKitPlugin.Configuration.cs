using AuthKit.Plugins.Abstractions.Contracts.Plugins;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AuthKit.Plugins.Abstractions.Contracts.PluginContract;

public partial interface IAuthKitPlugin
{
    /// <summary>
    /// Registers the plugin's services in the host dependency injection container.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> used to register plugin services.</param>
    /// <param name="context">Stable plugin context including the plugin-scoped configuration section.</param>
    /// <remarks>
    /// <para>
    /// This method is called while the host application is being configured,
    /// before the application is built.
    /// </para>
    /// <para>
    /// Plugins should register all services required by their functionality
    /// through this method rather than creating their own dependency injection
    /// container.
    /// </para>
    /// </remarks>
    void ConfigureServices(IServiceCollection services, AuthKitPluginContext context) =>
        throw new NotSupportedException(
            $"Plugin '{GetType().Name}' must implement ConfigureServices(IServiceCollection, AuthKitPluginContext).");

    /// <summary>
    /// Configures plugin services using the host application builder.
    /// </summary>
    /// <param name="builder">The host application builder used by AuthKit.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <remarks>
    /// This overload is optional. Its default implementation delegates to the
    /// service collection overload with a context built from the plugin
    /// identity and the scoped configuration section.
    /// </remarks>
    void ConfigureServices(
        IHostApplicationBuilder builder,
        IConfiguration configuration) =>
        ConfigureServices(
            builder.Services,
            new AuthKitPluginContext(
                Id,
                Name,
                this.GetPluginConfiguration(configuration),
                configuration));

    /// <summary>
    /// Binds strongly typed plugin options from the plugin configuration section using
    /// the standard options DI infrastructure.
    /// </summary>
    /// <typeparam name="TOptions">The plugin options type.</typeparam>
    /// <param name="services">The host service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <remarks>
    /// <para>
    /// Typically called from <c>ConfigureServices</c>. The section is resolved through
    /// <see cref="PluginExtensions.GetPluginConfiguration"/>:
    /// <c>Plugins:{Id}</c> wins when it exists, otherwise <c>Plugins:{Name}</c> is used,
    /// matching the scoping of <see cref="AuthKitPluginContext"/>.
    /// </para>
    /// <para>
    /// Registered options become readable through <c>IOptions&lt;TOptions&gt;</c> once
    /// the service provider is built.
    /// </para>
    /// </remarks>
    void BindConfiguration<TOptions>(IServiceCollection services, IConfiguration configuration)
        where TOptions : class =>
        services.Configure<TOptions>(this.GetPluginConfiguration(configuration));
}
