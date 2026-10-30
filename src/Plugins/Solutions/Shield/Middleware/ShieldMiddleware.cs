using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Shield.Detection;
using Shield.Options;
using Shield.State;

namespace Shield.Middleware;

/// <summary>
/// First-line HTTP defense. Orchestrates evaluation: allow/block lists,
/// bypass paths, manual and active lockdowns, then the registered
/// <see cref="IAttackDetector"/> strategies in order. Blocked requests receive
/// 503 with a <c>Retry-After</c> hint and never reach routing, authentication,
/// or endpoints.
/// </summary>
public sealed class ShieldMiddleware(RequestDelegate next)
{
    /// <summary>
    /// Evaluates the request against lists, lockdown state, and detectors.
    /// </summary>
    public async Task InvokeAsync(
        HttpContext context,
        IShieldState state,
        IEnumerable<IAttackDetector> detectors,
        IOptions<ShieldOptions> options)
    {
        var config = options.Value;
        var clientId = ClientIdResolver.Resolve(context, config.TrustForwardedHeader, config.TrustedProxyHops);

        if (IsBypassed(context, config))
        {
            await next(context);
            return;
        }

        state.CountEvaluation();

        if (config.ForceLockdown)
        {
            state.CountBlock();
            await BlockAsync(context, TimeSpan.FromSeconds(config.LockdownSeconds), "manual kill-switch");
            return;
        }

        if (state.IsLockdownActive(out var lockedUntil, out var reason))
        {
            state.CountBlock();
            await BlockAsync(context, lockedUntil - DateTimeOffset.UtcNow, reason ?? "lockdown");
            return;
        }

        if (IsListed(clientId, config.AllowedIps))
        {
            await next(context);
            return;
        }

        if (state.IsClientBlocked(clientId, out var blockedUntil, out var clientReason))
        {
            state.CountBlock();
            await BlockAsync(context, blockedUntil - DateTimeOffset.UtcNow, clientReason ?? "client blocked");
            return;
        }

        if (IsListed(clientId, config.BlockedIps))
        {
            state.CountBlock();
            await BlockAsync(context, TimeSpan.FromSeconds(config.LockdownSeconds), "blocklisted client");
            return;
        }

        var request = new ShieldRequest(
            clientId,
            context.Request.Path.Value ?? "/",
            context.Request.QueryString.Value ?? "",
            DateTimeOffset.UtcNow,
            context.Request.Headers.UserAgent.ToString(),
            context.Request.ContentLength);
        foreach (var signal in detectors.Select(detector => detector.Evaluate(request, config)))
        {
            if (signal is null)
                continue;

            await ApplySignalAsync(context, state, config, clientId, signal);
            return;
        }

        await next(context);

        // A post-response signal blocks this client on subsequent requests.
        foreach (var detector in detectors)
        {
            var signal = detector.ObserveResponse(request, context.Response.StatusCode, config);
            if (signal is null || signal.Action != ShieldAction.Lockdown)
                continue;

            state.RecordOffender(
                clientId,
                signal.Sensor,
                signal.Reason,
                TimeSpan.FromSeconds(config.LockdownSeconds),
                config.MaxTrackedClients,
                config.MinOffendingClientsForLockdown,
                config.MaxIncidents);
        }
    }

    private static Task ApplySignalAsync(
        HttpContext context,
        IShieldState state,
        ShieldOptions config,
        string clientId,
        AttackSignal signal)
    {
        var duration = TimeSpan.FromSeconds(config.LockdownSeconds);
        if (signal.Action == ShieldAction.Lockdown)
        {
            state.RecordOffender(clientId, signal.Sensor, signal.Reason, duration,
                config.MaxTrackedClients, config.MinOffendingClientsForLockdown, config.MaxIncidents);
            state.CountBlock();
            return BlockAsync(context, duration, signal.Reason);
        }

        state.RecordIncident(signal.Sensor, signal.Reason, "request-blocked", config.MaxIncidents);
        state.CountBlock();
        return BlockAsync(context, duration, signal.Reason);
    }

    private static bool IsListed(string clientId, string[] list) =>
        list.Any(entry => string.Equals(entry, clientId, StringComparison.Ordinal));

    private static bool IsBypassed(HttpContext context, ShieldOptions config) =>
        config.BypassPaths.Any(path =>
            context.Request.Path.Equals(path, StringComparison.OrdinalIgnoreCase));

    private static Task BlockAsync(HttpContext context, TimeSpan retryAfter, string reason)
    {
        var seconds = Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds));
        context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        context.Response.Headers.RetryAfter = seconds.ToString();
        context.Response.Headers["X-Shield"] = "lockdown";
        return context.Response.WriteAsync($"Service unavailable: traffic blocked by Shield ({reason}).");
    }
}
