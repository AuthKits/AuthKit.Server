using AuthKit.Plugins.Abstractions.Contracts.Discovery;
using AuthKit.Plugins.Abstractions.Models;
using Host.Plugins.Loading;
using Xunit;

namespace AuthKit.Host.Tests;

public sealed class DependencyGraphTests
{
    private static PluginManifest Manifest(string id, int priority = 0, params string[] dependsOn) =>
        new()
        {
            Id = id,
            Name = id,
            Version = new SemanticVersion(1, 0, 0),
            Priority = priority,
            DependsOn = dependsOn,
            Capabilities = new HashSet<string>(StringComparer.OrdinalIgnoreCase),
        };

    private static DiscoveredPlugin Discovered(PluginManifest manifest, string? location = null) =>
        new() { Manifest = manifest, Location = location ?? $"/plugins/{manifest.Id}" };

    private static List<(DiscoveredPlugin Candidate, int RegistrationOrder)> Indexed(params DiscoveredPlugin[] candidates) =>
        candidates.Select((candidate, index) => (candidate, index)).ToList();

    private static List<string> OrderIds(IReadOnlyList<DiscoveredPlugin> ordered) =>
        ordered.Select(candidate => candidate.Manifest!.Id).ToList();

    [Fact]
    public void IssueExample_SortsABC()
    {
        var indexed = Indexed(
            Discovered(Manifest("test.a", -100)),
            Discovered(Manifest("test.b", -200, "test.a")),
            Discovered(Manifest("test.c", 0)));

        DependencyGraph.Validate(indexed, new HashSet<string>(["test.a", "test.b", "test.c"], StringComparer.OrdinalIgnoreCase));

        Assert.Equal(["test.a", "test.b", "test.c"], OrderIds(DependencyGraph.Sort(indexed)));
    }

    [Fact]
    public void AltPriorities_SortCAB()
    {
        var indexed = Indexed(
            Discovered(Manifest("test.a", 100)),
            Discovered(Manifest("test.b", 0, "test.a")),
            Discovered(Manifest("test.c", 0)));

        Assert.Equal(["test.c", "test.a", "test.b"], OrderIds(DependencyGraph.Sort(indexed)));
    }

    [Fact]
    public void DependenciesComeFirstRegardlessOfDiscoveryOrder()
    {
        var indexed = Indexed(
            Discovered(Manifest("test.b", 0, "test.a")),
            Discovered(Manifest("test.a", 0)));

        Assert.Equal(["test.a", "test.b"], OrderIds(DependencyGraph.Sort(indexed)));
    }

    [Fact]
    public void Cycle_ThrowsStartupError()
    {
        var indexed = Indexed(
            Discovered(Manifest("test.a", 0, "test.b")),
            Discovered(Manifest("test.b", 0, "test.a")));

        var exception = Assert.Throws<InvalidOperationException>(() =>
            DependencyGraph.Validate(indexed, new HashSet<string>(["test.a", "test.b"], StringComparer.OrdinalIgnoreCase)));

        Assert.Contains("cycle", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ThreeCycle_ThrowsStartupError()
    {
        var indexed = Indexed(
            Discovered(Manifest("test.a", 0, "test.b")),
            Discovered(Manifest("test.b", 0, "test.c")),
            Discovered(Manifest("test.c", 0, "test.a")));

        var exception = Assert.Throws<InvalidOperationException>(() =>
            DependencyGraph.Validate(indexed, new HashSet<string>(["test.a", "test.b", "test.c"], StringComparer.OrdinalIgnoreCase)));

        Assert.Contains("cycle", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SelfCycle_ThrowsStartupError()
    {
        var indexed = Indexed(Discovered(Manifest("test.a", 0, "test.a")));

        Assert.Throws<InvalidOperationException>(() =>
            DependencyGraph.Validate(indexed, new HashSet<string>(["test.a"], StringComparer.OrdinalIgnoreCase)));
    }

    [Fact]
    public void UnknownDependency_ThrowsStartupError()
    {
        var indexed = Indexed(Discovered(Manifest("test.a", 0, "test.ghost")));

        var exception = Assert.Throws<InvalidOperationException>(() =>
            DependencyGraph.Validate(indexed, new HashSet<string>(["test.a"], StringComparer.OrdinalIgnoreCase)));

        Assert.Contains("test.ghost", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Sort_IsDeterministic()
    {
        var first = OrderIds(DependencyGraph.Sort(Indexed(
            Discovered(Manifest("test.b", 0, "test.a")),
            Discovered(Manifest("test.c", 0)),
            Discovered(Manifest("test.a", 0)))));
        var second = OrderIds(DependencyGraph.Sort(Indexed(
            Discovered(Manifest("test.b", 0, "test.a")),
            Discovered(Manifest("test.c", 0)),
            Discovered(Manifest("test.a", 0)))));

        Assert.Equal(first, second);
        Assert.Equal(["test.c", "test.a", "test.b"], first);
    }
}
