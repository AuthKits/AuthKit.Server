using AuthKit.Plugins.Abstractions;
using Microsoft.Extensions.Logging;

namespace Host.Plugins;

/// <summary>
/// Validates AuthKit plugin contracts against the host's supported capabilities.
/// </summary>
/// <remarks>
/// Ensures plugins declare only supported security scheme types and API key locations.
/// Unknown or unsupported values cause explicit validation failures rather than silent fallbacks.
/// </remarks>
public static class PluginContractValidator
{
    /// <summary>
    /// Security scheme types the host implements and can therefore accept from plugins.
    /// </summary>
    /// <remarks>
    /// Mutual TLS, session, custom, and HTTP Basic authentication are not implemented by
    /// this host. They are recognized explicitly and rejected rather than silently accepted
    /// as if they were supported. Hosts that implement them extend the supported set through
    /// <see cref="CreateCustom"/>.
    /// </remarks>
    private static readonly HashSet<AuthKitSecuritySchemeType> _supportedSchemeTypes =
    [
        AuthKitSecuritySchemeType.ApiKey,
        AuthKitSecuritySchemeType.Http,
        AuthKitSecuritySchemeType.OAuth2,
        AuthKitSecuritySchemeType.OpenIdConnect
    ];

    /// <summary>
    /// Credential locations the host's credential extraction strategies implement.
    /// </summary>
    /// <remarks>
    /// The host ships location extractor strategies for every value, including
    /// <see cref="AuthKitApiKeyLocation.GrpcMetadata"/> and
    /// <see cref="AuthKitApiKeyLocation.Body"/>. Note that gRPC metadata and body locations are
    /// still rejected by the OpenAPI mapper (<c>AuthKitOpenApiSecuritySchemeMapper</c>) because
    /// OpenAPI 3.0 cannot represent them — runtime support and OpenAPI representability are
    /// validated independently.
    /// </remarks>
    private static readonly HashSet<AuthKitApiKeyLocation> _supportedApiKeyLocations =
    [
        AuthKitApiKeyLocation.Header,
        AuthKitApiKeyLocation.Query,
        AuthKitApiKeyLocation.Cookie,
        AuthKitApiKeyLocation.GrpcMetadata,
        AuthKitApiKeyLocation.Body
    ];

    public static IReadOnlySet<AuthKitSecuritySchemeType> SupportedSchemeTypes => _supportedSchemeTypes;
    public static IReadOnlySet<AuthKitApiKeyLocation> SupportedApiKeyLocations => _supportedApiKeyLocations;

    /// <summary>
    /// Validates a plugin's security scheme descriptors against host capabilities.
    /// </summary>
    /// <param name="plugin">The plugin to validate.</param>
    /// <param name="logger">Logger for validation results.</param>
    /// <exception cref="InvalidPluginContractException">
    /// Thrown when the plugin declares unsupported or unknown scheme types/locations.
    /// </exception>
    public static void Validate(IAuthKitPlugin plugin, ILogger logger)
    {
        var schemes = plugin.GetSecuritySchemes();
        
        foreach (var (name, descriptor) in schemes)
        {
            ValidateSchemeType(name, descriptor.Type, SupportedSchemeTypes, logger);
            ValidateApiKeyLocation(name, descriptor.In, SupportedApiKeyLocations, logger);
            WarnIfCustomWithoutUsageDocumentation(name, descriptor, SupportedSchemeTypes, logger);
        }
    }

    internal static void ValidateSchemeType(
        string schemeName,
        AuthKitSecuritySchemeType type,
        IReadOnlySet<AuthKitSecuritySchemeType> supportedTypes,
        ILogger logger)
    {
        if (supportedTypes.Contains(type))
            return;

        var msg = Enum.IsDefined(type)
            ? $"Plugin declares security scheme type '{type}' for security scheme '{schemeName}', which is not implemented by this host. " +
              $"MutualTls, Session, Custom, and Basic must be explicitly rejected. Supported types: {string.Join(", ", supportedTypes)}."
            : $"Plugin declares unknown AuthKitSecuritySchemeType value '{(int)type}' for security scheme '{schemeName}'. " +
              $"Unknown future values must be rejected rather than treated as a known type.";

        logger.LogError(msg);
        throw new InvalidPluginContractException(msg);
    }

