using AuthKit.Plugins.Abstractions.Contracts;
using AuthKit.Plugins.Abstractions.Contracts.Discovery;
using AuthKit.Plugins.Abstractions.Models;
using Host.Plugins.Contract;
using Host.Plugins.Loading.Gate;
using Host.Plugins.Loading.Manifest;
using Host.Plugins.Loading.Results;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Host.Plugins.Loading.Pipeline;

/// <summary>
/// Host loading pipeline: discovery -> preload validation -> compatibility gate
/// -> isolated load -> contract validation -> manifest consistency -> accepted set.
/// </summary>
/// <remarks>
/// <para>
/// Custom <see cref="IPluginDiscoverer"/> / <see cref="IPluginLoader"/>
/// implementations can replace the defaults the pipeline stages around them
/// stay the same.
/// </para>
/// <para>
/// Loading is per candidate rather than batched, so a loader failure attributes
/// to exactly one location in the resulting <see cref="PluginLoadResult"/>.
/// </para>
/// </remarks>
/// <param name="discoverer">The candidate source. Never activated by the pipeline.</param>
/// <param name="loader">The instance constructor. Never judges compatibility.</param>
/// <param name="logger">The logger used to report stage outcomes.</param>
/// <param name="hostVersion">The running host version for the compatibility gate. Null skips the version rule.</param>
/// <param name="hostConfiguration">Optional host configuration for the effective enabled flag.</param>
public sealed class PluginLoadingPipeline(
    IPluginDiscoverer discoverer,
    IPluginLoader loader,
    ILogger logger,
    SemanticVersion? hostVersion,
    IConfiguration? hostConfiguration = null,
    IPluginDiscoveryCache? discoveryCache = null)
{
    /// <summary>
    /// Runs the full pipeline and returns accepted plugins with diagnostics.
    /// </summary>
    /// <param name="cancellationToken">Stops the pipeline between candidates.</param>
    /// <returns>Accepted plugins plus one issue per rejected candidate.</returns>
    /// <remarks>
    /// Stage order per candidate: discovery error → structural validation →
    /// duplicate ID → compatibility gate → dependency graph → load in
    /// topological order → manifest consistency → contract validation.
    /// The first failing stage reports the issue; later stages never run for
    /// that candidate. Dependents of failed plugins are rejected as
    /// dependency-unavailable without loading.
    /// </remarks>
    public async Task<PluginLoadResult> RunAsync(CancellationToken cancellationToken = default)
    {
        var issues = new List<PluginLoadIssue>();
        var discovered = new List<DiscoveredPlugin>();

        if (discoveryCache is not null
            && await discoveryCache.TryGetAsync(cancellationToken) is { } cached)
        {
            discovered.AddRange(cached);
        }
        else
        {
            await foreach (var candidate in discoverer.DiscoverAsync(cancellationToken))
                discovered.Add(candidate);

            if (discoveryCache is not null)
                await discoveryCache.StoreAsync(discovered, cancellationToken);
        }

        // 1. Discovery errors are invalid and never load.
        var candidates = new List<DiscoveredPlugin>();
        foreach (var candidate in discovered)
        {
            if (candidate.DiscoveryError is { } error)
            {
                issues.Add(new PluginLoadIssue(candidate.Location, null, PluginOutcome.Invalid, error));
                logger.LogError("Skipping plugin '{Location}': {Error}", candidate.Location, error);
                continue;
            }

            candidates.Add(candidate);
        }

        // 2. Structural preload validation (manifests only, no assemblies loaded).
        var structurallyValid = new List<DiscoveredPlugin>();
        foreach (var candidate in candidates)
        {
            if (candidate.Manifest is not { } manifest)
            {
                const string missingReason = "Missing manifest: candidate has no manifest and no discovery error.";
                issues.Add(new PluginLoadIssue(candidate.Location, null, PluginOutcome.Invalid, missingReason));
                logger.LogError("Skipping plugin '{Location}': {Reason}", candidate.Location, missingReason);
                continue;
            }

            var errors = ManifestValidator.Validate(manifest);
            if (errors.Count == 0)
            {
                structurallyValid.Add(candidate);
                continue;
            }

            var reason = $"Invalid manifest for plugin '{manifest.Id}': {string.Join("; ", errors)}";
            issues.Add(new PluginLoadIssue(candidate.Location, manifest.Id, PluginOutcome.Invalid, reason));
            logger.LogError("Skipping plugin '{Location}': {Reason}", candidate.Location, reason);
        }

        // 3. Duplicate Ids are rejected deterministically (first by location wins).
        var deduplicated = new List<DiscoveredPlugin>();
        var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var candidate in structurallyValid.OrderBy(c => c.Location, StringComparer.Ordinal))
        {
            // Manifest presence was verified in step 2.
            var manifest = candidate.Manifest!;
            if (!seenIds.Add(manifest.Id))
            {
                var reason = $"Duplicate plugin Id '{manifest.Id}': already discovered.";
                issues.Add(new PluginLoadIssue(candidate.Location, manifest.Id, PluginOutcome.Invalid, reason));
                logger.LogError("Skipping plugin '{Location}': {Reason}", candidate.Location, reason);
                continue;
            }

            deduplicated.Add(candidate);
        }

        // 4. Compatibility gate.
        var toLoad = new List<DiscoveredPlugin>();
        foreach (var candidate in deduplicated)
        {
            // Manifest presence was verified in step 2.
            var manifest = candidate.Manifest!;
            var (verdict, reason) = CompatibilityGate.Check(manifest, hostVersion, hostConfiguration);
            switch (verdict)
            {
                case GateVerdict.Accept:
                    toLoad.Add(candidate);
                    break;
                case GateVerdict.SkipDisabled:
                    issues.Add(new PluginLoadIssue(candidate.Location, manifest.Id, PluginOutcome.SkippedDisabled, reason!));
                    logger.LogInformation("Skipping disabled plugin '{Id}'.", manifest.Id);
                    break;
                case GateVerdict.Reject:
                    issues.Add(new PluginLoadIssue(candidate.Location, manifest.Id, PluginOutcome.Rejected, reason!));
                    logger.LogError("Rejecting plugin '{Id}': {Reason}", manifest.Id, reason);
                    break;
            }
        }

        // 5. Dependency graph: unknown ids and cycles are startup errors.
        var indexed = toLoad
            .Select((candidate, index) => (Candidate: candidate, RegistrationOrder: index))
            .ToList();
        var knownIds = deduplicated
            .Select(candidate => candidate.Manifest!.Id)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        DependencyGraph.Validate(indexed, knownIds);

        // 6. Dependents of unavailable plugins (skipped, rejected, invalid) are
        // rejected as dependency-unavailable without loading (fixpoint, so
        // transitive dependents propagate regardless of discovery order).
        var loadable = new List<(DiscoveredPlugin Candidate, int RegistrationOrder)>();
        var unavailable = new HashSet<string>(
            knownIds.Where(id => !indexed.Any(entry =>
                string.Equals(entry.Candidate.Manifest!.Id, id, StringComparison.OrdinalIgnoreCase))),
            StringComparer.OrdinalIgnoreCase);
        var pending = new List<(DiscoveredPlugin Candidate, int RegistrationOrder)>(indexed);
        bool progressed;
        do
        {
            progressed = false;
            var remaining = new List<(DiscoveredPlugin Candidate, int RegistrationOrder)>();
            foreach (var entry in pending)
            {
                // Manifest presence was verified in step 2.
                var manifest = entry.Candidate.Manifest!;
                var blockedBy = manifest.DependsOn
                    .FirstOrDefault(dependency => unavailable.Contains(dependency));
                if (blockedBy is null)
                {
                    remaining.Add(entry);
                    continue;
                }

                var reason = $"Dependency '{blockedBy}' is unavailable; plugin '{manifest.Id}' cannot load.";
                issues.Add(new PluginLoadIssue(entry.Candidate.Location, manifest.Id, PluginOutcome.Rejected, reason));
                logger.LogError("Rejecting plugin '{Id}': {Reason}", manifest.Id, reason);
                unavailable.Add(manifest.Id);
                progressed = true;
            }

            pending = remaining;
        }
        while (progressed);

        loadable.AddRange(pending);

        // 7. Topological order: dependencies first, ties by priority then registration.
        var ordered = DependencyGraph.Sort(loadable);

        // 8. Load (construct only) in dependency order, one candidate at a time
        // so loader failures attribute exactly. Dependents of failed plugins
        // are rejected as dependency-unavailable without loading.
        var accepted = new List<LoadedPlugin>();
        var failedIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var candidate in ordered)
        {
            // Manifest presence was verified in step 2.
            var orderedManifest = candidate.Manifest!;
            var missing = orderedManifest.DependsOn
                .FirstOrDefault(dependency => failedIds.Contains(dependency));
            if (missing is not null)
            {
                var reason = $"Dependency '{missing}' is unavailable; plugin '{orderedManifest.Id}' cannot load.";
                issues.Add(new PluginLoadIssue(candidate.Location, orderedManifest.Id, PluginOutcome.Rejected, reason));
                logger.LogError("Rejecting plugin '{Id}': {Reason}", orderedManifest.Id, reason);
                failedIds.Add(orderedManifest.Id);
                continue;
            }

            var loaded = await loader.LoadAsync([candidate], cancellationToken);
            if (loaded.Count == 0)
            {
                issues.Add(new PluginLoadIssue(
                    candidate.Location,
                    orderedManifest.Id,
                    PluginOutcome.Invalid,
                    "Loader failed to construct the plugin instance."));
                failedIds.Add(orderedManifest.Id);
                continue;
            }

            var contract = loaded[0];

            // 8a. Manifest ↔ instance consistency.
            try
            {
                PluginValidator.ValidateConsistency(contract.Manifest, contract.Instance);
            }
            catch (Exception ex)
            {
                var reason = $"Manifest/instance mismatch for plugin '{contract.Manifest.Id}': {ex.Message}";
                issues.Add(new PluginLoadIssue(candidate.Location, contract.Manifest.Id, PluginOutcome.Invalid, reason));
                logger.LogError("Rejecting plugin '{Id}': {Reason}", contract.Manifest.Id, reason);
                failedIds.Add(orderedManifest.Id);
                continue;
            }

            // 8b. Full contract validation. Throws on violation.
            try
            {
                PluginContractValidator.Validate(contract.Instance, logger);
            }
            catch (Exception ex)
            {
                var reason = $"Contract validation failed for plugin '{contract.Manifest.Id}': {ex.Message}";
                issues.Add(new PluginLoadIssue(candidate.Location, contract.Manifest.Id, PluginOutcome.Invalid, reason));
                logger.LogError("Rejecting plugin '{Id}': {Reason}", contract.Manifest.Id, reason);
                failedIds.Add(orderedManifest.Id);
                continue;
            }

            accepted.Add(new LoadedPlugin(
                contract.Instance,
                contract.PluginType.Assembly,
                candidate.Location,
                contract.Manifest));

            logger.LogInformation(
                "Loaded plugin '{Name}' v{Version} from {Dir}",
                contract.Instance.Name, contract.Instance.Version, candidate.Location);
        }

        return new PluginLoadResult(accepted, issues);
    }
}
