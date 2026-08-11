namespace PincabToolbox.Repair;

/// <summary>Coarse on purpose. A fake precise number is worse than an honest bucket.</summary>
public enum DurationBucket
{
    Seconds,        // « quelques secondes »
    UnderAMinute,   // « moins d'une minute »
    Minutes,        // « quelques minutes »
}

/// <summary>
/// What the FREE scanner shows about an available repair.
///
/// It carries enough to understand and to trust — what breaks, that a fix exists, that it is
/// reversible and backed up, roughly how long — but not enough to reproduce the fix by hand:
/// no paths, no values, no ordering. See ADR-006.
///
/// Every field is COMPUTED from the real plan, never declared in advance. "Reversible" and
/// "backup planned" must be facts derived from the changes that were actually computed,
/// otherwise they are marketing claims and the trust argument collapses.
/// </summary>
public sealed record RepairSummary
{
    /// <summary>How many individual writes the fix involves. No paths, no values.</summary>
    public required int ChangeCount { get; init; }

    /// <summary>Kinds of write involved — enough to gauge the risk, not to reproduce it.</summary>
    public required IReadOnlyList<ChangeKind> Kinds { get; init; }

    /// <summary>True only when EVERY change can be undone.</summary>
    public required bool FullyReversible { get; init; }

    public required bool BackupPlanned { get; init; }

    public required DurationBucket EstimatedDuration { get; init; }

    /// <summary>Ordered multi-step playbook — the user should know it is a sequence, not a click.</summary>
    public bool IsPlaybook => ChangeCount > 1;

    internal static RepairSummary From(IReadOnlyList<PlannedChange> changes, bool backupPlanned)
    {
        var seconds = changes.Sum(c => c.Kind switch
        {
            ChangeKind.FileAttribute => 0.05,
            ChangeKind.IniWrite => 0.1,
            ChangeKind.RegistryWrite => 0.1,
            ChangeKind.SqliteWrite => 0.5,
            ChangeKind.FileMove => 1.0,
            // Launching and waiting on an external registration tool is the slowest kind of change
            // this engine plans — deliberately bucketed high so the confirmation screen never
            // undersells how long it can take (LOT I).
            ChangeKind.ComReregistration => 3.0,
            _ => 0.5,
        });
        if (backupPlanned) seconds *= 2;   // the backup costs roughly what the write costs

        return new RepairSummary
        {
            ChangeCount = changes.Count,
            Kinds = changes.Select(c => c.Kind).Distinct().OrderBy(k => (int)k).ToList(),
            FullyReversible = changes.Count > 0 && changes.All(c => c.Reversible),
            BackupPlanned = backupPlanned,
            EstimatedDuration = seconds < 5 ? DurationBucket.Seconds
                              : seconds < 60 ? DurationBucket.UnderAMinute
                              : DurationBucket.Minutes,
        };
    }
}
