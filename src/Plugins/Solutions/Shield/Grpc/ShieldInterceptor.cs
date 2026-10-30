using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.Options;
using Shield.Options;
using Shield.State;

namespace Shield.Grpc;

/// <summary>
/// gRPC counterpart of the HTTP kill-switch: fails every call with
/// <c>Unavailable</c> while a lockdown is active or forced.
/// The plugin's own operator service (<c>ShieldService</c>) is exempt, mirroring
/// the HTTP bypass paths, so status stays observable during a lockdown.
/// </summary>
public sealed class ShieldInterceptor(IShieldState state, IOptions<ShieldOptions> options) : Interceptor
{
    private const string OperatorServicePrefix = "/authkit.shield.ShieldService/";

    private void ThrowIfLocked(ServerCallContext context)
    {
        if (context.Method.StartsWith(OperatorServicePrefix, StringComparison.Ordinal))
            return;
        if (options.Value.ForceLockdown)
            throw new RpcException(new Status(StatusCode.Unavailable, "Traffic blocked by Shield (manual kill-switch)."));

        if (state.IsLockdownActive(out _, out var reason))
            throw new RpcException(new Status(StatusCode.Unavailable, $"Traffic blocked by Shield ({reason ?? "lockdown"})."));
    }

    public override Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        ThrowIfLocked(context);
        return continuation(request, context);
    }

    public override Task<TResponse> ClientStreamingServerHandler<TRequest, TResponse>(
        IAsyncStreamReader<TRequest> requestStream,
        ServerCallContext context,
        ClientStreamingServerMethod<TRequest, TResponse> continuation)
    {
        ThrowIfLocked(context);
        return continuation(requestStream, context);
    }

    public override Task ServerStreamingServerHandler<TRequest, TResponse>(
        TRequest request,
        IServerStreamWriter<TResponse> responseStream,
        ServerCallContext context,
        ServerStreamingServerMethod<TRequest, TResponse> continuation)
    {
        ThrowIfLocked(context);
        return continuation(request, responseStream, context);
    }

    public override Task DuplexStreamingServerHandler<TRequest, TResponse>(
        IAsyncStreamReader<TRequest> requestStream,
        IServerStreamWriter<TResponse> responseStream,
        ServerCallContext context,
        DuplexStreamingServerMethod<TRequest, TResponse> continuation)
    {
        ThrowIfLocked(context);
        return continuation(requestStream, responseStream, context);
    }
}
