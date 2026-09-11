using System;
using AuthKit.Plugins.Abstractions;
using Xunit;

namespace AuthKit.Plugins.Abstractions.Tests.SecuritySchemes;

public class AuthKitApiKeyLocationTests
{
    [Theory]
    [InlineData(AuthKitApiKeyLocation.GrpcMetadata, "GrpcMetadata")]
    [InlineData(AuthKitApiKeyLocation.Body, "Body")]
    public void NewEnumValues_AreDefined(AuthKitApiKeyLocation location, string name)
    {
        var actualName = location.ToString();
        Assert.Equal(name, actualName);
    }

    [Fact]
    public void GrpcMetadata_HasCorrectNumericValue()
    {
        Assert.Equal(3, (int)AuthKitApiKeyLocation.GrpcMetadata);
    }

    [Fact]
    public void Body_HasCorrectNumericValue()
    {
        Assert.Equal(4, (int)AuthKitApiKeyLocation.Body);
    }

    [Fact]
    public void ExistingValues_AreUnchanged()
    {
        Assert.Equal(0, (int)AuthKitApiKeyLocation.Header);
        Assert.Equal(1, (int)AuthKitApiKeyLocation.Query);
        Assert.Equal(2, (int)AuthKitApiKeyLocation.Cookie);
    }

    [Fact]
    public void GrpcMetadata_IsDistinctFromHeader()
    {
        Assert.NotEqual(AuthKitApiKeyLocation.GrpcMetadata, AuthKitApiKeyLocation.Header);
        Assert.NotEqual((int)AuthKitApiKeyLocation.GrpcMetadata, (int)AuthKitApiKeyLocation.Header);
    }

    [Fact]
    public void Body_IsDistinctFromAllExistingLocations()
    {
        Assert.NotEqual(AuthKitApiKeyLocation.Body, AuthKitApiKeyLocation.Header);
        Assert.NotEqual(AuthKitApiKeyLocation.Body, AuthKitApiKeyLocation.Query);
        Assert.NotEqual(AuthKitApiKeyLocation.Body, AuthKitApiKeyLocation.Cookie);
        Assert.NotEqual(AuthKitApiKeyLocation.Body, AuthKitApiKeyLocation.GrpcMetadata);

        Assert.NotEqual((int)AuthKitApiKeyLocation.Body, (int)AuthKitApiKeyLocation.Header);
        Assert.NotEqual((int)AuthKitApiKeyLocation.Body, (int)AuthKitApiKeyLocation.Query);
        Assert.NotEqual((int)AuthKitApiKeyLocation.Body, (int)AuthKitApiKeyLocation.Cookie);
        Assert.NotEqual((int)AuthKitApiKeyLocation.Body, (int)AuthKitApiKeyLocation.GrpcMetadata);
    }

    [Fact]
    public void UnknownEnumValues_AreNotDefined()
    {
        var unknownValue = (AuthKitApiKeyLocation)(-1);
        Assert.False(Enum.IsDefined(typeof(AuthKitApiKeyLocation), unknownValue));
    }

    [Fact]
    public void NewValues_HaveUniqueNumericValues()
    {
        var newValues = new[]
        {
            AuthKitApiKeyLocation.GrpcMetadata,
            AuthKitApiKeyLocation.Body
        };

        Assert.Equal(newValues.Length, newValues.Distinct().Count());
    }
}