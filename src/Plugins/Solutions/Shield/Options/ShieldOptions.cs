using System.ComponentModel.DataAnnotations;

namespace Shield.Options;

/// <summary>
/// Tunables for the Shield critical-security plugin. Bound from the plugin
/// configuration section (<c>Plugins:authkit.shield</c>) and validated at
/// startup via data annotations.
/// </summary>
public sealed class ShieldOptions
{
    /// <summary>
    /// Maximum requests per client inside <see cref="WindowSeconds"/> before
    /// the client is temporarily blocked. Default 100.
    /// </summary>
    [Range(1, 1_000_000)]
    public int MaxRequestsPerWindow { get; set; } = 100;

    /// <summary>
    /// Sliding window size in seconds for rate counting. Default 60.
    /// </summary>
    [Range(1, 3600)]
    public int WindowSeconds { get; set; } = 60;

    /// <summary>
    /// Duration of client blocks and automatic global lockdown. Default 300s.
    /// </summary>
    [Range(1, 86_400)]
    public int LockdownSeconds { get; set; } = 300;

    /// <summary>
    /// Signature hits from a single client before a temporary block. Default 3.
    /// </summary>
    [Range(1, 100)]
    public int MaxSignatureHits { get; set; } = 3;

    /// <summary>Window for signature hits. Default one hour.</summary>
    [Range(1, 86_400)]
    public int SignatureWindowSeconds { get; set; } = 3600;

    /// <summary>Maximum client entries retained by each counter.</summary>
    [Range(1, 1_000_000)]
    public int MaxTrackedClients { get; set; } = 10_000;

    /// <summary>Distinct blocked clients required for an automatic global lockdown.</summary>
    [Range(2, 1_000_000)]
    public int MinOffendingClientsForLockdown { get; set; } = 20;

    /// <summary>
    /// Enables path/query attack-signature detection. Default true.
    /// </summary>
    public bool SignatureDetectionEnabled { get; set; } = true;

    /// <summary>
    /// Failed authentications (401/403) per client inside
    /// <see cref="AuthFailureWindowSeconds"/> before a client block. Default 10.
    /// </summary>
    [Range(1, 10_000)]
    public int MaxAuthFailures { get; set; } = 10;

    /// <summary>
    /// Sliding window in seconds for authentication-failure counting. Default 300.
    /// </summary>
    [Range(1, 3600)]
    public int AuthFailureWindowSeconds { get; set; } = 300;

    /// <summary>
    /// Additional case-insensitive fragments merged with the built-in signature
    /// list. Default empty.
    /// </summary>
    public string[] ExtraSignatures { get; set; } = [];

    /// <summary>
    /// Enables 404-probing detection. Default true.
    /// </summary>
    public bool ScannerDetectionEnabled { get; set; } = true;

    /// <summary>
    /// Missing paths per client inside <see cref="ScannerWindowSeconds"/>
    /// before a client block. Default 20.
    /// </summary>
    [Range(1, 10_000)]
    public int MaxNotFoundProbes { get; set; } = 20;

    /// <summary>
    /// Sliding window in seconds for 404-probe counting. Default 300.
    /// </summary>
    [Range(1, 3600)]
    public int ScannerWindowSeconds { get; set; } = 300;

    /// <summary>
    /// Enables scanner user-agent detection. Default true.
    /// </summary>
    public bool UserAgentDetectionEnabled { get; set; } = true;

    /// <summary>
    /// Additional case-insensitive user-agent fragments treated as attack
    /// tooling. Default empty.
    /// </summary>
    public string[] ExtraBlockedAgents { get; set; } = [];

    /// <summary>
    /// Maximum declared request body in bytes before the request is blocked
    /// (without a lockdown). Default 8 MiB.
    /// </summary>
    [Range(1024, 1_073_741_824)]
    public long MaxBodyBytes { get; set; } = 8 * 1024 * 1024;

    /// <summary>
    /// Maximum query-string length in chars before the request is blocked
    /// (without a lockdown). Default 4096.
    /// </summary>
    [Range(256, 65_536)]
    public int MaxQueryLength { get; set; } = 4096;

    /// <summary>
    /// Manual kill-switch. When true, every request is blocked until the flag
    /// is cleared. Default false.
    /// </summary>
    public bool ForceLockdown { get; set; } = false;

    /// <summary>
    /// Exact paths (case-insensitive) that bypass evaluation, e.g. the status
    /// endpoint so operators can observe an active lockdown.
    /// Default <c>/shield/status</c>, <c>/shield/incidents</c> and <c>/shield/release</c>.
    /// </summary>
    public string[] BypassPaths { get; set; } = ["/shield/status", "/shield/incidents", "/shield/release"];

    /// <summary>
    /// Client identifiers that skip evaluation entirely (e.g. monitoring).
    /// Compared verbatim against the resolved client id. Default empty.
    /// </summary>
    public string[] AllowedIps { get; set; } = [];

    /// <summary>
    /// Client identifiers that are always blocked with 503 without tripping a
    /// global lockdown. Compared verbatim against the resolved client id.
    /// Default empty.
    /// </summary>
    public string[] BlockedIps { get; set; } = [];

    /// <summary>
    /// Use <c>X-Forwarded-For</c> for client identification. Select the entry
    /// <see cref="TrustedProxyHops"/> positions from the right. Enable only
    /// when direct access to the host is blocked by trusted proxies.
    /// </summary>
    public bool TrustForwardedHeader { get; set; } = false;

    /// <summary>Number of trusted proxies that append to X-Forwarded-For.</summary>
    [Range(1, 16)]
    public int TrustedProxyHops { get; set; } = 1;

    /// <summary>
    /// Maximum retained incidents in the in-memory ring buffer. Default 100.
    /// </summary>
    [Range(1, 1000)]
    public int MaxIncidents { get; set; } = 100;

    /// <summary>
    /// Enables <c>POST /shield/release</c>. Default false.
    /// </summary>
    public bool ReleaseEnabled { get; set; } = false;

    /// <summary>
    /// Shared secret required in the <c>X-Shield-Release</c> header to unlock.
    /// The endpoint answers 404 unless enabled with a non-empty token.
    /// Provide via environment/secret store, never commit it.
    /// </summary>
    public string ReleaseToken { get; set; } = string.Empty;
}
