using System.Net;

using Google.Protobuf;
using Google.Protobuf.Reflection;
using Grpc.Core;
using Grpc.Net.Client;
using DevTools.Catalog;
using DevTools.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DevTools.Runtime;

/// <summary>
/// Executes unary gRPC calls discovered through the catalog without requiring
/// generated client stubs.
/// </summary>
/// <remarks>
/// <para>
/// Messages are marshaled over <see cref="IMessage"/> using the protobuf
/// descriptors returned by the server catalog, so no compiled client code is
/// needed.
/// </para>
/// <para>
/// TLS validation is relaxed because the host uses a development certificate.
/// Streaming methods are not executed and are reported as unsupported.
/// </para>
/// </remarks>
public sealed class GrpcDynamicInvoker(
    IOptions<DevToolsOptions> options,
    IGrpcServiceCatalog catalog,
    ILogger<GrpcDynamicInvoker> logger)
{
    private readonly Lazy<GrpcChannel> _channel = new(() => CreateChannel(options, logger));

    /// <summary>
    /// Invokes the requested unary method and returns a serializable result.
    /// </summary>
    /// <param name="request">The invocation request describing the method and payload.</param>
    /// <param name="cancellationToken">Cancellation token propagated to the gRPC call.</param>
    /// <returns>
    /// The <see cref="GrpcInvocationResult"/> describing the outcome of the
    /// invocation.
    /// </returns>
    public async Task<GrpcInvocationResult> InvokeAsync(
        GrpcInvocationRequest request,
        CancellationToken cancellationToken = default)
    {
        var entry = catalog.TryGetMethod(request.Service, request.Method);
        if (entry is null)
            return new GrpcInvocationResult
            {
                Success = false,
                StatusName = nameof(StatusCode.NotFound),
                StatusCode = (int)StatusCode.NotFound,
                Detail = $"Unknown method '{request.Service}/{request.Method}'."
            };

        var method = entry.Descriptor;
        if (method.IsClientStreaming || method.IsServerStreaming)
            return new GrpcInvocationResult
            {
                Success = false,
                StatusName = nameof(StatusCode.Unimplemented),
                StatusCode = (int)StatusCode.Unimplemented,
                Detail = "Streaming methods are not supported by the gRPC UI."
            };

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            var response = await InvokeUnaryAsync(method, request.Headers, request.RequestJson, cancellationToken);
            stopwatch.Stop();

            var responseBytes = response.ToByteArray();
            return new GrpcInvocationResult
            {
                Success = true,
                StatusName = nameof(StatusCode.OK),
                StatusCode = (int)StatusCode.OK,
                ResponseJson = JsonFormatter.Default.Format(response),
                ResponseBase64 = Convert.ToBase64String(responseBytes),
                ElapsedMs = Math.Round(stopwatch.Elapsed.TotalMilliseconds, 2)
            };
        }
        catch (RpcException ex)
        {
            stopwatch.Stop();
            logger.LogWarning(ex, "gRPC invocation of {Method} failed.", method.FullName);

            return new GrpcInvocationResult
            {
                Success = false,
                StatusName = ex.StatusCode.ToString(),
                StatusCode = (int)ex.StatusCode,
                Detail = ex.Status.Detail,
                Trailers = ex.Trailers.ToDictionary(t => t.Key, t => t.Value),
                ElapsedMs = Math.Round(stopwatch.Elapsed.TotalMilliseconds, 2)
            };
        }
        catch (InvalidJsonException ex)
        {
            stopwatch.Stop();
            return new GrpcInvocationResult
            {
                Success = false,
                StatusName = nameof(StatusCode.InvalidArgument),
                StatusCode = (int)StatusCode.InvalidArgument,
                Detail = $"Request JSON is invalid: {ex.Message}",
                ElapsedMs = Math.Round(stopwatch.Elapsed.TotalMilliseconds, 2)
            };
        }
        catch (InvalidProtocolBufferException ex)
        {
            stopwatch.Stop();
            return new GrpcInvocationResult
            {
                Success = false,
                StatusName = nameof(StatusCode.InvalidArgument),
                StatusCode = (int)StatusCode.InvalidArgument,
                Detail = $"Request JSON is invalid: {ex.Message}",
                ElapsedMs = Math.Round(stopwatch.Elapsed.TotalMilliseconds, 2)
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            logger.LogWarning(ex, "Unexpected failure invoking {Method}.", method.FullName);

            return new GrpcInvocationResult
            {
                Success = false,
                StatusName = nameof(StatusCode.Internal),
                StatusCode = (int)StatusCode.Internal,
                Detail = ex.Message,
                ElapsedMs = Math.Round(stopwatch.Elapsed.TotalMilliseconds, 2)
            };
        }
    }

    /// <summary>
    /// Serializes the request, builds a dynamic unary call over the shared
    /// channel, and deserializes the response into an <see cref="IMessage"/>.
    /// </summary>
    private async Task<IMessage> InvokeUnaryAsync(
        MethodDescriptor method,
        IReadOnlyDictionary<string, string> headers,
        string requestJson,
        CancellationToken cancellationToken)
    {
        var input = method.InputType;
        var output = method.OutputType;

        var requestMarshaller = new Marshaller<IMessage>(
            message => message.ToByteArray(),
            bytes => input.Parser.ParseFrom(bytes));

        var responseMarshaller = new Marshaller<IMessage>(
            message => message.ToByteArray(),
            bytes => output.Parser.ParseFrom(bytes));

        var call = new Method<IMessage, IMessage>(
            MethodType.Unary,
            method.Service.FullName,
            method.Name,
            requestMarshaller,
            responseMarshaller);

        var metadata = new Metadata();
        foreach (var (key, value) in headers)
            metadata.Add(key, value);

        var request = input.Parser.ParseJson(string.IsNullOrWhiteSpace(requestJson) ? "{}" : requestJson);
        var options = new CallOptions(metadata, cancellationToken: cancellationToken);

        var invoker = _channel.Value.CreateCallInvoker();
        var callResult = invoker.AsyncUnaryCall(call, host: null, options, request);

        return await callResult.ResponseAsync;
    }

    /// <summary>
    /// Creates the shared <see cref="GrpcChannel"/> used for invocations.
    /// </summary>
    /// <remarks>
    /// The channel is created lazily on the first invocation and is reused
    /// for every subsequent call.
    /// </remarks>
    private static GrpcChannel CreateChannel(IOptions<DevToolsOptions> options, ILogger<GrpcDynamicInvoker> logger)
    {
        var target = options.Value.ResolveGrpcTarget();

        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };

        // The host falls back to plain HTTP/2 (h2c) when no dev certificate is
        // present. gRPC over an unencrypted connection requires this switch.
        // H2C is only enabled for loopback targets to prevent sending caller-supplied
        // credentials (including bearer tokens) in plaintext to remote servers.
        if (IsLoopbackTarget(target) && target.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
        {
            AppContext.SetSwitch(
                "System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport",
                true);
        }

        logger.LogInformation("gRPC UI channel configured for target {Target}.", target);

        return GrpcChannel.ForAddress(target, new GrpcChannelOptions { HttpHandler = handler });
    }

    /// <summary>
    /// Determines whether the target URL points to a loopback address.
    /// </summary>
    /// <param name="target">The target URL.</param>
    /// <returns><c>true</c> if the target is a loopback address; otherwise, <c>false</c>.</returns>
    internal static bool IsLoopbackTarget(string target)
    {
        if (!Uri.TryCreate(target, UriKind.Absolute, out var uri))
            return false;

        var host = uri.Host;
        // Check for localhost first (doesn't parse as IP address)
        if (string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase))
            return true;

        // Then check if it's a loopback IP address
        return IPAddress.TryParse(host, out var ip) && IPAddress.IsLoopback(ip);
    }
}
