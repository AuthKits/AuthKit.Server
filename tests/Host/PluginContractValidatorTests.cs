using AuthKit.Plugins.Abstractions;
using Host.Plugins;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AuthKit.Host.Tests;

/// <summary>
/// Verifies that the host recognizes every extended security scheme contract value and
/// explicitly accepts (or rejects) it instead of silently treating it as supported.
/// </summary>
public class PluginContractValidatorTests
{
    private static readonly Microsoft.Extensions.Logging.ILogger Logger = NullLogger.Instance;

    private sealed class FakePlugin(AuthKitSecuritySchemeDescriptor descriptor) : IAuthKitPlugin
    {
        private readonly IReadOnlyDictionary<string, AuthKitSecuritySchemeDescriptor> _schemes = new Dictionary<string, AuthKitSecuritySchemeDescriptor>
        {
            [descriptor.Name] = descriptor
        };

        public string Name => "Fake";
        public string Version => "1.0.0";
        public static void ConfigureServices(IServiceCollection services, IConfiguration configuration) { }
        public IReadOnlyDictionary<string, AuthKitSecuritySchemeDescriptor> GetSecuritySchemes() => _schemes;
    }

    private static AuthKitSecuritySchemeDescriptor Describe(
        AuthKitSecuritySchemeType type,
        AuthKitApiKeyLocation location = AuthKitApiKeyLocation.Header,
        string? description = null) =>
        new()
        {
            Name = "scheme",
            Type = type,
            In = location,
            Description = description
        };

    private sealed class RecordingLogger : Microsoft.Extensions.Logging.ILogger
    {
        public List<string> Warnings { get; } = new();

        public static IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public static bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) => true;

        public void Log<TState>(
            Microsoft.Extensions.Logging.LogLevel logLevel,
            Microsoft.Extensions.Logging.EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (logLevel == Microsoft.Extensions.Logging.LogLevel.Warning)
                Warnings.Add(formatter(state, exception));
        }
    }

    [Theory]
    [InlineData(AuthKitSecuritySchemeType.ApiKey)]
    [InlineData(AuthKitSecuritySchemeType.Http)]
    [InlineData(AuthKitSecuritySchemeType.OAuth2)]
    [InlineData(AuthKitSecuritySchemeType.OpenIdConnect)]
    public void ImplementedSchemeTypes_AreAccepted(AuthKitSecuritySchemeType type)
    {
        PluginContractValidator.Validate(new FakePlugin(Describe(type)), Logger);
    }

    [Theory]
    [InlineData(AuthKitApiKeyLocation.Header)]
    [InlineData(AuthKitApiKeyLocation.Query)]
    [InlineData(AuthKitApiKeyLocation.Cookie)]
    [InlineData(AuthKitApiKeyLocation.GrpcMetadata)]
    [InlineData(AuthKitApiKeyLocation.Body)]
    public void HostLocations_AreAccepted(AuthKitApiKeyLocation location)
    {
        PluginContractValidator.Validate(
            new FakePlugin(Describe(AuthKitSecuritySchemeType.ApiKey, location)), Logger);
    }

    [Theory]
    [InlineData(AuthKitSecuritySchemeType.MutualTls)]
    [InlineData(AuthKitSecuritySchemeType.Session)]
    [InlineData(AuthKitSecuritySchemeType.Custom)]
    [InlineData(AuthKitSecuritySchemeType.Basic)]
    public void UnimplementedSchemeTypes_AreExplicitlyRejected(AuthKitSecuritySchemeType type)
    {
        Assert.Throws<InvalidPluginContractException>(() =>
            PluginContractValidator.Validate(new FakePlugin(Describe(type)), Logger));
    }

    [Fact]
    public void UnknownSchemeType_IsExplicitlyRejected()
    {
        var ex = Assert.Throws<InvalidPluginContractException>(() =>
            PluginContractValidator.Validate(
                new FakePlugin(Describe((AuthKitSecuritySchemeType)999)), Logger));

        Assert.Contains("unknown", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("999", ex.Message);
    }

    [Fact]
    public void UnknownApiKeyLocation_IsExplicitlyRejected()
    {
        var ex = Assert.Throws<InvalidPluginContractException>(() =>
            PluginContractValidator.Validate(
                new FakePlugin(Describe(AuthKitSecuritySchemeType.ApiKey, (AuthKitApiKeyLocation)999)),
                Logger));

        Assert.Contains("unknown", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("999", ex.Message);
    }

    [Fact]
    public void CustomValidator_CanEnableAdditionalSchemeTypes()
    {
        var custom = PluginContractValidator.CreateCustom(
            supportedSchemeTypes:
            [
                .. PluginContractValidator.SupportedSchemeTypes,
                AuthKitSecuritySchemeType.Basic
            ]);

        custom.Validate(new FakePlugin(Describe(AuthKitSecuritySchemeType.Basic)), Logger);
    }

    [Fact]
    public void CustomValidator_CanRestrictLocations()
    {
        var custom = PluginContractValidator.CreateCustom(
            supportedApiKeyLocations: [AuthKitApiKeyLocation.Header]);

        Assert.Throws<InvalidPluginContractException>(() =>
            custom.Validate(
                new FakePlugin(Describe(AuthKitSecuritySchemeType.ApiKey, AuthKitApiKeyLocation.Query)),
                Logger));
    }

    [Fact]
    public void NoFallback_CustomAndSessionAreNotTreatedAsSupported()
    {
        Assert.Throws<InvalidPluginContractException>(() =>
            PluginContractValidator.Validate(
                new FakePlugin(Describe(AuthKitSecuritySchemeType.Session)), Logger));

        Assert.Throws<InvalidPluginContractException>(() =>
            PluginContractValidator.Validate(
                new FakePlugin(Describe(AuthKitSecuritySchemeType.Custom)), Logger));
    }

    [Fact]
    public void CustomScheme_WithoutUsageDocumentation_EmitsWarning()
    {
        var logger = new RecordingLogger();
        var custom = PluginContractValidator.CreateCustom(
            supportedSchemeTypes:
            [
                .. PluginContractValidator.SupportedSchemeTypes,
                AuthKitSecuritySchemeType.Custom
            ]);

        custom.Validate(new FakePlugin(
            Describe(AuthKitSecuritySchemeType.Custom, description: null)), logger);

        Assert.Contains(logger.Warnings, w =>
            w.Contains("Custom", StringComparison.OrdinalIgnoreCase)
            && w.Contains("description", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CustomScheme_WithUsageDocumentation_DoesNotEmitWarning()
    {
        var logger = new RecordingLogger();
        var custom = PluginContractValidator.CreateCustom(
            supportedSchemeTypes:
            [
                .. PluginContractValidator.SupportedSchemeTypes,
                AuthKitSecuritySchemeType.Custom
            ]);

        custom.Validate(new FakePlugin(
            Describe(AuthKitSecuritySchemeType.Custom,
                description: "Plugin-defined API key verified against a plugin database.")),
            logger);

        Assert.Empty(logger.Warnings);
    }

    [Fact]
    public void CustomWarning_NeverMapsCustomToAnotherScheme()
    {
        var logger = new RecordingLogger();
        var custom = PluginContractValidator.CreateCustom(
            supportedSchemeTypes:
            [
                .. PluginContractValidator.SupportedSchemeTypes,
                AuthKitSecuritySchemeType.Custom
            ]);

        custom.Validate(new FakePlugin(
            Describe(AuthKitSecuritySchemeType.Custom, description: null)), logger);

        Assert.Contains(logger.Warnings, w => w.Contains("never mapped", StringComparison.OrdinalIgnoreCase));
    }
}