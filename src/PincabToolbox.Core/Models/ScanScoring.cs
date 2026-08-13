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

    /// <summary>What the FIRST occurrence of a distinct Critical code costs — unchanged since 2026-07.</summary>
    private const int CriticalPenaltyPerCode = 15;

    /// <summary>
    /// FIELD-LOG 2026-08-13: on a real ~500-table install, 8 different tables each missing their
    /// own ROM (same code, ROM_MISSING, 8 distinct real problems) used to cost 8×15=120 points —
    /// past the 100-point scale on its own, so the score floors at 0/F regardless of anything else
    /// on the install, and stays there exactly as flat whether 8 or 80 tables are affected. Two
    /// real problems with that: an otherwise-healthy large library reads as totally broken from one
    /// narrow, common issue, and the score cannot move as that issue gets fixed one table at a time
    /// (it stays pinned at the 0 floor until the LAST occurrence is gone) — which is the opposite of
    /// what a score is for. Same fix philosophy already applied to warnings below: the FIRST
    /// occurrence of a given critical CODE still costs full price (a genuinely different kind of
    /// problem is exactly as bad as it was), repeats of that SAME code diminish logarithmically
    /// instead of linearly. Deliberately not a flat "count once regardless of volume" — that would
    /// make 80 broken tables read identically to 1, which is not honest either; this keeps "more of
    /// the same problem is worse" true while stopping it from swallowing the whole scale.
    /// </summary>
    public static int ComputeScore(IEnumerable<Finding> findings)
    {
        var list = findings as IReadOnlyCollection<Finding> ?? findings.ToList();

        var criticalPenalty = list
            .Where(f => f.Severity == Severity.Critical)
            .GroupBy(f => f.Code)
            .Sum(g =>
            {
                var n = g.Count();
                return n <= 1
                    ? CriticalPenaltyPerCode
                    : CriticalPenaltyPerCode + (int)Math.Round(8 * Math.Log(n));
            });

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
                // FIELD-LOG 2026-08-13: a prior forum reply told a user every export format has
                // the full detail — false for HTML/Markdown/BBCode, which also call Rolled() and
                // hit this exact message. Name the formats that actually don't collapse, by their
                // file extension, so the message is actionable from inside the same Export dialog
                // rather than pointing at a vague "full text report" the user has to guess at.
                EnglishText = $"{members.Count} similar findings ({representative.Code}) — collapsed to keep this list readable. Export as .txt, .pdf or .json to see every one individually.",
            };
        }
    }
}
