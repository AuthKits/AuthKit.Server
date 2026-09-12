using AuthKit.Plugins.Abstractions.Contracts;
using AuthKit.Plugins.Abstractions.Contracts.Plugins;
using AuthKit.Plugins.Integrations;
using Host.Configuration;
using Host.Plugins;
using Marten;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;
using Xunit;

namespace AuthKit.Host.Tests;

/// <summary>
/// Verifies the optional OpenAPI (B9) and Marten (B10) integration hooks receive the
/// actual host integration object and are processed once, in deterministic order.
/// </summary>
public sealed class PluginInfrastructureIntegrationTests
{
    [Fact]
    public void OpenApi_HookContributionsCoexistWithHost_OnceDeterministic()
    {
        var services = new ServiceCollection();
        var order = new List<string>();
        var first = new OpenApiPluginA(order);
        var second = new OpenApiPluginB(order);

        services.AddLogging();
        services.AddRouting();
        services.AddRestfulServices(
            new[]
            {
                new LoadedPlugin(first, typeof(OpenApiPluginA).Assembly, "openapi-a"),
                new LoadedPlugin(second, typeof(OpenApiPluginB).Assembly, "openapi-b")
            },
            new ConfigurationBuilder().Build(),
            NullLogger.Instance);
        services.AddSingleton<IWebHostEnvironment>(new FakeWebHostEnvironment());
        services.AddSingleton<IHostEnvironment>(sp =>
            sp.GetRequiredService<IWebHostEnvironment>());

        using var provider = services.BuildServiceProvider();
        var swagger = provider.GetRequiredService<ISwaggerProvider>();
        var document = swagger.GetSwagger("v1");

        Assert.Equal(1, first.Calls);
        Assert.Equal(1, second.Calls);
        Assert.Equal(["openapi-a-plugin", "openapi-b-plugin"], order);
        Assert.Contains("PluginSecurityA", document.Components.SecuritySchemes.Keys);
        Assert.Contains("PluginSecurityB", document.Components.SecuritySchemes.Keys);
        Assert.Contains("Bearer", document.Components.SecuritySchemes.Keys);
        Assert.Equal("API", document.Info.Title);
    }

    [Fact]
    public void OpenApi_HookFailureSurfacesExplicitly()
    {
        var services = new ServiceCollection();
        var failing = new FailingOpenApiPlugin();

        var ex = Record.Exception(() =>
        {
            services.AddLogging();
            services.AddRouting();
            services.AddRestfulServices(
                new[] { new LoadedPlugin(failing, typeof(FailingOpenApiPlugin).Assembly, "openapi-boom") },
                new ConfigurationBuilder().Build(),
                NullLogger.Instance);
            services.AddSingleton<IWebHostEnvironment>(new FakeWebHostEnvironment());
            services.AddSingleton<IHostEnvironment>(sp =>
                sp.GetRequiredService<IWebHostEnvironment>());
            using var provider = services.BuildServiceProvider();
            _ = provider.GetRequiredService<ISwaggerProvider>().GetSwagger("v1");
        });

        var ioe = Assert.IsType<InvalidOperationException>(ex);
        Assert.Equal("openapi-boom", ioe.Message);
    }

    [Fact]
    public void Marten_HookReceivesActualStoreOptions_OnceDeterministic_SameInstance()
    {
        var services = new ServiceCollection();
        var configuration = CreateConfiguration("host=localhost;database=fake");
        var order = new List<string>();
        var first = new MartenPluginA(order);
        var second = new MartenPluginB(order);
        var plain = new PlainPlugin();

        services.ConfigureMarten(
            configuration,
            new[]
            {
                new LoadedPlugin(first, typeof(MartenPluginA).Assembly, "marten-a"),
                new LoadedPlugin(plain, typeof(PlainPlugin).Assembly, "plain"),
                new LoadedPlugin(second, typeof(MartenPluginB).Assembly, "marten-b")
            });

        Assert.NotNull(first.Received);
        Assert.NotNull(second.Received);

        using var provider = services.BuildServiceProvider();
        var storeOptions = provider.GetRequiredService<StoreOptions>();

        Assert.Same(storeOptions, first.Received);
        Assert.Same(storeOptions, second.Received);
        Assert.Equal(1, first.Calls);
        Assert.Equal(1, second.Calls);
        Assert.Equal(["marten-a-plugin", "marten-b-plugin"], order);
    }

    [Fact]
    public void Marten_HookRegistersPluginDocumentWithHostOptions()
    {
        var services = new ServiceCollection();
        var configuration = CreateConfiguration("host=localhost;database=fake");
        var plugin = new MartenPluginB([]);

        services.ConfigureMarten(
            configuration,
            new[] { new LoadedPlugin(plugin, typeof(MartenPluginB).Assembly, "marten-doc") });

        Assert.Same(plugin.Received, GetStoreOptions(services));
        Assert.Contains(typeof(MartenDocument), plugin.RegisteredDocuments);
    }

