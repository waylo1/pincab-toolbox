using PincabToolbox.Core.Models;

namespace PincabToolbox.Core.Scanning;

/// <summary>
/// Generic "system health" check: warns when the drive holding the tables is nearly full.
/// A near-full drive makes Visual Pinball fail to allocate textures ("Unable to Create
/// Offscreen Texture") or load media — a cause seen across community troubleshooting guides
/// (FIELD-LOG 2026-07-29, Pinball Nirvana). Cheap, generic, not pinball-specific.
/// </summary>
public sealed class DiskSpaceScanner : IScanner
{
    public string Id => "disk";
    public string Name => "Disk Space";

    /// <summary>Below this free space, texture/media load failures become likely.</summary>
    public const long WarnThresholdBytes = 5L * 1024 * 1024 * 1024; // 5 GiB

    public IEnumerable<Finding> Scan(ScanContext ctx)
    {
        var probe = ctx.Layout.TablesDir ?? ctx.Layout.RootPath;
        var (drive, free) = TryGetFreeSpace(probe);
        if (drive is null) yield break;

        var finding = Evaluate(drive, free, Id, WarnThresholdBytes);
        if (finding is not null) yield return finding;
    }

    /// <summary>Pure decision, so the threshold logic is testable without a real disk.</summary>
    public static Finding? Evaluate(string driveName, long freeBytes, string category, long warnThresholdBytes)
    {
        if (freeBytes >= warnThresholdBytes) return null;
        var freeGb = freeBytes / (1024.0 * 1024 * 1024);
        return new Finding
        {
            Code = "LOW_DISK_SPACE", Severity = Severity.Warning, Category = category,
            Subject = driveName,
            Args = new[] { driveName, freeGb.ToString("0.0") },
            EnglishText = $"Low disk space on {driveName}: {freeGb:0.0} GB free. Visual Pinball can fail to load "
                        + "textures (\"Unable to Create Offscreen Texture\") or media when the drive is nearly full.",
            FixHint = "Free up space on this drive (old backups, unused tables/media) — keep at least a few GB of headroom.",
        };
    }

    private static (string? drive, long free) TryGetFreeSpace(string probe)
    {
        try
        {
            var root = Path.GetPathRoot(Path.GetFullPath(probe));
            if (string.IsNullOrEmpty(root)) return (null, 0);
            var di = new DriveInfo(root);
            if (!di.IsReady) return (null, 0);
            return (di.Name, di.AvailableFreeSpace);
        }
        catch { return (null, 0); }
    }
}
