using System.Collections.Concurrent;
using System.Diagnostics;
using AuthKit.Plugins.Abstractions.Models;
using Microsoft.Extensions.Options;

namespace Host.Plugins;

/// <summary>
/// Executes plugin health checks in disposable scopes and caches completed results.
/// </summary>
/// <remarks>
/// <para>
/// Every uncached execution receives dedicated asynchronous dependency
/// injection scope. The scope remains alive until the plugin check completes and
/// is disposed even when the check fails or is cancelled.
/// </para>
/// <para>
/// Refreshes for the same plugin are serialized so concurrent requests share one
/// completed cache entry. Different plugins may refresh concurrently.
/// </para>
/// </remarks>
/// <param name="serviceProvider">The root provider used to create health check scopes.</param>
/// <param name="options">The host health execution and cache configuration.</param>
public sealed class PluginHealthExecutor(
    IServiceProvider serviceProvider,
    IOptions<PluginHealthExecutionOptions> options)
{
    private readonly ConcurrentDictionary<string, CachedPluginHealthExecution> _cache = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _refreshGates = new(StringComparer.Ordinal);
    private readonly TimeSpan _cacheTtl = ValidateTtl(options.Value.CacheTtl);

    /// <summary>
    /// Executes or retrieves the cached health result for plugin.
    /// </summary>
    /// <param name="plugin">The plugin whose health is being checked.</param>
    /// <param name="cancellationToken">A token that cancels waiting or execution.</param>
    /// <returns>The structured health result and host-observed duration.</returns>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the wait or plugin health check is cancelled.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when a plugin returns no health results.
    /// </exception>
    public async Task<PluginHealthExecutionResult> ExecuteAsync(
        LoadedPlugin plugin,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(plugin);

        if (TryGetCached(plugin.Plugin.Id, out var cached))
            return cached.ToExecutionResult();

        var gate = _refreshGates.GetOrAdd(plugin.Plugin.Id, static _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken);
        try
        {
            if (TryGetCached(plugin.Plugin.Id, out cached))
                return cached.ToExecutionResult();

            var stopwatch = Stopwatch.StartNew();
            IReadOnlyList<PluginHealthResult> results;
            await using (var scope = serviceProvider.CreateAsyncScope())
            {
                results = await plugin.Plugin.CheckHealthAsync(
                    scope.ServiceProvider,
                    cancellationToken);
            }

            if (results is null || results.Count == 0)
                throw new InvalidOperationException(
                    $"Plugin '{plugin.Plugin.Id}' returned no health results.");

            stopwatch.Stop();
            var execution = new PluginHealthExecutionResult(results, stopwatch.Elapsed);
            if (_cacheTtl > TimeSpan.Zero)
            {
                _cache[plugin.Plugin.Id] = new CachedPluginHealthExecution(
                    execution.Results,
                    execution.Duration,
                    DateTimeOffset.UtcNow.Add(_cacheTtl));
            }

            return execution;
        }
        finally
        {
            gate.Release();
        }
    }

    private bool TryGetCached(string pluginId, out CachedPluginHealthExecution cached)
    {
        if (_cacheTtl > TimeSpan.Zero
            && _cache.TryGetValue(pluginId, out cached!)
            && cached.ExpiresAt > DateTimeOffset.UtcNow)
            return true;

        _cache.TryRemove(pluginId, out _);
        cached = null!;
        return false;
    }

    private static TimeSpan ValidateTtl(TimeSpan ttl) =>
        ttl < TimeSpan.Zero
            ? throw new ArgumentOutOfRangeException(nameof(ttl), "Health cache TTL cannot be negative.")
            : ttl;
}