    [Fact]
    public void Marten_HookFailureSurfacesExplicitly()
    {
        var services = new ServiceCollection();
        var configuration = CreateConfiguration("host=localhost;database=fake");
        var failing = new FailingMartenPlugin();

        var ex = Record.Exception(() =>
        {
            services.ConfigureMarten(
                configuration,
                new[] { new LoadedPlugin(failing, typeof(FailingMartenPlugin).Assembly, "marten-boom") });
            _ = GetStoreOptions(services);
        });

        Assert.IsType<InvalidOperationException>(ex);
    }

    private static IReadOnlyStoreOptions GetStoreOptions(IServiceCollection services)
    {
        using var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<StoreOptions>();
    }

    private static IConfiguration CreateConfiguration(string connectionString) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Marten"] = connectionString
            })
            .Build();

    [PluginMetadata("openapi-a-plugin", "1.0.0", [], [], [], name: "OpenApi A", description: "OpenAPI test")]
    private sealed class OpenApiPluginA(List<string> order) : IAuthKitPlugin, IOpenApiPlugin
    {
        public int Calls { get; private set; }

        public void ConfigureOpenApi(SwaggerGenOptions options)
        {
            Calls++;
            order.Add("openapi-a-plugin");
            options.AddSecurityDefinition("PluginSecurityA", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.ApiKey,
                Name = "X-Plugin-A",
                In = ParameterLocation.Header
            });
        }
    }

    [PluginMetadata("openapi-b-plugin", "1.0.0", [], [], [], name: "OpenApi B", description: "OpenAPI test")]
    private sealed class OpenApiPluginB(List<string> order) : IAuthKitPlugin, IOpenApiPlugin
    {
        public int Calls { get; private set; }

        public void ConfigureOpenApi(SwaggerGenOptions options)
        {
            Calls++;
            order.Add("openapi-b-plugin");
            options.AddSecurityDefinition("PluginSecurityB", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.ApiKey,
                Name = "X-Plugin-B",
                In = ParameterLocation.Header
            });
        }
    }

    [PluginMetadata("openapi-boom", "1.0.0", [], [], [], name: "OpenApi Boom", description: "Failing OpenAPI test")]
    private sealed class FailingOpenApiPlugin : IAuthKitPlugin, IOpenApiPlugin
    {
        public void ConfigureOpenApi(SwaggerGenOptions options) =>
            throw new InvalidOperationException("openapi-boom");
    }

    [PluginMetadata("marten-a-plugin", "1.0.0", [], [], [], name: "Marten A", description: "Marten test")]
    private sealed class MartenPluginA(List<string> order) : IAuthKitPlugin, IMartenPlugin
    {
        public int Calls { get; private set; }

        public StoreOptions? Received { get; private set; }

        public List<Type> RegisteredDocuments { get; } = [];

        public void ConfigureMarten(StoreOptions options)
        {
            Calls++;
            order.Add("marten-a-plugin");
            Received = options;
            options.Schema.For<MartenDocument>();
            RegisteredDocuments.Add(typeof(MartenDocument));
        }
    }

    [PluginMetadata("marten-b-plugin", "1.0.0", [], [], [], name: "Marten B", description: "Marten test")]
    private sealed class MartenPluginB(List<string> order) : IAuthKitPlugin, IMartenPlugin
    {
        public int Calls { get; private set; }

        public StoreOptions? Received { get; private set; }

        public List<Type> RegisteredDocuments { get; } = [];

        public void ConfigureMarten(StoreOptions options)
        {
            Calls++;
            order.Add("marten-b-plugin");
            Received = options;
            options.Schema.For<MartenDocument>();
            RegisteredDocuments.Add(typeof(MartenDocument));
        }
    }

    [PluginMetadata("marten-boom", "1.0.0", [], [], [], name: "Marten Boom", description: "Failing Marten test")]
    private sealed class FailingMartenPlugin : IAuthKitPlugin, IMartenPlugin
    {
        public void ConfigureMarten(StoreOptions options) =>
            throw new InvalidOperationException("marten-boom");
    }

    [PluginMetadata("plain-plugin", "1.0.0", [], [], [], name: "Plain Plugin", description: "No integrations")]
    private sealed class PlainPlugin : IAuthKitPlugin
    {
    }

    private sealed class MartenDocument
    {
        public Guid Id { get; set; }
    }

    private sealed class FakeWebHostEnvironment : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "AuthKit.Host.Tests";

        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();

        public string WebRootPath { get; set; } = string.Empty;

        public string EnvironmentName { get; set; } = "Production";

        public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}