using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Newtonsoft.Json.Linq;
using Host.Plugins.Loading;
using Microsoft.AspNetCore.Authentication;

namespace Host.Configuration.Authentication;

/// <summary>
/// Provides configuration for Keycloak-based JWT authentication.
/// </summary>
/// <remarks>
/// <para>
/// Configures ASP.NET Core JWT bearer authentication using a Keycloak realm
/// as the token authority.
/// Keycloak configuration is read from environment variables and defaults to
/// the local AuthKit development environment when values are not provided.
/// </para>
/// <para>
/// Keycloak client roles contained in the <c>resource_access</c> claim are
/// converted into standard <see cref="ClaimTypes.Role"/> claims so they can
/// be consumed by ASP.NET Core authorization policies.
/// </para>
/// </remarks>
public static class KeycloakConfiguration
{
    /// <summary>
    /// Registers and configures Keycloak JWT bearer authentication.
    /// </summary>
    /// <param name="services">The service collection used to register authentication services.</param>
    /// <param name="plugins"></param>
    /// <remarks>
    /// Reads the Keycloak URL, realm, and client identifier from the
    /// <c>KEYCLOAK_URL</c>, <c>KEYCLOAK_REALM</c>, and
    /// <c>KEYCLOAK_CLIENT_ID</c> environment variables respectively.
    /// </remarks>
    public static void AddKeycloakServices(
        this IServiceCollection services,
        IReadOnlyList<LoadedPlugin> plugins)
    {
        var baseUrl = Environment.GetEnvironmentVariable("KEYCLOAK_URL") ?? "http://keycloak:8080";
        var realm = Environment.GetEnvironmentVariable("KEYCLOAK_REALM") ?? "authz";
        var clientId = Environment.GetEnvironmentVariable("KEYCLOAK_CLIENT_ID") ?? "workspace-authz";

        JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = $"{baseUrl}/realms/{realm}";
                options.Audience = clientId;
                options.RequireHttpsMetadata = false;

                options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuers =
                    [
                        "http://localhost:8081/realms/authz",
                        "http://keycloak:8080/realms/authz"
                    ],
                    ValidateAudience = true,
                    ValidAudience = clientId,
                    NameClaimType = "preferred_username",
                    RoleClaimType = ClaimTypes.Role
                };

                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = context =>
                    {
                        if (context.Principal?.Identity
                            is ClaimsIdentity identity)
                        {
                            AddKeycloakClientRoles(identity);
                        }
                        return Task.CompletedTask;
                    }
                };

                options.BackchannelHttpHandler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback =
                        HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                };
            });

        foreach (var plugin in plugins.OrderBy(plugin => plugin.Plugin.Id, StringComparer.Ordinal))
            plugin.Plugin.ConfigureAuthentication(
                new AuthenticationBuilder(services));

        services.AddAuthorization(options =>
        {
            AuthorizationPolicyCollisionGuard.Configure(options, plugins);
        });
    }

    /// <summary>
    /// Extracts roles assigned to the configured Keycloak client from the
    /// <c>resource_access</c> token claim and adds them as ASP.NET Core role
    /// claims.
    /// </summary>
    /// <param name="identity">
    /// The authenticated claims identity to which role claims are added.
    /// </param>
    private static void AddKeycloakClientRoles(ClaimsIdentity identity)
    {
        var resourceAccessClaim = identity.FindFirst("resource_access")?.Value;

        if (string.IsNullOrWhiteSpace(resourceAccessClaim))
            return;

        var resourceAccess = JObject.Parse(resourceAccessClaim);

        if (!resourceAccess.TryGetValue(
            "workspace-authz",
             out var workspaceClient))
        {
            return;
        }

        var roles = workspaceClient["roles"]?.ToObject<string[]>();

        if (roles is null)
            return;

        identity.AddClaims(
            roles.Select(role =>
                new Claim(
                    ClaimTypes.Role,
                    role)));
    }
}
