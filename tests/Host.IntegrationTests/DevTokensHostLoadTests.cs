using System.Net;
using AuthKit.Plugins.Abstractions;
using AuthKit.Plugins.Abstractions.Contracts.SecuritySchemes;
using Host.Plugins;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AuthKit.Host.IntegrationTests;

/// <summary>
/// Verifies that the unmodified DevTokens plugin ships, is discovered through
/// the real host entry point, passes contract validation, and participates in
/// the running application.
/// </summary>
public sealed class DevTokensHostLoadTests : IClassFixture<AuthKitWebApplicationFactory>
{
    private readonly AuthKitWebApplicationFactory _factory;

    public DevTokensHostLoadTests(AuthKitWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public void DevTokens_IsLoadedThroughTheRealHostEntryPoint()
    {
        _ = _factory.CreateClient();

        var plugins = _factory.Services.GetRequiredService<IReadOnlyList<LoadedPlugin>>();
        var devTokens = Assert.Single(plugins, plugin => plugin.Plugin.Id == "authkit.devtokens");

        Assert.Equal("DevTokens", devTokens.Plugin.Name);
        Assert.Equal("Developer Tokens", devTokens.Plugin.DisplayName);
        Assert.Equal("1.0.0", devTokens.Plugin.Version.ToString());
        Assert.Equal("DevTokens", devTokens.Assembly.GetName().Name);
    }

    [Fact]
    public async Task DevTokens_DeclaredSecurityScheme_SurvivesContractValidation()
    {
        _ = _factory.CreateClient();

        var plugins = _factory.Services.GetRequiredService<IReadOnlyList<LoadedPlugin>>();
        var devTokens = Assert.Single(plugins, plugin => plugin.Plugin.Id == "authkit.devtokens");

        var scheme = Assert.Single(devTokens.Plugin.GetSecuritySchemes());
        Assert.Equal("X-Developer-Token", scheme.Key);
        Assert.Equal(AuthKitSecuritySchemeType.ApiKey, scheme.Value.Type);
        Assert.Equal(AuthKitApiKeyLocation.Header, scheme.Value.In);
    }

    [Fact]
    public async Task Host_ServesRequestsWithDevTokensLoaded()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}