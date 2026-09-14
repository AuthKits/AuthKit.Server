using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using AuthKit.Plugins.Abstractions.Contracts;
using AuthKit.Plugins.Abstractions.Contracts.Plugins;
using Host.Configuration.Authentication;
using Host.Plugins.Loading;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace AuthKit.Host.Tests;

/// <summary>
/// Verifies a plugin-declared authentication scheme and authorization policy
/// protect a plugin-mapped endpoint through the standard ASP.NET Core pipeline.
/// </summary>
public sealed class PluginSecurityEndpointIntegrationTests
{
    [Fact]
    public async Task PluginSchemeAndPolicy_ProtectPluginEndpoint()
    {
        var plugins = new[]
        {
            new LoadedPlugin(new EndpointSecurityPlugin(), typeof(EndpointSecurityPlugin).Assembly, "endpoint-security")
        };

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddKeycloakServices(plugins);

        await using var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();

        plugins[0].Plugin.MapEndpoints(app);

        await app.StartAsync();
        var client = app.GetTestServer().CreateClient();

        var unauthenticated = await client.GetAsync("/plugin-secure");
        Assert.Equal(HttpStatusCode.Unauthorized, unauthenticated.StatusCode);

        var authenticated = await client.GetAsync("/plugin-secure?token=valid");
        Assert.Equal(HttpStatusCode.OK, authenticated.StatusCode);
        Assert.Equal("ok", await authenticated.Content.ReadAsStringAsync());
    }

    [PluginMetadata(
        "authkit.endpointsecurity",
        "1.0.0",
        [],
        [],
        [],
        name: "Endpoint Security",
        description: "Security endpoint test")]
    private sealed class EndpointSecurityPlugin : IAuthKitPlugin
    {
        public void ConfigureAuthentication(AuthenticationBuilder builder) =>
            builder.AddScheme<AuthenticationSchemeOptions, EndpointAuthenticationHandler>(
                "PluginEndpointScheme", _ => { });

        public void ConfigureAuthorization(AuthorizationOptions options) =>
            options.AddPolicy(
                "plugin.read",
                policy => policy
                    .RequireAuthenticatedUser()
                    .AddAuthenticationSchemes("PluginEndpointScheme"));

        public void MapEndpoints(IEndpointRouteBuilder endpoints) =>
            endpoints.MapGet("/plugin-secure", () => Results.Text("ok"))
                .RequireAuthorization("plugin.read");
    }

    private sealed class EndpointAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (Context.Request.Query["token"].FirstOrDefault() == "valid")
            {
                var identity = new ClaimsIdentity(
                    new[] { new Claim(ClaimTypes.Name, "security-tester") },
                    Scheme.Name);
                var principal = new ClaimsPrincipal(identity);
                return Task.FromResult(AuthenticateResult.Success(
                    new AuthenticationTicket(principal, Scheme.Name)));
            }

            return Task.FromResult(AuthenticateResult.NoResult());
        }
    }
}