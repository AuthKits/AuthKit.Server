using System.Text.Json;
using System.Text.Json.Serialization;
using AuthKit.Plugins.Abstractions.Models;

namespace AuthKit.ManifestGenerator.Core.Generators;

/// <summary>
/// Provides JSON serialization and deserialization support for
/// <see cref="SemanticVersion"/> values.
/// </summary>
/// <remarks>
/// <para>
/// The converter represents semantic versions as their standard string
/// representation in generated JSON manifests.
/// </para>
/// <para>
/// During deserialization, the converter validates the input using
/// <see cref="SemanticVersion.TryParse(string?, out SemanticVersion)"/>.
/// Invalid values result in a <see cref="JsonException"/>.
/// </para>
/// </remarks>
public sealed class SemanticVersionJsonConverter : JsonConverter<SemanticVersion>
{
    /// <summary>
    /// Reads and parses a <see cref="SemanticVersion"/> value from JSON.
    /// </summary>
    /// <param name="reader">
    /// The JSON reader used to read the semantic version value.
    /// </param>
    /// <param name="typeToConvert">
    /// The type being converted.
    /// </param>
    /// <param name="options">
    /// The serializer options used for the conversion.
    /// </param>
    /// <returns>
    /// The parsed <see cref="SemanticVersion"/> value.
    /// </returns>
    /// <exception cref="JsonException">
    /// Thrown when the JSON value does not contain a valid semantic version.
    /// </exception>
    public override SemanticVersion Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        var value = reader.GetString();

        if (SemanticVersion.TryParse(value, out var version))
        {
            return version;
        }

        throw new JsonException(
            "Invalid SemanticVersion format.");
    }

    /// <summary>
    /// Writes a <see cref="SemanticVersion"/> value to JSON.
    /// </summary>
    /// <param name="writer">
    /// The JSON writer used to write the converted value.
    /// </param>
    /// <param name="value">
    /// The semantic version to serialize.
    /// </param>
    /// <param name="options">
    /// The serializer options used for the conversion.
    /// </param>
    public override void Write(
        Utf8JsonWriter writer,
        SemanticVersion value,
        JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}