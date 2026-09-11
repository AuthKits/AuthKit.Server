using System.Text.Json.Serialization;

namespace AuthKit.Plugins.Abstractions.Models;

/// <summary>
/// Immutable Semantic Versioning 2.0.0 value type.
/// </summary>
/// <remarks>
/// Build metadata does not affect equality or precedence, as defined by
/// Semantic Versioning 2.0.0.
/// </remarks>
[JsonConverter(typeof(SemanticVersionJsonConverter))]
public readonly record struct SemanticVersion : IComparable<SemanticVersion>
{
    public int Major { get; }
    public int Minor { get; }
    public int Patch { get; }

    public string? PreRelease { get; }
    public string? Build { get; }

    public SemanticVersion(
        int major,
        int minor,
        int patch,
        string? preRelease = null,
        string? build = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(major);
        ArgumentOutOfRangeException.ThrowIfNegative(minor);
        ArgumentOutOfRangeException.ThrowIfNegative(patch);

        ValidateIdentifiers(preRelease, nameof(preRelease));
        ValidateIdentifiers(build, nameof(build));

        Major = major;
        Minor = minor;
        Patch = patch;
        PreRelease = preRelease;
        Build = build;
    }

    public override string ToString()
    {
        var value = $"{Major}.{Minor}.{Patch}";

        if (PreRelease is not null)
            value += $"-{PreRelease}";

        if (Build is not null)
            value += $"+{Build}";

        return value;
    }

    public static bool TryParse(
        string? input,
        out SemanticVersion version)
    {
        version = default;

        if (string.IsNullOrWhiteSpace(input))
            return false;

        var value = input.Trim();

        var buildSeparator = value.IndexOf('+');
        var build = buildSeparator >= 0
            ? value[(buildSeparator + 1)..]
            : null;

        if (buildSeparator >= 0)
            value = value[..buildSeparator];

        var preReleaseSeparator = value.IndexOf('-');
        var preRelease = preReleaseSeparator >= 0
            ? value[(preReleaseSeparator + 1)..]
            : null;

        if (preReleaseSeparator >= 0)
            value = value[..preReleaseSeparator];

        var parts = value.Split('.');

        if (parts.Length != 3)
            return false;

        if (!TryParseCore(parts[0], out var major) ||
            !TryParseCore(parts[1], out var minor) ||
            !TryParseCore(parts[2], out var patch))
        {
            return false;
        }

        if (!ValidateIdentifiers(preRelease) ||
            !ValidateIdentifiers(build))
        {
            return false;
        }

        version = new SemanticVersion(
            major,
            minor,
            patch,
            preRelease,
            build);

        return true;
    }

    public static SemanticVersion Parse(string input)
    {
        if (!TryParse(input, out var version))
            throw new FormatException(
                $"Invalid semantic version: '{input}'");

        return version;
    }

    public int CompareTo(SemanticVersion other)
    {
        var result = Major.CompareTo(other.Major);

        if (result != 0)
            return result;

        result = Minor.CompareTo(other.Minor);

        if (result != 0)
            return result;

        result = Patch.CompareTo(other.Patch);

        if (result != 0)
            return result;

        return ComparePreRelease(PreRelease, other.PreRelease);
    }

    public bool Equals(SemanticVersion other)
    {
        return Major == other.Major &&
               Minor == other.Minor &&
               Patch == other.Patch &&
               string.Equals(
                   PreRelease,
                   other.PreRelease,
                   StringComparison.Ordinal);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(
            Major,
            Minor,
            Patch,
            PreRelease);
    }

    public static bool operator <(
        SemanticVersion left,
        SemanticVersion right) =>
        left.CompareTo(right) < 0;

    public static bool operator <=(SemanticVersion left, SemanticVersion right) =>
        left.CompareTo(right) <= 0;

    public static bool operator >(SemanticVersion left, SemanticVersion right) =>
        left.CompareTo(right) > 0;

    public static bool operator >=(SemanticVersion left, SemanticVersion right) =>
        left.CompareTo(right) >= 0;

    private static int ComparePreRelease(
        string? left,
        string? right)
    {
        // A release version has higher precedence than a prerelease.
        if (left is null && right is null)
            return 0;

        if (left is null)
            return 1;

        if (right is null)
            return -1;

        var leftParts = left.Split('.');
        var rightParts = right.Split('.');

        var length = Math.Min(
            leftParts.Length,
            rightParts.Length);

        for (var i = 0; i < length; i++)
        {
            var result = CompareIdentifier(
                leftParts[i],
                rightParts[i]);

            if (result != 0)
                return result;
        }

        return leftParts.Length.CompareTo(rightParts.Length);
    }

    private static int CompareIdentifier(
        string left,
        string right)
    {
        var leftNumeric = IsNumeric(left);
        var rightNumeric = IsNumeric(right);

        return leftNumeric switch
        {
            true when rightNumeric => CompareNumericIdentifiers(left, right),
            true => -1,
            _ => rightNumeric ? 1 : string.CompareOrdinal(left, right)
        };
    }

    private static int CompareNumericIdentifiers(
        string left,
        string right)
    {
        // Both identifiers are valid numeric identifiers without leading zeros,
        // so length comparison is enough before ordinal comparison.
        var result = left.Length.CompareTo(right.Length);

        return result != 0
            ? result
            : string.CompareOrdinal(left, right);
    }

    private static bool TryParseCore(
        string value,
        out int result)
    {
        result = 0;

        switch (value.Length)
        {
            case 0:
            case > 1 when value[0] == '0':
                return false;
            default:
                return int.TryParse(
                    value,
                    out result);
        }
    }

    private static void ValidateIdentifiers(string? value,
        string parameterName)
    {
        if (!ValidateIdentifiers(value))
            throw new ArgumentException(
                $"Invalid SemVer identifier: '{value}'.",
                parameterName);
    }

    private static bool ValidateIdentifiers(string? value)
    {
        if (value is null)
            return true;

        if (value.Length == 0)
            return false;

        var identifiers = value.Split('.');

        foreach (var identifier in identifiers)
        {
            if (identifier.Length == 0)
                return false;

            var numeric = true;

            foreach (var c in identifier)
            {
                if (c is >= '0' and <= '9')
                    continue;

                numeric = false;

                if (c is not
                    (>= '0' and <= '9') and
                    not (>= 'A' and <= 'Z') and
                    not (>= 'a' and <= 'z') and
                    not '-')
                {
                    return false;
                }
            }

            if (numeric &&
                identifier.Length > 1 &&
                identifier[0] == '0')
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsNumeric(string value)
    {
        foreach (var c in value)
        {
            if (c is < '0' or > '9')
                return false;
        }

        return true;
    }
}
