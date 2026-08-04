using System.Text.Json;
using System.Text.RegularExpressions;
using PincabToolbox.Core.Profiles;

namespace PincabToolbox.Core.Services;

public sealed class VpsGame
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public string Manufacturer { get; init; } = "";
    public int? Year { get; init; }
    public List<VpsTableFile> TableFiles { get; } = new();
}

public sealed class VpsTableFile
{
    public string Id { get; init; } = "";
    public string? Version { get; init; }
    public long? UpdatedAt { get; init; }
    /// <summary>"VPX", "FP", "FX3"… — null on older entries.</summary>
    public string? TableFormat { get; init; }
}

/// <summary>
/// Downloads and caches the open-source Virtual Pinball Spreadsheet database
/// (JSON on GitHub — a legal, API-key-free source). Offline-tolerant: on any
/// failure returns the stale cache or null, never throws.
/// </summary>
public sealed partial class VpsDatabase
{
    private readonly UpdateSource _source;
    private readonly string _cacheFile;

    public VpsDatabase(UpdateSource source, string? cacheDir = null)
    {
        _source = source;
        cacheDir ??= Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PincabToolbox");
        Directory.CreateDirectory(cacheDir);
        _cacheFile = Path.Combine(cacheDir, "vpsdb.json");
    }

    public string CacheFile => _cacheFile;

    public async Task<List<VpsGame>?> LoadAsync(CancellationToken ct = default)
    {
        var json = await GetJsonAsync(ct).ConfigureAwait(false);
        if (json is null) return null;
        try { return Parse(json); }
        catch { return null; }
    }

    private async Task<string?> GetJsonAsync(CancellationToken ct)
    {
        // Fresh cache?
        try
        {
            if (File.Exists(_cacheFile) &&
                DateTime.UtcNow - File.GetLastWriteTimeUtc(_cacheFile) < TimeSpan.FromHours(Math.Max(1, _source.CacheHours)))
                return await File.ReadAllTextAsync(_cacheFile, ct).ConfigureAwait(false);
        }
        catch { /* fall through to download */ }

        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        http.DefaultRequestHeaders.UserAgent.ParseAdd("PincabToolbox/0.1 (+free scanner)");

        foreach (var url in _source.Urls)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                var json = await http.GetStringAsync(url, ct).ConfigureAwait(false);
                if (json.Length > 100)
                {
                    try { await File.WriteAllTextAsync(_cacheFile, json, ct).ConfigureAwait(false); } catch { }
                    return json;
                }
            }
            catch { /* try next url */ }
        }

        // Stale cache is better than nothing.
        try
        {
            if (File.Exists(_cacheFile))
                return await File.ReadAllTextAsync(_cacheFile, ct).ConfigureAwait(false);
        }
        catch { }
        return null;
    }

    /// <summary>Parses the VPS db JSON — tolerant to either a root array or an object of games.</summary>
    public static List<VpsGame> Parse(string json)
    {
        var games = new List<VpsGame>();
        using var doc = JsonDocument.Parse(json);

        IEnumerable<JsonElement> items = doc.RootElement.ValueKind switch
        {
            JsonValueKind.Array => doc.RootElement.EnumerateArray().ToArray(),
            JsonValueKind.Object => doc.RootElement.EnumerateObject().Select(p => p.Value).ToArray(),
            _ => Array.Empty<JsonElement>(),
        };

        foreach (var el in items)
        {
            if (el.ValueKind != JsonValueKind.Object) continue;
            var game = new VpsGame
            {
                Id = GetString(el, "id") ?? "",
                Name = GetString(el, "name") ?? "",
                Manufacturer = GetString(el, "manufacturer") ?? "",
                Year = GetInt(el, "year"),
            };
            if (el.TryGetProperty("tableFiles", out var tf) && tf.ValueKind == JsonValueKind.Array)
            {
                foreach (var t in tf.EnumerateArray())
                {
                    if (t.ValueKind != JsonValueKind.Object) continue;
                    game.TableFiles.Add(new VpsTableFile
                    {
                        Id = GetString(t, "id") ?? "",
                        Version = GetString(t, "version"),
                        UpdatedAt = GetLong(t, "updatedAt"),
                        TableFormat = GetString(t, "tableFormat"),
                    });
                }
            }
            if (game.Name.Length > 0) games.Add(game);
        }
        return games;
    }

    private static string? GetString(JsonElement el, string prop) =>
        el.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    private static int? GetInt(JsonElement el, string prop) =>
        el.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.Number && v.TryGetInt32(out var i) ? i : null;

    private static long? GetLong(JsonElement el, string prop) =>
        el.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.Number && v.TryGetInt64(out var l) ? l : null;

    // ---------- matching helpers ----------

    [GeneratedRegex(@"^(?<name>.+?)\s*\((?<manuf>[^)]*?)\s*(?<year>\d{4})\)")]
    private static partial Regex FileNamePattern();

    [GeneratedRegex(@"[^a-z0-9]+")]
    private static partial Regex NonAlnum();

    public static string Normalize(string s) => NonAlnum().Replace(s.ToLowerInvariant(), "");

    /// <summary>Parses "Table Name (Manufacturer Year) v1.2" style file names.</summary>
    public static (string name, int? year)? ParseTableFileName(string fileNameWithoutExt)
    {
        var m = FileNamePattern().Match(fileNameWithoutExt);
        if (!m.Success) return null;
        return (m.Groups["name"].Value.Trim(), int.TryParse(m.Groups["year"].Value, out var y) ? y : null);
    }

    /// <summary>Finds the VPS game entry matching a local table file name.</summary>
    public static VpsGame? Match(List<VpsGame> games, string fileNameWithoutExt)
    {
        var parsed = ParseTableFileName(fileNameWithoutExt);
        if (parsed is null) return null;
        var normName = Normalize(parsed.Value.name);
        if (normName.Length == 0) return null;

        VpsGame? best = null;
        foreach (var g in games)
        {
            if (Normalize(g.Name) != normName) continue;
            if (parsed.Value.year is int y && g.Year is int gy && y != gy) continue;
            best = g;
            if (parsed.Value.year is not null && g.Year is not null) break; // exact name+year
        }
        return best;
    }

    /// <summary>Compares dotted version strings; returns &gt;0 when a &gt; b. Unparseable → 0.</summary>
    public static int CompareVersions(string? a, string? b)
    {
        if (string.IsNullOrWhiteSpace(a) || string.IsNullOrWhiteSpace(b)) return 0;
        static int[] Parts(string v) =>
            v.Trim().TrimStart('v', 'V')
             .Split('.', StringSplitOptions.RemoveEmptyEntries)
             .Select(p => int.TryParse(new string(p.TakeWhile(char.IsDigit).ToArray()), out var n) ? n : 0)
             .ToArray();
        var pa = Parts(a);
        var pb = Parts(b);
        if (pa.Length == 0 || pb.Length == 0) return 0;
        for (int i = 0; i < Math.Max(pa.Length, pb.Length); i++)
        {
            int va = i < pa.Length ? pa[i] : 0;
            int vb = i < pb.Length ? pb[i] : 0;
            if (va != vb) return va.CompareTo(vb);
        }
        return 0;
    }
}
