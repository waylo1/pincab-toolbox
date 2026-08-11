using PincabToolbox.Core.Models;
using PincabToolbox.Repair;
using PincabToolbox.Repair.Licensing;

namespace PincabToolbox.Repair.Tests;

/// <summary>
/// LOT H (spec 10/08) — <see cref="RepairSession"/>, the write-path orchestrator behind Écran 2.
/// Uses real temp directories on disk (same posture as <see cref="FileRepairJournalTests"/>) since
/// this class composes the REAL services (<c>RealFileSystem</c>, <c>RealEnvironmentProbe</c>), not
/// the fakes the rest of the Repair suite injects — that composition is exactly what this class
/// exists to get right, so it is what these tests exercise.
/// </summary>
public static class RepairSessionTests
{
    private sealed class StubVerifier : ILicenseVerifier
    {
        public LicenseCheckResult Result { get; set; } = LicenseCheckResult.Invalid("stub: not configured");
        public string? LastKeySeen { get; private set; }
        public LicenseCheckResult Verify(string? licenseKey) { LastKeySeen = licenseKey; return Result; }
    }

    private static string NewTempRoot()
        => Path.Combine(Path.GetTempPath(), "pincab-repairsession-tests-" + Guid.NewGuid().ToString("N"));

    private static void TryDelete(string root)
    {
        try { if (Directory.Exists(root)) Directory.Delete(root, recursive: true); }
        catch { /* best-effort cleanup */ }
    }

    // ───────────────────────── License wiring (H.4) ─────────────────────────

    public static void Test_VerifyLicense_RealEmbeddedKey_IsInvalidToday()
    {
        // Honest current-state test: the embedded public key is still the LicenseVerifier
        // placeholder (see its own header comment), so a real RepairSession can never grant
        // licensed:true today — Apply() is safe by construction until Maxime runs
        // `license-tool init` for real. This test exists to catch the day someone accidentally
        // ships a real key here without meaning to license anything by surprise.
        var root = NewTempRoot();
        try
        {
            var session = new RepairSession(KnowledgePack.Empty, new[] { root }, appDataRoot: root);
            var result = session.VerifyLicense("anything-at-all");
            A.False(result.IsValid, "placeholder public key must never validate any key");
        }
        finally { TryDelete(root); }
    }

    public static void Test_VerifyLicense_DelegatesToInjectedVerifier()
    {
        var root = NewTempRoot();
        try
        {
            var stub = new StubVerifier { Result = LicenseCheckResult.Valid(new LicensePayload
            {
                Email = "maxime@example.com",
                IssuedUtc = DateTimeOffset.UtcNow,
                UpdatesUntilUtc = DateTimeOffset.UtcNow.AddYears(1),
            }) };
            var session = new RepairSession(KnowledgePack.Empty, new[] { root }, appDataRoot: root, licenseVerifier: stub);

            var result = session.VerifyLicense("some-key");
            A.True(result.IsValid, "the session must trust its injected verifier's real answer");
            A.Equal("some-key", stub.LastKeySeen, "the exact key text must reach the verifier unmodified");
        }
        finally { TryDelete(root); }
    }

    // ───────────────────────── Journal persistence (H.1, through the session) ─────────────────────────

    public static void Test_Plan_PersistsAcrossSessions_SamePlanIdKnownAfterRestart()
    {
        var root = NewTempRoot();
        try
        {
            var session1 = new RepairSession(KnowledgePack.Empty, new[] { root }, appDataRoot: root);
            var plan = session1.Plan("scan-1", Array.Empty<Finding>(), licensed: false);

            // A fresh RepairSession, as if the app had been closed and reopened.
            var session2 = new RepairSession(KnowledgePack.Empty, new[] { root }, appDataRoot: root);
            var knownIds = session2.KnownPlanIds();

            A.True(knownIds.Contains(plan.PlanId), "the plan created by the first session must be visible to a brand new one");
        }
        finally { TryDelete(root); }
    }

