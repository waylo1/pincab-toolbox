using PincabToolbox.Core.Models;

namespace PincabToolbox.Core.Scanning;

/// <summary>
/// Flags a B2S Backglass Server install with no <c>GlobalConfig_B2SServer.xml</c> next to it — a
/// documented, exact-filename VPU pain point (SPEC-lot-communaute-2026-08-10 §6.1, "candidat n°1
/// du prochain lot"): B2S loads with no global config at all when the file is absent, silently
/// dropping any global setting (default plugins, DOF integration…) the user configured.
///
/// <para>
/// 🟢 Deterministic (ADR-010): the DLL's presence and the config file's presence are both plain
/// facts, so this ships directly at <see cref="Severity.Warning"/>, no Doctrine-Note escalation
/// needed. Only fires when B2S is actually installed — if it is not, <c>DependencyScanner</c>
/// already owns that gap (<c>B2S_SERVER_MISSING</c>); reporting both would double the same
/// underlying problem under two codes.
/// </para>
///
/// <para>
/// Reuses the profile's <c>b2s</c> binary role (same pattern/scope table <c>DependencyScanner</c>
/// and <c>CompletenessScanner</c> already read) so this scanner can never disagree with them about
/// what "B2S is installed" means, and never redefines the DLL name pattern in a second place.
/// </para>
/// </summary>
public sealed class GlobalConfigB2SScanner : IScanner
{
    public string Id => "globalconfig-b2s";
    public string Name => "B2S Global Config Check";

    private readonly Func<string, string, int, IEnumerable<string>> _findFiles;
    private readonly Func<string, bool> _fileExists;

    /// <param name="findFiles">(root, pattern, maxDepth) → matching file paths. Defaults to a real
    /// bounded directory walk; injected in tests. Same shape as <see cref="LayoutDetector.FindFilesByPattern"/>.</param>
    /// <param name="fileExists">Defaults to a real <see cref="File.Exists(string)"/>; injected in tests.</param>
    public GlobalConfigB2SScanner(
        Func<string, string, int, IEnumerable<string>>? findFiles = null,
        Func<string, bool>? fileExists = null)
    {
        _findFiles = findFiles ?? LayoutDetector.FindFilesByPattern;
        _fileExists = fileExists ?? File.Exists;
    }

    public IEnumerable<Finding> Scan(ScanContext ctx)
    {
        ctx.Cancellation.ThrowIfCancellationRequested();
        if (string.IsNullOrEmpty(ctx.Layout.RootPath)) yield break;

        string? b2sPath = null;
        foreach (var br in ctx.Profile.BinaryRoles)
        {
            if (!string.Equals(br.Role, "b2s", StringComparison.OrdinalIgnoreCase)) continue;

            var scopeRoot = br.Scope switch
            {
                "vpinmame" => ctx.Layout.VPinMameDir,
                "tables" => ctx.Layout.TablesDir,
                _ => ctx.Layout.RootPath,
            };
            if (scopeRoot is null) continue;

            IEnumerable<string> hits;
            try { hits = _findFiles(scopeRoot, br.Pattern, 5); }
            catch { continue; } // unreadable subtree → keep looking, never crash the scan

            b2sPath = hits.FirstOrDefault();
            if (b2sPath is not null) break;
        }

        // B2S not installed at all: DependencyScanner already reports B2S_SERVER_MISSING for this
        // gap. Staying silent here avoids reporting the same underlying absence twice.
        if (b2sPath is null) yield break;

        var dir = Path.GetDirectoryName(b2sPath);
        if (string.IsNullOrEmpty(dir)) yield break;

        var configPath = Path.Combine(dir, "GlobalConfig_B2SServer.xml");

        bool exists;
        try { exists = _fileExists(configPath); }
        catch { yield break; } // unreadable → silence, never a false positive

        if (exists) yield break;

        yield return new Finding
        {
            Code = "GLOBALCONFIG_B2S_MISSING", Severity = Severity.Warning, Category = Id,
            Subject = "GlobalConfig_B2SServer.xml", FilePath = configPath,
            EnglishText = "B2S Backglass Server is installed, but its GlobalConfig_B2SServer.xml file is missing — B2S loads with no global config at all, which silently drops any global setting (default plugins, DOF integration…) you configured across every table.",
            FixHint = "Open any table's backglass once in the B2S Backglass Designer and save it — B2S recreates GlobalConfig_B2SServer.xml with defaults. Then reapply your global settings.",
        };
    }
}
