using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.Logging;

namespace ExamplePlugin.Grpc;

/// <summary>
/// Reference gRPC interceptor contributed declaratively.
/// Streaming RPCs are supported through the base <see cref="Interceptor"/>
/// overloads; only unary is customized here.
/// </summary>
public sealed class ExampleLoggingInterceptor(ILogger<ExampleLoggingInterceptor> logger) : Interceptor
{
    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        logger.LogDebug("Example gRPC call started: {Method}.", context.Method);
        var response = await continuation(request, context);
        
        // Post endpoint processing (AfterEndpointExecution semantic position).
        logger.LogDebug("Example gRPC call finished: {Method}.", context.Method);
        return response;
    }
}
