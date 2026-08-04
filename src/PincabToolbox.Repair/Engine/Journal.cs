using System.Text;

namespace PincabToolbox.Repair;

/// <summary>
/// Journal events, append-only, one per line (JSONL).
/// The journal IS the undo information; the backup is only the fallback for when
/// the undo itself fails.
/// </summary>
public enum JournalEvent
{
    PlanCreated,
    PreflightPassed,
    PreflightFailed,

    /// <summary>A rule targeted something outside the install — rejected by the engine (ADR-005).</summary>
    RuleRejected,

    /// <summary>The finding was no longer true when it came time to act. Dropped, not "fixed".</summary>
    StaleDropped,

    ItemSkipped,
    BackupCreated,
    ChangeApplied,
    ChangeFailed,
    ChangeReverted,
    ItemCompleted,

    /// <summary>An item failed and was fully compensated.</summary>
    ItemRolledBack,

    ItemUndone,

    /// <summary>
    /// Worst case: a rollback failed. We STOPPED compensating so as not to make things worse.
    /// The entry carries the backup path and the exact list of files to restore.
    /// </summary>
    RecoveryRequired,

    PlanCompleted,
}

public sealed record JournalEntry
{
    public required DateTimeOffset AtUtc { get; init; }
    public required JournalEvent Event { get; init; }
    public required string PlanId { get; init; }
    public string? ItemId { get; init; }
    public string? Detail { get; init; }
    public PlannedChange? Change { get; init; }
}

public interface IRepairJournal
{
    void Write(JournalEntry entry);
    IReadOnlyList<PlannedChange> AppliedChanges(string planId, string itemId);
    IReadOnlyList<JournalEntry> Read(string planId);

    /// <summary>
    /// Human-readable export. SAME anonymisation rule as the scan report (ADR-003):
    /// user paths are truncated, because this file ends up pasted on a forum.
    /// </summary>
    string ExportAnonymized(string planId);
}

/// <summary>
/// Path anonymisation for the journal export. Delegates to the Core scrubber so the journal
/// and the scan report can never drift apart — one rule, one implementation (ADR-003).
/// </summary>
public static class PathAnonymizer
{
    public static string Anonymize(string? path)
        => PincabToolbox.Core.Services.PathScrubber.Scrub(path);
}

/// <summary>In-memory journal. The file-backed one wraps this and appends JSONL.</summary>
public class InMemoryRepairJournal : IRepairJournal
{
    private readonly List<JournalEntry> _entries = new();

    public IReadOnlyList<JournalEntry> Entries => _entries;

    public virtual void Write(JournalEntry entry) => _entries.Add(entry);

    public IReadOnlyList<PlannedChange> AppliedChanges(string planId, string itemId)
        => _entries.Where(e => e.PlanId == planId && e.ItemId == itemId
                               && e.Event == JournalEvent.ChangeApplied && e.Change is not null)
                   .Select(e => e.Change!)
                   .ToList();

    public IReadOnlyList<JournalEntry> Read(string planId)
        => _entries.Where(e => e.PlanId == planId).ToList();

    public string ExportAnonymized(string planId)
    {
        var sb = new StringBuilder();
        foreach (var e in Read(planId))
        {
            sb.Append(e.AtUtc.ToString("O")).Append(' ').Append(e.Event);
            if (e.ItemId is not null) sb.Append(' ').Append(e.ItemId);
            if (e.Change is not null)
                sb.Append(' ').Append(PathAnonymizer.Anonymize(e.Change.Target))
                  .Append(" [").Append(e.Change.Before).Append(" -> ").Append(e.Change.After).Append(']');
            if (!string.IsNullOrEmpty(e.Detail)) sb.Append(" — ").Append(e.Detail);
            sb.AppendLine();
        }
        return sb.ToString();
    }

    public bool Has(JournalEvent e) => _entries.Any(x => x.Event == e);
    public int Count(JournalEvent e) => _entries.Count(x => x.Event == e);
}
