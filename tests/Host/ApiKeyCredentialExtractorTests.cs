using AuthKit.Plugins.Abstractions;
using AuthKit.Plugins.Abstractions.Contracts.SecuritySchemes;
using Host.Security;
using Host.Security.LocationExtractors;
using Host.Security.Middleware;
using Host.Security.Validation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AuthKit.Host.Tests;

public class ApiKeyCredentialExtractorTests
{
    private const string SchemeName = "DevTokens";

    private sealed class FakeRegistry(string registeredName = SchemeName) : ISecuritySchemeRegistry
    {
        private readonly AuthKitSecuritySchemeDescriptor _descriptor = new()
        {
            Name = registeredName,
            Type = AuthKitSecuritySchemeType.ApiKey,
            In = AuthKitApiKeyLocation.Header,
            CredentialName = "X-Api-Key"
        };

        public bool TryGet(string schemeName, out AuthKitSecuritySchemeDescriptor result)
        {
            result = _descriptor;
            return string.Equals(schemeName, _descriptor.Name, StringComparison.OrdinalIgnoreCase);
        }
    }

    private sealed class FakeLocationRegistry(IApiKeyLocationExtractor extractor) : IApiKeyLocationExtractorRegistry
    {
        public IApiKeyLocationExtractor Resolve(AuthKitApiKeyLocation location) => extractor;
    }

    private sealed class FakeExtractor(string? value) : IApiKeyLocationExtractor
    {
        public AuthKitApiKeyLocation Location => AuthKitApiKeyLocation.Header;

        public Task<string?> ExtractAsync(HttpContext context, AuthKitSecuritySchemeDescriptor scheme)
            => Task.FromResult(value);
    }

    private sealed class FakeValidator(ApiKeyPrincipal? principal) : IApiKeyValidator
    {
        public Task<ApiKeyPrincipal?> ValidateAsync(string apiKey) => Task.FromResult(principal);
    }

    private sealed class PipelineProbe
    {
        public bool NextCalled { get; private set; }

        public RequestDelegate Next => context =>
        {
            NextCalled = true;
            return Task.CompletedTask;
        };
    }

    private sealed class EndpointFeature : IEndpointFeature
    {
        public Endpoint? Endpoint { get; set; }
    }

    private static HttpContext CreateContext(string? schemeName = SchemeName)
    {
        var context = new DefaultHttpContext();

        if (schemeName is not null)
        {
            var endpoint = new Endpoint(
                requestDelegate: _ => Task.CompletedTask,
                metadata: new EndpointMetadataCollection(new SecuritySchemeAttribute(schemeName)),
                displayName: "test");
            context.Features.Set<IEndpointFeature>(new EndpointFeature { Endpoint = endpoint });
        }

        return context;
    }

    private static ApiKeyCredentialExtractor CreateMiddleware(
        ISecuritySchemeRegistry registry,
        IApiKeyLocationExtractorRegistry locationRegistry,
        RequestDelegate next) =>
        new(next, registry, locationRegistry, NullLogger<ApiKeyCredentialExtractor>.Instance);

    [Fact]
    public async Task EndpointWithoutSchemeAttribute_ContinuesPipeline()
    {
        var state = new PipelineProbe();
        var middleware = CreateMiddleware(
            new FakeRegistry(),
            new FakeLocationRegistry(new FakeExtractor("secret")),
            state.Next);

        var context = CreateContext(schemeName: null);
        await middleware.InvokeAsync(
            context,
            new FakeValidator(new ApiKeyPrincipal { Subject = "s1" }));

        Assert.True(state.NextCalled);
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
    }

    [Fact]
    public async Task ValidCredential_AuthenticatesPrincipalAndContinues()
    {
        var state = new PipelineProbe();
        var middleware = CreateMiddleware(
            new FakeRegistry(),
            new FakeLocationRegistry(new FakeExtractor("secret")),
            state.Next);

        var context = CreateContext();
        var principal = new ApiKeyPrincipal { Subject = "s1" };
        await middleware.InvokeAsync(context, new FakeValidator(principal));

        Assert.True(state.NextCalled);
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        Assert.True(context.User.Identity?.IsAuthenticated);
        Assert.Equal("ApiKey", context.User.Identity.AuthenticationType);
    }

    [Fact]
    public async Task MissingCredential_ContinuesPipeline()
    {
        var state = new PipelineProbe();
        var middleware = CreateMiddleware(
            new FakeRegistry(),
            new FakeLocationRegistry(new FakeExtractor(null)),
            state.Next);

        var context = CreateContext();
        await middleware.InvokeAsync(
            context,
            new FakeValidator(new ApiKeyPrincipal { Subject = "s1" }));

        Assert.True(state.NextCalled);
        Assert.NotEqual(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvalidCredential_FailsClosedWith401()
    {
        var state = new PipelineProbe();
        var middleware = CreateMiddleware(
            new FakeRegistry(),
            new FakeLocationRegistry(new FakeExtractor("wrong-key")),
            state.Next);

        var context = CreateContext();
        await middleware.InvokeAsync(context, new FakeValidator(null));

        Assert.False(state.NextCalled);
        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
        Assert.Equal("ApiKey", context.Response.Headers.WWWAuthenticate);
    }

    [Fact]
    public async Task SchemeNotRegistered_FailsClosedAsConfigurationError()
    {
        var state = new PipelineProbe();
        var middleware = CreateMiddleware(
            new FakeRegistry(),
            new FakeLocationRegistry(new FakeExtractor("secret")),
            state.Next);

        var context = CreateContext(schemeName: "UnknownScheme");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            middleware.InvokeAsync(
                context,
                new FakeValidator(new ApiKeyPrincipal { Subject = "s1" })));

        Assert.False(state.NextCalled);
    }
}