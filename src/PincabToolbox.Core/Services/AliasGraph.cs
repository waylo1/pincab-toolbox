namespace PincabToolbox.Core.Services;

/// <summary>
/// Cycle detection over a VPMAlias.txt mapping (alias -&gt; target ROM name), parsed by the existing
/// <see cref="AliasFile"/> reader. VPinMAME resolves an alias by following the mapping until it
/// reaches a name that is no longer itself an alias key; if the chain instead loops back on itself,
/// VPinMAME recurses forever and crashes with a stack overflow the instant a table needs that ROM
/// name. Deterministic: the mapping is data read verbatim from disk, not a judgement call — a loop
/// either exists in the file or it does not.
/// </summary>
public static class AliasGraph
{
    /// <summary>
    /// Every cycle reachable from the map's keys, each reported once as the ordered list of aliases
    /// that form the loop (e.g. ["A", "B"] for A -&gt; B -&gt; A -&gt; ...). A key that maps to itself is a
    /// one-element cycle. Returns an empty list for an acyclic (or empty) map — including the
    /// overwhelmingly common case of a flat one-hop alias table, which every real VPMAlias.txt is.
    /// </summary>
    public static List<List<string>> FindCycles(IReadOnlyDictionary<string, string> aliases)
    {
        var cycles = new List<List<string>>();
        // Nodes already walked to completion (either an acyclic tail, or absorbed into a cycle
        // already reported from an earlier start) — never re-walked from another starting alias.
        var resolved = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var start in aliases.Keys)
        {
            if (resolved.Contains(start)) continue;

            var path = new List<string>();
            var indexInPath = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var current = start;

            while (aliases.TryGetValue(current, out var next))
            {
                if (indexInPath.TryGetValue(current, out var idx))
                {
                    // The walk revisited a node from THIS path: path[idx..] is the loop. Any acyclic
                    // lead-in before idx (e.g. "A" in A->B->C->B) is correctly excluded.
                    cycles.Add(path.Skip(idx).ToList());
                    break;
                }
                if (resolved.Contains(current)) break; // completed from an earlier start; stop here

                indexInPath[current] = path.Count;
                path.Add(current);
                current = next;
            }

            foreach (var node in path) resolved.Add(node);
        }

        return cycles;
    }
}
