using System.Net.Http;
using System.Text.Json;

namespace PincabToolbox.Core.Services;

/// <summary>
/// Result of a manual "check for updates" call. <see cref="Success"/> false means the check itself
/// failed (offline, GitHub unreachable, malformed response) — never thrown, always returned, so a
/// flaky or absent connection degrades to a message instead of an exception reaching the UI.
/// </summary>
public sealed record UpdateCheckResult(bool Success, string? LatestVersion, string? ReleaseUrl, string? ErrorMessage)
{
    public static UpdateCheckResult Failure(string message) => new(false, null, null, message);
    public static UpdateCheckResult Ok(string version, string url) => new(true, version, url, null);
}

/// <summary>
/// Manual, opt-in "check for updates" — the ONLY network call anywhere in Pincab Toolbox. Never
/// invoked automatically (no startup check, no background timer): it fires exclusively when the
/// user clicks the button in the About tab. Reads the latest published GitHub release tag; never
/// downloads, installs, or replaces anything itself — the user is handed a link to the release
/// page and decides. This is a deliberate scope boundary, not an oversight — see ADR (pending,
/// TRANSMISSION 07/08).
/// </summary>
public interface IUpdateChecker
{
    Task<UpdateCheckResult> CheckAsync(CancellationToken ct);
}

public sealed class GitHubUpdateChecker : IUpdateChecker
{
    // Public, unauthenticated GitHub REST endpoint — no token, no user data sent beyond the
    // request itself (IP/User-Agent, same as any HTTP request). Matches the repo pushed to in
    // TRANSMISSION (`github.com/waylo1/pincab-toolbox`).
    private const string ReleasesApiUrl = "https://api.github.com/repos/waylo1/pincab-toolbox/releases/latest";

    // Short timeout on purpose: a cab PC that's offline (a documented, deliberate setup for some
    // users — see FIELD-LOG 07/08) must not make the button feel hung. Fail fast, fail quiet.
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(6) };

    public async Task<UpdateCheckResult> CheckAsync(CancellationToken ct)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, ReleasesApiUrl);
            // GitHub's API rejects requests with no User-Agent.
            request.Headers.UserAgent.ParseAdd("PincabToolbox-UpdateCheck");

            using var response = await Http.SendAsync(request, ct).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
                return UpdateCheckResult.Failure($"HTTP {(int)response.StatusCode}");

            var json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(json);

            var tag = doc.RootElement.TryGetProperty("tag_name", out var tagProp) ? tagProp.GetString() : null;
            var url = doc.RootElement.TryGetProperty("html_url", out var urlProp) ? urlProp.GetString() : null;

            if (string.IsNullOrWhiteSpace(tag) || string.IsNullOrWhiteSpace(url))
                return UpdateCheckResult.Failure("unexpected response shape");

            return UpdateCheckResult.Ok(tag, url);
        }
        catch (OperationCanceledException)
        {
            throw; // real cancellation (app closing) — let it propagate, not a check failure
        }
        catch (Exception ex)
        {
            // Offline, DNS failure, GitHub down, malformed JSON, whatever — the button always
            // resolves to a result, never an unhandled exception reaching the UI thread.
            return UpdateCheckResult.Failure(ex.Message);
        }
    }
}

/// <summary>
/// Pure version comparison, testable without any network access. Tags are expected as "vX.Y.Z"
/// (GitHub convention) or "X.Y.Z"; the leading "v" is stripped before parsing. Any tag that isn't a
/// parseable <see cref="Version"/> is treated as "not newer" — a malformed or unexpected tag format
/// must never cause a false "update available".
/// </summary>
public static class AppVersionCompare
{
    public static bool IsNewer(string latestTag, string currentVersion)
    {
        var latestText = latestTag.StartsWith('v') || latestTag.StartsWith('V') ? latestTag[1..] : latestTag;
        if (!Version.TryParse(NormalizeForVersion(latestText), out var latest)) return false;
        if (!Version.TryParse(NormalizeForVersion(currentVersion), out var current)) return false;
        return latest > current;
    }

    // System.Version requires at least Major.Minor; pad a bare "1" or "1.2.3-alpha"-style suffix.
    private static string NormalizeForVersion(string text)
    {
        var dashIdx = text.IndexOf('-');
        var core = dashIdx >= 0 ? text[..dashIdx] : text;
        var parts = core.Split('.');
        return parts.Length switch
        {
            0 => "0.0",
            1 => core + ".0",
            _ => core,
        };
    }
}
