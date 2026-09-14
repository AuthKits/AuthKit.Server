using AuthKit.Plugins.Abstractions;
using AuthKit.Plugins.Abstractions.Contracts;
using AuthKit.Plugins.Abstractions.Contracts.Plugins;
using Host.Configuration.Authentication;
using Host.Plugins.Loading;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;
using IAuthKitPlugin = AuthKit.Plugins.Abstractions.Contracts.PluginContract.IAuthKitPlugin;

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

    [Fact]
    public void DuplicateAuthenticationScheme_Collision_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        services.AddKeycloakServices(
        [
            new LoadedPlugin(new DuplicateSchemePlugin(), typeof(DuplicateSchemePlugin).Assembly, "dup-scheme-a"),
            new LoadedPlugin(new DuplicateSchemePlugin(), typeof(DuplicateSchemePlugin).Assembly, "dup-scheme-b")
        ]);

        using var provider = services.BuildServiceProvider();
        var ex = Record.Exception(() => provider.GetRequiredService<IAuthenticationSchemeProvider>());

        var ioe = Assert.IsType<InvalidOperationException>(ex);
        Assert.Contains("DuplicatePluginScheme", ioe.Message);
    }

    [Fact]
    public void DuplicateAuthorizationPolicy_Collision_ThrowsInvalidOperationExceptionWithBothOwners()
    {
        var services = new ServiceCollection();
        services.AddKeycloakServices(
        [
            new LoadedPlugin(new FirstPolicyOwner(), typeof(FirstPolicyOwner).Assembly, "owner-a"),
            new LoadedPlugin(new SecondPolicyOwner(), typeof(SecondPolicyOwner).Assembly, "owner-b")
        ]);

        using var provider = services.BuildServiceProvider();
        var ex = Record.Exception(() => provider.GetRequiredService<IAuthorizationPolicyProvider>());

        var ioe = Assert.IsType<InvalidOperationException>(ex);
        Assert.Contains("plugin.read", ioe.Message);
        Assert.Contains("owner-a", ioe.Message);
        Assert.Contains("owner-b", ioe.Message);
    }

    [Fact]
    public void BuildingProviderInvokesEachPluginHookExactlyOnce()
    {
        var services = new ServiceCollection();
        var counting = new CountingPlugin();
        services.AddKeycloakServices(
            new[] { new LoadedPlugin(counting, typeof(CountingPlugin).Assembly, "counting.plugin") });

        using var provider = services.BuildServiceProvider();
        _ = provider.GetRequiredService<IAuthenticationSchemeProvider>();
        _ = provider.GetRequiredService<IAuthorizationPolicyProvider>();

        Assert.Equal(1, counting.AuthenticationCalls);
        Assert.Equal(1, counting.AuthorizationCalls);
    }

    [Fact]
    public async Task HostSecurityDefaultsRemainUnchanged()
    {
        var services = new ServiceCollection();
        services.AddKeycloakServices(
            new[] { new LoadedPlugin(new SecurityPlugin(), typeof(SecurityPlugin).Assembly, "security.plugin") });

        using var provider = services.BuildServiceProvider();
        var schemes = provider.GetRequiredService<IAuthenticationSchemeProvider>();
        var options = provider.GetRequiredService<IOptions<AuthorizationOptions>>().Value;

        Assert.Equal("Bearer", (await schemes.GetDefaultAuthenticateSchemeAsync())?.Name);
        Assert.NotNull(options.DefaultPolicy);
        Assert.Null(options.FallbackPolicy);
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

    [PluginMetadata("dup-scheme", "1.0.0", [], [], [], description: "Duplicate scheme")]
    private sealed class DuplicateSchemePlugin : IAuthKitPlugin
    {
        public void ConfigureAuthentication(AuthenticationBuilder builder) =>
            builder.AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(
                "DuplicatePluginScheme", _ => { });
    }

    [PluginMetadata("owner-a", "1.0.0", [], [], [], description: "Policy owner A")]
    private sealed class FirstPolicyOwner : IAuthKitPlugin
    {
        public void ConfigureAuthorization(AuthorizationOptions options) =>
            options.AddPolicy("plugin.read", policy => policy.RequireAuthenticatedUser());
    }

    [PluginMetadata("owner-b", "1.0.0", [], [], [], description: "Policy owner B")]
    private sealed class SecondPolicyOwner : IAuthKitPlugin
    {
        public void ConfigureAuthorization(AuthorizationOptions options) =>
            options.AddPolicy("plugin.read", policy => policy.RequireAuthenticatedUser());
    }

    [PluginMetadata("counting.plugin", "1.0.0", [], [], [], description: "Counting")]
    private sealed class CountingPlugin : IAuthKitPlugin
    {
        public int AuthenticationCalls { get; private set; }

        public int AuthorizationCalls { get; private set; }

        public void ConfigureAuthentication(AuthenticationBuilder builder) => AuthenticationCalls++;

        public void ConfigureAuthorization(AuthorizationOptions options) => AuthorizationCalls++;
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