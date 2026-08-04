namespace PincabToolbox.Core.Services;

/// <summary>
/// Parses VPMAlias.txt — lines of "alias,target" mapping a script's cGameName
/// to the real ROM set name VPinMAME should load.
/// </summary>
public static class AliasFile
{
    public static Dictionary<string, string> Parse(string path)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (!File.Exists(path)) return map;
        foreach (var raw in File.ReadAllLines(path))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith("//") || line.StartsWith('#') || line.StartsWith('\'')) continue;
            var idx = line.IndexOf(',');
            if (idx <= 0 || idx >= line.Length - 1) continue;
            var alias = line[..idx].Trim();
            var target = line[(idx + 1)..].Trim();
            if (alias.Length > 0 && target.Length > 0)
                map[alias] = target;
        }
        return map;
    }
}