    /// <summary>
    /// Warns when a plugin declares a <see cref="AuthKitSecuritySchemeType.Custom"/> scheme without
    /// usage documentation.
    /// </summary>
    /// <remarks>
    /// The warning is only emitted for hosts that actually support custom schemes (the default host
    /// rejects them with an explicit error). It must never cause the custom scheme to be mapped to
    /// another security scheme.
    /// </remarks>
    internal static void WarnIfCustomWithoutUsageDocumentation(
        string schemeName,
        AuthKitSecuritySchemeDescriptor descriptor,
        IReadOnlySet<AuthKitSecuritySchemeType> supportedTypes,
        ILogger logger)
    {
        if (descriptor.Type != AuthKitSecuritySchemeType.Custom || !supportedTypes.Contains(descriptor.Type))
            return;

        if (string.IsNullOrWhiteSpace(descriptor.Description))
        {
            logger.LogWarning(
                "Security scheme '{Scheme}' declares AuthKitSecuritySchemeType.Custom but provides no description " +
                "explaining how the custom authentication mechanism is intended to be used. Add a Description to the " +
                "AuthKitSecuritySchemeDescriptor so clients and operators understand the mechanism. " +
                "Custom schemes are never mapped to a built-in security scheme.",
                schemeName);
        }
    }

    internal static void ValidateApiKeyLocation(
        string schemeName,
        AuthKitApiKeyLocation location,
        IReadOnlySet<AuthKitApiKeyLocation> supportedLocations,
        ILogger logger)
    {
        if (supportedLocations.Contains(location))
            return;

        var msg = Enum.IsDefined(location)
            ? $"Plugin declares API key location '{location}' for security scheme '{schemeName}', which is not supported by this host's " +
              $"credential extraction strategies. Configure a host with {location} support or change the plugin configuration."
            : $"Plugin declares unknown AuthKitApiKeyLocation value '{(int)location}' for security scheme '{schemeName}'. " +
              $"Unknown future values must be rejected rather than treated as a known location.";

        logger.LogError(msg);
        throw new InvalidPluginContractException(msg);
    }

    /// <summary>
    /// Creates a validator with custom supported locations (for hosts with extended capabilities).
    /// </summary>
    public static PluginContractValidatorCustom CreateCustom(
        IEnumerable<AuthKitSecuritySchemeType>? supportedSchemeTypes = null,
        IEnumerable<AuthKitApiKeyLocation>? supportedApiKeyLocations = null)
    {
        return new PluginContractValidatorCustom(supportedSchemeTypes, supportedApiKeyLocations);
    }
}

/// <summary>
/// Customizable validator for hosts with extended capabilities (e.g., gRPC metadata, Body support).
/// </summary>
public sealed class PluginContractValidatorCustom
{
    private readonly HashSet<AuthKitSecuritySchemeType> _supportedSchemeTypes;
    private readonly HashSet<AuthKitApiKeyLocation> _supportedApiKeyLocations;

    public PluginContractValidatorCustom(
        IEnumerable<AuthKitSecuritySchemeType>? supportedSchemeTypes = null,
        IEnumerable<AuthKitApiKeyLocation>? supportedApiKeyLocations = null)
    {
        _supportedSchemeTypes = new HashSet<AuthKitSecuritySchemeType>(supportedSchemeTypes ?? PluginContractValidator.SupportedSchemeTypes);
        _supportedApiKeyLocations = new HashSet<AuthKitApiKeyLocation>(supportedApiKeyLocations ?? PluginContractValidator.SupportedApiKeyLocations);
    }

    public void Validate(IAuthKitPlugin plugin, ILogger logger)
    {
        var schemes = plugin.GetSecuritySchemes();
        
        foreach (var (name, descriptor) in schemes)
        {
            PluginContractValidator.ValidateSchemeType(name, descriptor.Type, _supportedSchemeTypes, logger);
            PluginContractValidator.ValidateApiKeyLocation(name, descriptor.In, _supportedApiKeyLocations, logger);
            PluginContractValidator.WarnIfCustomWithoutUsageDocumentation(name, descriptor, _supportedSchemeTypes, logger);
        }
    }
}

/// <summary>
/// Exception thrown when a plugin contract violates host capabilities.
/// </summary>
public sealed class InvalidPluginContractException : Exception
{
    public InvalidPluginContractException(string message) : base(message) { }
}