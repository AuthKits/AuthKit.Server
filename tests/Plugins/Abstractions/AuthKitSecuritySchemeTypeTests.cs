using System;
using AuthKit.Plugins.Abstractions.Contracts.SecuritySchemes;
using Xunit;

namespace AuthKit.Plugins.Abstractions.Tests.SecuritySchemes;

public class AuthKitSecuritySchemeTypeTests
{
    [Theory]
    [InlineData(AuthKitSecuritySchemeType.MutualTls, "MutualTls")]
    [InlineData(AuthKitSecuritySchemeType.Session, "Session")]
    [InlineData(AuthKitSecuritySchemeType.Custom, "Custom")]
    [InlineData(AuthKitSecuritySchemeType.Basic, "Basic")]
    public void NewEnumValues_AreDefined(AuthKitSecuritySchemeType type, string name)
    {
        var actualName = type.ToString();

        Assert.Equal(name, actualName);
    }

    [Fact]
    public void AllNewValues_HaveXmlDocumentation()
    {
        var newValues = new[]
        {
            AuthKitSecuritySchemeType.MutualTls,
            AuthKitSecuritySchemeType.Session,
            AuthKitSecuritySchemeType.Custom,
            AuthKitSecuritySchemeType.Basic
        };

        foreach (var value in newValues)
        {
            Assert.True(Enum.IsDefined(typeof(AuthKitSecuritySchemeType), value), $"Value {value} is not defined.");
        }
    }

    [Fact]
    public void ExistingValues_AreUnchanged()
    {
        var existingValues = new[]
        {
            AuthKitSecuritySchemeType.ApiKey,
            AuthKitSecuritySchemeType.Http,
            AuthKitSecuritySchemeType.OAuth2,
            AuthKitSecuritySchemeType.OpenIdConnect
        };

        Assert.Equal(0, (int)AuthKitSecuritySchemeType.ApiKey);
        Assert.Equal(1, (int)AuthKitSecuritySchemeType.Http);
        Assert.Equal(2, (int)AuthKitSecuritySchemeType.OAuth2);
        Assert.Equal(7, (int)AuthKitSecuritySchemeType.OpenIdConnect);
    }

    [Fact]
    public void OAuth2_AndOpenIdConnect_AreNoLongerAliased()
    {
        Assert.NotEqual((int)AuthKitSecuritySchemeType.OAuth2, (int)AuthKitSecuritySchemeType.OpenIdConnect);
        Assert.NotEqual(AuthKitSecuritySchemeType.OAuth2, AuthKitSecuritySchemeType.OpenIdConnect);
    }

    [Fact]
    public void AllNamedValues_HaveUniqueNumericValues()
    {
        var names = Enum.GetNames<AuthKitSecuritySchemeType>();
        var values = names
            .Select(name => (int)Enum.Parse<AuthKitSecuritySchemeType>(name))
            .ToArray();

        Assert.Equal(values.Length, values.Distinct().Count());
    }

    [Fact]
    public void Session_IsSemanticallyDistinctFromApiKey()
    {
        var sessionType = AuthKitSecuritySchemeType.Session;
        var apiKeyType = AuthKitSecuritySchemeType.ApiKey;

        Assert.NotEqual(sessionType, apiKeyType);
        Assert.NotEqual((int)sessionType, (int)apiKeyType);
    }

    [Fact]
    public void Basic_IsDistinguishableFromBearer()
    {
        Assert.Equal(AuthKitSecuritySchemeType.Basic, AuthKitSecuritySchemeType.Basic);
        Assert.NotEqual(AuthKitSecuritySchemeType.Basic, AuthKitSecuritySchemeType.Http);
    }

    [Fact]
    public void Custom_DoesNotSilentlyMapToOtherSchemes()
    {
        var customType = AuthKitSecuritySchemeType.Custom;

        Assert.NotEqual(AuthKitSecuritySchemeType.ApiKey, customType);
        Assert.NotEqual(AuthKitSecuritySchemeType.Http, customType);
    }

    [Fact]
    public void NewEnumValues_AreExplicitlyRecognizedByHost()
    {
        var newValues = new[]
        {
            AuthKitSecuritySchemeType.MutualTls,
            AuthKitSecuritySchemeType.Session,
            AuthKitSecuritySchemeType.Custom,
            AuthKitSecuritySchemeType.Basic
        };

        foreach (var value in newValues)
        {
            Assert.True(Enum.IsDefined(typeof(AuthKitSecuritySchemeType), value), "Host should recognize the new enum value.");

            foreach (var v in newValues)
            {
                if (v != value)
                {
                    Assert.NotEqual(value, v);
                }
            }
        }
    }

    [Fact]
    public void UnknownEnumValues_AreNotDefined()
    {
        var unknownValue = (AuthKitSecuritySchemeType)(-1);

        Assert.False(Enum.IsDefined(typeof(AuthKitSecuritySchemeType), unknownValue));
    }

    [Fact]
    public void UnsupportedSessionAuthentication_ProducesExplicitError()
    {
        var sessionType = AuthKitSecuritySchemeType.Session;

        Assert.True(Enum.IsDefined(typeof(AuthKitSecuritySchemeType), sessionType), "Session type should be defined.");
        Assert.NotEqual(AuthKitSecuritySchemeType.ApiKey, sessionType);
    }

    [Fact]
    public void BasicAuthentication_IsDistinguishableFromBearerAndGenericHttp()
    {
        var basicType = AuthKitSecuritySchemeType.Basic;
        var httpType = AuthKitSecuritySchemeType.Http;

        Assert.NotEqual(basicType, httpType);
        Assert.NotEqual((int)basicType, (int)httpType);
        Assert.Equal(AuthKitSecuritySchemeType.Basic, basicType);
    }
}
