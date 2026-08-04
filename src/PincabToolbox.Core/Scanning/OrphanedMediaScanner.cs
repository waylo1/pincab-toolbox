using PincabToolbox.Core.Models;
using PincabToolbox.Core.Services;

namespace PincabToolbox.Core.Scanning;

/// <summary>
/// Informative-only: counts PinUP Popper media files (POPMedia / PUPVideos) whose name doesn't
/// relate to any installed table — leftovers from removed or renamed tables that quietly eat
/// disk space over time (FIELD-LOG 2026-07-29, "Nettoyer automatiquement votre dossier
/// PinupSystem", 11 replies).
///
/// Only the TOP level of each media root and one level of subfolders is scanned (Audio,
/// BackGlass, DMD, Loading, Wheel… are flat in a normal Popper install) — matches the depth the
/// Repair action (<see cref="Repair.Actions.QuarantineOrphanedMediaAction"/> in the Repair
/// project) walks, via the shared <see cref="OrphanMediaMatcher"/>, so the two can never
/// disagree about what an orphan is. This scanner never deletes or moves anything.
/// </summary>
public sealed class OrphanedMediaScanner : IScanner
{
    public string Id => "media-orphan";
    public string Name => "Orphaned Media";

    public IEnumerable<Finding> Scan(ScanContext ctx)
    {
        var tableNames = ctx.Layout.VpxTables
            .Select(t => Path.GetFileNameWithoutExtension(t))
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .ToList();

        var count = 0;
        foreach (var root in new[] { ctx.Layout.PopMediaDir, ctx.Layout.PupVideosDir })
        {
            if (root is null || !Directory.Exists(root)) continue;
            foreach (var file in EnumerateOneLevelDeep(root))
            {
                var baseName = Path.GetFileNameWithoutExtension(file);
                if (OrphanMediaMatcher.IsOrphan(baseName, tableNames)) count++;
            }
        }

        if (count == 0) yield break;

        yield return new Finding
        {
            Code = "ORPHANED_MEDIA_FILE", Severity = Severity.Info, Category = Id,
            Subject = $"{count} file(s)",
            Args = new[] { count.ToString() },
            EnglishText = $"{count} media file(s) in PinUP Popper's media folders don't match any installed table — "
                        + "likely leftovers from removed or renamed tables.",
            FixHint = "Safe to review by hand, or quarantine with Repair once available (moves them aside with a "
                    + "backup, never deletes). Don't delete media by hand without checking first — files like "
                    + "\"(SCREEN2)\"/\"(SCREEN3)\" variants can still be in use even when the base name looks unfamiliar.",
        };
    }

    private static IEnumerable<string> EnumerateOneLevelDeep(string root)
    {
        IEnumerable<string> top;
        try { top = Directory.EnumerateFiles(root, "*", SearchOption.TopDirectoryOnly); }
        catch { yield break; }
        foreach (var f in top) yield return f;

        string[] subdirs;
        try { subdirs = Directory.GetDirectories(root); }
        catch { yield break; }

        foreach (var dir in subdirs)
        {
            IEnumerable<string> files;
            try { files = Directory.EnumerateFiles(dir, "*", SearchOption.TopDirectoryOnly); }
            catch { continue; }
            foreach (var f in files) yield return f;
        }
    }
}
