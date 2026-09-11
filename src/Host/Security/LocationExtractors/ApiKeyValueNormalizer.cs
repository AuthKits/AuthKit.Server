namespace Host.Security.LocationExtractors;

/// <summary>
/// Normalizes API key credential values extracted from HTTP requests.
/// </summary>
/// <remarks>
/// <para>
/// Credential values may contain surrounding whitespace or quotes introduced
/// by the transport. The normalizer removes these characters before validation.
/// </para>
/// <para>
/// <see cref="StripBearerPrefix"/> additionally removes case-insensitive
/// Bearer prefix from the normalized value.
/// </para>
/// </remarks>
public static class ApiKeyValueNormalizer
{
    private const string BearerPrefix = "Bearer ";

    /// <summary>
    /// Trims surrounding whitespace and wrapping double quotes.
    /// </summary>
    /// <param name="value">The raw credential value.</param>
    /// <returns>The normalized credential value.</returns>
    public static string Normalize(string value) =>
        value.Trim().Trim('"');

    /// <summary>
    /// Normalizes the credential value and removes Bearer prefix,
    /// if present.
    /// </summary>
    /// <param name="value">The raw credential value.</param>
    /// <returns>The normalized credential value without the bearer prefix.</returns>
    public static string StripBearerPrefix(string value)
    {
        var normalized = Normalize(value);

        return normalized.StartsWith(
            BearerPrefix,
            StringComparison.OrdinalIgnoreCase)
            ? normalized[BearerPrefix.Length..].Trim()
            : normalized;
    }
}