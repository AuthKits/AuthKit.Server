using AuthKit.Plugins.Abstractions;
using AuthKit.Plugins.Abstractions.Contracts.Plugins;
using Host.Configuration.Authentication;
using Host.Plugins.Loading;
using Microsoft.AspNetCore.Authorization;
using Xunit;
using IAuthKitPlugin = AuthKit.Plugins.Abstractions.Contracts.PluginContract.IAuthKitPlugin;

namespace AuthKit.Host.Tests;

public sealed class AuthorizationPolicyCollisionGuardTests
{
    [Fact]
    public void LaterPluginWithoutPolicies_DoesNotThrow()
    {
        var options = new AuthorizationOptions();
        var plugins = new[]
        {
            Load(new PolicyPlugin("authkit.a", "shared.policy")),
            Load(new NoopPlugin("authkit.b")),
        };

        var exception = Record.Exception(() => AuthorizationPolicyCollisionGuard.Configure(options, plugins));

        Assert.Null(exception);
    }

    [Fact]
    public void TwoPluginsRegisteringSamePolicy_ThrowsNamingBoth()
    {
        var options = new AuthorizationOptions();
        var plugins = new[]
        {
            Load(new PolicyPlugin("authkit.a", "shared.policy")),
            Load(new PolicyPlugin("authkit.b", "shared.policy")),
        };

        var exception = Assert.Throws<InvalidOperationException>(() =>
            AuthorizationPolicyCollisionGuard.Configure(options, plugins));

        Assert.Contains("authkit.a", exception.Message, StringComparison.Ordinal);
        Assert.Contains("authkit.b", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void PluginOverwritingHostPolicy_ThrowsNamingHost()
    {
        var options = new AuthorizationOptions();
        options.AddPolicy("host.policy", policy => policy.RequireAuthenticatedUser());
        var plugins = new[] { Load(new PolicyPlugin("authkit.a", "host.policy")) };

        var exception = Assert.Throws<InvalidOperationException>(() =>
            AuthorizationPolicyCollisionGuard.Configure(options, plugins));

        Assert.Contains("<host>", exception.Message, StringComparison.Ordinal);
        Assert.Contains("authkit.a", exception.Message, StringComparison.Ordinal);
    }

    private static LoadedPlugin Load(IAuthKitPlugin plugin) =>
        new(plugin, plugin.GetType().Assembly, "test");

    [PluginMetadata("authkit.a", "1.0.0", [], [], [], description: "Policy test")]
    private sealed class PolicyPlugin(string id, string policy) : IAuthKitPlugin
    {
        public string Id => id;

        public void ConfigureAuthorization(AuthorizationOptions options) =>
            options.AddPolicy(policy, builder => builder.RequireAuthenticatedUser());
    }

    [PluginMetadata("authkit.b", "1.0.0", [], [], [], description: "Noop test")]
    private sealed class NoopPlugin(string id) : IAuthKitPlugin
    {
        public string Id => id;
    }
}
