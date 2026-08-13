using System;
using PincabToolbox.App.Localization;
using PincabToolbox.Repair;

namespace PincabToolbox.App;

/// <summary>
/// App-side reader for the Knowledge Pack's per-code editorial content (<see cref="PackEntry"/>) —
/// the plain-language "what you'll notice", "good to know" and "how to verify" text a pack author
/// writes by hand for a code, distinct from <see cref="Knowledge"/>'s hardcoded Impact/Cause table.
///
/// <para>
/// Only wired up for the fields the pack does NOT already duplicate from <see cref="Knowledge"/>
/// (07/08/2026 decision, see FIELD-LOG.md): a pack entry's own <c>impactFr/impactEn</c> and
/// <c>causeFr/causeEn</c> are read by nothing here on purpose, <see cref="Knowledge"/> stays the
/// single source of truth for those two fields because it covers all 51 known codes, not just the
/// 7 the pack currently annotates. Adding a second source for the same two fields would only risk
/// the pack and Knowledge.cs silently disagreeing with each other.
/// </para>
///
/// <para>
/// Degrades the same way the rest of the pack does (ADR-005): a code with no pack entry, or a
/// field left blank for that entry, returns null and the caller simply doesn't show that section.
/// </para>
/// </summary>
public static class PackKnowledge
{
    public static string? Player(string? code) => Pick(code, e => e.PlayerFr, e => e.PlayerEn, e => e.PlayerEs);

    public static string? Explanation(string? code) =>
        Pick(code, e => e.ExplanationFr, e => e.ExplanationEn, e => e.ExplanationEs);

    public static string? Verification(string? code) =>
        Pick(code, e => e.VerificationFr, e => e.VerificationEn, e => e.VerificationEs);

    private static string? Pick(string? code, Func<PackEntry, string?> fr, Func<PackEntry, string?> en, Func<PackEntry, string?> es)
    {
        if (code is null) return null;
        var entry = RepairOfferBuilder.LoadPack().EntryFor(code);
        if (entry is null) return null;
        return Loc.Lang switch
        {
            "fr" => fr(entry) ?? en(entry),
            "es" => es(entry) ?? en(entry),
            _ => en(entry) ?? fr(entry),
        };
    }
}
