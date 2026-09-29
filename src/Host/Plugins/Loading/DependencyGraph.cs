using AuthKit.Plugins.Abstractions.Contracts.Discovery;

namespace Host.Plugins.Loading;

/// <summary>
/// Dependency graph over accepted candidates: validation and deterministic
/// topological ordering.
/// </summary>
/// <remarks>
/// <para>
/// Ordering is Kahn's algorithm: at each step, among the currently
/// dependency-ready plugins, the next is picked by <c>Priority</c> ascending,
/// then by stable registration (discovery) order ascending. The result is never
/// re-sorted afterward, so dependency edges always hold.
/// </para>
/// </remarks>
internal static class DependencyGraph
{
    /// <summary>
    /// Validates the dependency graph of accepted candidates.
    /// </summary>
    /// <param name="accepted">Gate-accepted candidates with their registration index.</param>
    /// <param name="knownIds">All discovered ids (accepted or not).</param>
    /// <exception cref="InvalidOperationException">
    /// A dependency id is entirely unknown, or the accepted graph contains a cycle.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// A candidate has no manifest.
    /// </exception>
    public static void Validate(
        IReadOnlyList<(DiscoveredPlugin Candidate, int RegistrationOrder)> accepted,
        IReadOnlySet<string> knownIds)
    {
        var nodes = Nodes(accepted);

        foreach (var node in nodes)
        {
            foreach (var dependency in node.DependsOn)
            {
                if (!knownIds.Contains(dependency))
                    throw new InvalidOperationException(
                        $"Plugin '{node.Id}' depends on unknown plugin '{dependency}'.");
            }
        }

        if (FindCycle(nodes) is { } cycle)
            throw new InvalidOperationException(
                $"Plugin dependency cycle detected: {string.Join(" -> ", cycle)}.");
    }

    /// <summary>
    /// Topologically sorts accepted candidates: dependencies first, ties broken
    /// by priority, then registration order.
    /// </summary>
    /// <param name="accepted">Gate-accepted candidates with their registration index.</param>
    /// <returns>Candidates in load order.</returns>
    /// <exception cref="ArgumentException">
    /// A candidate has no manifest.
    /// </exception>
    public static IReadOnlyList<DiscoveredPlugin> Sort(
        IReadOnlyList<(DiscoveredPlugin Candidate, int RegistrationOrder)> accepted)
    {
        var nodes = Nodes(accepted);
        var byId = nodes.ToDictionary(node => node.Id, StringComparer.OrdinalIgnoreCase);
        var pending = nodes.ToDictionary(
            node => node.Id,
            node => node.DependsOn.Length,
            StringComparer.OrdinalIgnoreCase);

        var ready = nodes
            .Where(node => pending[node.Id] == 0)
            .OrderBy(node => node.Priority)
            .ThenBy(node => node.RegistrationOrder)
            .Select(node => node.Id)
            .ToList();

        var order = new List<DiscoveredPlugin>();
        while (ready.Count > 0)
        {
            var id = ready[0];
            ready.RemoveAt(0);
            order.Add(byId[id].Candidate);

            foreach (var node in nodes)
            {
                if (!node.DependsOn.Contains(id))
                    continue;

                pending[node.Id]--;
                if (pending[node.Id] == 0)
                    InsertReady(ready, byId, node);
            }
        }

        return order;
    }

    private static void InsertReady(
        List<string> ready,
        Dictionary<string, GraphNode> byId,
        GraphNode node)
    {
        var index = ready.FindIndex(existing =>
            byId[existing].Priority > node.Priority ||
            (byId[existing].Priority == node.Priority &&
             byId[existing].RegistrationOrder > node.RegistrationOrder));

        if (index < 0)
            ready.Add(node.Id);
        else
            ready.Insert(index, node.Id);
    }

    private static IReadOnlyList<string>? FindCycle(IReadOnlyList<GraphNode> nodes)
    {
        var ids = nodes.Select(node => node.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var byId = nodes.ToDictionary(node => node.Id, StringComparer.OrdinalIgnoreCase);
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var stack = new Stack<string>();

        foreach (var node in nodes)
        {
            if (Dfs(node.Id, byId, ids, visited, stack) is { } cycle)
                return cycle;
        }

        return null;
    }

    private static IReadOnlyList<string>? Dfs(
        string id,
        Dictionary<string, GraphNode> byId,
        HashSet<string> ids,
        HashSet<string> visited,
        Stack<string> stack)
    {
        if (stack.Contains(id, StringComparer.OrdinalIgnoreCase))
            return stack.Reverse().Append(id).ToList();

        if (!visited.Add(id))
            return null;

        stack.Push(id);
        try
        {
            foreach (var dependency in byId[id].DependsOn)
            {
                if (!ids.Contains(dependency))
                    continue;

                if (Dfs(dependency, byId, ids, visited, stack) is { } cycle)
                    return cycle;
            }

            return null;
        }
        finally
        {
            stack.Pop();
        }
    }

    private static IReadOnlyList<GraphNode> Nodes(
        IReadOnlyList<(DiscoveredPlugin Candidate, int RegistrationOrder)> accepted) =>
        accepted.Select(entry => entry.Candidate.Manifest is { } manifest
            ? new GraphNode(
                entry.Candidate,
                manifest.Id,
                manifest.Priority,
                manifest.DependsOn.Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
                entry.RegistrationOrder)
            : throw new ArgumentException(
                $"Candidate at '{entry.Candidate.Location}' has no manifest.",
                nameof(accepted))).ToList();

    private sealed record GraphNode(
        DiscoveredPlugin Candidate,
        string Id,
        int Priority,
        string[] DependsOn,
        int RegistrationOrder);
}
