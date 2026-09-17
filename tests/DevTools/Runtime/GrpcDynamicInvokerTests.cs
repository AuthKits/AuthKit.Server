using DevTools.Runtime;
using Xunit;

namespace DevTools.Runtime.Tests;

/// <summary>
/// Tests for <see cref="GrpcDynamicInvoker"/> security and channel configuration.
/// </summary>
public class GrpcDynamicInvokerTests
{
    [Theory]
    [InlineData("http://localhost:5001")]
    [InlineData("http://127.0.0.1:5001")]
    [InlineData("http://[::1]:5001")]
    [InlineData("https://localhost:5001")]
    [InlineData("https://127.0.0.1")]
    public void IsLoopbackTarget_ReturnsTrueForLoopbackAddresses(string target)
    {
        // Act
        var result = GrpcDynamicInvoker.IsLoopbackTarget(target);

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData("http://example.com")]
    [InlineData("http://192.168.1.1")]
    [InlineData("http://10.0.0.1")]
    [InlineData("http://172.16.0.1")]
    [InlineData("https://example.com")]
    [InlineData("https://192.168.1.1")]
    [InlineData("http://10.10.10.10:8080")]
    [InlineData("http://api.remote-server.com")]
    public void IsLoopbackTarget_ReturnsFalseForNonLoopbackAddresses(string target)
    {
        // Act
        var result = GrpcDynamicInvoker.IsLoopbackTarget(target);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsLoopbackTarget_ReturnsFalseForInvalidUri()
    {
        // Act
        var result = GrpcDynamicInvoker.IsLoopbackTarget("not-a-valid-uri");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsLoopbackTarget_ReturnsFalseForEmptyString()
    {
        // Act
        var result = GrpcDynamicInvoker.IsLoopbackTarget(string.Empty);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsLoopbackTarget_ReturnsFalseForNull()
    {
        // Act
        var result = GrpcDynamicInvoker.IsLoopbackTarget(null!);

        // Assert
        Assert.False(result);
    }
}