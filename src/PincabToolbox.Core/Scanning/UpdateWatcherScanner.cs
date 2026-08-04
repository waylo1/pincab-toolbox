using PincabToolbox.Core.Models;
using PincabToolbox.Core.Services;

namespace PincabToolbox.Core.Scanning;

/// <summary>
/// Compares local tables against the open Virtual Pinball Spreadsheet database and
/// reports when a newer version exists. Links to the VPS site — NEVER downloads content.
/// Beta: matching is heuristic (name + year from the file name).
/// </summary>
public sealed class UpdateWatcherScanner : IScanner
{
    public string Id => "updates";
    public string Name => "Update Watcher (beta)";

    private readonly List<VpsGame>? _games;

    /// <param name="games">Pre-loaded VPS database (null → scanner reports unavailability).</param>
    public UpdateWatcherScanner(List<VpsGame>? games)
    {
        _games = games;
    }

    public IEnumerable<Finding> Scan(ScanContext ctx)
    {
        if (_games is null || _games.Count == 0)
        {
            yield return new Finding
            {
                Code = "VPS_UNAVAILABLE", Severity = Severity.Info, Category = Id,
                EnglishText = "VPS database unavailable (offline?) — update checks skipped. They will run next time you are online.",
            };
            yield break;
        }

        int matched = 0;
        int derivatives = 0;
        foreach (var (path, table) in ctx.Tables)
        {
            ctx.Cancellation.ThrowIfCancellationRequested();
            var baseName = Path.GetFileNameWithoutExtension(path);
            var game = VpsDatabase.Match(_games, baseName);
            if (game is null) continue;
            matched++;

            // A mod carries the base table's name and year but versions on its own track, so
            // comparing it to the base table's latest release manufactures a phantom update.
            // Reported once in the summary rather than per table — the point is to remove noise,
            // not to relocate it. (Chad Greenaway + Gregg, FIELD-LOG 2026-08-03.)
            if (TableVariantDetector.IsDerivative(baseName)) { derivatives++; continue; }

            // Only compare against VPX-format files — the VPS db also lists FP/FX versions.
            var latest = game.TableFiles
                .Where(t => !string.IsNullOrWhiteSpace(t.Version))
                .Where(t => t.TableFormat is null || t.TableFormat.Equals("VPX", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(t => t.Version, Comparer<string?>.Create(VpsDatabase.CompareVersions))
                .FirstOrDefault();
            if (latest?.Version is null) continue;

            var localVersion = table.TableVersion;
            if (string.IsNullOrWhiteSpace(localVersion)) continue;

            if (VpsDatabase.CompareVersions(latest.Version, localVersion) > 0)
            {
                // Direct link when the profile knows how to build one, search hint otherwise.
                var directUrl = ctx.Profile.UpdateSource.GameUrl(game.Id);
                var whereToLook = directUrl ?? $"{ctx.Profile.UpdateSource.SiteUrl} (search: {game.Name})";

                yield return new Finding
                {
                    Code = "UPDATE_AVAILABLE", Severity = Severity.Info, Category = Id,
                    Subject = baseName, FilePath = path,
                    // The VPS id trails the existing args so nothing downstream shifts position.
                    Args = new[]
                    {
                        baseName, localVersion!, latest.Version,
                        directUrl ?? ctx.Profile.UpdateSource.SiteUrl,
                        game.Id,
                    },
                    EnglishText = $"'{baseName}' — you have v{localVersion}, v{latest.Version} is listed on the Virtual Pinball Spreadsheet. " +
                                  $"Check {whereToLook}.",
                };
            }
        }

        yield return new Finding
        {
            Code = "VPS_MATCH_SUMMARY", Severity = Severity.Info, Category = Id,
            Args = new[] { matched.ToString(), ctx.Tables.Count.ToString(), derivatives.ToString() },
            EnglishText = $"Update Watcher matched {matched}/{ctx.Tables.Count} tables against the VPS database (heuristic, beta)." +
                          (derivatives > 0
                              ? $" {derivatives} look like mods/variants and were not version-checked — they version independently of the base table."
                              : ""),
        };
    }
}
