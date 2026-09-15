using ExamplePlugin.Options;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ExamplePlugin.Grpc;

/// <summary>
/// Demonstrates a plugin-owned gRPC service.
/// </summary>
/// <remarks>
/// The service resolves the plugin's <see cref="ExampleOptions"/> through the
/// options infrastructure and echoes the configured greeting, mirroring the
/// host's <c>GreeterService</c> pattern.
/// </remarks>
public sealed class ExampleGreeterService(IOptions<ExampleOptions> options, ILogger<ExampleGreeterService> logger)
    : ExampleGreeter.ExampleGreeterBase
{
    /// <inheritdoc />
    public override Task<ExampleHelloReply> SayHello(ExampleHelloRequest request, ServerCallContext context)
    {
        logger.LogInformation("Example gRPC greeting received from {Name}", request.Name);

        return Task.FromResult(new ExampleHelloReply
        {
            Message = $"{options.Value.Greeting} {request.Name}"
        });
    }
}