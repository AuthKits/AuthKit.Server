using AuthKit.Plugins.Abstractions.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shield.Options;
using Shield.State;

namespace Shield.Health;

/// <summary>
/// Builds the structured health report from the lockdown snapshot.
/// Unhealthy while a lockdown is active, with reason and metrics attached.
/// </summary>
public static class ShieldHealth
{
    /// <summary>
    /// Reports the current Shield health.
    /// </summary>
    public static IReadOnlyList<PluginHealthResult> Report(
        IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var state = services.GetService<IShieldState>();
        var options = services.GetService<IOptions<ShieldOptions>>()?.Value;
        var snapshot = state is null || options is null ? null : State.ShieldStatus.GetSnapshot(state, options);

        var data = new Dictionary<string, object>
        {
            ["locked"] = snapshot?.IsLocked ?? false,
            ["evaluated"] = snapshot?.TotalEvaluated ?? 0L,
            ["blocked"] = snapshot?.TotalBlocked ?? 0L,
            ["trips"] = snapshot?.TotalTrips ?? 0L,
            ["auth_failures"] = snapshot?.TotalAuthFailures ?? 0L,
            ["probes"] = snapshot?.TotalProbes ?? 0L,
        };

        if (snapshot is { IsLocked: true })
        {
            data["locked_until"] = snapshot.LockedUntil?.ToString("O") ?? "?";
            data["reason"] = snapshot.Reason ?? "lockdown";
            return
            [
                new(
                    PluginHealthStatus.Unhealthy,
                    $"Shield lockdown active ({snapshot.Reason}).",
                    data,
                    ["security", "critical"])
            ];
        }

        return
        [
            new(
                PluginHealthStatus.Healthy,
                "Shield is armed, no lockdown active.",
                data,
                ["security", "readiness"])
        ];
    }
}
