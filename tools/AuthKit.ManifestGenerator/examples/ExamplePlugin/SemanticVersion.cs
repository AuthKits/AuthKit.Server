namespace ExamplePlugin;

/// <summary>
/// Represents semantic version consisting of major, minor, and patch
/// version components.
/// </summary>
/// <remarks>
/// <para>
/// The version components are compared in hierarchical order: major first,
/// followed by minor and patch. A higher major, minor, or patch component
/// represents higher version.
/// </para>
/// <para>
/// Equality is based on all three version components. The textual
/// representation follows the <c>major.minor.patch</c> format.
/// </para>
/// </remarks>
public readonly struct SemanticVersion :
    IComparable<SemanticVersion>,
    IEquatable<SemanticVersion>
{
    /// <summary>
    /// Gets the major version component.
    /// </summary>
    public int Major { get; }

    /// <summary>
    /// Gets the minor version component.
    /// </summary>
    public int Minor { get; }

    /// <summary>
    /// Gets the patch version component.
    /// </summary>
    public int Patch { get; }

    /// <summary>
    /// Initializes new instance of the <see cref="SemanticVersion"/> struct.
    /// </summary>
    /// <param name="major">The major version component.</param>
    /// <param name="minor">The minor version component.</param>
    /// <param name="patch">The patch version component.</param>
    public SemanticVersion(int major, int minor, int patch)
    {
        Major = major;
        Minor = minor;
        Patch = patch;
    }

    /// <summary>
    /// Compares this version with another semantic version.
    /// </summary>
    /// <param name="other">
    /// The <see cref="SemanticVersion"/> to compare with this instance.
    /// </param>
    /// <returns>
    /// A value less than zero if this version precedes
    /// <paramref name="other"/>, zero if both versions are equal, or a value
    /// greater than zero if this version follows <paramref name="other"/>.
    /// </returns>
    public int CompareTo(SemanticVersion other)
    {
        if (Major != other.Major)
        {
            return Major.CompareTo(other.Major);
        }

        if (Minor != other.Minor)
        {
            return Minor.CompareTo(other.Minor);
        }

        return Patch.CompareTo(other.Patch);
    }

    /// <summary>
    /// Determines whether this version is equal to another semantic version.
    /// </summary>
    /// <param name="other">
    /// The <see cref="SemanticVersion"/> to compare with this instance.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when all version components are equal;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    public bool Equals(SemanticVersion other)
    {
        return Major == other.Major &&
               Minor == other.Minor &&
               Patch == other.Patch;
    }

    /// <summary>
    /// Determines whether this version is equal to the specified object.
    /// </summary>
    /// <param name="obj">The object to compare with this version. </param>
    /// <returns>
    /// <see langword="true"/> when <paramref name="obj"/> is a
    /// <see cref="SemanticVersion"/> with identical version components;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    public override bool Equals(object? obj)
        => obj is SemanticVersion other && Equals(other);

    /// <summary>
    /// Returns the hash code for this semantic version.
    /// </summary>
    /// <returns>A hash code based on the major, minor, and patch version components. </returns>
    public override int GetHashCode()
        => HashCode.Combine(Major, Minor, Patch);

    /// <summary>
    /// Returns the string representation of this semantic version.
    /// </summary>
    /// <returns>The version formatted as <c>major.minor.patch</c>. </returns>
    public override string ToString()
        => $"{Major}.{Minor}.{Patch}";
}