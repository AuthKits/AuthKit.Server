using Host.Plugins.Loading;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AuthKit.Host.IntegrationTests;

public sealed class ShieldHostLoadTests(AuthKitWebApplicationFactory factory) : IClassFixture<AuthKitWebApplicationFactory>
{
    private readonly AuthKitWebApplicationFactory _factory = factory;

    [Fact]
    public void Shield_LoadsThroughTheRealHost()
    {
        _ = _factory.CreateClient();
        var plugins = _factory.Services.GetRequiredService<IReadOnlyList<LoadedPlugin>>();
        Assert.Single(plugins, plugin => plugin.Plugin.Id == "authkit.shield");
    }
}
