namespace Host.Security.BodyParsers;

/// <summary>
/// Extracts an API key field from an
/// <c>application/x-www-form-urlencoded</c> request body.
/// </summary>
/// <remarks>
/// <para>Form fields are matched by name using case-insensitive comparison. </para>
/// <para>
/// Values are decoded according to standard URL form encoding rules,
/// including restoring + characters as spaces.
/// </para>
/// </remarks>
public sealed class FormUrlEncodedApiKeyBodyParser : IApiKeyBodyParser
{
    /// <summary>
    /// Determines whether this parser supports the specified content type.
    /// </summary>
    /// <param name="contentType">The request content type.</param>
    /// <returns>
    /// <c>true</c> if the content type is
    /// <c>application/x-www-form-urlencoded</c>; otherwise, <c>false</c>.
    /// </returns>
    public bool CanHandle(string? contentType) =>
        !string.IsNullOrEmpty(contentType)
        && contentType.Contains(
            "application/x-www-form-urlencoded",
            StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Extracts an API key from the specified form field.
    /// </summary>
    /// <param name="body">The URL encoded form request body.</param>
    /// <param name="fieldName">The name of the field containing the API key.</param>
    /// <returns>
    /// The decoded API key, or null if the specified field is not present.
    /// </returns>
    public string? Parse(string body, string fieldName)
    {
        foreach (var pair in body.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var separatorIndex = pair.IndexOf('=');

            if (separatorIndex <= 0)
                continue;

            var name = FormDecode(pair[..separatorIndex]);

            if (!string.Equals(name, fieldName, StringComparison.OrdinalIgnoreCase))
                continue;

            var value = pair[(separatorIndex + 1)..];

            return string.IsNullOrEmpty(value)
                ? value
                : FormDecode(value);
        }

        return null;
    }

    /// <summary>
    /// Decodes URL encoded form value, restoring + characters as spaces.
    /// </summary>
    /// <param name="value">The encoded form value.</param>
    /// <returns>The decoded form value.</returns>
    private static string FormDecode(string value) =>
        Uri.UnescapeDataString(value.Replace('+', ' '));
}