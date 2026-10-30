using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using Shield.Options;
using Shield.Security;
using Shield.State;

namespace Shield.Endpoints;

/// <summary>
/// Operator surfaces. Status and incident paths are bypassed from blocking by
/// default (<see cref="ShieldOptions.BypassPaths"/>) so they stay reachable
/// during a lockdown.
/// </summary>
public static class ShieldEndpoints
{
    /// <summary>
    /// Maps <c>/shield/status</c> (lockdown state + metrics),
    /// <c>/shield/incidents</c> (recent incidents, newest first) and
    /// <c>POST /shield/release</c> (token-guarded unlock).
    /// </summary>
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/shield/status", (HttpContext context, IShieldState state, IOptions<ShieldOptions> options) =>
        {
            var denied = AuthorizeRead(context, options.Value);
            if (denied is not null)
                return denied;

            var snapshot = State.ShieldStatus.GetSnapshot(state, options.Value);
            return (IResult)Results.Ok(new
            {
                locked = snapshot.IsLocked,
                lockedUntil = snapshot.LockedUntil,
                reason = snapshot.Reason,
                trackedClients = snapshot.TrackedClients,
                evaluated = snapshot.TotalEvaluated,
                blocked = snapshot.TotalBlocked,
                trips = snapshot.TotalTrips,
                authFailures = snapshot.TotalAuthFailures,
                probes = snapshot.TotalProbes,
            });
        });

        endpoints.MapGet("/shield/incidents", (HttpContext context, IShieldState state, IOptions<ShieldOptions> options, int? take) =>
        {
            var denied = AuthorizeRead(context, options.Value);
            if (denied is not null)
                return denied;

            var incidents = state.GetIncidents(take ?? options.Value.MaxIncidents);
            return (IResult)Results.Ok(incidents.Select(i => new
            {
                timestamp = i.Timestamp,
                sensor = i.Sensor,
                reason = i.Reason,
                action = i.Action,
            }));
        });

        endpoints.MapPost("/shield/release",
            (HttpContext context, IShieldState state, IOptions<ShieldOptions> options) =>
                Release(context, state, options));
    }

    /// <summary>
    /// Unlocks an active lockdown when the release endpoint is enabled and the
    /// <c>X-Shield-Release</c> header matches <see cref="ShieldOptions.ReleaseToken"/>
    /// in constant time. Answers 404 when disabled and 403 on token mismatch,
    /// so a disabled endpoint is indistinguishable from a missing one.
    /// </summary>
    public static IResult Release(HttpContext context, IShieldState state, IOptions<ShieldOptions> options)
    {
        var config = options.Value;
        if (!ReleaseGuard.IsEnabled(config))
            return Results.NotFound();

        if (!context.Request.Headers.TryGetValue(ReleaseGuard.HttpHeader, out var provided) ||
            !ReleaseGuard.IsAuthorized(config, provided.ToString()))
            return Results.StatusCode(StatusCodes.Status403Forbidden);

        if (config.ForceLockdown)
            return Results.Conflict(new { error = "Clear ForceLockdown in configuration before releasing lockdown." });

        state.ReleaseLockdown(config.MaxIncidents);
        return Results.Ok(new { released = true });
    }

    private static IResult? AuthorizeRead(HttpContext context, ShieldOptions options)
    {
        if (!ReleaseGuard.IsOperatorEnabled(options))
            return Results.NotFound();

        return ReleaseGuard.IsOperatorAuthorized(
            options, context.Request.Headers[ReleaseGuard.HttpHeader].ToString())
            ? null
            : Results.StatusCode(StatusCodes.Status403Forbidden);
    }
}
