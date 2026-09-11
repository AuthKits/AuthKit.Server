using System.Text.Json;
using System.Text.Json.Serialization;

namespace AuthKit.Plugins.Abstractions.Models;

/// <summary>
/// Converter for <see cref="SemanticVersion"/> type.
/// Parses JSON string into <see cref="SemanticVersion"/> value and
/// writes the semantic version string back to JSON.
/// </summary>
/// <remarks>
/// Build metadata does not affect equality or precedence, as defined by
/// Semantic Versioning 2.0.0.
/// </remarks>
public sealed class SemanticVersionJsonConverter : JsonConverter<SemanticVersion>
{
    /// <summary>
    /// Parses JSON string into <see cref="SemanticVersion"/> value.
    /// </summary>
    /// <param name="reader">The reader to read from.</param>
    /// <param name="typeToConvert">The type to convert to.</param>
    /// <param name="options">Serializer options.</param>
    /// <returns>The parsed <see cref="SemanticVersion"/> value.</returns>
    /// <exception cref="JsonException">Thrown if the reader token is not a string.</exception>
    public override SemanticVersion Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var s = reader.GetString()!;
            return SemanticVersion.Parse(s);
        }

        throw new JsonException("Expected JSON string for SemanticVersion");
    }

    /// <summary>
    /// Writes the <see cref="SemanticVersion"/> value as JSON string.
    /// </summary>
    /// <param name="writer">The writer to write to.</param>
    /// <param name="value">The value to write.</param>
    /// <param name="options">Serializer options.</param>
    public override void Write(Utf8JsonWriter writer, SemanticVersion value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.ToString());
}