namespace Host.Security.BodyParsers;

/// <summary>
/// Extracts an API key field from a request body payload of a specific format.
/// </summary>
/// <remarks>
/// <para>
/// Implementations are selected by content type, allowing new request body
/// formats to be supported without modifying existing code.
/// </para>
/// <para>
/// The parser is expected to return <c>null</c> when the requested field is
/// absent or the payload cannot be parsed.
/// </para>
/// </remarks>
public interface IApiKeyBodyParser
{
    /// <summary>
    /// Determines whether this parser can handle the given content type.
    /// </summary>
    /// <param name="contentType">The request content type value.</param>
    /// <returns>
    /// <c>true</c> when the parser can handle the content type; otherwise,
    /// <c>false</c>.
    /// </returns>
    bool CanHandle(string? contentType);

    /// <summary>
    /// Extracts the value of the named field from the request body.
    /// </summary>
    /// <param name="body">The raw request body payload.</param>
    /// <param name="fieldName">The name of the field holding the API key.</param>
    /// <returns>
    /// The extracted API key, or <c>null</c> when the field is absent.
    /// </returns>
    string? Parse(string body, string fieldName);
}