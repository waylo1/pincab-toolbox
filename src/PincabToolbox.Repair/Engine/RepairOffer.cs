namespace PincabToolbox.Repair;

/// <summary>
/// The free, aggregate answer to "what could Repair actually do for my install?".
///
/// <para>
/// Every per-item fact already exists on <see cref="RepairPlanItem"/>. What was missing is the
/// one number a user decides on: how many of my problems does this fix. Without it the UI has to
/// aggregate by hand, and an aggregation written in the UI is an aggregation nobody tests — which
/// is precisely how a free tier starts overstating what a licence unlocks.
/// </para>
///
/// <para>
/// ADR-006: computed from a real plan, never declared. It is deliberately built from an
/// <em>unlicensed</em> plan (see <see cref="From"/>) so that it can only ever describe what the
/// free tier is allowed to describe — no paths, no values, no ordering. The engine redacts
/// <see cref="RepairPlanItem.Changes"/> at its own boundary, so there is nothing here to leak.
/// </para>
///
/// <para>
/// ADR-005: an item whose ActionId is absent from the compiled registry degrades to
/// <see cref="RepairMode.ManualOnly"/> upstream, so it is counted as manual here. A pack newer
/// than the app therefore advertises less, never more.
/// </para>
/// </summary>
public sealed record RepairOffer
{
    /// <summary>The unlicensed plan this offer was computed from — quote it in support threads.</summary>
    public required string PlanId { get; init; }

    /// <summary>Findings considered. The denominator of the sentence shown to the user.</summary>
    public required int FindingsConsidered { get; init; }

    /// <summary>
    /// Findings a licence would genuinely fix — items that resolved to <see cref="RepairMode.Locked"/>
    /// AND produced at least one real planned change. Both halves matter: mode alone would count
    /// actions that failed closed and planned nothing.
    /// </summary>
    public required int FixableCount { get; init; }

    /// <summary>
    /// Findings that stay manual whatever happens: no rule, an action missing from this build,
    /// confidence below the safety threshold, or an action that found nothing to change.
    /// Shown next to <see cref="FixableCount"/>, never hidden — an honest denominator is the
    /// whole trust argument.
    /// </summary>
    public required int ManualOnlyCount { get; init; }

    /// <summary>Distinct finding codes a licence would fix, sorted. Lets the UI badge them in the list.</summary>
    public required IReadOnlyList<string> FixableCodes { get; init; }

    /// <summary>Total individual writes across every fixable item.</summary>
    public required int TotalChangeCount { get; init; }

    /// <summary>Kinds of write involved — enough to gauge risk, not to reproduce the fix.</summary>
    public required IReadOnlyList<ChangeKind> Kinds { get; init; }

    /// <summary>
    /// True only when EVERY fixable item is fully reversible. One irreversible item (a process
    /// kill) makes this false for the whole offer — the claim is only worth making when absolute.
    /// </summary>
    public required bool EveryFixReversible { get; init; }

    /// <summary>True only when EVERY fixable item is backed up before it is touched.</summary>
    public required bool EveryFixBackedUp { get; init; }

    /// <summary>Coarse on purpose — a fake precise number is worse than an honest bucket.</summary>
    public required DurationBucket EstimatedDuration { get; init; }

    /// <summary>
    /// What Repair will NOT do, gathered from the items. Surfaced before purchase, not after:
    /// a limitation discovered post-payment is a refund, a limitation stated up front is trust.
    /// Bilingual (<see cref="RepairLimitation"/>) so the App can render it in the user's language
    /// instead of the raw English fallback (13/08/2026 fix — it used to leak English into the FR UI).
    /// </summary>
    public required IReadOnlyList<RepairLimitation> NotAutomatable { get; init; }

    /// <summary>Nothing to sell on this install. The UI must then show no pitch at all.</summary>
    public bool IsEmpty => FixableCount == 0;

    /// <summary>
    /// Builds the offer from a plan. The plan MUST be unlicensed — passing a licensed plan is a
    /// programming error, because a licensed plan carries the detail this type promises not to
    /// expose, and callers would quietly start rendering the paid view in the free surface.
    /// </summary>
    public static RepairOffer From(RepairPlan plan, int findingsConsidered)
    {
        ArgumentNullException.ThrowIfNull(plan);

        if (plan.Items.Any(i => i.Changes.Count > 0))
            throw new ArgumentException(
                "RepairOffer describes the FREE tier and must be built from an unlicensed plan " +
                "(Plan(..., licensed: false)); this plan carries redacted-tier detail.",
                nameof(plan));

        // Locked AND a real computed fix behind it. Summary is null when the action planned
        // nothing, which is exactly the case that must not be advertised.
        var fixable = plan.Items
            .Where(i => i.Mode == RepairMode.Locked && i.Summary is { ChangeCount: > 0 })
            .ToList();

        // Grouped by Code (falling back to the English text when there is none) rather than by
        // raw string: two items sharing the same underlying reason must collapse to one line
        // regardless of which language it renders in later.
        var notAutomatable = plan.Items
            .SelectMany(i => i.Missing)
            .Where(m => !string.IsNullOrWhiteSpace(m.MessageEn))
            .GroupBy(m => m.Code ?? m.MessageEn, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .OrderBy(m => m.Code ?? m.MessageEn, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var totalSeconds = fixable.Sum(i => i.Summary!.EstimatedDuration switch
        {
            DurationBucket.Seconds => 2.0,
            DurationBucket.UnderAMinute => 30.0,
            _ => 120.0,
        });

        return new RepairOffer
        {
            PlanId = plan.PlanId,
            FindingsConsidered = findingsConsidered,
            FixableCount = fixable.Count,
            ManualOnlyCount = plan.Items.Count(i => i.Mode == RepairMode.ManualOnly),
            FixableCodes = fixable.Select(i => i.TargetCode)
                                  .Distinct(StringComparer.Ordinal)
                                  .OrderBy(c => c, StringComparer.Ordinal)
                                  .ToList(),
            TotalChangeCount = fixable.Sum(i => i.Summary!.ChangeCount),
            Kinds = fixable.SelectMany(i => i.Summary!.Kinds)
                           .Distinct()
                           .OrderBy(k => (int)k)
                           .ToList(),
            // Vacuously-true claims are still lies to a reader: with nothing fixable there is
            // nothing reversible either.
            EveryFixReversible = fixable.Count > 0 && fixable.All(i => i.Summary!.FullyReversible),
            EveryFixBackedUp = fixable.Count > 0 && fixable.All(i => i.Summary!.BackupPlanned),
            EstimatedDuration = totalSeconds < 5 ? DurationBucket.Seconds
                              : totalSeconds < 60 ? DurationBucket.UnderAMinute
                              : DurationBucket.Minutes,
            NotAutomatable = notAutomatable,
        };
    }
}
