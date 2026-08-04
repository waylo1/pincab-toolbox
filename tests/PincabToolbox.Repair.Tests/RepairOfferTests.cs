using PincabToolbox.Core.Models;
using PincabToolbox.Repair;
using PincabToolbox.Repair.Actions;

namespace PincabToolbox.Repair.Tests;

/// <summary>
/// Commercial honesty. Everything here answers one question: can the free scan promise something
/// that buying Repair does not deliver? Every "no" in this file is a refund that does not happen.
/// FIELD-LOG 2026-08-03.
/// </summary>
public static class RepairOfferTests
{
    // ═══════════ 1. The gate ordering — Locked must mean "a licence unlocks this" ═══════════

    /// <summary>
    /// The regression that motivated the reorder: confidence below the safety threshold used to
    /// read Locked while unlicensed and ManualOnly once licensed. The free tier advertised a fix
    /// the purchase would not deliver.
    /// </summary>
    public static void Test_LowConfidence_IsManualOnly_LicensedOrNot()
    {
        A.Equal(RepairMode.ManualOnly, RepairModeResolver.Resolve(true, false, 50, true),
            "unlicensed, confidence 50");
        A.Equal(RepairMode.ManualOnly, RepairModeResolver.Resolve(true, true, 50, true),
            "licensed, confidence 50");
    }

    /// <summary>Locked, once resolved, must always become something actionable with a licence.</summary>
    public static void Test_EveryLockedConfidence_BecomesActionableWhenLicensed()
    {
        for (var confidence = 0; confidence <= 100; confidence++)
        {
            foreach (var reversible in new[] { true, false })
            {
                if (RepairModeResolver.Resolve(true, false, confidence, reversible) != RepairMode.Locked)
                    continue;

                var licensed = RepairModeResolver.Resolve(true, true, confidence, reversible);
                A.True(licensed is RepairMode.ConfirmationRequired or RepairMode.Automatic,
                    $"confidence {confidence} shows as Locked but licences to {licensed}");
            }
        }
    }

    /// <summary>The safety gate must still only ever downgrade.</summary>
    public static void Test_SafetyGate_NeverUpgrades()
    {
        A.Equal(RepairMode.ConfirmationRequired, RepairModeResolver.Resolve(true, true, 100, false),
            "irreversible stays confirmation, never automatic");
        A.Equal(RepairMode.ManualOnly, RepairModeResolver.Resolve(false, true, 100, true),
            "no rule stays manual");
    }

    // ═══════════ 2. An action that plans nothing has nothing to sell ═══════════

    /// <summary>
    /// Actions fail closed on purpose. Before this, such an item still surfaced as Locked — a
    /// paid-for fix that would then do literally nothing.
    /// </summary>
    public static void Test_ActionThatPlansNothing_IsNotAdvertisedAsFixable()
    {
        var (eng, findings) = WithBarrenAction(licensedProbe: false);
        var plan = eng.Plan("scan-1", findings, licensed: false);

        A.Equal(RepairMode.ManualOnly, plan.Items[0].Mode,
            "an action that planned nothing must not read as Locked");
        A.True(plan.Items[0].Summary is null, "and it must carry no summary");
        A.True(plan.Items[0].Missing.Count > 0, "the reason must be stated, not swallowed");

        var offer = RepairOffer.From(plan, findings.Count);
        A.Equal(0, offer.FixableCount, "nothing to sell");
        A.True(offer.IsEmpty, "the offer is empty");
    }

    // ═══════════ 3. The offer aggregate ═══════════

    public static void Test_Offer_CountsOnlyGenuinelyFixableFindings()
    {
        var (eng, findings) = TwoFixableOneManual();
        var offer = RepairOffer.From(eng.Plan("scan-1", findings, licensed: false), findings.Count);

        A.Equal(3, offer.FindingsConsidered, "denominator is every finding");
        A.Equal(2, offer.FixableCount, "two have a real fix behind them");
        A.Equal(1, offer.ManualOnlyCount, "the third stays manual");
        A.Sequence(new[] { "BLOCKED_DLL", "OTHER_DLL" }, offer.FixableCodes, "codes badge-able in the UI");
    }

    public static void Test_Offer_ReversibilityIsUnanimousOrFalse()
    {
        var (eng, findings) = TwoFixableOneManual(secondReversible: false);
        var offer = RepairOffer.From(eng.Plan("scan-1", findings, licensed: false), findings.Count);

        A.False(offer.EveryFixReversible,
            "one irreversible item must sink the claim for the whole offer");
    }

