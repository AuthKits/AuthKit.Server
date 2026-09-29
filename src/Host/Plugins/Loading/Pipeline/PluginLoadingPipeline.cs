using AuthKit.Plugins.Abstractions.Contracts;
using AuthKit.Plugins.Abstractions.Contracts.Discovery;
using AuthKit.Plugins.Abstractions.Models;
using Host.Plugins.Contract;
using Host.Plugins.Loading.Gate;
using Host.Plugins.Loading.Manifest;
using Host.Plugins.Loading.Results;

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
public sealed class PluginLoadingPipeline(
    IPluginDiscoverer discoverer,
    IPluginLoader loader,
    ILogger logger,
    SemanticVersion? hostVersion)
{
    /// <summary>
    /// Runs the full pipeline and returns accepted plugins with diagnostics.
    /// </summary>
    /// <param name="cancellationToken">Stops the pipeline between candidates.</param>
    /// <returns>Accepted plugins plus one issue per rejected candidate.</returns>
    /// <remarks>
    /// Stage order per candidate: discovery error -> structural validation ->
    /// duplicate ID -> compatibility gate -> load -> manifest consistency ->
    /// contract validation. The first failing stage reports the issue later
    /// stages never run for that candidate.
    /// </remarks>
    public async Task<PluginLoadResult> RunAsync(CancellationToken cancellationToken = default)
    {
        var issues = new List<PluginLoadIssue>();
        var discovered = new List<DiscoveredPlugin>();

        await foreach (var candidate in discoverer.DiscoverAsync(cancellationToken))
            discovered.Add(candidate);

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
            var (verdict, reason) = CompatibilityGate.Check(manifest, hostVersion);
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

        // 5. Load (construct only), one candidate at time so loader failures
        // attribute exactly.
        var accepted = new List<LoadedPlugin>();
        foreach (var candidate in toLoad)
        {
            var loaded = await loader.LoadAsync([candidate], cancellationToken);
            if (loaded.Count == 0)
            {
                // Manifest presence was verified in step 2.
                issues.Add(new PluginLoadIssue(
                    candidate.Location,
                    candidate.Manifest!.Id,
                    PluginOutcome.Invalid,
                    "Loader failed to construct the plugin instance."));
                continue;
            }

            var contract = loaded[0];

            // 6a. Manifest ↔ instance consistency.
            try
            {
                PluginValidator.ValidateConsistency(contract.Manifest, contract.Instance);
            }
            catch (Exception ex)
            {
                var reason = $"Manifest/instance mismatch for plugin '{contract.Manifest.Id}': {ex.Message}";
                issues.Add(new PluginLoadIssue(candidate.Location, contract.Manifest.Id, PluginOutcome.Invalid, reason));
                logger.LogError("Rejecting plugin '{Id}': {Reason}", contract.Manifest.Id, reason);
                continue;
            }

            // 6b. Full contract validation. Throws on violation.
            try
            {
                PluginContractValidator.Validate(contract.Instance, logger);
            }
            catch (Exception ex)
            {
                var reason = $"Contract validation failed for plugin '{contract.Manifest.Id}': {ex.Message}";
                issues.Add(new PluginLoadIssue(candidate.Location, contract.Manifest.Id, PluginOutcome.Invalid, reason));
                logger.LogError("Rejecting plugin '{Id}': {Reason}", contract.Manifest.Id, reason);
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
