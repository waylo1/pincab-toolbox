namespace PincabToolbox.Repair;

/// <summary>
/// Crosses the gates. Pure function: no state, no I/O.
///
/// Gate 1 — existence: is there a rule backed by a compiled action at all?
/// Gate 2 — safety:    does confidence allow anything other than a manual procedure?
/// Gate 3 — commercial: is the licence valid?
///
/// The safety gate can only DOWNGRADE the mode, never upgrade it.
/// </summary>
/// <remarks>
/// <para>
/// Safety is evaluated BEFORE the licence, and that ordering is a commercial-honesty
/// requirement, not a detail. The gates used to run commercial-then-safety, so a rule with
/// confidence below <see cref="ConfirmationThreshold"/> resolved to <see cref="RepairMode.Locked"/>
/// while unlicensed and to <see cref="RepairMode.ManualOnly"/> once licensed: the free scan
/// advertised "a fix exists, unlock Repair" for a finding that, after payment, produced nothing
/// but a manual procedure. Selling a fix that the licence does not actually unlock is the one
/// mistake this product cannot afford. (FIELD-LOG 2026-08-03.)
/// </para>
/// <para>
/// The ADR-006 promise is unchanged: anything genuinely fixable still shows as Locked without a
/// licence, summary visible, detail withheld.
/// </para>
/// </remarks>
public static class RepairModeResolver
{
    public const int AutomaticThreshold = 95;
    public const int ConfirmationThreshold = 70;

    /// <param name="hasRule">A rule exists AND its ActionId is in the compiled registry.</param>
    /// <param name="licensed">Valid Repair licence.</param>
    /// <param name="repairConfidence">0–100, from the Knowledge Pack.</param>
    /// <param name="reversible">Logical AND of what the rule declares and IsReversibleByNature.</param>
    public static RepairMode Resolve(bool hasRule, bool licensed, int repairConfidence, bool reversible)
    {
        // Gate 1 — existence.
        if (!hasRule) return RepairMode.ManualOnly;

        // Gate 2 — safety. Runs first so that Locked can only ever mean "a licence unlocks this".
        if (repairConfidence < ConfirmationThreshold) return RepairMode.ManualOnly;

        // Gate 3 — commercial. Plan stays visible, detail withheld — ADR-006.
        if (!licensed) return RepairMode.Locked;

        if (repairConfidence < AutomaticThreshold) return RepairMode.ConfirmationRequired;

        // Golden rule: a non-reversible action is NEVER automatic, whatever the confidence.
        return reversible ? RepairMode.Automatic : RepairMode.ConfirmationRequired;
    }
}
