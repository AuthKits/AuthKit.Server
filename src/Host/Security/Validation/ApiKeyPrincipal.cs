using System.Security.Claims;

namespace Host.Security.Validation;

/// <summary>
/// Represents the principal produced after successful API key validation.
/// </summary>
/// <remarks>
/// <para>
/// Contains the stable subject identifier and claims associated with the
/// authenticated API key.
/// </para>
/// </remarks>
public sealed class ApiKeyPrincipal
{
    /// <summary>
    /// Gets or sets the stable identifier of the subject that owns the
    /// validated API key.
    /// </summary>
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the claims associated with the authenticated identity.
    /// </summary>
    public IEnumerable<Claim> Claims { get; set; } = [];
}