using System.Text.Json;
using System.Text.Json.Serialization;

namespace PincabToolbox.Core.Profiles;

/// <summary>
/// Ecosystem profile — the engine knows nothing about VPX; everything specific
/// (paths, file roles, signatures) comes from a JSON profile so new ecosystems
/// (sim racing, flight sim…) are data, not code.
/// </summary>
public sealed class Profile
{
    [JsonPropertyName("id")] public string Id { get; set; } = "";
    [JsonPropertyName("name")] public string Name { get; set; } = "";

    /// <summary>Candidate relative paths for well-known locations, tried in order.</summary>
    [JsonPropertyName("locations")] public LocationCandidates Locations { get; set; } = new();

    /// <summary>Executable/library roles used by the bitness scanner.</summary>
    [JsonPropertyName("binaryRoles")] public List<BinaryRole> BinaryRoles { get; set; } = new();

    /// <summary>Script signatures used by the compatibility scanner.</summary>
    [JsonPropertyName("scriptSignatures")] public List<ScriptSignature> ScriptSignatures { get; set; } = new();

    /// <summary>Update source (VPS database) configuration.</summary>
    [JsonPropertyName("updateSource")] public UpdateSource UpdateSource { get; set; } = new();

    public static Profile Load(string jsonPath)
    {
        var json = File.ReadAllText(jsonPath);
        return FromJson(json);
    }

    public static Profile FromJson(string json)
    {
        var p = JsonSerializer.Deserialize<Profile>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
        });
        return p ?? throw new InvalidDataException("Invalid profile JSON.");
    }
}

public sealed class LocationCandidates
{
    [JsonPropertyName("tables")] public List<string> Tables { get; set; } = new();
    [JsonPropertyName("vpinmame")] public List<string> VPinMame { get; set; } = new();
    [JsonPropertyName("roms")] public List<string> Roms { get; set; } = new();
    [JsonPropertyName("pupDatabase")] public List<string> PupDatabase { get; set; } = new();
    [JsonPropertyName("popMedia")] public List<string> PopMedia { get; set; } = new();
    [JsonPropertyName("pupVideos")] public List<string> PupVideos { get; set; } = new();
}

public sealed class BinaryRole
{
    /// <summary>Glob-ish file name pattern (simple * wildcard), matched case-insensitively.</summary>
    [JsonPropertyName("pattern")] public string Pattern { get; set; } = "";

    /// <summary>Role id: main-exe | vpinmame | dmddevice | dmddevice64 | b2s | flexdmd | other.</summary>
    [JsonPropertyName("role")] public string Role { get; set; } = "other";

    /// <summary>Where to look: root | vpinmame | tables | anywhere.</summary>
    [JsonPropertyName("scope")] public string Scope { get; set; } = "anywhere";
}

public sealed class ScriptSignature
{
    [JsonPropertyName("id")] public string Id { get; set; } = "";
    [JsonPropertyName("regex")] public string Regex { get; set; } = "";
    /// <summary>Human meaning, English.</summary>
    [JsonPropertyName("meaning")] public string Meaning { get; set; } = "";
    /// <summary>info | warning</summary>
    [JsonPropertyName("level")] public string Level { get; set; } = "info";
}

public sealed class UpdateSource
{
    [JsonPropertyName("urls")] public List<string> Urls { get; set; } = new();
    [JsonPropertyName("cacheHours")] public int CacheHours { get; set; } = 24;
    [JsonPropertyName("siteUrl")] public string SiteUrl { get; set; } = "";

    /// <summary>
    /// Optional deep-link template for a single VPS game, with <c>{id}</c> substituted by the
    /// matched VPS game id — e.g. <c>https://example.org/game/{id}</c>.
    ///
    /// <para>
    /// Empty by default, on purpose. Chad Greenaway asked for a direct link "rather than having
    /// to search" (FIELD-LOG 2026-08-03) and the matched id is available, but the VPS front end
    /// has moved host at least once and its route could not be confirmed at the time of writing —
    /// so the format lives in the profile, where it can be corrected without a rebuild. While it
    /// is empty the report keeps the current search hint; a wrong link is worse than no link.
    /// </para>
    /// </summary>
    [JsonPropertyName("gameUrlTemplate")] public string GameUrlTemplate { get; set; } = "";

    /// <summary>Builds the direct link for a VPS game id, or null when no template is configured.</summary>
    public string? GameUrl(string? vpsGameId)
        => string.IsNullOrWhiteSpace(GameUrlTemplate) || string.IsNullOrWhiteSpace(vpsGameId)
            ? null
            : GameUrlTemplate.Replace("{id}", Uri.EscapeDataString(vpsGameId));
}
