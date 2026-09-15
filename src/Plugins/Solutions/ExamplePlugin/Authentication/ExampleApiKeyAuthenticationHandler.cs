using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ExamplePlugin.Authentication;

/// <summary>
/// Reference authentication handler demonstrating how a plugin contributes its own
/// authentication scheme without replacing the host's default handlers.
/// </summary>
/// <remarks>
/// The handler is intentionally minimal: it accepts requests carrying the
/// <c>X-Example-Api-Key</c> header and authenticates them as the <c>ExampleUser</c>
/// principal. Real plugins validate the credential against their own storage or
/// signing infrastructure.
/// </remarks>
public sealed class ExampleApiKeyAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Example.ApiKey";

    private const string ApiKeyHeader = "X-Example-Api-Key";

    /// <inheritdoc />
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(ApiKeyHeader, out var header) || string.IsNullOrWhiteSpace(header))
            return Task.FromResult(AuthenticateResult.NoResult());

        var principal = new ClaimsPrincipal(
            new ClaimsIdentity(
                [new Claim(ClaimTypes.Name, header!)],
                SchemeName));

        var ticket = new AuthenticationTicket(principal, SchemeName);

        Logger.LogDebug("Authenticated example API key for {Scheme}.", SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    /// <inheritdoc />
    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = StatusCodes.Status401Unauthorized;
        Response.ContentType = "application/json";
        return Response.WriteAsync(
            JsonSerializer.Serialize(new { error = "A valid API key is required." }),
            Context.RequestAborted);
    }
}