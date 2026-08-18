using PincabToolbox.Core.Models;
using PincabToolbox.Core.Services;

namespace PincabToolbox.Core.Scanning;

/// <summary>
/// A1 — VBScript Shared-Script Doctor, detection-only slice (session prompt 18/08). Flags a local
/// copy of <c>core.vbs</c>/<c>controller.vbs</c>/<c>VPMKeys.vbs</c>/<c>nudge.vbs</c> found directly
/// under <c>Tables/</c> — pure presence, no version extraction or comparison, no fix.
///
/// <para>
/// Deliberately narrower than the full A1 audit spec (which also compares the local copy's
/// internal version against a profile-declared floor): the handoff marks that comparison 🟡 with a
/// higher proof bar ("deux signaux terrain requis"), while pure presence is a plain, unambiguous
/// fact. <see cref="Severity.Note"/> either way (ADR-010 Doctrine) — a local copy existing is not
/// itself proof of harm (it may be intentional, e.g. a table author deliberately pinning a specific
/// controller version), only something worth the user's attention.
/// </para>
///
/// <para>
/// The <em>fix</em> (providing the correct shared script via Repair) stays out of scope: it turns
/// on an open-source-redistribution question under ADR-004 that has not been decided for this pair
/// of files, and R3-e of the handoff reserves that decision for Maxime/CTO, not an autonomous
/// session. Only detection ships here.
/// </para>
/// </summary>
public sealed class ScriptDoctorScanner : IScanner
{
    public string Id => "script-doctor";
    public string Name => "Shared Script Doctor";

    private readonly Func<string, IEnumerable<string>> _enumerateVbsFiles;

    /// <param name="enumerateVbsFiles">Lists .vbs file paths directly under a folder (top-level
    /// only — this checks Tables/ itself, not every table's own resource subfolder). Defaults to a
    /// real directory scan; injected in tests.</param>
    public ScriptDoctorScanner(Func<string, IEnumerable<string>>? enumerateVbsFiles = null)
        => _enumerateVbsFiles = enumerateVbsFiles ?? EnumerateDisk;

    public IEnumerable<Finding> Scan(ScanContext ctx)
    {
        ctx.Cancellation.ThrowIfCancellationRequested();
        if (ctx.Layout.TablesDir is null) yield break;

        List<string> files;
        try { files = _enumerateVbsFiles(ctx.Layout.TablesDir).ToList(); }
        catch { yield break; } // unreadable folder → silence, never a false positive

        foreach (var path in files)
        {
            ctx.Cancellation.ThrowIfCancellationRequested();

            // Split by hand rather than Path.GetFileName: off Windows System.IO.Path does not
            // treat '\' as a separator, so a Windows-style injected test path would come back
            // whole (same trap documented on BlockedFileScanner.SeverityFor).
            var cut = path.LastIndexOfAny(new[] { '/', '\\' });
            var name = cut >= 0 ? path[(cut + 1)..] : path;
            if (!SharedScriptDetector.IsKnownSharedScript(name)) continue;

            yield return new Finding
            {
                Code = "SHARED_SCRIPT_LOCAL_COPY", Severity = Severity.Note, Category = Id,
                Subject = name, FilePath = path,
                Args = new[] { name },
                EnglishText = $"A local copy of '{name}' was found directly in the Tables folder. Visual Pinball loads a shared script like this one from wherever it finds one first — a local copy here silently overrides the shared/global version for every table, not just the one it came with, which can leave some tables running an older or different version than you expect.",
                FixHint = "If you didn't put this copy there on purpose, delete it so tables fall back to the shared script folder. If you deliberately pinned this version for a reason, no action needed — this is only a heads-up.",
            };
        }
    }

    private static IEnumerable<string> EnumerateDisk(string tablesDir)
    {
        if (!Directory.Exists(tablesDir)) return Array.Empty<string>();
        return Directory.EnumerateFiles(tablesDir, "*.vbs", SearchOption.TopDirectoryOnly);
    }
}
