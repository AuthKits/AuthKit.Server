using System.Text;

namespace DevTools.Catalog;

/// <summary>
/// Extracts leading <c>//</c> doc comments from a <c>.proto</c> source file
/// so the UI can show method and field descriptions like Swagger does.
/// </summary>
/// <remarks>
/// The generated descriptors do not carry source comments (protoc is invoked
/// without source info), so the catalog resolves comments by reading the
/// original file that was copied next to the host binaries at build time.
/// Only <c>//</c> line comments directly above a declaration are collected;
/// block comments and summary banners are ignored.
/// </remarks>
public static class ProtoDocComments
{
    /// <summary>
    /// Resolves the on-disk path of a <c>.proto</c> file for the given
    /// descriptor file name (e.g. <c>protos/jwks.proto</c>).
    /// </summary>
    /// <param name="fileName">The descriptor file name relative to the proto root.</param>
    /// <param name="additionalRoots">
    /// Additional directories to search (e.g. plugin output folders) before
    /// falling back to the app base directory.
    /// </param>
    /// <returns>The absolute path when the file exists, otherwise <c>null</c>.</returns>
    public static string? TryResolvePath(string fileName, IEnumerable<string>? additionalRoots = null)
    {
        var roots = new List<string>(additionalRoots ?? [])
        {
            AppContext.BaseDirectory,
            Directory.GetCurrentDirectory()
        };

        foreach (var root in roots)
        {
            foreach (var candidate in PotentialPaths(root, fileName))
            {
                if (File.Exists(candidate))
                    return candidate;
            }
        }

        return null;
    }

    private static IEnumerable<string> PotentialPaths(string root, string fileName)
    {
        var normalized = Normalize(fileName);
        if (Path.IsPathRooted(normalized))
            yield return normalized;

        yield return Path.Combine(root, "Grpc", normalized);
        yield return Path.Combine(root, "protos", normalized);
        yield return Path.Combine(root, normalized);
    }

    private static string Normalize(string fileName) =>
        fileName.Replace('\\', '/')
            .TrimStart('/')
            .Replace('/', Path.DirectorySeparatorChar);

    /// <summary>
    /// Parses the <c>.proto</c> file and returns leading comments keyed by
    /// member: <c>service.Name</c>, <c>service.rpc</c>, <c>message</c> and
    /// <c>message.field</c>.
    /// </summary>
    /// <param name="path">The absolute path of the proto file.</param>
    /// <returns>A map of member key to comment text.</returns>
    public static IReadOnlyDictionary<string, string> Parse(string path)
    {
        var comments = new Dictionary<string, string>(StringComparer.Ordinal);
        var pending = new List<string>();
        var serviceName = "";
        var messageName = "";

        foreach (var rawLine in File.ReadLines(path))
        {
            var line = rawLine.Trim();

            if (line.StartsWith("//", StringComparison.Ordinal))
            {
                pending.Add(line[2..].Trim());
                continue;
            }

            if (line.Length == 0)
            {
                pending.Clear();
                continue;
            }

            if (line.StartsWith("rpc ", StringComparison.Ordinal) && serviceName.Length > 0)
            {
                var rpc = Ident(line, 4);
                if (rpc is not null && pending.Count > 0)
                    comments[$"{serviceName}.{rpc}"] = Join(pending);
                pending.Clear();
                continue;
            }

            if (line.StartsWith("service ", StringComparison.Ordinal))
            {
                var service = Ident(line, 8);
                if (service is not null)
                {
                    if (pending.Count > 0)
                        comments[service] = Join(pending);
                    serviceName = service;
                }
                pending.Clear();
                continue;
            }

            if (line.StartsWith("message ", StringComparison.Ordinal))
            {
                var message = Ident(line, 8);
                if (message is not null)
                {
                    if (pending.Count > 0)
                        comments[message] = Join(pending);
                    messageName = message;
                }
                pending.Clear();
                continue;
            }

            if (messageName.Length > 0 && IsFieldDeclaration(line))
            {
                var field = FieldIdent(line);
                if (field is not null && pending.Count > 0)
                    comments[$"{messageName}.{field}"] = Join(pending);
                pending.Clear();
                continue;
            }

            pending.Clear();
        }

        return comments;
    }

    private static bool IsFieldDeclaration(string line)
    {
        if (!line.EndsWith(';'))
            return false;
        if (line.StartsWith("option ", StringComparison.Ordinal) || line.StartsWith("reserved ", StringComparison.Ordinal) || line.StartsWith("map ", StringComparison.Ordinal))
            return false;
        return line.IndexOf(' ') > 0 && line.IndexOf('=') > 0;
    }

    private static string? Ident(string line, int start)
    {
        var part = line[start..];
        var builder = new StringBuilder();
        foreach (var ch in part)
        {
            if (!char.IsLetterOrDigit(ch) && ch != '_')
                break;
            builder.Append(ch);
        }
        return builder.Length > 0 ? builder.ToString() : null;
    }

    /// <summary>
    /// Extracts the field name from a declaration like
    /// <c>repeated string x5c = 7;</c> or <c>map&lt;string, string&gt; details = 5;</c>.
    /// </summary>
    private static string? FieldIdent(string line)
    {
        var equal = line.IndexOf('=', StringComparison.Ordinal);
        if (equal <= 0)
            return null;

        var start = equal - 1;
        while (start >= 0 && (char.IsLetterOrDigit(line[start]) || line[start] == '_'))
            start--;
        start++;
        return start < equal ? line[start..equal].Trim() : null;
    }

    private static string Join(IReadOnlyList<string> lines) =>
        string.Join(' ', lines.Select(TrimAsterisk)).Trim();

    private static string TrimAsterisk(string value) => value.Trim().TrimStart('*', ' ').Trim();
}