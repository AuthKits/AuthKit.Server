using AuthKit.Plugins.Abstractions;
using AuthKit.Plugins.Abstractions.Contracts.SecuritySchemes;
using AuthKit.Plugins.Abstractions.Models;
using ExamplePlugin.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using IAuthKitPlugin = AuthKit.Plugins.Abstractions.Contracts.PluginContract.IAuthKitPlugin;

namespace ExamplePlugin;

public sealed partial class ExamplePlugin : IAuthKitPlugin
{
    /// <summary>
    /// Contributes the plugin's OpenAPI security scheme metadata.
    /// </summary>
    /// <returns>
    /// A readonly dictionary keyed by security scheme name. Keys must match the
    /// descriptor's <see cref="AuthKitSecuritySchemeDescriptor.Name"/>.
    /// </returns>
    public IReadOnlyDictionary<string, AuthKitSecuritySchemeDescriptor> GetSecuritySchemes() =>
        new Dictionary<string, AuthKitSecuritySchemeDescriptor>
        {
            ["example-api-key"] = new()
            {
                Name = "example-api-key",
                Type = AuthKitSecuritySchemeType.ApiKey,
                In = AuthKitApiKeyLocation.Header,
                CredentialName = "X-Example-Api-Key",
                Description = "Reference API key scheme contributed by ExamplePlugin."
            }
        };

    /// <summary>
    /// Registers the reference API key authentication scheme on the host builder.
    /// </summary>
    /// <param name="builder">The host authentication builder.</param>
    /// <remarks>
    /// The scheme is registered without changing the host default scheme. See
    /// <see cref="ExampleApiKeyAuthenticationHandler"/> for the minimal handler.
    /// </remarks>
    public void ConfigureAuthentication(AuthenticationBuilder builder)
    {
        builder.AddScheme<AuthenticationSchemeOptions, ExampleApiKeyAuthenticationHandler>(
            ExampleApiKeyAuthenticationHandler.SchemeName,
            _ => { });
    }

    /// <summary>
    /// Registers a reference authorization policy used by plugin endpoints.
    /// </summary>
    /// <param name="options">The host authorization options.</param>
    /// <remarks>
    /// Policy names are globally significant; use namespaced names to avoid collisions
    /// with the host or other plugins.
    /// </remarks>
    public void ConfigureAuthorization(AuthorizationOptions options)
    {
        options.AddPolicy(
            "example.read",
            policy => policy
                .RequireAuthenticatedUser()
                .AddAuthenticationSchemes(ExampleApiKeyAuthenticationHandler.SchemeName));
    }
}
