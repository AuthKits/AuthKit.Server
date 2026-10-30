using Grpc.Core;
using System.Threading.Channels;
using Microsoft.Extensions.Options;
using Shield.Options;
using Shield.Security;
using Shield.State;

namespace Shield.Grpc;

/// <summary>
/// gRPC operator surface for Shield: status and incidents. Served by the host
/// like any plugin-owned gRPC service, so it shows up in the service catalog
/// and grpc-ui.
/// </summary>
public sealed class ShieldStatusService(IShieldState state, IOptions<ShieldOptions> options) : ShieldService.ShieldServiceBase
{
    /// <summary>
    /// Returns the current lockdown state and metrics.
    /// </summary>
    public override Task<ShieldStatus> GetStatus(GetStatusRequest request, ServerCallContext context)
    {
        AuthorizeRead(context);
        var snapshot = State.ShieldStatus.GetSnapshot(state, options.Value);
        return Task.FromResult(new ShieldStatus
        {
            Locked = snapshot.IsLocked,
            LockedUntil = snapshot.LockedUntil?.ToString("O") ?? string.Empty,
            Reason = snapshot.Reason ?? string.Empty,
            TrackedClients = snapshot.TrackedClients,
            Evaluated = snapshot.TotalEvaluated,
            Blocked = snapshot.TotalBlocked,
            Trips = snapshot.TotalTrips,
            AuthFailures = snapshot.TotalAuthFailures,
            Probes = snapshot.TotalProbes,
        });
    }

    /// <summary>
    /// Returns recent incidents, newest first.
    /// </summary>
    public override Task<ListIncidentsResponse> ListIncidents(ListIncidentsRequest request, ServerCallContext context)
    {
        AuthorizeRead(context);
        var take = request.Take is > 0 ? request.Take : options.Value.MaxIncidents;
        var response = new ListIncidentsResponse();
        response.Incidents.AddRange(state.GetIncidents(take).Select(Map));
        return Task.FromResult(response);
    }

    /// <summary>
    /// Streams the incident backlog, then live incidents until cancelled.
    /// </summary>
    public override async Task WatchIncidents(
        WatchIncidentsRequest request,
        IServerStreamWriter<ShieldIncident> responseStream,
        ServerCallContext context)
    {
        AuthorizeRead(context);
        // Subscribe first so nothing is missed, then replay the backlog.
        // An incident landing in between may be delivered twice; consumers
        // dedupe by timestamp + sensor + reason.
        var channel = Channel.CreateUnbounded<State.ShieldIncident>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false,
        });
        using var subscription = state.SubscribeIncidents(incident => channel.Writer.TryWrite(incident));

        var take = request.Take is > 0 ? request.Take : options.Value.MaxIncidents;
        foreach (var incident in state.GetIncidents(take).Reverse())
            await responseStream.WriteAsync(Map(incident));

        await foreach (var incident in channel.Reader.ReadAllAsync(context.CancellationToken))
            await responseStream.WriteAsync(Map(incident));
    }

    /// <summary>
    /// Unlocks an active lockdown on a valid release token.
    /// </summary>
    public override Task<ReleaseLockdownResponse> ReleaseLockdown(ReleaseLockdownRequest request, ServerCallContext context)
    {
        var config = options.Value;
        if (!ReleaseGuard.IsEnabled(config))
            throw new RpcException(new Status(StatusCode.NotFound, "Unknown method."));

        var token = context.RequestHeaders.FirstOrDefault(entry =>
            string.Equals(entry.Key, ReleaseGuard.MetadataKey, StringComparison.OrdinalIgnoreCase))?.Value;
        if (!ReleaseGuard.IsAuthorized(config, token))
            throw new RpcException(new Status(StatusCode.PermissionDenied, "Invalid release token."));

        if (config.ForceLockdown)
            throw new RpcException(new Status(StatusCode.FailedPrecondition, "Clear ForceLockdown in configuration before releasing lockdown."));

        state.ReleaseLockdown(config.MaxIncidents);
        return Task.FromResult(new ReleaseLockdownResponse { Released = true });
    }

    private static ShieldIncident Map(State.ShieldIncident incident) => new()
    {
        Timestamp = incident.Timestamp.ToString("O"),
        Sensor = incident.Sensor,
        Reason = incident.Reason,
        Action = incident.Action,
    };

    private void AuthorizeRead(ServerCallContext context)
    {
        if (!ReleaseGuard.IsOperatorEnabled(options.Value))
            throw new RpcException(new Status(StatusCode.NotFound, "Unknown method."));

        var token = context.RequestHeaders.FirstOrDefault(entry =>
            string.Equals(entry.Key, ReleaseGuard.MetadataKey, StringComparison.OrdinalIgnoreCase))?.Value;
        if (!ReleaseGuard.IsOperatorAuthorized(options.Value, token))
            throw new RpcException(new Status(StatusCode.PermissionDenied, "Invalid operator token."));
    }
}
