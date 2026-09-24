using Grpc.Core;
using Grpc.Core.Interceptors;
using Xunit;

namespace AuthKit.Host.Tests;

/// <summary>
/// Verifies that a plugin-contributed <see cref="Interceptor"/> (C7) handles all
/// four gRPC call patterns through the native base-class overloads — no custom
/// streaming pipeline is required.
/// </summary>
public sealed class GrpcInterceptorStreamingTests
{
    [Fact]
    public async Task UnaryServerHandler_InvokesContinuationAndPostProcesses()
    {
        var interceptor = new RecordingInterceptor();
        var context = new FakeServerCallContext();

        var response = await interceptor.UnaryServerHandler(
            "request", context, (req, _) => Task.FromResult(req + "-response"));

        Assert.Equal("request-response", response);
        Assert.True(interceptor.UnaryPostProcessed);
    }

    [Fact]
    public async Task ServerStreamingServerHandler_DispatchesToContinuation()
    {
        var interceptor = new RecordingInterceptor();
        var context = new FakeServerCallContext();
        var writer = new RecordingStreamWriter<string>();
        var dispatched = false;

        await interceptor.ServerStreamingServerHandler(
            "request", writer, context,
            (req, stream, _) =>
            {
                dispatched = true;
                return stream.WriteAsync(req + "-chunk");
            });

        Assert.True(dispatched);
        Assert.Equal(["request-chunk"], writer.Written);
    }

    [Fact]
    public async Task ClientStreamingServerHandler_DispatchesToContinuation()
    {
        var interceptor = new RecordingInterceptor();
        var context = new FakeServerCallContext();
        var reader = new RecordingStreamReader<string>(["a", "b"]);

        var response = await interceptor.ClientStreamingServerHandler(
            reader, context, (_, _) => Task.FromResult("done"));

        Assert.Equal("done", response);
    }

    [Fact]
    public async Task DuplexStreamingServerHandler_DispatchesToContinuation()
    {
        var interceptor = new RecordingInterceptor();
        var context = new FakeServerCallContext();
        var reader = new RecordingStreamReader<string>(["a"]);
        var writer = new RecordingStreamWriter<string>();
        var dispatched = false;

        await interceptor.DuplexStreamingServerHandler(
            reader, writer, context,
            (_, _, _) =>
            {
                dispatched = true;
                return Task.CompletedTask;
            });

        Assert.True(dispatched);
    }

    private sealed class RecordingInterceptor : Interceptor
    {
        public bool UnaryPostProcessed { get; private set; }

        public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
            TRequest request,
            ServerCallContext context,
            UnaryServerMethod<TRequest, TResponse> continuation)
        {
            var response = await base.UnaryServerHandler(request, context, continuation);
            UnaryPostProcessed = true;
            return response;
        }
    }

    private sealed class FakeServerCallContext : ServerCallContext
    {
        protected override string MethodCore => "authkit.example.ExampleGreeter/SayHello";
        protected override string HostCore => "localhost";
        protected override string PeerCore => "test-peer";
        protected override DateTime DeadlineCore => DateTime.UtcNow.AddMinutes(1);
        protected override Metadata RequestHeadersCore => [];
        protected override CancellationToken CancellationTokenCore => CancellationToken.None;
        protected override Metadata ResponseTrailersCore => [];
        protected override Status StatusCore { get => new(); set { } }
        protected override WriteOptions? WriteOptionsCore { get => null; set { } }
        protected override AuthContext AuthContextCore => new("test", new Dictionary<string, List<AuthProperty>>());

        protected override Task WriteResponseHeadersAsyncCore(Metadata responseHeaders) =>
            Task.CompletedTask;

        protected override ContextPropagationToken CreatePropagationTokenCore(ContextPropagationOptions? options) =>
            throw new NotImplementedException("Propagation is not used by these tests.");
    }

    private sealed class RecordingStreamReader<T>(IReadOnlyList<T> items) : IAsyncStreamReader<T>
    {
        private int _index = -1;

        public T Current => items[_index];

        public Task<bool> MoveNext(CancellationToken cancellationToken = default) =>
            Task.FromResult(++_index < items.Count);
    }

    private sealed class RecordingStreamWriter<T> : IServerStreamWriter<T>
    {
        public List<T> Written { get; } = [];

        public WriteOptions? WriteOptions { get; set; }

        public Task WriteAsync(T message)
        {
            Written.Add(message);
            return Task.CompletedTask;
        }
    }
}