    public static void Test_Offer_EmptyPlanClaimsNothing()
    {
        var (eng, _) = TwoFixableOneManual();
        var offer = RepairOffer.From(eng.Plan("scan-1", Array.Empty<Finding>(), licensed: false), 0);

        A.Equal(0, offer.FixableCount, "nothing fixable");
        A.False(offer.EveryFixReversible, "a vacuous 'all reversible' is still a false claim");
        A.False(offer.EveryFixBackedUp, "same for backups");
        A.True(offer.IsEmpty, "the UI must show no pitch at all");
    }

    /// <summary>ADR-006 enforced at the type boundary, not by convention.</summary>
    public static void Test_Offer_RefusesALicensedPlan()
    {
        var (eng, findings) = TwoFixableOneManual();
        var licensedPlan = eng.Plan("scan-1", findings, licensed: true);

        var threw = false;
        try { RepairOffer.From(licensedPlan, findings.Count); }
        catch (ArgumentException) { threw = true; }

        A.True(threw, "building the free offer from a licensed plan must be refused outright");
    }

    /// <summary>The free offer must never carry a path, a value or an ordering.</summary>
    public static void Test_Offer_CarriesNoPlanDetail()
    {
        var (eng, findings) = TwoFixableOneManual();
        var plan = eng.Plan("scan-1", findings, licensed: false);
        var offer = RepairOffer.From(plan, findings.Count);

        var rendered = string.Join("|", offer.FixableCodes)
                     + "|" + string.Join("|", offer.NotAutomatable)
                     + "|" + offer.PlanId;

        A.False(rendered.Contains(@"C:\", StringComparison.OrdinalIgnoreCase),
            "no target path may reach the free surface");
    }

    public static void Test_Offer_StatesWhatItWillNotDo()
    {
        var (eng, findings) = WithBarrenAction(licensedProbe: false);
        var offer = RepairOffer.From(eng.Plan("scan-1", findings, licensed: false), findings.Count);

        A.True(offer.NotAutomatable.Count > 0,
            "limitations are surfaced before purchase, not discovered after");
    }

    // ───────────────────────────── fixtures ─────────────────────────────

    private static (IRepairEngine, IReadOnlyList<Finding>) TwoFixableOneManual(bool secondReversible = true)
    {
        var fs = new FakeFs();
        fs.AddFile(@"C:\vpx\a.dll"); fs.Blocked.Add(@"C:\vpx\a.dll");
        fs.AddFile(@"C:\vpx\b.dll"); fs.Blocked.Add(@"C:\vpx\b.dll");

        var second = new ScriptedAction(fs, "second") { IsReversibleByNature = secondReversible };

        var pack = new KnowledgePack("2026.08", new[]
        {
            Build.Rule("unblock", "BLOCKED_DLL", "unblock_file"),
            Build.Rule("second", "OTHER_DLL", "second", reversible: secondReversible),
            // NO rule for UNKNOWN_CODE — it must land in ManualOnly.
        });

        var eng = new RepairEngine(
            new RepairActionRegistry(new UnblockFileAction(fs), second),
            pack, new InMemoryRepairJournal(), new FakeBackup(), new FakeProbe(),
            new FakeClock(), Build.Roots);

        var findings = new[]
        {
            Build.Finding("BLOCKED_DLL", @"C:\vpx\a.dll"),
            Build.Finding("OTHER_DLL", @"C:\vpx\b.dll"),
            Build.Finding("UNKNOWN_CODE", @"C:\vpx\c.dll"),
        };

        return (eng, findings);
    }

    /// <summary>An action wired to a rule, whose Plan() legitimately returns nothing.</summary>
    private static (IRepairEngine, IReadOnlyList<Finding>) WithBarrenAction(bool licensedProbe)
    {
        var pack = new KnowledgePack("2026.08", new[] { Build.Rule("barren", "BARREN_CODE", "barren") });

        var eng = new RepairEngine(
            new RepairActionRegistry(new BarrenAction()),
            pack, new InMemoryRepairJournal(), new FakeBackup(), new FakeProbe(),
            new FakeClock(), Build.Roots);

        return (eng, new[] { Build.Finding("BARREN_CODE", @"C:\vpx\nothing.dll") });
    }

    /// <summary>Fails closed: it is wired up, valid, and finds nothing it can safely change.</summary>
    private sealed class BarrenAction : IRepairAction
    {
        public string ActionId => "barren";
        public ChangeKind Kind => ChangeKind.FileAttribute;
        public bool IsReversibleByNature => true;

        public ValidationResult ValidateParameters(IReadOnlyDictionary<string, string> p)
            => ValidationResult.Ok;

        public IReadOnlyList<PlannedChange> Plan(RepairContext ctx, IReadOnlyDictionary<string, string> p)
            => Array.Empty<PlannedChange>();

        public bool StillApplies(RepairContext ctx) => true;
        public ExecutionResult Execute(PlannedChange c) => ExecutionResult.Ok;
        public ExecutionResult Revert(PlannedChange c) => ExecutionResult.Ok;
    }
}
