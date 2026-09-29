using AuthKit.Plugins.Abstractions.Contracts.Plugins;
using ExamplePlugin.Options;
using Microsoft.Extensions.DependencyInjection;
using ExamplePlugin.Middleware;
using AuthKit.Plugins.Abstractions.Contracts;
using IAuthKitPlugin = AuthKit.Plugins.Abstractions.Contracts.PluginContract.IAuthKitPlugin;

namespace ExamplePlugin;

public sealed partial class ExamplePlugin : IAuthKitPlugin
{
    /// <summary>
    /// Registers the plugin's options, services, and hosted services.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> used to register plugin services.</param>
    /// <param name="context">Stable plugin context including the plugin-scoped configuration section.</param>
    /// <remarks>
    /// The plugin binds its options from <see cref="AuthKitPluginContext.Configuration"/>, which is
    /// scoped to <c>Plugins:authkit.example</c> (or the plugin name when the ID section is absent).
    /// The same section is available to any plugin services through the options infrastructure.
    /// </remarks>
    public void ConfigureServices(IServiceCollection services, AuthKitPluginContext context)
    {
        services.Configure<ExampleOptions>(context.Configuration);

        services.AddSingleton(TimeProvider.System);
        // Registered so the host can resolve ExampleScopedMiddleware (IAuthKitMiddleware)
        // from the request service provider within the single request scope.
        services.AddScoped<ExampleScopedMiddleware>();
    }
}
