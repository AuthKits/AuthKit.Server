using AuthKit.Plugins.Abstractions.Contracts.SecuritySchemes;
using ExamplePlugin.Grpc;
using ExamplePlugin.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using IAuthKitPlugin = AuthKit.Plugins.Abstractions.Contracts.PluginContract.IAuthKitPlugin;

namespace ExamplePlugin;

public sealed partial class ExamplePlugin : IAuthKitPlugin
{
    /// <summary>
    /// Registers the reference endpoints on the application's route builder.
    /// </summary>
    /// <param name="endpoints">The application's endpoint route builder.</param>
    /// <remarks>
    /// Endpoints protected by <see cref="SecuritySchemeAttribute"/> participate in the host's
    /// request-aware security resolution. The scheme name must match a key returned by
    /// <see cref="GetSecuritySchemes"/>.
    /// </remarks>
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/example/hello", ([FromServices] IOptions<ExampleOptions> options) =>
                Results.Ok(new { options.Value.Greeting }))
            .WithMetadata(new SecuritySchemeAttribute("example-api-key"));

        endpoints.MapGrpcService<ExampleGreeterService>();
    }
}
