using System.Security.Cryptography;
using System.Text;
using Shield.Options;

namespace Shield.Security;

/// <summary>
/// Token gate for the unlock surfaces (HTTP header, gRPC metadata).
/// Disabled or tokenless configuration never authorizes.
/// </summary>
public static class ReleaseGuard
{
    /// <summary>
    /// HTTP header carrying the release token.
    /// </summary>
    public const string HttpHeader = "X-Shield-Release";

    /// <summary>
    /// gRPC metadata key carrying the release token (lowercase per HTTP/2).
    /// </summary>
    public const string MetadataKey = "x-shield-release";

    /// <summary>
    /// Whether the endpoint may exist at all: enabled with a non-empty token.
    /// </summary>
    public static bool IsEnabled(ShieldOptions config) =>
        config.ReleaseEnabled && !string.IsNullOrEmpty(config.ReleaseToken);

    /// <summary>Operator reads require the configured token even when release is disabled.</summary>
    public static bool IsOperatorEnabled(ShieldOptions config) =>
        !string.IsNullOrEmpty(config.ReleaseToken);

    public static bool IsOperatorAuthorized(ShieldOptions config, string? provided)
    {
        if (!IsOperatorEnabled(config) || string.IsNullOrEmpty(provided))
            return false;

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(config.ReleaseToken),
            Encoding.UTF8.GetBytes(provided));
    }

    /// <summary>
    /// Constant-time token check. False when disabled.
    /// </summary>
    public static bool IsAuthorized(ShieldOptions config, string? provided)
    {
        return IsEnabled(config) && IsOperatorAuthorized(config, provided);
    }
}
