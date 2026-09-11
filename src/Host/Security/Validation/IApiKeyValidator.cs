namespace Host.Security.Validation;

/// <summary>
/// Validates extracted API key credentials and produces authenticated principals.
/// </summary>
/// <remarks>
/// Implementations determine how API keys are authenticated and may validate
/// them against a plugin database or another credential store.
/// </remarks>
public interface IApiKeyValidator
{
    /// <summary>
    /// Validates an API key and produces the authenticated principal.
    /// </summary>
    /// <param name="apiKey">The raw API key credential.</param>
    /// <returns>An <see cref="ApiKeyPrincipal"/> when the key is valid; otherwise, null
    /// </returns>
    Task<ApiKeyPrincipal?> ValidateAsync(string apiKey);
}