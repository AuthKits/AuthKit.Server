using AuthKit.Plugins.Abstractions;
using AuthKit.Plugins.Abstractions.Contracts;
using AuthKit.Plugins.Abstractions.Contracts.Discovery;
using AuthKit.Plugins.Abstractions.Contracts.Plugins;
using AuthKit.Plugins.Abstractions.Models;
using Host.Plugins.Loading;
using Host.Plugins.Loading.Gate;
using Host.Plugins.Loading.Pipeline;
using Host.Plugins.Loading.Results;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using ContractLoadedPlugin = AuthKit.Plugins.Abstractions.Contracts.Discovery.LoadedPlugin;
using IAuthKitPlugin = AuthKit.Plugins.Abstractions.Contracts.PluginContract.IAuthKitPlugin;

namespace AuthKit.Host.Tests;

public sealed class PluginPipelineBehaviorTests : IDisposable
{
    private readonly List<string> _tempDirs = [];

    public void Dispose()
    {
        foreach (var dir in _tempDirs)
        {
            try { Directory.Delete(dir, recursive: true); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }

    private string NewRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(root);
        _tempDirs.Add(root);
        return root;
    }

    private static PluginManifest Manifest(
        string id,
        int priority = 0,
        string[]? dependsOn = null,
        bool enabled = true) =>
        new()
        {
            Id = id,
            Name = id,
            Version = new SemanticVersion(1, 0, 0),
            Priority = priority,
            DependsOn = dependsOn ?? [],
            IsEnabled = enabled,
            Capabilities = new HashSet<string>(StringComparer.OrdinalIgnoreCase),
        };

    private sealed class DepPlugin(PluginManifest manifest) : IAuthKitPlugin
    {
        public string Id => manifest.Id;
        public string Name => manifest.Name;
        public SemanticVersion Version => manifest.Version;
        public IReadOnlyList<string> DependsOn => manifest.DependsOn;
        public bool IsEnabled => manifest.IsEnabled;
        public SemanticVersion? MinHostVersion => manifest.MinHostVersion;
        public IReadOnlySet<string> Capabilities =>
            System.Collections.Immutable.ImmutableHashSet.CreateRange(
                StringComparer.OrdinalIgnoreCase, manifest.Capabilities);

        public void ConfigureServices(IServiceCollection services, AuthKitPluginContext context) { }
    }

    private sealed class StubDiscoverer(IReadOnlyList<DiscoveredPlugin> items) : IPluginDiscoverer
    {
        public async IAsyncEnumerable<DiscoveredPlugin> DiscoverAsync(
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            foreach (var item in items)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return item;
                await Task.Yield();
            }
        }
    }

    private sealed class RecordingLoader(List<string> loadedOrder) : IPluginLoader
    {
        public Task<IReadOnlyList<ContractLoadedPlugin>> LoadAsync(
            IReadOnlyList<DiscoveredPlugin> plugins,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ContractLoadedPlugin>>(plugins
                .Select(candidate =>
                {
                    loadedOrder.Add(candidate.Manifest!.Id);
                    return new ContractLoadedPlugin
                    {
                        Manifest = candidate.Manifest,
                        PluginType = typeof(DepPlugin),
                        Instance = new DepPlugin(candidate.Manifest),
                        LoadContext = System.Runtime.Loader.AssemblyLoadContext.Default,
                    };
                })
                .ToList());
    }

    private static DiscoveredPlugin Discovered(PluginManifest manifest, string? location = null) =>
        new() { Manifest = manifest, Location = location ?? $"/plugins/{manifest.Id}" };

    private static PluginLoadingPipeline Pipeline(
        IReadOnlyList<DiscoveredPlugin> discovered,
        List<string> loadedOrder,
        IConfiguration? configuration = null) =>
        new(
            new StubDiscoverer(discovered),
            new RecordingLoader(loadedOrder),
            NullLogger.Instance,
            SemanticVersion.Parse("1.0.0"),
            configuration);

    private static IConfiguration Config(params (string Key, string Value)[] values) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(values.Select(value =>
                new KeyValuePair<string, string?>(value.Key, value.Value)))
            .Build();

    // ---------- G7: ordering + unavailable propagation ----------

    [Fact]
    public async Task Pipeline_LoadsInDependencyOrder()
    {
        var loadedOrder = new List<string>();
        var pipeline = Pipeline(
        [
            Discovered(Manifest("test.b", -200, ["test.a"])),
            Discovered(Manifest("test.c", 0)),
            Discovered(Manifest("test.a", -100)),
        ],
        loadedOrder);

        var result = await pipeline.RunAsync();

        Assert.Equal(3, result.Loaded.Count);
        Assert.Equal(["test.a", "test.b", "test.c"], loadedOrder);
    }

