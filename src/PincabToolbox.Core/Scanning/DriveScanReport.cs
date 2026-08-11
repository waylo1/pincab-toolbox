using PincabToolbox.Core.Models;

namespace PincabToolbox.Core.Scanning;

/// <summary>
/// Aggregated result of scanning every pincab install found under a drive (TRANSMISSION #14,
/// 10/08). One <see cref="ScanReport"/> per install found by <see cref="DriveInstallFinder"/>,
/// merged. No scanner and no single-root <see cref="ScanReport"/> shape were changed to make this
/// work — see <see cref="ScanEngine.RunAcrossDrive"/>.
/// </summary>
public sealed class DriveScanReport
{
    /// <summary>The folder the drive walk started from (e.g. "C:\").</summary>
    public required string DriveRoot { get; init; }

    /// <summary>One entry per install found — empty when nothing was found under <see cref="DriveRoot"/>.</summary>
    public List<ScanReport> Reports { get; } = new();

    public DateTimeOffset StartedAt { get; init; }
    public DateTimeOffset FinishedAt { get; set; }

    public IEnumerable<Finding> AllFindings => Reports.SelectMany(r => r.Findings);

    public int Count(Severity s) => AllFindings.Count(f => f.Severity == s);

    /// <summary>Same formula as <see cref="ScanReport.Score"/>, applied across every install found — see <see cref="ScanScoring"/>.</summary>
    public int Score => ScanScoring.ComputeScore(AllFindings);

    public string Grade => ScanScoring.GradeFor(Score);

    public IEnumerable<Finding> Ordered() => ScanScoring.Ordered(AllFindings);

    public IEnumerable<Finding> Rolled(int threshold = ScanReport.DefaultRollupThreshold) => ScanScoring.Rolled(AllFindings, threshold);

    /// <summary>
    /// Merges every install into a single <see cref="ScanReport"/> so the rest of the app (HTML/
    /// Markdown/BBCode export, MainWindow bindings, RepairOfferBuilder…) needs ZERO changes to
    /// consume a multi-root scan — they just get a normal ScanReport. Every Finding already
    /// carries its own absolute FilePath, so nothing about WHICH install a finding belongs to is
    /// lost by merging; only the synthesized Layout is approximate (see below).
    /// </summary>
    public ScanReport ToMergedScanReport()
    {
        // Layout is a single-root concept (RootPath, one TablesDir, one VPinMameDir…) and doesn't
        // literally fit "many installs found across a drive". Rather than invent a multi-root
        // Layout shape (which would ripple into every scanner that reads ctx.Layout), the merged
        // Layout keeps RootPath = the drive, VpxTables/VpxExecutables concatenated from every
        // install (used by exports/counts), and the FIRST install's other paths as a
        // representative value (cosmetic only — no scanner runs against this merged Layout, each
        // scanner already ran per-install inside ScanEngine.RunAcrossDrive).
        var layout = new InstallLayout { RootPath = DriveRoot };
        foreach (var r in Reports)
        {
            layout.VpxTables.AddRange(r.Layout.VpxTables);
            layout.VpxExecutables.AddRange(r.Layout.VpxExecutables);
        }
        if (Reports.Count > 0)
        {
            var first = Reports[0].Layout;
            layout.TablesDir = first.TablesDir;
            layout.VPinMameDir = first.VPinMameDir;
            layout.RomsDir = first.RomsDir;
            layout.AliasFilePath = first.AliasFilePath;
            layout.PupDatabasePath = first.PupDatabasePath;
            layout.PopMediaDir = first.PopMediaDir;
            layout.PupVideosDir = first.PupVideosDir;
        }

        var merged = new ScanReport { Layout = layout, StartedAt = StartedAt, FinishedAt = FinishedAt };
        merged.Findings.AddRange(AllFindings);

        // When more than one install was found, say so up front — a user scanning "C:\" has no
        // other way to know how many separate pincab installs were discovered and merged into
        // this one report (their individual root paths are still visible via each finding's own
        // FilePath, this just orients the reader before they start reading rows).
        if (Reports.Count > 1)
        {
            merged.Findings.Insert(0, new Finding
            {
                Code = "DRIVE_SCAN_SUMMARY", Severity = Severity.Info, Category = "drive",
                Subject = $"{Reports.Count} installs",
                Args = new[] { Reports.Count.ToString(), DriveRoot, string.Join(", ", Reports.Select(r => r.Layout.RootPath)) },
                EnglishText = $"Found and scanned {Reports.Count} separate pincab installs under {DriveRoot}: " +
                              string.Join(", ", Reports.Select(r => r.Layout.RootPath)) + ".",
            });
        }
        else if (Reports.Count == 0)
        {
            merged.Findings.Add(new Finding
            {
                Code = "DRIVE_SCAN_NONE_FOUND", Severity = Severity.Info, Category = "drive",
                EnglishText = $"No pincab install (Tables folder, VPinMAME folder, or PinUP Popper database) was found under {DriveRoot}.",
            });
        }

        return merged;
    }
}
