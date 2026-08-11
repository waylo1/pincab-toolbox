namespace PincabToolbox.Core.Models;

/// <summary>
/// Score/grade/ordering/rollup logic shared by <see cref="ScanReport"/> (single install) and
/// <see cref="Scanning.DriveScanReport"/> (every install found across a drive, TRANSMISSION #14,
/// 10/08 — feu vert explicite de Maxime pour rouvrir ce point du Scanner gelé). Extracted out of
/// ScanReport so the multi-root path reuses the exact same, already-trusted formula rather than a
/// second implementation silently drifting from it over time. Pure extraction — no behaviour
/// change versus the pre-10/08 ScanReport.
/// </summary>
public static class ScanScoring
{
    /// <summary>Max total points warnings alone can subtract (keeps warning-only installs at grade B or better).</summary>
    private const int WarningPenaltyCap = 30;

    public static int ComputeScore(IEnumerable<Finding> findings)
    {
        var list = findings as IReadOnlyCollection<Finding> ?? findings.ToList();
        var criticalPenalty = list.Count(f => f.Severity == Severity.Critical) * 15;
        var warnings = list.Count(f => f.Severity == Severity.Warning);
        var warningPenalty = warnings == 0
            ? 0
            : Math.Min(WarningPenaltyCap, (int)Math.Round(12 * Math.Log(1 + warnings)));
        return Math.Max(0, 100 - criticalPenalty - warningPenalty);
    }

    public static string GradeFor(int score) => score switch
    {
        >= 100 => "A+",
        >= 90 => "A",
        >= 70 => "B",
        >= 40 => "C",
        _ => "F",
    };

    public static IEnumerable<Finding> Ordered(IEnumerable<Finding> findings) =>
        findings.OrderByDescending(f => f.Severity).ThenBy(f => f.Category).ThenBy(f => f.Subject);

    /// <summary>
    /// Code emitted for a collapsed group. One shared code rather than one per collapsed finding
    /// type: the UI needs a single template to localize, and the group's own meaning already
    /// travels in <see cref="Finding.Args"/>.
    /// </summary>
    public const string RollupCode = "GROUPED";

    /// <summary>Same findings as <see cref="Ordered"/>, but with repetitive ones collapsed to a single row carrying their count. Criticals are never collapsed, whatever the count.</summary>
    public static IEnumerable<Finding> Rolled(IEnumerable<Finding> findings, int threshold)
    {
        if (threshold < 2) threshold = 2;

        // Grouping by code alone would merge findings a scanner deliberately split by severity —
        // BLOCKED_DLL is Critical for a core plugin and Warning for anything else.
        foreach (var group in Ordered(findings).GroupBy(f => (f.Severity, f.Code)))
        {
            var members = group.ToList();

            if (group.Key.Severity == Severity.Critical || members.Count < threshold)
            {
                foreach (var f in members) yield return f;
                continue;
            }

            var representative = members[0];
            yield return new Finding
            {
                Code = RollupCode,
                Severity = representative.Severity,
                Category = representative.Category,
                Subject = "",
                Args = new[] { members.Count.ToString(), representative.Code },
                EnglishText = $"{members.Count} similar findings ({representative.Code}) — collapsed to keep this list readable. The full text report has every one of them.",
            };
        }
    }
}