    [Fact]
    public async Task Pipeline_RejectsTransitiveDependentsOfDisabled()
    {
        var loadedOrder = new List<string>();
        var pipeline = Pipeline(
        [
            Discovered(Manifest("test.a", enabled: false)),
            Discovered(Manifest("test.b", dependsOn: ["test.a"])),
            Discovered(Manifest("test.c", dependsOn: ["test.b"])),
        ],
        loadedOrder);

        var result = await pipeline.RunAsync();

        Assert.Empty(result.Loaded);
        Assert.Empty(loadedOrder);
        Assert.Equal(3, result.Issues.Count);
        Assert.Contains(result.Issues, i => i.PluginId == "test.a" && i.Outcome == PluginOutcome.SkippedDisabled);
        Assert.Contains(result.Issues, i => i.PluginId == "test.b" && i.Outcome == PluginOutcome.Rejected && i.Reason.Contains("dependency", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Issues, i => i.PluginId == "test.c" && i.Outcome == PluginOutcome.Rejected);
    }

    [Fact]
    public async Task Pipeline_UnknownDependency_IsStartupError()
    {
        var pipeline = Pipeline([Discovered(Manifest("test.a", dependsOn: ["test.ghost"]))], []);

        await Assert.ThrowsAsync<InvalidOperationException>(() => pipeline.RunAsync());
    }

    [Fact]
    public async Task Pipeline_Cycle_IsStartupError()
    {
        var pipeline = Pipeline(
        [
            Discovered(Manifest("test.a", dependsOn: ["test.b"])),
            Discovered(Manifest("test.b", dependsOn: ["test.a"])),
        ],
        []);

        await Assert.ThrowsAsync<InvalidOperationException>(() => pipeline.RunAsync());
    }

    // ---------- G8: EffectiveIsEnabled ----------

    [Theory]
    [InlineData(true, null, true)]
    [InlineData(true, "true", true)]
    [InlineData(true, "false", false)]
    [InlineData(false, null, false)]
    [InlineData(false, "true", false)]
    public void EffectiveIsEnabled_TruthTable(bool manifest, string? configured, bool expected)
    {
        var config = configured is null
            ? Config()
            : Config(("Plugins:test.fake:IsEnabled", configured));

        Assert.Equal(expected, CompatibilityGate.EffectiveIsEnabled(
            new PluginManifest { Id = "test.fake", Name = "Fake", IsEnabled = manifest }, config));
    }

    [Fact]
    public async Task Pipeline_HostConfigDisablesWithoutLoading()
    {
        var loadedOrder = new List<string>();
        var pipeline = Pipeline(
            [Discovered(Manifest("test.a"))],
            loadedOrder,
            Config(("Plugins:test.a:IsEnabled", "false")));

        var result = await pipeline.RunAsync();

        Assert.Empty(result.Loaded);
        Assert.Empty(loadedOrder);
        Assert.Equal(PluginOutcome.SkippedDisabled, Assert.Single(result.Issues).Outcome);
    }

    [Fact]
    public async Task Pipeline_HostConfigCannotReenable()
    {
        var loadedOrder = new List<string>();
        var pipeline = Pipeline(
            [Discovered(Manifest("test.a", enabled: false))],
            loadedOrder,
            Config(("Plugins:test.a:IsEnabled", "true")));

        var result = await pipeline.RunAsync();

        Assert.Empty(result.Loaded);
        Assert.Equal(PluginOutcome.SkippedDisabled, Assert.Single(result.Issues).Outcome);
    }

    // ---------- G9: cache ----------

    private sealed class CountingDiscoverer(IPluginDiscoverer inner, Action onCall) : IPluginDiscoverer
    {
        public async IAsyncEnumerable<DiscoveredPlugin> DiscoverAsync(
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            onCall();
            await foreach (var candidate in inner.DiscoverAsync(cancellationToken))
                yield return candidate;
        }
    }

    private static string WriteManifest(string dir, string id) =>
        WriteManifest(dir, id, """{"Id":"__ID__","Name":"N","Version":"1.0.0"}""".Replace("__ID__", id));

    private static string WriteManifest(string dir, string id, string json)
    {
        File.WriteAllText(Path.Combine(dir, "manifest.json"), json);
        return dir;
    }

    [Fact]
    public async Task Cache_HitSkipsDiscoverer()
    {
        var root = NewRoot();
        WriteManifest(Directory.CreateDirectory(Path.Combine(root, "a")).FullName, "test.a");
        var cachePath = Path.Combine(root, "cache.json");
        var calls = 0;

        PluginLoadResult first;
        {
            var counting = new CountingDiscoverer(new DirectoryPluginDiscoverer(root, NullLogger.Instance), () => calls++);
            var cache = new FilePluginDiscoveryCache(cachePath, NullLogger.Instance);
            var pipeline = new PluginLoadingPipeline(counting, new RecordingLoader([]), NullLogger.Instance, SemanticVersion.Parse("1.0.0"), null, cache);
            first = await pipeline.RunAsync();
        }

        Assert.Equal(1, calls);
        Assert.Single(first.Loaded);

        {
            var counting = new CountingDiscoverer(new DirectoryPluginDiscoverer(root, NullLogger.Instance), () => calls++);
            var cache = new FilePluginDiscoveryCache(cachePath, NullLogger.Instance);
            var pipeline = new PluginLoadingPipeline(counting, new RecordingLoader([]), NullLogger.Instance, SemanticVersion.Parse("1.0.0"), null, cache);
            var second = await pipeline.RunAsync();

            Assert.Equal(1, calls);
            Assert.Single(second.Loaded);
        }
    }

    [Fact]
    public async Task Cache_StaleFingerprintFallsBackToDiscovery()
    {
        var root = NewRoot();
        var dir = WriteManifest(Directory.CreateDirectory(Path.Combine(root, "a")).FullName, "test.a");
        var cachePath = Path.Combine(root, "cache.json");
        var cache = new FilePluginDiscoveryCache(cachePath, NullLogger.Instance);
        var inner = new DirectoryPluginDiscoverer(root, NullLogger.Instance);

        var discovered = new List<DiscoveredPlugin>();
        await foreach (var candidate in inner.DiscoverAsync())
            discovered.Add(candidate);
        await cache.StoreAsync(discovered);

        Assert.NotNull(await cache.TryGetAsync());

        await File.WriteAllTextAsync(Path.Combine(dir, "manifest.json"),
            """{"Id":"test.a","Name":"N","Version":"2.0.0"}""");

        Assert.Null(await cache.TryGetAsync());
    }

    [Fact]
    public async Task Cache_CorruptFileIsMiss()
    {
        var root = NewRoot();
        var cachePath = Path.Combine(root, "cache.json");
        await File.WriteAllTextAsync(cachePath, "{broken");

        var cache = new FilePluginDiscoveryCache(cachePath, NullLogger.Instance);

        Assert.Null(await cache.TryGetAsync());
    }

    [Fact]
    public async Task Cache_StoresMetadataOnly()
    {
        var root = NewRoot();
        WriteManifest(Directory.CreateDirectory(Path.Combine(root, "a")).FullName, "test.a");
        var cachePath = Path.Combine(root, "cache.json");
        var inner = new DirectoryPluginDiscoverer(root, NullLogger.Instance);

        var discovered = new List<DiscoveredPlugin>();
        await foreach (var candidate in inner.DiscoverAsync())
            discovered.Add(candidate);
        var cache = new FilePluginDiscoveryCache(cachePath, NullLogger.Instance);
        await cache.StoreAsync(discovered);

        var json = await File.ReadAllTextAsync(cachePath);
        Assert.Contains("test.a", json, StringComparison.Ordinal);
        Assert.DoesNotContain("AssemblyLoadContext", json, StringComparison.Ordinal);
        Assert.DoesNotContain("PluginType", json, StringComparison.Ordinal);
    }

    // ---------- G10: logging ----------

    private sealed class RecordingLogger : ILogger
    {
        public List<(LogLevel Level, string Message)> Entries { get; } = [];

        IDisposable ILogger.BeginScope<TState>(TState state) => null!;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter) =>
            Entries.Add((logLevel, formatter(state, exception)));
    }

    [Fact]
    public async Task Logging_DiscoveryAndOutcomeAreLogged()
    {
        var root = NewRoot();
        WriteManifest(Directory.CreateDirectory(Path.Combine(root, "a")).FullName, "test.a");
        var recording = new RecordingLogger();
        var pipeline = new PluginLoadingPipeline(
            new DirectoryPluginDiscoverer(root, recording),
            new RecordingLoader([]),
            recording,
            SemanticVersion.Parse("1.0.0"));

        await pipeline.RunAsync();

        Assert.Contains(recording.Entries, e =>
            e.Level == LogLevel.Information && e.Message.Contains("test.a", StringComparison.Ordinal));
        Assert.Contains(recording.Entries, e =>
            e.Message.Contains("Loaded plugin", StringComparison.Ordinal));
    }
}
