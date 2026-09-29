using AuthKit.Plugins.Abstractions;
using AuthKit.Plugins.Abstractions.Contracts;
using AuthKit.Plugins.Abstractions.Contracts.Discovery;
using AuthKit.Plugins.Abstractions.Contracts.Plugins;
using AuthKit.Plugins.Abstractions.Contracts.SecuritySchemes;
using AuthKit.Plugins.Abstractions.Models;
using Host.Plugins.Loading;
using Host.Plugins.Loading.Gate;
using Host.Plugins.Loading.Manifest;
using Host.Plugins.Loading.Pipeline;
using Host.Plugins.Loading.Results;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using ContractLoadedPlugin = AuthKit.Plugins.Abstractions.Contracts.Discovery.LoadedPlugin;
using IAuthKitPlugin = AuthKit.Plugins.Abstractions.Contracts.PluginContract.IAuthKitPlugin;

namespace AuthKit.Host.Tests;

public sealed class PluginDiscoveryTests : IDisposable
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
        string id = "test.fake",
        string version = "1.0.0",
        bool enabled = true,
        string? minHost = null) =>
        new()
        {
            Id = id,
            Name = "Fake",
            Version = SemanticVersion.Parse(version),
            IsEnabled = enabled,
            MinHostVersion = minHost is null ? null : SemanticVersion.Parse(minHost),
            Capabilities = new HashSet<string>(StringComparer.OrdinalIgnoreCase),
        };

    // ---------- DirectoryPluginDiscoverer ----------

    [Fact]
    public async Task Discoverer_ReadsManifestsWithoutLoadingAssemblies()
    {
        var root = NewRoot();
        var withJson = Directory.CreateDirectory(Path.Combine(root, "a")).FullName;
        var withManifest = Directory.CreateDirectory(Path.Combine(root, "b")).FullName;
        await File.WriteAllTextAsync(Path.Combine(withJson, "plugin.json"),
            """{"Id":"test.a","Name":"A","Version":"1.0.0"}""");
        await File.WriteAllTextAsync(Path.Combine(withManifest, "manifest.json"),
            """{"Id":"test.b","Name":"B","Version":"2.0.0"}""");

        var discoverer = new DirectoryPluginDiscoverer(root, NullLogger.Instance);
        var found = new List<DiscoveredPlugin>();
        await foreach (var candidate in discoverer.DiscoverAsync())
            found.Add(candidate);

        Assert.Equal(2, found.Count);
        Assert.Equal("test.a", found.First(c => c.Location == withJson).Manifest!.Id);
        Assert.Equal("test.b", found.First(c => c.Location == withManifest).Manifest!.Id);
        Assert.All(found, c => Assert.Null(c.DiscoveryError));
    }

    [Fact]
    public async Task Discoverer_MissingManifest_IsDiscoveryError()
    {
        var root = NewRoot();
        Directory.CreateDirectory(Path.Combine(root, "nomani"));

        var discoverer = new DirectoryPluginDiscoverer(root, NullLogger.Instance);
        var found = new List<DiscoveredPlugin>();
        await foreach (var candidate in discoverer.DiscoverAsync())
            found.Add(candidate);

        var single = Assert.Single(found);
        Assert.NotNull(single.DiscoveryError);
    }

    [Fact]
    public async Task Discoverer_ReportsCorruptManifestWithoutThrowing()
    {
        var root = NewRoot();
        var broken = Directory.CreateDirectory(Path.Combine(root, "broken")).FullName;
        await File.WriteAllTextAsync(Path.Combine(broken, "manifest.json"), "{not json");

        var discoverer = new DirectoryPluginDiscoverer(root, NullLogger.Instance);
        var found = new List<DiscoveredPlugin>();
        await foreach (var candidate in discoverer.DiscoverAsync())
            found.Add(candidate);

        var single = Assert.Single(found);
        Assert.Null(single.Manifest);
        Assert.NotNull(single.DiscoveryError);
    }

    [Fact]
    public async Task Discoverer_MissingRoot_YieldsNothing()
    {
        var discoverer = new DirectoryPluginDiscoverer(
            Path.Combine(NewRoot(), "nope"), NullLogger.Instance);

        var count = 0;
        await foreach (var _ in discoverer.DiscoverAsync())
            count++;

        Assert.Equal(0, count);
    }

    // ---------- ManifestValidator ----------

    [Fact]
    public void Validator_RejectsEmptyIdAndName()
    {
        var errors = ManifestValidator.Validate(new PluginManifest { Id = "", Name = " " });

        Assert.Contains(errors, e => e.Contains("Id"));
        Assert.Contains(errors, e => e.Contains("Name"));
    }

    [Fact]
    public void Validator_RejectsBadCollections()
    {
        var errors = ManifestValidator.Validate(new PluginManifest
        {
            Id = "x",
            Name = "X",
            Tags = ["ok", " "],
            DependsOn = ["y", "y", "x"],
        });

        Assert.Contains(errors, e => e.Contains("Tags"));
        Assert.Contains(errors, e => e.Contains("duplicates"));
        Assert.Contains(errors, e => e.Contains("itself"));
    }

    [Fact]
    public void Validator_FindsDuplicateIdsCaseInsensitively()
    {
        var duplicates = ManifestValidator.FindDuplicateIds([
            Manifest("test.a"), Manifest("TEST.A"), Manifest("test.b"),
        ]);

        Assert.Equal(["test.a"], duplicates.Select(d => d.ToLowerInvariant()).Distinct());
    }

    // ---------- CompatibilityGate ----------

    [Theory]
    [InlineData("2.3.9", false)]
    [InlineData("2.4.0", true)]
    [InlineData("2.4.1", true)]
    [InlineData("3.0.0", true)]
    public void Gate_MinHostVersionMatrix(string host, bool accepted)
    {
        var (verdict, _) = CompatibilityGate.Check(
            Manifest(minHost: "2.4.0"), SemanticVersion.Parse(host));

        Assert.Equal(accepted ? GateVerdict.Accept : GateVerdict.Reject, verdict);
    }

    [Theory]
    [InlineData("1.2.3", "1.2.3-alpha", true)]
    [InlineData("1.2.3-alpha.1", "1.2.3-alpha", true)]
    [InlineData("1.2.3-beta", "1.2.3-alpha.1", true)]
    [InlineData("1.2.3-alpha", "1.2.3", false)]
    public void Gate_PrereleaseOrdering(string min, string host, bool reject)
    {
        var (verdict, _) = CompatibilityGate.Check(
            Manifest(minHost: min), SemanticVersion.Parse(host));

        Assert.Equal(reject ? GateVerdict.Reject : GateVerdict.Accept, verdict);
    }

    [Fact]
    public void Gate_BuildMetadataIgnoredForPrecedence()
    {
        var (verdict, _) = CompatibilityGate.Check(
            Manifest(minHost: "2.4.0+build"), SemanticVersion.Parse("2.4.0"));

        Assert.Equal(GateVerdict.Accept, verdict);
    }

    [Fact]
    public void Gate_NoMinHostVersion_IsUnaffected()
    {
        var (verdict, _) = CompatibilityGate.Check(Manifest(), SemanticVersion.Parse("0.0.1"));

        Assert.Equal(GateVerdict.Accept, verdict);
    }

    [Fact]
    public void Gate_Enabled_IsAccepted()
    {
        var (verdict, reason) = CompatibilityGate.Check(Manifest(), SemanticVersion.Parse("1.0.0"));

        Assert.Equal(GateVerdict.Accept, verdict);
        Assert.Null(reason);
    }

    [Fact]
    public void Gate_Disabled_Skips()
    {
        var (verdict, reason) = CompatibilityGate.Check(Manifest(enabled: false), SemanticVersion.Parse("1.0.0"));

        Assert.Equal(GateVerdict.SkipDisabled, verdict);
        Assert.NotNull(reason);
    }

    // ---------- Pipeline with swappable discovery/loading ----------

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

    private sealed class StubLoader(Func<DiscoveredPlugin, ContractLoadedPlugin?> load) : IPluginLoader
    {
        public Task<IReadOnlyList<ContractLoadedPlugin>> LoadAsync(
            IReadOnlyList<DiscoveredPlugin> plugins,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ContractLoadedPlugin>>(
                plugins.Select(load).Where(p => p is not null).Cast<ContractLoadedPlugin>().ToList());
    }

    [PluginMetadata("test.fake", "1.0.0", [], [], [], name: "Fake", description: "Fake plugin")]
    private sealed class FakePlugin : IAuthKitPlugin
    {
    }

    [PluginMetadata("test.fake", "1.0.0", [], [], [], name: "Fake", description: "Fake plugin")]
    private sealed class BadSchemePlugin : IAuthKitPlugin
    {
        public void ConfigureServices(IServiceCollection services, AuthKitPluginContext context) { }

        public IReadOnlyDictionary<string, AuthKitSecuritySchemeDescriptor> GetSecuritySchemes() =>
            new Dictionary<string, AuthKitSecuritySchemeDescriptor>
            {
                ["bad"] = new()
                {
                    Name = "bad",
                    Type = (AuthKitSecuritySchemeType)999,
                    In = AuthKitApiKeyLocation.Header,
                    Description = "Invalid scheme.",
                },
            };
    }

    private static DiscoveredPlugin Discovered(
        PluginManifest? manifest,
        string location = "/plugins/fake",
        string? error = null) =>
        new() { Manifest = manifest, Location = location, DiscoveryError = error };

    private static ContractLoadedPlugin Loaded(PluginManifest manifest, IAuthKitPlugin? instance = null) =>
        new()
        {
            Manifest = manifest,
            PluginType = (instance ?? new FakePlugin()).GetType(),
            Instance = instance ?? new FakePlugin(),
            LoadContext = System.Runtime.Loader.AssemblyLoadContext.Default,
        };

    private static PluginLoadingPipeline Pipeline(
        IReadOnlyList<DiscoveredPlugin> discovered,
        Func<DiscoveredPlugin, ContractLoadedPlugin?> load,
        string host = "1.0.0") =>
        new(new StubDiscoverer(discovered), new StubLoader(load), NullLogger.Instance, SemanticVersion.Parse(host));

    [Fact]
    public async Task Pipeline_AcceptsCompatibleManifestPlugin()
    {
        var pipeline = Pipeline([Discovered(Manifest())], candidate => Loaded(candidate.Manifest!));

        var result = await pipeline.RunAsync();

        var single = Assert.Single(result.Loaded);
        Assert.Equal("test.fake", single.Plugin.Id);
        Assert.Equal("test.fake", single.Manifest?.Id);
        Assert.Empty(result.Issues);
    }

    [Fact]
    public async Task Pipeline_SkipsDisabledBeforeLoading()
    {
        var loaderCalls = 0;
        var pipeline = Pipeline([Discovered(Manifest(enabled: false))], candidate =>
        {
            loaderCalls++;
            return Loaded(candidate.Manifest!);
        });

        var result = await pipeline.RunAsync();

        Assert.Empty(result.Loaded);
        Assert.Equal(0, loaderCalls);
        var issue = Assert.Single(result.Issues);
        Assert.Equal(PluginOutcome.SkippedDisabled, issue.Outcome);
    }

    [Fact]
    public async Task Pipeline_RejectsIncompatibleHost()
    {
        var loaderCalls = 0;
        var pipeline = Pipeline([Discovered(Manifest(minHost: "2.0.0"))], candidate =>
        {
            loaderCalls++;
            return Loaded(candidate.Manifest!);
        });

        var result = await pipeline.RunAsync();

        Assert.Empty(result.Loaded);
        Assert.Equal(0, loaderCalls);
        var issue = Assert.Single(result.Issues);
        Assert.Equal(PluginOutcome.Rejected, issue.Outcome);
    }

    [Fact]
    public async Task Pipeline_RejectsDuplicateIdsDeterministically()
    {
        var pipeline = Pipeline(
        [
            Discovered(Manifest(), "/plugins/b"),
            Discovered(Manifest(), "/plugins/a"),
        ],
        candidate => Loaded(candidate.Manifest!));

        var result = await pipeline.RunAsync();

        var single = Assert.Single(result.Loaded);
        Assert.Equal("/plugins/a", single.PluginDirectory);
        var issue = Assert.Single(result.Issues);
        Assert.Equal(PluginOutcome.Invalid, issue.Outcome);
        Assert.Contains("uplicate", issue.Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Pipeline_RejectsManifestInstanceMismatch()
    {
        var pipeline = Pipeline(
            [Discovered(Manifest(id: "test.other"))],
            candidate => Loaded(candidate.Manifest!));

        var result = await pipeline.RunAsync();

        Assert.Empty(result.Loaded);
        var issue = Assert.Single(result.Issues);
        Assert.Equal(PluginOutcome.Invalid, issue.Outcome);
    }

    [Fact]
    public async Task Pipeline_RejectsDiscoveryErrorsWithoutLoading()
    {
        var loaderCalls = 0;
        var pipeline = Pipeline([Discovered(null, error: "boom")], candidate =>
        {
            loaderCalls++;
            return Loaded(candidate.Manifest!);
        });

        var result = await pipeline.RunAsync();

        Assert.Empty(result.Loaded);
        Assert.Equal(0, loaderCalls);
        Assert.Equal(PluginOutcome.Invalid, Assert.Single(result.Issues).Outcome);
    }

    [Fact]
    public async Task Pipeline_RejectsMissingManifest()
    {
        var root = NewRoot();
        var dir = Directory.CreateDirectory(Path.Combine(root, "nomani")).FullName;
        var discoverer = new DirectoryPluginDiscoverer(root, NullLogger.Instance);
        var loader = new DefaultPluginLoader(NullLogger.Instance);
        var pipeline = new PluginLoadingPipeline(discoverer, loader, NullLogger.Instance, SemanticVersion.Parse("1.0.0"));

        var result = await pipeline.RunAsync();

        Assert.Empty(result.Loaded);
        var issue = Assert.Single(result.Issues);
        Assert.Equal(PluginOutcome.Invalid, issue.Outcome);
        Assert.Equal(dir, issue.Location);
    }

    [Fact]
    public async Task Pipeline_RejectsContractViolations()
    {
        var pipeline = Pipeline(
            [Discovered(Manifest())],
            _ => Loaded(Manifest(), new BadSchemePlugin()));

        var result = await pipeline.RunAsync();

        Assert.Empty(result.Loaded);
        var issue = Assert.Single(result.Issues);
        Assert.Equal(PluginOutcome.Invalid, issue.Outcome);
    }

    [Fact]
    public async Task Pipeline_ReportsLoaderFailuresAsInvalid()
    {
        var pipeline = Pipeline([Discovered(Manifest())], _ => null);

        var result = await pipeline.RunAsync();

        Assert.Empty(result.Loaded);
        Assert.Equal(PluginOutcome.Invalid, Assert.Single(result.Issues).Outcome);
    }

    [Fact]
    public void LoadContext_SharesContracts()
    {
        var field = typeof(global::Host.Plugins.Loading.PluginLoadContext).GetField(
            "SharedContracts",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);

        var shared = Assert.IsAssignableFrom<System.Collections.Generic.HashSet<string>>(
            field?.GetValue(null));

        Assert.Contains("AuthKit.Plugins.Abstractions", shared, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("Grpc.Core.Api", shared, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("Google.Protobuf", shared, StringComparer.OrdinalIgnoreCase);
    }
}
