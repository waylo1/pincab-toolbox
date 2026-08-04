namespace PincabToolbox.Core.Models;

/// <summary>Aggregated result of a full scan.</summary>
public sealed class ScanReport
{
    public required InstallLayout Layout { get; init; }

    public List<Finding> Findings { get; } = new();

    public DateTimeOffset StartedAt { get; init; }
    public DateTimeOffset FinishedAt { get; set; }

    public int Count(Severity s) => Findings.Count(f => f.Severity == s);

    /// <summary>
    /// Health score 0–100, base 100, floored at 0. Reflects the severity of *real problems*
    /// only — Info and Ok never move it.
    /// <para>
    /// Criticals genuinely break a table or the cab, so each weighs a flat −15 with no cap:
    /// a critical-heavy install should read badly.
    /// </para>
    /// <para>
    /// Warnings degrade behaviour but their *count scales with collection size* (one compat
    /// note per table, one missing-B2S per table…). A flat penalty per warning made every
    /// large-but-healthy library floor to 0/F — the whole point of this rework. So warnings
    /// use diminishing returns capped at −<see cref="WarningPenaltyCap"/>: volume alone can
    /// never sink the score below grade B ("a few things to watch"). Only criticals take it
    /// lower. (FIELD-LOG 2026-07-30 / FD's 2090-table report scored 0/100·F with 0 criticals.)
    /// </para>
    /// </summary>
    public int Score
    {
        get
        {
            var criticalPenalty = Count(Severity.Critical) * 15;
            var warnings = Count(Severity.Warning);
            var warningPenalty = warnings == 0
                ? 0
                : Math.Min(WarningPenaltyCap, (int)Math.Round(12 * Math.Log(1 + warnings)));
            return Math.Max(0, 100 - criticalPenalty - warningPenalty);
        }
    }

    /// <summary>Max total points warnings alone can subtract (keeps warning-only installs at grade B or better).</summary>
    private const int WarningPenaltyCap = 30;

    /// <summary>Letter grade derived from <see cref="Score"/>.</summary>
    public string Grade => Score switch
    {
        >= 100 => "A+",
        >= 90 => "A",
        >= 70 => "B",
        >= 40 => "C",
        _ => "F",
    };

    public IEnumerable<Finding> Ordered() =>
        Findings.OrderByDescending(f => f.Severity).ThenBy(f => f.Category).ThenBy(f => f.Subject);

    /// <summary>Below this many findings of one code, listing them individually is more useful.</summary>
    public const int DefaultRollupThreshold = 5;

    /// <summary>
    /// Code emitted for a collapsed group. Deliberately one shared code rather than one per
    /// collapsed finding type: the UI needs a single template to localize, and the group's own
    /// meaning already travels in <see cref="Finding.Args"/>.
    /// </summary>
    public const string RollupCode = "GROUPED";

    /// <summary>
    /// Same findings as <see cref="Ordered"/>, but with repetitive ones collapsed to a single row
    /// carrying their count.
    ///
    /// <para>
    /// Several scanners emit one finding PER TABLE by design — ROM_OK, ROM_NOT_REQUIRED,
    /// UPDATE_AVAILABLE. On a 2000-table collection that is thousands of rows, and the handful of
    /// findings that actually matter drown in them. FD's report was 2711 info lines
    /// (FIELD-LOG 2026-07-30). Capping the SCORE fixed the number; it did not fix the report.
    /// </para>
    ///
    /// <para>
    /// <b>Criticals are never collapsed, whatever the count.</b> A critical is a table that will
    /// not start, and the user needs each name to act on it. An install with 300 broken tables
    /// should look as bad as it is — collapsing that into one tidy line would be the same
    /// dishonesty as the old score, in the other direction.
    /// </para>
    /// </summary>
    /// <param name="threshold">Minimum group size before collapsing. Must be at least 2.</param>
    public IEnumerable<Finding> Rolled(int threshold = DefaultRollupThreshold)
    {
        if (threshold < 2) threshold = 2;

        // Grouping by code alone would merge findings a scanner deliberately split by severity —
        // BLOCKED_DLL is Critical for a core plugin and Warning for anything else.
        foreach (var group in Ordered().GroupBy(f => (f.Severity, f.Code)))
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
                // Count first so a template can lead with it; the collapsed code second so the
                // reader can still tell WHAT was collapsed — an unlabelled count is not evidence.
                Args = new[] { members.Count.ToString(), representative.Code },
                EnglishText = $"{members.Count} similar findings ({representative.Code}) — collapsed to keep this list readable. The full text report has every one of them.",
            };
        }
    }
}
