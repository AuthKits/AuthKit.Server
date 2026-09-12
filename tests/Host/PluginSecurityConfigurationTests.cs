using AuthKit.Plugins.Abstractions;
using AuthKit.Plugins.Abstractions.Contracts;
using AuthKit.Plugins.Abstractions.Contracts.Plugins;
using Host.Configuration;
using Host.Plugins;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace AuthKit.Host.Tests;

/// <summary>
/// Verifies plugin authentication and authorization hooks use standard ASP.NET Core infrastructure.
/// </summary>
public sealed class PluginSecurityConfigurationTests
{
    [Fact]
    public async Task PluginSchemeAndPolicy_AreAvailableThroughHostInfrastructure()
    {
        var services = new ServiceCollection();
        var plugin = new SecurityPlugin();

        services.AddKeycloakServices([new LoadedPlugin(plugin, typeof(SecurityPlugin).Assembly, "test")]);

        using var provider = services.BuildServiceProvider();
        var schemes = provider.GetRequiredService<IAuthenticationSchemeProvider>();
        var policies = provider.GetRequiredService<IAuthorizationPolicyProvider>();

        Assert.NotNull(await schemes.GetSchemeAsync("PluginScheme"));
        Assert.NotNull(await policies.GetPolicyAsync("plugin.read"));
        Assert.Equal("Bearer", (await schemes.GetDefaultAuthenticateSchemeAsync())?.Name);
    }

    [PluginMetadata("security-plugin", "1.0.0", [], [], [], name: "Security Plugin", description: "Security test")]
    private sealed class SecurityPlugin : IAuthKitPlugin
    {
        public void ConfigureAuthentication(AuthenticationBuilder builder) =>
            builder.AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(
                "PluginScheme", _ => { });

        public void ConfigureAuthorization(AuthorizationOptions options) =>
            options.AddPolicy("plugin.read", policy => policy.RequireAuthenticatedUser());
    }

    private sealed class TestAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        Microsoft.Extensions.Logging.ILoggerFactory logger,
        System.Text.Encodings.Web.UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync() =>
            Task.FromResult(AuthenticateResult.NoResult());
    }
}