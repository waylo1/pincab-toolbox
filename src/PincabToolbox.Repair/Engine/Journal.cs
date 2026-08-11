using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

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

    /// <summary>
    /// The backup call itself threw or otherwise could not be completed — spec LOT H.2 rule 4:
    /// "si la sauvegarde échoue, l'action n'est pas appliquée". The item is skipped entirely, no
    /// change in it is ever executed.
    /// </summary>
    BackupFailed,

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

    /// <summary>
    /// 11/08/2026, ADR-012 "Suite" — <see cref="RepairSession"/>'s forced-dry-run kill switch
    /// (<c>PINCAB_REPAIR_FORCE_DRYRUN</c>) was active for this Apply call: nothing was actually
    /// written, <see cref="ApplyResult.ItemOutcomes"/> reports what WOULD have happened. Exists so
    /// the journal — the same file a future Undo screen or a forum bug report reads — can never be
    /// mistaken for a record of real writes. <see cref="JournalEntry.Detail"/> carries the count of
    /// items that would have been applied.
    /// </summary>
    ForcedDryRunApplied,
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

    /// <summary>
    /// Every distinct PlanId seen so far, most recent first (insertion order reversed — entries
    /// are only ever appended). Used to build an "Undo history" surface in the App: without this,
    /// a persistent journal on disk would still have no way to be BROWSED — only queried by a
    /// PlanId the caller already knows (LOT H, spec §H.2.5: "Undo doit être accessible depuis
    /// l'interface").
    /// </summary>
    public IReadOnlyList<string> KnownPlanIds()
        => _entries.Select(e => e.PlanId).Distinct(StringComparer.Ordinal).Reverse().ToList();
}

/// <summary>
/// Persistent journal (LOT H.1, spec 10/08 — "sans lui, Undo ne survit pas à la fermeture de
/// l'app"). Wraps <see cref="InMemoryRepairJournal"/> (kept as the single source of truth for
/// queries) and appends each entry to a JSONL file ON DISK AS IT HAPPENS — not batched at the end
/// of a plan, so a killed-mid-Apply app still leaves behind everything that was actually done.
///
/// <para>
/// Format: one JSON object per line (JSON Lines), human-readable — spec requirement: "un
/// utilisateur bloqué puisse être dépanné à distance en lisant le fichier". Enums are written as
/// their name (<see cref="JsonStringEnumConverter"/>), not their numeric value, for the same
/// reason. <c>System.Text.Json</c> only — no new dependency (Core/Repair's zero-external-dependency
/// contract).
/// </para>
///
/// <para>
/// Never throws, in either direction. A corrupt or unreadable journal degrades to "no history
/// available" (skips the bad line, keeps loading the rest) — exactly like
/// <c>KnowledgePack.Empty</c> degrades from a bad pack — because a broken journal must never
/// prevent the app from starting, a scan from running, or (critically) a NEW <c>Apply</c> from
/// being journaled from this point forward. A disk write failure during <see cref="Write"/> is
/// swallowed the same way: <see cref="LastWriteFailed"/> lets a caller surface a warning, but the
/// in-memory copy (this run's own Undo) still works even if cross-session persistence just broke.
/// </para>
/// </summary>
public sealed class FileRepairJournal : InMemoryRepairJournal
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly string _filePath;
    private readonly object _writeLock = new();

    /// <summary>True when the most recent disk append failed. Never throws — read this instead.</summary>
    public bool LastWriteFailed { get; private set; }

    /// <param name="root">
    /// Directory the journal file lives in (created on first write if absent). Spec: "à côté de
    /// %APPDATA%\PincabToolbox\repair-backups" — callers pass a sibling directory, e.g.
    /// %APPDATA%\PincabToolbox\repair-journal.
    /// </param>
    public FileRepairJournal(string root)
    {
        _filePath = Path.Combine(root, "journal.jsonl");
        Load();
    }

    private void Load()
    {
        string[] lines;
        try
        {
            if (!File.Exists(_filePath)) return;
            lines = File.ReadAllLines(_filePath);
        }
        catch
        {
            return; // unreadable file on startup — degrade to "no history available", never throw
        }

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            try
            {
                var entry = JsonSerializer.Deserialize<JournalEntry>(line, JsonOptions);
                if (entry is not null) base.Write(entry);   // base only — do not re-append to disk
            }
            catch
            {
                // One corrupt line (e.g. a half-written line from a killed process) must not lose
                // every entry around it — skip it and keep loading the rest of the file.
            }
        }
    }

    public override void Write(JournalEntry entry)
    {
        base.Write(entry);   // in-memory copy always succeeds — this run's own Undo must still work

        try
        {
            lock (_writeLock)
            {
                var dir = Path.GetDirectoryName(_filePath);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                File.AppendAllText(_filePath, JsonSerializer.Serialize(entry, JsonOptions) + Environment.NewLine);
            }
            LastWriteFailed = false;
        }
        catch
        {
            // A disk failure (full disk, permissions, AV lock) must never crash mid-Apply — the
            // write that matters most (the change itself) has already happened by the time the
            // journal entry is written. Degrade: this run keeps working from memory.
            LastWriteFailed = true;
        }
    }
}
