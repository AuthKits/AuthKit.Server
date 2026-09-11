using Host.Configuration;
using Host.Plugins;
using Host.Cli;

var builder = WebApplication.CreateBuilder(args);

// === Plugin discovery (before DI/Wolverine/Marten are configured) ===
var pluginsPath = builder.Configuration["AuthKit:PluginsPath"]
    ?? Path.Combine(AppContext.BaseDirectory, "plugins");

var pluginLogger = LoggerFactory.Create(logging => logging.AddConsole()).CreateLogger("PluginLoader");
var plugins = PluginLoader.LoadPlugins(pluginsPath, pluginLogger);

var restfulLogger = LoggerFactory.Create(logging => logging.AddConsole()).CreateLogger("RestfulConfiguration");

// === Core Config ===
builder.Services.AddSingleton(plugins);
builder.Services.AddAuthKitCore();

builder.Services.ConfigureApp(builder.Configuration, plugins)
    .AddGrpcServices()
    .AddRestfulServices(plugins, builder.Configuration, restfulLogger)
    .AddKeycloakServices();

foreach (var lp in plugins)
    lp.Plugin.ConfigureServices(builder.Services, builder.Configuration);

builder.ConfigureWolverine(plugins);
builder.Services.ConfigureMarten(builder.Configuration);
builder.WebHost.ConfigureKestrelServer();

builder.Services.Configure<AuthKitServerOptions>(
    builder.Configuration.GetSection("Server")
);

builder.Services.AddHostedService<ServerHost>();


// === Pipeline ===
var app = builder.Build();

app.ConfigureMiddleware(plugins)
    .MapAppEndpoints()
    .MapGrpcEndpoints();

app.Run();
