using Host.Configuration.Authentication;
using Host.Configuration.Bootstrap;
using Host.Configuration.Grpc;
using Host.Configuration.Infrastructure;
using Host.Configuration.Pipeline;
using Host.Configuration.Restful;
using Host.Configuration.Server;
using Host.Plugins.Loading;
using Host.Plugins.Configuration;
using Host.Plugins.Lifecycle;
using Host.Cli;
using Host.Security;
using Host.Security.Registrations;
using AuthKit.Plugins.Abstractions;
using System.Reflection;
using AuthKit.Plugins.Abstractions.Models;
using Host.Plugins.Health;

var builder = WebApplication.CreateBuilder(args);

// === Plugin discovery (before DI/Wolverine/Marten are configured) ===
var pluginsPath = builder.Configuration["AuthKit:PluginsPath"]
    ?? Path.Combine(AppContext.BaseDirectory, "plugins");

var pluginLogger = LoggerFactory.Create(logging => logging.AddConsole()).CreateLogger("PluginLoader");
var hostVersion = SemanticVersion.Parse(Assembly.GetEntryAssembly()!
        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()!
        .InformationalVersion!.Split('+')[0]);

var plugins = PluginLoader.LoadPlugins(pluginsPath, pluginLogger, hostVersion);

var restfulLogger = LoggerFactory.Create(logging => logging.AddConsole()).CreateLogger("RestfulConfiguration");

// === Core Config ===
builder.Services.AddSingleton(plugins);
builder.Services.Configure<PluginHealthExecutionOptions>(
    builder.Configuration.GetSection("Health"));
builder.Services.AddSingleton<PluginHealthExecutor>();
builder.Services.AddAuthKitCore();

builder.Services.ConfigureApp(builder.Configuration, plugins)
    .AddGrpcServices()
    .AddRestfulServices(plugins, builder.Configuration, restfulLogger)
    .AddApiKeyCredentialExtraction()
    .AddKeycloakServices(plugins);

foreach (var lp in plugins)
    PluginConfigurationInvoker.Configure(lp.Plugin, builder, builder.Configuration);

PluginHostedServiceRegistration.Register(builder.Services, plugins);

builder.ConfigureWolverine(plugins);
builder.Services.ConfigureMarten(builder.Configuration, plugins);
builder.WebHost.ConfigureKestrelServer();

builder.Services.Configure<AuthKitServerOptions>(
    builder.Configuration.GetSection("Server")
);

builder.Services.AddHostedService<ServerHost>();


// === Pipeline ===
var app = builder.Build();

app.ConfigureMiddleware(plugins)
    .MapAppEndpoints(plugins)
    .MapGrpcEndpoints();

PluginApplicationConfiguration.ConfigurePipeline(
    app, plugins, PluginPipelinePosition.AfterEndpoints);

app.Run();

/// <summary>
/// Exposes the application entry point to test hosts such as
/// <c>WebApplicationFactory</c>.
/// </summary>
public partial class Program
{
}
