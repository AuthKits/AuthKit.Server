using DevTokens.Options;
using AuthKit.Plugins.Abstractions;
using AuthKit.Plugins.Abstractions.Contracts;
using AuthKit.Plugins.Abstractions.Contracts.Plugins;
using AuthKit.Plugins.Abstractions.Contracts.SecuritySchemes;
using FluentValidation;
using DevTokens.Interfaces;
using DevTokens.Middleware;
using DevTokens.Repositories;
using DevTokens.Security;
using DevTokens.Services;
using DevTokens.UseCase.Commands.Requests;
using DevTokens.UseCase.Commands.Validations;
using Marten;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DevTokens;

/// <summary>
/// AuthKit plugin responsible for issuing, validating, and authorizing developer tokens
/// used for SDK access.
/// </summary>
/// <remarks>
/// <para>
/// The plugin provides the application services required to manage the developer token
/// lifecycle, including token creation, validation, persistence, and scope-based authorization.
/// </para>
/// <para>
/// Signing keys and token to key bindings are provided by the Host core through
/// dependency injection. The plugin does not own or manage the underlying signing-key
/// infrastructure.
/// </para>
/// </remarks>
[PluginMetadata(
    id: "authkit.devtokens",
    version: "1.0.0",
    tags: ["auth", "tokens", "sdk"],
    dependsOn: [],
    capabilities: ["auth"],
    name: "DevTokens",
    displayName: "Developer Tokens",
    description: "Issues and validates developer tokens for SDK access.",
    author: "AuthKit Contributors",
    license: "MIT",
    licenseUrl: "https://opensource.org/licenses/MIT",
    homepage: "https://example.org/devtokens",
    repositoryUrl: "https://example.org/devtokens.git"
)]
public sealed class DevTokensPlugin : IAuthKitPlugin
{
    /// <summary>
    /// Registers developer token services, repositories, validators,
    /// and authorization components in the dependency injection container.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> used to register plugin services.</param>
    /// <param name="configuration">Application configuration used to configure <see cref="AuthKitOptions"/>.</param>
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AuthKitOptions>(configuration.GetSection("AuthKit"));

        services.AddScoped<IDeveloperTokenValidator, DeveloperTokenValidatorService>();
        services.AddScoped<IDeveloperTokenManager, DeveloperTokenManager>();
        services.AddScoped<IDeveloperTokenService, DeveloperTokenService>();
        services.AddScoped<IDeveloperTokenRepository, DeveloperTokenRepository>();

        services.AddSingleton<IAuthorizationPolicyProvider, DeveloperScopePolicyProvider>();
        services.AddSingleton<IAuthorizationHandler, DeveloperScopeHandler>();

        services.AddScoped<IValidator<CreateDeveloperTokenCommand>, CreateDeveloperTokenValidator>();
        services.AddScoped<IValidator<DeleteTokenCommand>, DeleteTokenCommandValidator>();
    }

    public Type MiddlewareType => typeof(DeveloperTokenMiddleware);

    public async Task<bool> CheckHealthAsync(IServiceProvider services)
    {
        var store = services.GetService<IDocumentStore>();
        if (store is null) return false;

        try
        {
            await using var session = store.LightweightSession();
            await session.Query<DeveloperToken>().Take(1).ToListAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public IReadOnlyDictionary<string, AuthKitSecuritySchemeDescriptor> GetSecuritySchemes() =>
        new Dictionary<string, AuthKitSecuritySchemeDescriptor>
        {
            ["X-Developer-Token"] = new()
            {
                Name = "X-Developer-Token",
                Type = AuthKitSecuritySchemeType.ApiKey,
                In = AuthKitApiKeyLocation.Header,
                Description = "AuthKit developer JWT token"
            }
        };
}
