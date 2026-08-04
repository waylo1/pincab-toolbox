using System.Text.RegularExpressions;
using PincabToolbox.Core.Models;

namespace PincabToolbox.Core.Scanning;

/// <summary>
/// Cross-checks what the tables actually NEED against what is installed. Two very common —
/// and silent — pincab failures live here: backglasses that never show because the B2S
/// Backglass Server was never installed, and FlexDMD tables whose score display is dead
/// because FlexDMD.dll is absent. Strictly read-only.
///
/// Conservative by design (the engine's #1 rule is "very low false positives"): a finding
/// only surfaces when there is a concrete NEED signal — a .directb2s file on disk, or an
/// explicit CreateObject in a table script — AND the runtime component is nowhere under the
/// selected install. Both components are COM-registered, so a truly exotic install could put
/// them outside the tree; that is why these are Warnings with honest "under this install"
/// wording rather than Criticals.
/// </summary>
public sealed class DependencyScanner : IScanner
{
    public string Id => "dependencies";
    public string Name => "Dependency Check";

    private static readonly Regex UsesB2S = new(@"(?i)CreateObject\(\s*""B2S\.Server""", RegexOptions.Compiled);
    private static readonly Regex UsesFlexDmd = new(@"(?i)CreateObject\(\s*""FlexDMD", RegexOptions.Compiled);

    public IEnumerable<Finding> Scan(ScanContext ctx)
    {
        var root = ctx.Layout.RootPath;
        if (string.IsNullOrEmpty(root) || !Directory.Exists(root)) yield break;

        bool b2sInstalled = HasBinary(ctx, "b2s");
        bool flexInstalled = HasBinary(ctx, "flexdmd");

        // --- B2S Backglass Server ---
        int backglassFiles = 0;
        if (ctx.Layout.TablesDir is not null)
        {
            try { backglassFiles = Directory.EnumerateFiles(ctx.Layout.TablesDir, "*.directb2s", SearchOption.AllDirectories).Count(); }
            catch { backglassFiles = 0; }
        }
        bool anyScriptUsesB2S = ctx.Tables.Values.Any(t => t.Script is not null && UsesB2S.IsMatch(t.Script));

        if ((backglassFiles > 0 || anyScriptUsesB2S) && !b2sInstalled)
        {
            yield return new Finding
            {
                Code = "B2S_SERVER_MISSING", Severity = Severity.Warning, Category = Id,
                Subject = "B2S Backglass Server",
                Args = new[] { backglassFiles.ToString() },
                EnglishText = backglassFiles > 0
                    ? $"Found {backglassFiles} backglass file(s) but no B2SBackglassServer.dll under this install — backglasses will not display until the B2S Backglass Server is installed and registered."
                    : "A table script uses the B2S Backglass Server but no B2SBackglassServer.dll was found under this install.",
                FixHint = "Install the B2S Backglass Server (it registers B2SBackglassServer.dll), then place/keep it in your Tables folder and register it as administrator.",
            };
        }
        else if (backglassFiles > 0 && b2sInstalled)
        {
            yield return new Finding
            {
                Code = "B2S_SERVER_OK", Severity = Severity.Ok, Category = Id,
                Subject = "B2S Backglass Server",
                EnglishText = "B2S Backglass Server is installed and backglass files are present.",
            };
        }

        // --- FlexDMD ---
        if (!flexInstalled)
        {
            var flexTables = ctx.Tables
                .Where(kv => kv.Value.Script is not null && UsesFlexDmd.IsMatch(kv.Value.Script))
                .Select(kv => Path.GetFileNameWithoutExtension(kv.Key))
                .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (flexTables.Count > 0)
            {
                yield return new Finding
                {
                    Code = "FLEXDMD_MISSING", Severity = Severity.Warning, Category = Id,
                    Subject = flexTables.Count == 1 ? flexTables[0] : $"{flexTables.Count} tables",
                    Args = new[] { flexTables.Count.ToString(), string.Join(", ", flexTables.Take(5)) },
                    EnglishText = $"{flexTables.Count} table(s) use FlexDMD but no FlexDMD.dll was found under this install — " +
                                  "their DMD/score display will not work until FlexDMD is installed and registered.",
                    FixHint = "Download FlexDMD, place FlexDMD.dll in your Visual Pinball folder and register it (regsvr32 as administrator).",
                };
            }
        }
    }

    /// <summary>
    /// True when any binary carrying the given role (per the profile) is found under its scope.
    /// Reuses the profile's role/pattern/scope table so no dependency names are hard-coded here.
    /// </summary>
    private static bool HasBinary(ScanContext ctx, string role)
    {
        foreach (var br in ctx.Profile.BinaryRoles)
        {
            if (!string.Equals(br.Role, role, StringComparison.OrdinalIgnoreCase)) continue;

            var roots = br.Scope switch
            {
                "vpinmame" => ctx.Layout.VPinMameDir is null ? Array.Empty<string>() : new[] { ctx.Layout.VPinMameDir },
                "tables" => ctx.Layout.TablesDir is null ? Array.Empty<string>() : new[] { ctx.Layout.TablesDir },
                _ => new[] { ctx.Layout.RootPath },
            };

            foreach (var r in roots)
                if (LayoutDetector.FindFilesByPattern(r, br.Pattern, 5).Any())
                    return true;
        }
        return false;
    }
}
