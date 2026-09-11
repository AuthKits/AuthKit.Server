using AuthKit.Plugins.Abstractions;
using Host.Configuration;
using Microsoft.OpenApi;
using Xunit;

namespace AuthKit.Host.Tests;

/// <summary>
/// Verifies that every <see cref="AuthKitSecuritySchemeType"/> and
/// <see cref="AuthKitApiKeyLocation"/> value is either mapped to a semantically
/// correct OpenAPI representation or explicitly rejected — never silently
/// mapped to a generic or unrelated scheme.
/// </summary>
public class AuthKitOpenApiSecuritySchemeMapperTests
{
    private static readonly OpenApiSpecVersion Version = OpenApiSpecVersion.OpenApi3_0;

    private static readonly OpenApiSpecVersion OpenApi3_1 = OpenApiSpecVersion.OpenApi3_1;

    private static AuthKitSecuritySchemeDescriptor Describe(
        AuthKitSecuritySchemeType type,
        AuthKitApiKeyLocation location = AuthKitApiKeyLocation.Header,
        string? scheme = null) =>
        new()
        {
            Name = "scheme",
            Type = type,
            In = location,
            Scheme = scheme,
            Description = "desc"
        };

    [Theory]
    [InlineData(AuthKitApiKeyLocation.Header, ParameterLocation.Header)]
    [InlineData(AuthKitApiKeyLocation.Query, ParameterLocation.Query)]
    [InlineData(AuthKitApiKeyLocation.Cookie, ParameterLocation.Cookie)]
    public void ApiKey_WithHttpLocations_MapsToApiKeyAtLocation(
        AuthKitApiKeyLocation location, ParameterLocation expected)
    {
        var mapped = AuthKitOpenApiSecuritySchemeMapper.Map(
            Describe(AuthKitSecuritySchemeType.ApiKey, location), Version);

        Assert.Equal(SecuritySchemeType.ApiKey, mapped.Type);
        Assert.Equal(expected, mapped.In);
        Assert.Equal("scheme", mapped.Name);
    }

    [Fact]
    public void Http_MapsToHttpWithSchemePassthrough()
    {
        var mapped = AuthKitOpenApiSecuritySchemeMapper.Map(
            Describe(AuthKitSecuritySchemeType.Http, scheme: "digest"), Version);

        Assert.Equal(SecuritySchemeType.Http, mapped.Type);
        Assert.Equal("digest", mapped.Scheme);
    }

    [Fact]
    public void Basic_MapsExplicitlyToHttpBasic()
    {
        var mapped = AuthKitOpenApiSecuritySchemeMapper.Map(
            Describe(AuthKitSecuritySchemeType.Basic), Version);

        Assert.Equal(SecuritySchemeType.Http, mapped.Type);
        Assert.Equal("basic", mapped.Scheme);
        Assert.Equal("scheme", mapped.Name);
    }

    [Fact]
    public void OAuth2_MapsToOAuth2()
    {
        var mapped = AuthKitOpenApiSecuritySchemeMapper.Map(
            Describe(AuthKitSecuritySchemeType.OAuth2), Version);

        Assert.Equal(SecuritySchemeType.OAuth2, mapped.Type);
    }

    [Fact]
    public void OpenIdConnect_MapsToOpenIdConnect()
    {
        var mapped = AuthKitOpenApiSecuritySchemeMapper.Map(
            Describe(AuthKitSecuritySchemeType.OpenIdConnect), Version);

        Assert.Equal(SecuritySchemeType.OpenIdConnect, mapped.Type);
        Assert.NotEqual(SecuritySchemeType.OAuth2, mapped.Type);
    }

    [Theory]
    [InlineData(AuthKitSecuritySchemeType.MutualTls)]
    [InlineData(AuthKitSecuritySchemeType.Session)]
    [InlineData(AuthKitSecuritySchemeType.Custom)]
    public void UnrepresentableSchemeTypes_AreExplicitlyRejected(AuthKitSecuritySchemeType type)
    {
        Assert.Throws<NotSupportedException>(() =>
            AuthKitOpenApiSecuritySchemeMapper.Map(Describe(type), Version));
    }

    [Theory]
    [InlineData(AuthKitApiKeyLocation.GrpcMetadata)]
    [InlineData(AuthKitApiKeyLocation.Body)]
    public void NonOpenApiLocations_AreExplicitlyRejected(AuthKitApiKeyLocation location)
    {
        Assert.Throws<NotSupportedException>(() =>
            AuthKitOpenApiSecuritySchemeMapper.Map(
                Describe(AuthKitSecuritySchemeType.ApiKey, location), Version));
    }

