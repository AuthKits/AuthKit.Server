using System.Text.Json;

namespace Host.Security.BodyParsers;

/// <summary>
/// Extracts an API key field from a JSON request body.
/// </summary>
/// <remarks>
/// <para>
/// Field names are matched using case-insensitive comparison, and only
/// string-valued properties are considered.
/// </para>
/// <para>Invalid JSON payloads are ignored and result in null.</para>
/// </remarks>
public sealed class JsonApiKeyBodyParser(
    ILogger<JsonApiKeyBodyParser> logger) : IApiKeyBodyParser
{
    /// <summary>
    /// Determines whether this parser supports the specified content type.
    /// </summary>
    /// <param name="contentType">The request content type.</param>
    /// <returns>true if the content type is application/json otherwise, false </returns>
    public bool CanHandle(string? contentType) =>
        !string.IsNullOrEmpty(contentType)
        && contentType.Contains(
            "application/json",
            StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Extracts an API key from the specified JSON field.
    /// </summary>
    /// <param name="body">The JSON request body.</param>
    /// <param name="fieldName">The name of the field containing the API key.</param>
    /// <returns>
    /// The API key value, or null if the field is not found or
    /// the request body contains invalid or unsupported JSON.
    /// </returns>
    public string? Parse(string body, string fieldName)
    {
        if (!body.AsSpan().TrimStart().StartsWith('{'))
            return null;

        try
        {
            using var doc = JsonDocument.Parse(body);

            if (doc.RootElement.ValueKind != JsonValueKind.Object)
                return null;

            foreach (var property in doc.RootElement.EnumerateObject())
            {
                if (property.Value.ValueKind == JsonValueKind.String
                    && string.Equals(
                        property.Name,
                        fieldName,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return property.Value.GetString();
                }
            }
        }
        catch (JsonException ex)
        {
            logger.LogDebug(
                ex,
                "Request body is not valid JSON; skipping JSON parsing");
        }

        return null;
    }
}