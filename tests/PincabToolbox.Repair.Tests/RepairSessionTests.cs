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

    public static void Test_VerifyLicense_RealEmbeddedKey_GarbageInputStaysInvalid()
    {
        // 11/08/2026: EmbeddedPublicKeyBase64 is now a REAL key (Maxime ran `license-tool init`
        // for real, see LicenseVerifier's own header) — Apply() is no longer a guaranteed no-op
        // the way it was before this date, see ADR-012's follow-up note. What this test still
        // locks in: an arbitrary string is not a validly-signed license under ANY key, real or
        // placeholder, so it must stay Invalid regardless of which key is embedded.
        var root = NewTempRoot();
        try
        {
            var session = new RepairSession(KnowledgePack.Empty, new[] { root }, appDataRoot: root);
            var result = session.VerifyLicense("anything-at-all");
            A.False(result.IsValid, "an arbitrary string is never a validly-signed license");
        }
        finally { TryDelete(root); }
    }

    public static void Test_EmbeddedPublicKey_IsARealKey_NotThePlaceholder()
    {
        // Regression guard: catches an accidental revert to the placeholder string just as surely
        // as it caught the placeholder being forgotten before — same spirit, opposite direction.
        A.False(LicenseVerifier.EmbeddedPublicKeyBase64.Contains("PLACEHOLDER", StringComparison.Ordinal),
            "EmbeddedPublicKeyBase64 must be the real key, not the placeholder string");

        // Must parse as a well-formed P-256 SubjectPublicKeyInfo — exactly the check the
        // 2026-08-04 audit found broken on the placeholder. Throws (test failure) if malformed.
        using var key = System.Security.Cryptography.ECDsa.Create();
        key.ImportSubjectPublicKeyInfo(Convert.FromBase64String(LicenseVerifier.EmbeddedPublicKeyBase64), out _);
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

    // ───────────────────────── Forced dry-run kill switch (11/08/2026, ADR-012 "Suite") ─────────────────────────

    public static void Test_IsForceDryRunRequestedByEnvironment_UnsetIsFalse()
    {
        var before = Environment.GetEnvironmentVariable("PINCAB_REPAIR_FORCE_DRYRUN");
        try
        {
            Environment.SetEnvironmentVariable("PINCAB_REPAIR_FORCE_DRYRUN", null);
            A.False(RepairSession.IsForceDryRunRequestedByEnvironment(), "unset must default to normal behavior");
        }
        finally { Environment.SetEnvironmentVariable("PINCAB_REPAIR_FORCE_DRYRUN", before); }
    }

    public static void Test_IsForceDryRunRequestedByEnvironment_RecognizesTrueVariants()
    {
        var before = Environment.GetEnvironmentVariable("PINCAB_REPAIR_FORCE_DRYRUN");
        try
        {
            foreach (var v in new[] { "1", "true", "TRUE", "yes", "YES" })
            {
                Environment.SetEnvironmentVariable("PINCAB_REPAIR_FORCE_DRYRUN", v);
                A.True(RepairSession.IsForceDryRunRequestedByEnvironment(), $"'{v}' must be recognized as enabled");
            }
            foreach (var v in new[] { "0", "false", "no", "" })
            {
                Environment.SetEnvironmentVariable("PINCAB_REPAIR_FORCE_DRYRUN", v);
                A.False(RepairSession.IsForceDryRunRequestedByEnvironment(), $"'{v}' must NOT be recognized as enabled");
            }
        }
        finally { Environment.SetEnvironmentVariable("PINCAB_REPAIR_FORCE_DRYRUN", before); }
    }

    public static void Test_Apply_ForcedDryRun_NeverWritesTheFileAndReportsSelectedItemsAsOk()
    {
        var root = NewTempRoot();
        try
        {
            Directory.CreateDirectory(root);
            var target = Path.Combine(root, "would-be-written.txt");
            File.WriteAllText(target, "untouched");

            var session = new RepairSession(KnowledgePack.Empty, new[] { root }, appDataRoot: root, forceDryRun: true);
            A.True(session.ForceDryRunActive, "constructor override must be honored");

            // A fictitious ActionId is safe here specifically BECAUSE forced dry-run never reaches
            // the registry — if it did, this would fail with "unknown action", proving the isolation.
            var plan = new RepairPlan
            {
                PlanId = "plan-test-1",
                CreatedAtUtc = DateTimeOffset.UtcNow,
                ScanReportId = "scan-1",
                Items = new[]
                {
                    Item("i1", RepairMode.Automatic, new[]
                    {
                        new PlannedChange
                        {
                            ActionId = "not_a_real_action", Kind = ChangeKind.FileAttribute,
                            Target = target, Before = "untouched", After = "would-be-changed", Reversible = true,
                        },
                    }),
                },
            };

            var result = session.Apply(plan, new HashSet<string> { "i1" });

            A.True(result.ForcedDryRun, "the result must say plainly this was a forced dry-run");
            A.True(result.ItemOutcomes.TryGetValue("i1", out var ok) && ok, "selected item reports as ok");
            A.False(result.RecoveryRequired, "a forced dry-run never needs recovery");
            A.Equal("untouched", File.ReadAllText(target), "forced dry-run must never touch the real file");
        }
        finally { TryDelete(root); }
    }

    public static void Test_Apply_ForcedDryRun_UnselectedItemsAreNotInOutcomes()
    {
        var root = NewTempRoot();
        try
        {
            var session = new RepairSession(KnowledgePack.Empty, new[] { root }, appDataRoot: root, forceDryRun: true);
            var plan = new RepairPlan
            {
                PlanId = "plan-test-2",
                CreatedAtUtc = DateTimeOffset.UtcNow,
                ScanReportId = "scan-1",
                Items = new[]
                {
                    Item("i1", RepairMode.Automatic, new[] { Change("/x/a", true) }),
                    Item("i2", RepairMode.Automatic, new[] { Change("/x/b", true) }),
                },
            };

            var result = session.Apply(plan, new HashSet<string> { "i1" });

            A.Equal(1, result.ItemOutcomes.Count, "only the explicitly selected item is reported — opt-in, even in a forced dry-run");
            A.True(result.ItemOutcomes.ContainsKey("i1"), "the selected item must be present");
        }
        finally { TryDelete(root); }
    }

    public static void Test_Apply_WithoutForceDryRun_DefaultIsRealEngine_AndFailsOnUnknownAction()
    {
        // Companion to the forced-dry-run test above: proves the fictitious ActionId trick would
        // NOT silently succeed if forced dry-run were somehow bypassed — the real engine actually
        // gets called by default, and rejects an unregistered action rather than pretending it ran.
        var root = NewTempRoot();
        try
        {
            Directory.CreateDirectory(root);
            var target = Path.Combine(root, "target.txt");
            File.WriteAllText(target, "untouched");

            var session = new RepairSession(KnowledgePack.Empty, new[] { root }, appDataRoot: root, forceDryRun: false);
            A.False(session.ForceDryRunActive, "explicit forceDryRun:false must be honored");

            var plan = new RepairPlan
            {
                PlanId = "plan-test-3",
                CreatedAtUtc = DateTimeOffset.UtcNow,
                ScanReportId = "scan-1",
                Items = new[]
                {
                    Item("i1", RepairMode.Automatic, new[]
                    {
                        new PlannedChange
                        {
                            ActionId = "not_a_real_action", Kind = ChangeKind.FileAttribute,
                            Target = target, Before = "untouched", After = "would-be-changed", Reversible = true,
                        },
                    }),
                },
            };

            var result = session.Apply(plan, new HashSet<string> { "i1" });

            A.False(result.ForcedDryRun, "the real engine path must never claim to be a forced dry-run");
            A.True(result.ItemOutcomes.TryGetValue("i1", out var ok) && !ok, "an unknown action must be reported as failed, never silently ok");
            A.Equal("untouched", File.ReadAllText(target), "an unknown action must never write, but this is a genuine engine rejection, not a dry-run skip");
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
