using Microsoft.AspNetCore.Http;

namespace Shield.Detection;

/// <summary>
/// Resolves the client identifier used for rate counting, lists, and incidents:
/// the leftmost <c>X-Forwarded-For</c> entry when trusted, otherwise the
/// transport address.
/// </summary>
public static class ClientIdResolver
{
    /// <summary>
    /// Resolves the client id for the request. Never returns null or empty.
    /// </summary>
    public static string Resolve(HttpContext context, bool trustForwardedHeader)
    {
        if (trustForwardedHeader
            && context.Request.Headers.TryGetValue("X-Forwarded-For", out var forwarded))
        {
            var first = forwarded.ToString().Split(',', 2)[0].Trim();
            if (!string.IsNullOrEmpty(first))
                return first;
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}
