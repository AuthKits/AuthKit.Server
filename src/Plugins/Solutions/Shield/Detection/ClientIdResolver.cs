using Microsoft.AspNetCore.Http;
using System.Net;

namespace Shield.Detection;

/// <summary>
/// Resolves the client identifier used for rate counting, lists, and incidents:
/// the address before the configured number of trusted proxy hops, counted
/// from the right of <c>X-Forwarded-For</c>, or the transport address.
/// </summary>
public static class ClientIdResolver
{
    /// <summary>
    /// Resolves the client id for the request. Never returns null or empty.
    /// </summary>
    public static string Resolve(HttpContext context, bool trustForwardedHeader, int trustedProxyHops = 1)
    {
        if (trustForwardedHeader
            && context.Request.Headers.TryGetValue("X-Forwarded-For", out var forwarded))
        {
            var entries = forwarded.ToString().Split(',', StringSplitOptions.TrimEntries);
            var hops = Math.Max(1, trustedProxyHops);
            if (entries.Length >= hops && IPAddress.TryParse(entries[^hops], out var address))
                return address.ToString();
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}