    [Fact]
    public void UnknownSchemeType_IsExplicitlyRejected()
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            AuthKitOpenApiSecuritySchemeMapper.Map(
                Describe((AuthKitSecuritySchemeType)999), Version));

        Assert.Contains("999", ex.Message);
    }

    [Fact]
    public void UnknownApiKeyLocation_IsExplicitlyRejected()
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            AuthKitOpenApiSecuritySchemeMapper.Map(
                Describe(AuthKitSecuritySchemeType.ApiKey, (AuthKitApiKeyLocation)999), Version));

        Assert.Contains("999", ex.Message);
    }

    [Fact]
    public void NullDescriptor_IsRejected()
    {
        Assert.Throws<ArgumentNullException>(() =>
            AuthKitOpenApiSecuritySchemeMapper.Map(null!, Version));
    }

    [Fact]
    public void NoFallback_MutualTlsDoesNotResolveToHttp()
    {
        Assert.Throws<NotSupportedException>(() =>
            AuthKitOpenApiSecuritySchemeMapper.Map(
                Describe(AuthKitSecuritySchemeType.MutualTls), Version));
    }

    [Fact]
    public void MutualTls_UnderOpenApi3_1_MapsToNativeMutualTLS()
    {
        var mapped = AuthKitOpenApiSecuritySchemeMapper.Map(
            Describe(AuthKitSecuritySchemeType.MutualTls), OpenApi3_1);

        Assert.Equal(SecuritySchemeType.MutualTLS, mapped.Type);
    }

    [Fact]
    public void NoFallback_SessionDoesNotResolveToApiKeyOrHttp()
    {
        Assert.Throws<NotSupportedException>(() =>
            AuthKitOpenApiSecuritySchemeMapper.Map(
                Describe(AuthKitSecuritySchemeType.Session), Version));
    }

    [Fact]
    public void Session_IsDistinctFromApiKeyWithCookieTransport()
    {
        var sessionDescriptor = Describe(AuthKitSecuritySchemeType.Session);
        var apiKeyInCookie = Describe(AuthKitSecuritySchemeType.ApiKey, AuthKitApiKeyLocation.Cookie);

        Assert.NotEqual(apiKeyInCookie.Type, sessionDescriptor.Type);

        var mappedApiKey = AuthKitOpenApiSecuritySchemeMapper.Map(apiKeyInCookie, Version);

        Assert.Equal(SecuritySchemeType.ApiKey, mappedApiKey.Type);
        Assert.Equal(ParameterLocation.Cookie, mappedApiKey.In);

        Assert.Throws<NotSupportedException>(() =>
            AuthKitOpenApiSecuritySchemeMapper.Map(sessionDescriptor, Version));
    }

    [Fact]
    public void NoFallback_CustomDoesNotResolveToBuiltInScheme()
    {
        Assert.Throws<NotSupportedException>(() =>
            AuthKitOpenApiSecuritySchemeMapper.Map(
                Describe(AuthKitSecuritySchemeType.Custom), Version));
    }

    [Fact]
    public void NoFallback_BasicIsDistinctFromGenericHttp()
    {
        var generic = AuthKitOpenApiSecuritySchemeMapper.Map(
            Describe(AuthKitSecuritySchemeType.Http), Version);
        var basic = AuthKitOpenApiSecuritySchemeMapper.Map(
            Describe(AuthKitSecuritySchemeType.Basic), Version);

        Assert.Equal(SecuritySchemeType.Http, generic.Type);
        Assert.Equal(SecuritySchemeType.Http, basic.Type);
        Assert.NotEqual(generic.Scheme, basic.Scheme);
        Assert.Equal("basic", basic.Scheme);
    }

    [Fact]
    public void NoFallback_GrpcMetadataDoesNotResolveToHeader()
    {
        Assert.Throws<NotSupportedException>(() =>
            AuthKitOpenApiSecuritySchemeMapper.Map(
                Describe(AuthKitSecuritySchemeType.ApiKey, AuthKitApiKeyLocation.GrpcMetadata), Version));
    }

    [Fact]
    public void NoFallback_BodyDoesNotResolveToAnotherLocation()
    {
        Assert.Throws<NotSupportedException>(() =>
            AuthKitOpenApiSecuritySchemeMapper.Map(
                Describe(AuthKitSecuritySchemeType.ApiKey, AuthKitApiKeyLocation.Body), Version));
    }
}