    public static void Test_KnownPlanIds_EmptyForAFreshRoot()
    {
        var root = NewTempRoot();
        try
        {
            var session = new RepairSession(KnowledgePack.Empty, new[] { root }, appDataRoot: root);
            A.Equal(0, session.KnownPlanIds().Count, "no plan created yet");
        }
        finally { TryDelete(root); }
    }

    // ───────────────────────── Apply() selection wiring (H.2 rule 3 — opt-in, never silent) ─────────────────────────

    public static void Test_Apply_EmptySelection_AppliesNothing()
    {
        var root = NewTempRoot();
        try
        {
            var session = new RepairSession(KnowledgePack.Empty, new[] { root }, appDataRoot: root);
            var plan = session.Plan("scan-1", Array.Empty<Finding>(), licensed: false);

            var result = session.Apply(plan, new HashSet<string>());
            A.Equal(0, result.ItemOutcomes.Count, "nothing was selected");
        }
        finally { TryDelete(root); }
    }

    // ───────────────────────── Describe() — pure (H.2 rule 3 confirmation text facts) ─────────────────────────

    private static RepairPlanItem Item(string id, RepairMode mode, IReadOnlyList<PlannedChange> changes, RepairSummary? summary = null)
        => new() { ItemId = id, TargetCode = "X", Mode = mode, Changes = changes, Summary = summary };

    private static PlannedChange Change(string target, bool reversible) => new()
    {
        ActionId = "a", Kind = ChangeKind.FileAttribute, Target = target, Before = "b", After = "a", Reversible = reversible,
    };

    public static void Test_Describe_ReversibleItem_ReportsReversibleTrue()
    {
        var items = new[] { Item("i1", RepairMode.ConfirmationRequired, new[] { Change("/x/a", true) }) };
        var desc = RepairSession.Describe(items);
        A.Equal(1, desc.Count, "one item in, one description out");
        A.True(desc[0].Reversible, "all changes reversible -> item reversible");
    }

    public static void Test_Describe_MixedReversibility_ReportsFalse()
    {
        var items = new[] { Item("i1", RepairMode.ConfirmationRequired, new[] { Change("/x/a", true), Change("/x/b", false) }) };
        var desc = RepairSession.Describe(items);
        A.False(desc[0].Reversible, "one non-reversible change makes the whole item non-reversible");
    }

    public static void Test_Describe_UsesSummaryWhenPresent_OverPerChangeComputation()
    {
        // When a Summary already exists (the normal case — the engine always computes one for a
        // real, licensed plan), it is the source of truth rather than a second computation that
        // could drift from it.
        var summary = new RepairSummary
        {
            ChangeCount = 1, Kinds = new[] { ChangeKind.FileAttribute },
            FullyReversible = true, BackupPlanned = true, EstimatedDuration = DurationBucket.Seconds,
        };
        var items = new[] { Item("i1", RepairMode.ConfirmationRequired, new[] { Change("/x/a", true) }, summary) };
        var desc = RepairSession.Describe(items);
        A.True(desc[0].BackupPlanned, "backup-planned must come from the real computed summary");
    }

    public static void Test_Describe_TargetsAreDistinct()
    {
        var items = new[] { Item("i1", RepairMode.ConfirmationRequired, new[] { Change("/x/a", true), Change("/x/a", true) }) };
        var desc = RepairSession.Describe(items);
        A.Equal(1, desc[0].Targets.Count, "duplicate target must be de-duplicated");
    }

    public static void Test_Describe_NoChanges_BackupNotPlanned_NotReversible()
    {
        // A ManualOnly/Locked item with no changes has nothing to back up or undo — must not
        // falsely claim either.
        var items = new[] { Item("i1", RepairMode.Locked, Array.Empty<PlannedChange>()) };
        var desc = RepairSession.Describe(items);
        A.False(desc[0].BackupPlanned, "nothing planned -> no backup claim");
        A.False(desc[0].Reversible, "nothing planned -> no reversibility claim");
    }
}
