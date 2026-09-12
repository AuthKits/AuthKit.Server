using Core.KeyManagement.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AuthKit.Host.IntegrationTests;

/// <summary>
/// Hosts the real AuthKit <c>Program</c> over an in-memory test server with
/// production plugin discovery configured against the integration test output.
/// </summary>
public sealed class AuthKitWebApplicationFactory : WebApplicationFactory<Program>
{
    /// <summary>
    /// Points plugin discovery and Marten at sandboxes: the staged plugin
    /// directory under the test output and a fake PostgreSQL connection string
    /// (Marten connects lazily, so requests that do not touch document sessions
    /// are unaffected).
    /// </summary>
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var pluginsPath = Path.Join(AppContext.BaseDirectory, "plugins");

        builder.UseSetting("AuthKit:PluginsPath", pluginsPath);
        builder.UseSetting("AuthKit:SkipStorageMigrationOnStartup", "true");
        builder.UseSetting(
            "ConnectionStrings:Marten",
            "Host=localhost;Port=5432;Database=authkit_test;Username=authkit;Password=authkit");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IKeyStoreRepository>();
            services.AddSingleton<IKeyStoreRepository, InMemoryKeyStoreRepository>();
        });
    }

    /// <summary>
    /// In-memory stand-in for the Marten-backed keystore persistence, so the
    /// real host lifecycle runs without an external PostgreSQL database.
    /// </summary>
    private sealed class InMemoryKeyStoreRepository : IKeyStoreRepository
    {
        private byte[]? _data;

        public Task<Memory<byte>> LoadAsync()
            => Task.FromResult<Memory<byte>>(_data ?? Memory<byte>.Empty);

        public Task SaveAsync(ReadOnlyMemory<byte> data)
        {
            _data = data.ToArray();
            return Task.CompletedTask;
        }
    }
}