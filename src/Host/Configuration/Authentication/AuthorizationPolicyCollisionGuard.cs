using System.Collections;
using System.Reflection;
using AuthKit.Plugins.Abstractions.Contracts.PluginContract;
using Host.Plugins.Loading;
using Microsoft.AspNetCore.Authorization;

namespace Host.Configuration.Authentication;

/// <summary>
/// Detects authorization policy name collisions between loaded plugins.
/// </summary>
/// <remarks>
/// <para>
/// ASP.NET Core's <see cref="AuthorizationOptions.AddPolicy(string, AuthorizationPolicy)"/>
/// silently replaces a previously registered policy when the same name is added twice, which
/// would let a plugin silently overwrite another plugin's policy. To keep collisions explicit,
/// the host snapshots the policy map before and after each plugin's
/// <see cref="IAuthKitPlugin.ConfigureAuthorization"/> hook
/// and fails startup when a name already owned by another plugin is registered again.
/// </para>
/// <para>
/// <see cref="AuthorizationOptions"/> does not expose a public way to enumerate previously
/// registered policy names. This guard reads the internal policy map through reflection. The
/// layout is pinned to the target framework (.NET 10); if the runtime layout changes, an
/// explanatory exception is thrown instead of silently allowing overwrites.
/// </para>
/// </remarks>
internal static class AuthorizationPolicyCollisionGuard
{
    private static readonly Func<AuthorizationOptions, IReadOnlySet<string>> PolicyNameReader =
        CreatePolicyNameReader();

    /// <summary>
    /// Runs every plugin's authorization configuration against the host options while
    /// rejecting duplicate policy ownership.
    /// </summary>
    /// <param name="options">The actual host authorization options.</param>
    /// <param name="plugins">The loaded plugins, processed in ascending plugin ID order.</param>
    /// <exception cref="InvalidOperationException">
    /// A policy name is registered by more than one plugin.
    /// </exception>
    public static void Configure(
        AuthorizationOptions options,
        IReadOnlyList<LoadedPlugin> plugins)
    {
        var owners = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var plugin in plugins.OrderBy(plugin => plugin.Plugin.Id, StringComparer.Ordinal))
        {
            var before = PolicyNameReader(options);
            plugin.Plugin.ConfigureAuthorization(options);
            var after = PolicyNameReader(options);

            foreach (var name in after)
            {
                if (!before.Contains(name) && !owners.ContainsKey(name))
                {
                    owners[name] = plugin.Plugin.Id;
                    continue;
                }

                var owner = owners.TryGetValue(name, out var existing)
                    ? existing
                    : "<host>";
                throw new InvalidOperationException(
                    $"Authorization policy '{name}' is registered by both plugin '{owner}' " +
                    $"and plugin '{plugin.Plugin.Id}'. Authorization policy names are globally " +
                    "significant and each policy must be owned by exactly one plugin. Use " +
                    "namespaced policy names (for example 'PluginName.Read') to avoid collisions.");
            }
        }
    }

    private static Func<AuthorizationOptions, IReadOnlySet<string>> CreatePolicyNameReader()
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;

        var singleMap = typeof(AuthorizationOptions).GetField("<PolicyMap>k__BackingField", flags);
        if (singleMap is not null)
        {
            return options => ReadKeys(singleMap.GetValue(options));
        }

        var policyMap = typeof(AuthorizationOptions).GetField("_policyMap", flags);
        var configurePolicyMap = typeof(AuthorizationOptions).GetField("_configurePolicyMap", flags);
        if (policyMap is not null && configurePolicyMap is not null)
        {
            return options =>
            {
                var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                names.UnionWith(ReadKeys(policyMap.GetValue(options)));
                names.UnionWith(ReadKeys(configurePolicyMap.GetValue(options)));
                return names;
            };
        }

        throw new NotSupportedException(
            "The internal policy map of Microsoft.AspNetCore.Authorization.AuthorizationOptions has an " +
            "unexpected layout for the targeted .NET runtime, so plugin authorization policy collisions " +
            "cannot be detected. This host intentionally does not start rather than silently overwriting " +
            "policies registered by plugins.");
    }

    private static IReadOnlySet<string> ReadKeys(object? map)
    {
        if (map is not IDictionary dictionary)
        {
            throw new NotSupportedException(
                "The internal policy map of Microsoft.AspNetCore.Authorization.AuthorizationOptions is not " +
                "an enumerable dictionary for the targeted .NET runtime, so plugin authorization policy " +
                "collisions cannot be detected.");
        }

        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var key in dictionary.Keys)
        {
            names.Add((string)key!);
        }

        return names;
    }
}