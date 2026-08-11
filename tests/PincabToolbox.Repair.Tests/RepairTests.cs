using PincabToolbox.Core.Models;
using PincabToolbox.Repair;
using PincabToolbox.Repair.Actions;

namespace PincabToolbox.Repair.Tests;

/// <summary>
/// Executable specification of the Repair engine.
/// Each test maps to a rule stated in docs/DESIGN-Repair-v1.md.
/// </summary>
public static class RepairTests
{
    // ═══════════════ 1. Gating — two gates, safety can only downgrade ═══════════════

    public static void Test_Gating_NoRule_IsManualOnly()
        => A.Equal(RepairMode.ManualOnly, RepairModeResolver.Resolve(false, true, 99, true), "no rule");

    public static void Test_Gating_NoLicence_IsLocked()
        => A.Equal(RepairMode.Locked, RepairModeResolver.Resolve(true, false, 99, true), "no licence");

    public static void Test_Gating_LicenceGateBeatsPerfectConfidence()
        => A.Equal(RepairMode.Locked, RepairModeResolver.Resolve(true, false, 100, true), "licence first");

    public static void Test_Gating_LowConfidence_IsManualOnly()
        => A.Equal(RepairMode.ManualOnly, RepairModeResolver.Resolve(true, true, 69, true), "conf 69");

    public static void Test_Gating_LowerBoundOfConfirmationIsInclusive()
        => A.Equal(RepairMode.ConfirmationRequired, RepairModeResolver.Resolve(true, true, 70, true), "conf 70");

    public static void Test_Gating_UpperBoundOfConfirmationIsExclusive()
        => A.Equal(RepairMode.ConfirmationRequired, RepairModeResolver.Resolve(true, true, 94, true), "conf 94");

    public static void Test_Gating_HighConfidenceAndReversible_IsAutomatic()
        => A.Equal(RepairMode.Automatic, RepairModeResolver.Resolve(true, true, 95, true), "conf 95");

    /// <summary>Golden rule: never automatic when it cannot be undone, whatever the confidence.</summary>
    public static void Test_Gating_NonReversibleIsNeverAutomatic()
        => A.Equal(RepairMode.ConfirmationRequired, RepairModeResolver.Resolve(true, true, 100, false), "golden rule");

    // ═══════════════ 2. Plan — the dry-run is pure ═══════════════

    public static void Test_Plan_DoesNotTouchTheDisk()
    {
        var (fs, eng, _) = Setup(blocked: @"C:\vpx\a.dll");
        eng.Plan("scan-1", new[] { Build.Finding("BLOCKED_DLL", @"C:\vpx\a.dll") }, licensed: true);
        A.True(fs.HasZoneIdentifier(@"C:\vpx\a.dll"), "planning must not unblock anything");
    }

    public static void Test_Plan_CarriesTheObservedBeforeValue()
    {
        var (_, eng, _) = Setup(blocked: @"C:\vpx\a.dll");
        var p = eng.Plan("scan-1", new[] { Build.Finding("BLOCKED_DLL", @"C:\vpx\a.dll") }, true);
        A.Equal("blocked by Windows", p.Items[0].Changes[0].Before, "before value");
    }

    public static void Test_Plan_IsIdempotent()
    {
        var (_, eng, _) = Setup(blocked: @"C:\vpx\a.dll");
        var f = new[] { Build.Finding("BLOCKED_DLL", @"C:\vpx\a.dll") };
        var a = eng.Plan("scan-1", f, true);
        var b = eng.Plan("scan-1", f, true);
        A.Equal(a.Items[0].Changes[0].After, b.Items[0].Changes[0].After, "planning twice");
    }

    public static void Test_Plan_IsTiedToItsScan()
    {
        var (_, eng, _) = Setup(blocked: @"C:\vpx\a.dll");
        var p = eng.Plan("scan-42", new[] { Build.Finding("BLOCKED_DLL", @"C:\vpx\a.dll") }, true);
        A.Equal("scan-42", p.ScanReportId, "scan id");
    }

    /// <summary>ADR-006: without a licence the DETAIL is withheld — that is what Repair sells.</summary>
    public static void Test_Plan_WithoutLicence_DetailIsWithheld()
    {
        var (_, eng, _) = Setup(blocked: @"C:\vpx\a.dll");
        var p = eng.Plan("scan-1", new[] { Build.Finding("BLOCKED_DLL", @"C:\vpx\a.dll") }, licensed: false);

        A.Equal(RepairMode.Locked, p.Items[0].Mode, "mode locked");
        A.Equal(0, p.Items[0].Changes.Count, "no path, no value, no ordering leaks");
    }

    /// <summary>...but enough is shown to understand and to trust.</summary>
    public static void Test_Plan_WithoutLicence_SummaryIsStillShown()
    {
        var (_, eng, _) = Setup(blocked: @"C:\vpx\a.dll");
        var s = eng.Plan("scan-1", new[] { Build.Finding("BLOCKED_DLL", @"C:\vpx\a.dll") }, licensed: false)
                   .Items[0].Summary;

        A.True(s is not null, "a summary is offered");
        A.Equal(1, s!.ChangeCount, "how many writes");
        A.True(s.FullyReversible, "reversible");
        A.True(s.BackupPlanned, "backed up");
        A.Equal(DurationBucket.Seconds, s.EstimatedDuration, "roughly this long");
    }

    /// <summary>The summary is COMPUTED from the real plan — never a declared promise.</summary>
    public static void Test_Summary_IsDerivedFromTheRealPlanNotDeclared()
    {
        var fs = new FakeFs();
        fs.AddFile(@"C:\vpx\1", "a");
        var pack = new KnowledgePack("2026.08",
            new[] { Build.Rule("r", "CODE", "scripted", confidence: 98, reversible: true) });
        // The rule claims reversible, but the ACTION says otherwise: the action wins.
        var action = new ScriptedAction(fs) { IsReversibleByNature = false };
        var eng = Engine(fs, pack, new RepairActionRegistry(action), out _, out _);

        var s = eng.Plan("scan-1", new[] { Build.Finding("CODE", @"C:\vpx\1") }, licensed: false).Items[0].Summary;
        A.False(s!.FullyReversible, "a rule cannot promise reversibility the action cannot deliver");
    }

    /// <summary>Licensed: the full detail comes back, same objects, unchanged.</summary>
    public static void Test_Plan_WithLicence_DetailIsRestored()
    {
        var (_, eng, _) = Setup(blocked: @"C:\vpx\a.dll");
        var f = new[] { Build.Finding("BLOCKED_DLL", @"C:\vpx\a.dll") };

        var free = eng.Plan("scan-1", f, licensed: false);
        var paid = eng.Plan("scan-1", f, licensed: true);

        A.Equal(0, free.Items[0].Changes.Count, "withheld");
        A.Equal(1, paid.Items[0].Changes.Count, "restored");
        A.Equal(free.Items[0].Summary!.ChangeCount, paid.Items[0].Changes.Count,
            "the free summary told the truth about the count all along");
    }

    /// <summary>Hiding a LIMITATION would be overselling, not protecting value.</summary>
    public static void Test_Plan_WithoutLicence_PartialityIsStillDisclosed()
    {
        var fs = new FakeFs();
        fs.AddFile(@"C:\vpx\1", "a");
        var pack = new KnowledgePack("2026.08",
            new[] { Build.Rule("auto", "CODE_A", "scripted") },
            new[] { Scenario("MIG", new[] { "CODE_A", "CODE_B" }, ("auto", false), ("manual", true)) });
        var eng = Engine(fs, pack, new RepairActionRegistry(new ScriptedAction(fs)), out _, out _);

        var item = eng.Plan("scan-1", new[]
        {
            Build.Finding("CODE_A", @"C:\vpx\1"),
            Build.Finding("CODE_B", @"C:\vpx\2"),
        }, licensed: false).Items.First(i => i.TargetCode == "MIG");

        A.Equal(Completeness.Partial, item.Completeness, "partiality is not a secret");
        A.True(item.Missing.Count > 0, "and it still says what it cannot do");
        A.Equal(0, item.Changes.Count, "while the how-to stays behind the licence");
    }

    public static void Test_Plan_ItemsAreNotSelectedByDefault()
    {
        var (_, eng, _) = Setup(blocked: @"C:\vpx\a.dll");
        var p = eng.Plan("scan-1", new[] { Build.Finding("BLOCKED_DLL", @"C:\vpx\a.dll") }, true);
        A.False(p.Items[0].Selected, "opt-in by default");
    }

    /// <summary>ADR-005: a pack newer than the app degrades cleanly instead of crashing.</summary>
    public static void Test_Plan_UnknownActionFallsBackToManualOnly()
    {
        var fs = new FakeFs();
        fs.AddFile(@"C:\vpx\a.dll");
        var pack = new KnowledgePack("2026.08",
            new[] { Build.Rule("r", "BLOCKED_DLL", "action_from_the_future") });
        var eng = Engine(fs, pack, new RepairActionRegistry(new UnblockFileAction(fs)), out _, out _);

        var p = eng.Plan("scan-1", new[] { Build.Finding("BLOCKED_DLL", @"C:\vpx\a.dll") }, true);
        A.Equal(RepairMode.ManualOnly, p.Items[0].Mode, "unknown action → manual");
        A.Equal(0, p.Items[0].Changes.Count, "no change planned");
    }

    // ═══════════════ 3. Preflight — five checks ═══════════════

    public static void Test_Preflight_RefusesWhileVpxIsRunning()
    {
        var (fs, eng, probe) = Setup(blocked: @"C:\vpx\a.dll");
        probe.Blocking.Add("VPinballX");
        var plan = Build.Select(eng.Plan("scan-1", new[] { Build.Finding("BLOCKED_DLL", @"C:\vpx\a.dll") }, true));

        var pre = eng.Preflight(plan);
        A.False(pre.Passed, "must refuse");
        A.True(pre.Blockers.Any(b => b.Code == "VPX_RUNNING"), "blocker named");
        A.True(fs.HasZoneIdentifier(@"C:\vpx\a.dll"), "nothing written");
    }

    public static void Test_Apply_IsBlockedWhileVpxIsRunning()
    {
        var (fs, eng, probe) = Setup(blocked: @"C:\vpx\a.dll");
        probe.Blocking.Add("PinUpPlayer");
        var plan = Build.Select(eng.Plan("scan-1", new[] { Build.Finding("BLOCKED_DLL", @"C:\vpx\a.dll") }, true));

        var res = eng.Apply(plan);
        A.True(fs.HasZoneIdentifier(@"C:\vpx\a.dll"), "apply must run preflight itself");
        A.True(res.Blockers.Any(b => b.Code == "VPX_RUNNING"), "blockers surfaced");
    }

    /// <summary>
    /// A zombie's mere presence must not forever block the one action that exists to end it —
    /// otherwise KillZombiePinUpDisplayAction could never run.
    /// </summary>
    public static void Test_Preflight_ProcessKillTarget_IsNotBlockedByItsOwnPresence()
    {
        var fs = new FakeFs();
        var eng = Engine(fs, KnowledgePack.Empty, new RepairActionRegistry(), out _, out var probe);
        probe.Blocking.Add("PinUpDisplay");

        var plan = ZombieKillPlan();
        var pre = eng.Preflight(plan);
        A.True(pre.Passed, "the zombie itself must not block the very action that kills it");
    }

    /// <summary>The exemption is scoped to the exact process being killed, not to blocking in general.</summary>
    public static void Test_Preflight_UnrelatedBlockingProcess_StillBlocksAlongsideAKillTarget()
    {
        var fs = new FakeFs();
        var eng = Engine(fs, KnowledgePack.Empty, new RepairActionRegistry(), out _, out var probe);
        probe.Blocking.Add("PinUpDisplay");
        probe.Blocking.Add("VPinballX");   // a table really is running

        var plan = ZombieKillPlan();
        var pre = eng.Preflight(plan);
        A.False(pre.Passed, "VPinballX actually running must still block");
        A.True(pre.Blockers.Any(b => b.Code == "VPX_RUNNING"), "blocker still surfaced");
    }

    private static RepairPlan ZombieKillPlan() => new()
    {
        PlanId = "p1", CreatedAtUtc = DateTimeOffset.UtcNow, ScanReportId = "scan-1",
        Items = new[]
        {
            new RepairPlanItem
            {
                ItemId = "i1", TargetCode = "PINUP_DISPLAY_ZOMBIE", Mode = RepairMode.ConfirmationRequired,
                Changes = new[]
                {
                    new PlannedChange
                    {
                        ActionId = "kill_zombie_pinup_display", Kind = ChangeKind.ProcessTermination,
                        Target = @"C:\popper\PinupSystem\PinUpDisplay.exe",
                        Before = "running", After = "terminated", Reversible = false,
                    }
                },
                Selected = true,
            }
        },
    };

    public static void Test_Preflight_RefusesWithoutRoomForTheBackup()
    {
        var (_, eng, probe) = Setup(blocked: @"C:\vpx\a.dll");
        probe.FreeSpace = 1024;
        var plan = Build.Select(eng.Plan("scan-1", new[] { Build.Finding("BLOCKED_DLL", @"C:\vpx\a.dll") }, true));

        var pre = eng.Preflight(plan);
        A.False(pre.Passed, "must refuse");
        A.True(pre.Blockers.Any(b => b.Code == "NO_DISK_SPACE"), "blocker named");
    }

    public static void Test_Preflight_DropsReadOnlyTargets()
    {
        var (_, eng, probe) = Setup(blocked: @"C:\vpx\a.dll");
        probe.ReadOnly.Add(@"C:\vpx\a.dll");
        var plan = eng.Plan("scan-1", new[] { Build.Finding("BLOCKED_DLL", @"C:\vpx\a.dll") }, true);

        A.Equal(0, eng.Preflight(plan).RetainedItems.Count, "read-only target dropped before writing");
    }

    /// <summary>ADR-005 net: even a rule that passed validation cannot leave the install.</summary>
    public static void Test_Preflight_RejectsTargetsOutsideTheInstall()
    {
        var fs = new FakeFs();
        fs.AddFile(@"C:\Windows\System32\evil.dll");
        fs.Blocked.Add(@"C:\Windows\System32\evil.dll");
        var pack = new KnowledgePack("2026.08", new[] { Build.Rule("r", "BLOCKED_DLL", "unblock_file") });
        var eng = Engine(fs, pack, new RepairActionRegistry(new UnblockFileAction(fs)), out var journal, out _);

        var plan = Build.Select(eng.Plan("scan-1",
            new[] { Build.Finding("BLOCKED_DLL", @"C:\Windows\System32\evil.dll") }, true));
        var pre = eng.Preflight(plan);

        A.Equal(0, pre.RetainedItems.Count, "rejected");
        A.True(journal.Has(JournalEvent.RuleRejected), "journalled RuleRejected");
        A.True(fs.HasZoneIdentifier(@"C:\Windows\System32\evil.dll"), "system file untouched");
    }

    /// <summary>
    /// Audit 2026-08-04: the containment check used to be a bare string StartsWith, so a SIBLING
    /// folder that merely shares the install root as a text prefix (no path-separator boundary)
    /// passed as "contained" — e.g. root "C:\vpx" would accept "C:\vpxtra\evil.dll". Locks the fix
    /// (segment-based comparison) in place.
    /// </summary>
    public static void Test_Preflight_RejectsSiblingFolderThatOnlySharesATextPrefix()
    {
        var fs = new FakeFs();
        fs.AddFile(@"C:\vpxtra\evil.dll");
        fs.Blocked.Add(@"C:\vpxtra\evil.dll");
        var pack = new KnowledgePack("2026.08", new[] { Build.Rule("r", "BLOCKED_DLL", "unblock_file") });
        var eng = Engine(fs, pack, new RepairActionRegistry(new UnblockFileAction(fs)), out var journal, out _);

        var plan = Build.Select(eng.Plan("scan-1",
            new[] { Build.Finding("BLOCKED_DLL", @"C:\vpxtra\evil.dll") }, true));
        var pre = eng.Preflight(plan);

        A.Equal(0, pre.RetainedItems.Count, "a sibling folder must not pass containment just because it shares a text prefix");
        A.True(journal.Has(JournalEvent.RuleRejected), "journalled RuleRejected");
    }

    /// <summary>
    /// Audit 2026-08-04: ".." segments were never collapsed before the containment check, so a
    /// traversal-shaped path could pass the string comparison even though the OS would resolve it
    /// outside the install root.
    /// </summary>
    public static void Test_Preflight_RejectsPathTraversalOutOfTheInstallRoot()
    {
        var fs = new FakeFs();
        fs.AddFile(@"C:\vpx\..\Windows\evil.dll");
        fs.Blocked.Add(@"C:\vpx\..\Windows\evil.dll");
        var pack = new KnowledgePack("2026.08", new[] { Build.Rule("r", "BLOCKED_DLL", "unblock_file") });
        var eng = Engine(fs, pack, new RepairActionRegistry(new UnblockFileAction(fs)), out var journal, out _);

        var plan = Build.Select(eng.Plan("scan-1",
            new[] { Build.Finding("BLOCKED_DLL", @"C:\vpx\..\Windows\evil.dll") }, true));
        var pre = eng.Preflight(plan);

        A.Equal(0, pre.RetainedItems.Count, "a '..' that resolves outside the root must not pass containment");
        A.True(journal.Has(JournalEvent.RuleRejected), "journalled RuleRejected");
    }

    /// <summary>
    /// Revue qualité pré-v1.0 (2026-08-04, décision Maxime 2026-08-05) : set_default_audio_device
    /// produces a Target that is a Windows audio endpoint GUID, not a filesystem path. Containment
    /// (IsContained) compares path segments — before this fix, that Target was rejected on every
    /// single run, no matter what install root was configured, so the action could never actually
    /// apply even once wired to a Finding. Locks the fix: ChangeKind.AudioDeviceDefault is exempt
    /// from the path check, same pattern as the ProcessTermination exemption above.
    /// </summary>
    public static void Test_Preflight_AudioDeviceTarget_IsExemptFromPathContainment()
    {
        var fs = new FakeFs();
        var audio = new FakeAudioDeviceControl { DefaultId = "hdmi-1" };
        audio.DevicesByName["Speakers (Realtek)"] = "spk-1";
        var rule = new RepairRule
        {
            Id = "r", TargetCode = "AUDIO_RESET", ActionId = "set_default_audio_device",
            RepairConfidence = 98, Reversible = true,
            Parameters = new Dictionary<string, string> { ["deviceNameContains"] = "Speakers" },
        };
        var pack = new KnowledgePack("2026.08", new[] { rule });
        var eng = Engine(fs, pack, new RepairActionRegistry(new SetDefaultAudioDeviceAction(audio)),
            out var journal, out _);

        var plan = Build.Select(eng.Plan("scan-1",
            new[] { Build.Finding("AUDIO_RESET", @"C:\vpx\irrelevant") }, true));
        var pre = eng.Preflight(plan);

        A.Equal(1, pre.RetainedItems.Count, "a device-id target must not be rejected as 'outside the install'");
        A.False(journal.Has(JournalEvent.RuleRejected), "no containment rejection for this ChangeKind");
    }

    /// <summary>A scan is a snapshot: the world may have moved since.</summary>
    public static void Test_Preflight_DropsStaleFindings()
    {
        var fs = new FakeFs();
        fs.AddFile(@"C:\vpx\a.dll");                       // present but NOT blocked anymore
        var pack = new KnowledgePack("2026.08", new[] { Build.Rule("r", "BLOCKED_DLL", "scripted") });
        var action = new ScriptedAction(fs) { StillAppliesResult = false };
        var eng = Engine(fs, pack, new RepairActionRegistry(action), out var journal, out _);

        var plan = Build.Select(eng.Plan("scan-1", new[] { Build.Finding("BLOCKED_DLL", @"C:\vpx\a.dll") }, true));
        var pre = eng.Preflight(plan);

        A.Equal(0, pre.RetainedItems.Count, "stale item dropped");
        A.True(journal.Has(JournalEvent.StaleDropped), "journalled StaleDropped");
    }

    // ═══════════════ 4. Transactions ═══════════════

    public static void Test_Apply_DoesNothingWithoutExplicitSelection()
    {
        var (fs, eng, _) = Setup(blocked: @"C:\vpx\a.dll");
        var plan = eng.Plan("scan-1", new[] { Build.Finding("BLOCKED_DLL", @"C:\vpx\a.dll") }, true);

        eng.Apply(plan);   // not selected
        A.True(fs.HasZoneIdentifier(@"C:\vpx\a.dll"), "opt-in respected");
    }

    public static void Test_Apply_BacksUpBeforeTheFirstWrite()
    {
        var (fs, eng, _) = Setup(blocked: @"C:\vpx\a.dll");
        var journal = LastJournal!;
        var plan = Build.Select(eng.Plan("scan-1", new[] { Build.Finding("BLOCKED_DLL", @"C:\vpx\a.dll") }, true));
        eng.Apply(plan);

        var events = journal.Entries.Select(e => e.Event).ToList();
        var backupIdx = events.IndexOf(JournalEvent.BackupCreated);
        var applyIdx = events.IndexOf(JournalEvent.ChangeApplied);
        A.True(backupIdx >= 0 && applyIdx > backupIdx, "backup must precede the first write");
        A.False(fs.HasZoneIdentifier(@"C:\vpx\a.dll"), "and the fix did happen");
    }

    /// <summary>
    /// LOT H.2 rule 4 (spec 10/08): "si la sauvegarde échoue, l'action n'est pas appliquée".
    /// Also LOT H.6's required test list: "sauvegarde en échec → aucune écriture".
    /// </summary>
    public static void Test_Apply_BackupFailure_NeverWrites()
    {
        var fs = new FakeFs();
        fs.AddFile(@"C:\vpx\a.dll");
        fs.Blocked.Add(@"C:\vpx\a.dll");
        var pack = new KnowledgePack("2026.08", new[] { Build.Rule("unblock", "BLOCKED_DLL", "unblock_file") });
        var journal = new InMemoryRepairJournal();
        var backup = new FakeBackup { RefuseBackup = true };
        var eng = new RepairEngine(
            new RepairActionRegistry(new UnblockFileAction(fs)), pack, journal, backup,
            new FakeProbe(), new FakeClock(), Build.Roots);

        var plan = Build.Select(eng.Plan("scan-1", new[] { Build.Finding("BLOCKED_DLL", @"C:\vpx\a.dll") }, true));
        var result = eng.Apply(plan);

        A.True(fs.HasZoneIdentifier(@"C:\vpx\a.dll"), "the file must NOT have been unblocked — no write without a backup");
        A.False(result.ItemOutcomes.Values.Any(ok => ok), "no item outcome can report success");
        A.True(journal.Has(JournalEvent.BackupFailed), "the failure must be journalled");
        A.False(journal.Has(JournalEvent.ChangeApplied), "no ChangeApplied can follow a failed backup");
    }

    /// <summary>A playbook that fails at step 3 is rolled back whole, in reverse order.</summary>
    public static void Test_Apply_RollsBackAnOrderedPlaybookInReverse()
    {
        var (fs, eng, journal) = ThreeStepPlaybook(failOn: @"C:\vpx\3");
        var plan = Build.Select(eng.Plan("scan-1", Findings3(), true));

        var res = eng.Apply(plan);

        A.Equal("a", Text(fs, @"C:\vpx\1"), "step 1 rolled back");
        A.Equal("b", Text(fs, @"C:\vpx\2"), "step 2 rolled back");
        A.Sequence(new[] { @"C:\vpx\2", @"C:\vpx\1" },
            journal.Entries.Where(e => e.Event == JournalEvent.ChangeReverted).Select(e => e.Change!.Target),
            "reverse order");
        A.True(journal.Has(JournalEvent.ItemRolledBack), "journalled ItemRolledBack");
        A.False(res.RecoveryRequired, "no manual recovery needed");
    }

    public static void Test_Apply_OneFailingItemDoesNotRollBackIndependentOnes()
    {
        var fs = new FakeFs();
        fs.AddFile(@"C:\vpx\ok", "a");
        fs.AddFile(@"C:\vpx\ko", "b");
        fs.FailWriteOn.Add(@"C:\vpx\ko");

        var pack = new KnowledgePack("2026.08", new[] { Build.Rule("r", "BLOCKED_DLL", "scripted") });
        var eng = Engine(fs, pack, new RepairActionRegistry(new ScriptedAction(fs)), out _, out _);

        var plan = Build.Select(eng.Plan("scan-1", new[]
        {
            Build.Finding("BLOCKED_DLL", @"C:\vpx\ok"),
            Build.Finding("BLOCKED_DLL", @"C:\vpx\ko"),
        }, true));

        var res = eng.Apply(plan);
        A.Equal("fixed", Text(fs, @"C:\vpx\ok"), "independent success preserved");
        A.Equal("b", Text(fs, @"C:\vpx\ko"), "failing item rolled back");
        A.Equal(2, res.ItemOutcomes.Count, "two independent items");
    }

    // ═══════════════ 5. Worst case: the rollback itself fails ═══════════════

    public static void Test_Apply_WhenRollbackFails_StopsAndAsksForRecovery()
    {
        // An ordered playbook: step 2 fails to write, AND undoing step 1 fails too.
        var fs = new FakeFs();
        fs.AddFile(@"C:\vpx\1", "a");
        fs.AddFile(@"C:\vpx\2", "b");
        fs.FailWriteOn.Add(@"C:\vpx\2");
        fs.FailRevertOn.Add(@"C:\vpx\1");

        var pack = new KnowledgePack("2026.08",
            new[] { Build.Rule("s1", "STEP1", "scripted"), Build.Rule("s2", "STEP2", "scripted") },
            new[] { Scenario("PLAYBOOK", new[] { "STEP1", "STEP2" }, ("s1", false), ("s2", false)) });
        var eng = Engine(fs, pack, new RepairActionRegistry(new ScriptedAction(fs)), out var journal, out _);

        var plan = Build.Select(eng.Plan("scan-1", new[]
        {
            Build.Finding("STEP1", @"C:\vpx\1"),
            Build.Finding("STEP2", @"C:\vpx\2"),
        }, true));

        A.Equal(1, plan.Items.Count, "one transactional playbook item");
        A.Equal(2, plan.Items[0].Changes.Count, "two ordered steps");

        var res = eng.Apply(plan);

        A.True(res.RecoveryRequired, "recovery must be surfaced, not swallowed");
        A.Equal(0, journal.Count(JournalEvent.ChangeReverted), "stop compensating rather than make it worse");
        A.True(journal.Has(JournalEvent.RecoveryRequired), "journalled RecoveryRequired");
        A.True(!string.IsNullOrWhiteSpace(res.BackupPath), "backup path handed to the user");
        A.True(journal.Entries.Any(e => e.Event == JournalEvent.RecoveryRequired
                                        && !string.IsNullOrWhiteSpace(e.Detail)),
            "the entry names the files to restore");
    }

    // ═══════════════ 6. Undo ═══════════════

    public static void Test_Undo_RestoresFromTheJournal()
    {
        var (fs, eng, _) = Setup(blocked: @"C:\vpx\a.dll");
        var plan = Build.Select(eng.Plan("scan-1", new[] { Build.Finding("BLOCKED_DLL", @"C:\vpx\a.dll") }, true));
        eng.Apply(plan);
        A.False(fs.HasZoneIdentifier(@"C:\vpx\a.dll"), "applied");

        eng.Undo(plan.PlanId);
        A.True(fs.HasZoneIdentifier(@"C:\vpx\a.dll"), "undone");
    }

    public static void Test_Undo_IsIdempotent()
    {
        var (_, eng, _) = Setup(blocked: @"C:\vpx\a.dll");
        var plan = Build.Select(eng.Plan("scan-1", new[] { Build.Finding("BLOCKED_DLL", @"C:\vpx\a.dll") }, true));
        eng.Apply(plan);
        eng.Undo(plan.PlanId);

        var second = eng.Undo(plan.PlanId);
        A.True(second.Success, "undoing twice is a no-op, not an error");
    }

    public static void Test_Undo_IsRefusedWhileVpxIsRunning()
    {
        var (_, eng, probe) = Setup(blocked: @"C:\vpx\a.dll");
        var plan = Build.Select(eng.Plan("scan-1", new[] { Build.Finding("BLOCKED_DLL", @"C:\vpx\a.dll") }, true));
        eng.Apply(plan);

        probe.Blocking.Add("VPinballX");
        A.False(eng.Undo(plan.PlanId).Success, "same preflight rule applies to undo");
    }

    // ═══════════════ 7. Journal ═══════════════

    public static void Test_Journal_AnonymizesUserPaths()
    {
        A.Equal(@"C:\Users\<user>\Desktop\vpx\a.dll",
            PathAnonymizer.Anonymize(@"C:\Users\Maxime\Desktop\vpx\a.dll"), "windows path");
        A.Equal("/home/<user>/vpx/a.dll",
            PathAnonymizer.Anonymize("/home/maxime/vpx/a.dll"), "unix path");
        A.Equal(@"C:\vpx\a.dll",
            PathAnonymizer.Anonymize(@"C:\vpx\a.dll"), "path without a user folder is untouched");
    }

    public static void Test_Journal_AppliedChangesCarryBeforeAndAfter()
    {
        var (_, eng, _) = Setup(blocked: @"C:\vpx\a.dll");
        var journal = LastJournal!;
        var plan = Build.Select(eng.Plan("scan-1", new[] { Build.Finding("BLOCKED_DLL", @"C:\vpx\a.dll") }, true));
        eng.Apply(plan);

        var applied = journal.Entries.Where(e => e.Event == JournalEvent.ChangeApplied).ToList();
        A.True(applied.Count > 0, "something was applied");
        A.True(applied.All(e => e.Change is not null
                                && !string.IsNullOrEmpty(e.Change.Before)
                                && !string.IsNullOrEmpty(e.Change.After)),
            "before+after are the undo information");
    }

    public static void Test_Journal_OpensOnPlanCreatedAndClosesOnPlanCompleted()
    {
        var (_, eng, _) = Setup(blocked: @"C:\vpx\a.dll");
        var journal = LastJournal!;
        var plan = Build.Select(eng.Plan("scan-1", new[] { Build.Finding("BLOCKED_DLL", @"C:\vpx\a.dll") }, true));
        eng.Apply(plan);

        A.Equal(JournalEvent.PlanCreated, journal.Entries.First().Event, "opens");
        A.Equal(JournalEvent.PlanCompleted, journal.Entries.Last().Event, "closes");
    }

    public static void Test_Journal_ExportIsAnonymized()
    {
        var (_, eng, _) = Setup(blocked: @"C:\Users\Maxime\vpx\a.dll", roots: new[] { @"C:\Users\Maxime\vpx" });
        var journal = LastJournal!;
        var plan = Build.Select(eng.Plan("scan-1",
            new[] { Build.Finding("BLOCKED_DLL", @"C:\Users\Maxime\vpx\a.dll") }, true));
        eng.Apply(plan);

        var export = journal.ExportAnonymized(plan.PlanId);
        A.False(export.Contains("Maxime"), "no user name leaks into a file meant for a forum");
        A.True(export.Contains("<user>"), "placeholder present");
    }

    // ═══════════════ 8. Partial plans (Migration 32→64) ═══════════════

    public static void Test_Scenario_PlaybookIsOneTransactionalItem()
    {
        var (fs, eng, journal) = ThreeStepPlaybook(failOn: null);
        var plan = Build.Select(eng.Plan("scan-1", Findings3(), true));

        A.Equal(1, plan.Items.Count, "a playbook is ONE item, not three");
        A.Equal(3, plan.Items[0].Changes.Count, "with three ordered changes");
        eng.Apply(plan);
        A.Equal("fixed", Text(fs, @"C:\vpx\3"), "all steps applied");
    }

    public static void Test_Scenario_ManualStepMakesThePlanPartialAndSaysWhy()
    {
        var fs = new FakeFs();
        fs.AddFile(@"C:\vpx\1", "a");
        var pack = new KnowledgePack("2026.08",
            new[] { Build.Rule("auto", "CODE_A", "scripted") },
            new[]
            {
                Scenario("MIG", new[] { "CODE_A", "CODE_B" },
                    ("auto", false),
                    ("manual-dmd64", true))
            });
        var eng = Engine(fs, pack, new RepairActionRegistry(new ScriptedAction(fs)), out _, out _);

        var plan = eng.Plan("scan-1", new[]
        {
            Build.Finding("CODE_A", @"C:\vpx\1"),
            Build.Finding("CODE_B", @"C:\vpx\2"),
        }, true);

        var item = plan.Items.First(i => i.TargetCode == "MIG");
        A.Equal(Completeness.Partial, item.Completeness, "partial");
        A.True(item.Missing.Count > 0, "and it says what is missing, before acting");
    }

    /// <summary>The whole point of ADR-004: what we cannot supply stays manual, and says so.</summary>
    public static void Test_CodeWithoutRuleFallsBackToManualOnly()
    {
        var fs = new FakeFs();
        fs.AddFile(@"C:\vpx\dmddevice64.dll");
        var pack = new KnowledgePack("2026.08", Array.Empty<RepairRule>());
        var eng = Engine(fs, pack, new RepairActionRegistry(new UnblockFileAction(fs)), out _, out _);

        var p = eng.Plan("scan-1",
            new[] { Build.Finding("BITNESS_DMD64_MISSING", @"C:\vpx\dmddevice64.dll") }, true);
        A.Equal(RepairMode.ManualOnly, p.Items[0].Mode, "no rule → manual");
    }

    // ═══════════════ 9. Verify ═══════════════

    public static void Test_Verify_ReportsWhetherTheProblemIsGone()
    {
        var (_, eng, _) = Setup(blocked: @"C:\vpx\a.dll");
        var plan = Build.Select(eng.Plan("scan-1", new[] { Build.Finding("BLOCKED_DLL", @"C:\vpx\a.dll") }, true));
        eng.Apply(plan);

        var verdict = eng.Verify(plan);
        A.Equal(1, verdict.Count, "one item verified");
        A.True(verdict.Values.First(), "the block is gone");
    }

    // ═══════════════ helpers ═══════════════

    private static InMemoryRepairJournal? LastJournal;

    private static (FakeFs, IRepairEngine, FakeProbe) Setup(string blocked, string[]? roots = null)
    {
        var fs = new FakeFs();
        fs.AddFile(blocked);
        fs.Blocked.Add(blocked);
        var pack = new KnowledgePack("2026.08", new[] { Build.Rule("unblock", "BLOCKED_DLL", "unblock_file") });
        var eng = Engine(fs, pack, new RepairActionRegistry(new UnblockFileAction(fs)),
                         out var journal, out var probe, roots);
        LastJournal = journal;
        return (fs, eng, probe);
    }

    private static IRepairEngine Engine(FakeFs fs, IKnowledgePack pack, IRepairActionRegistry registry,
                                        out InMemoryRepairJournal journal, out FakeProbe probe,
                                        string[]? roots = null)
    {
        journal = new InMemoryRepairJournal();
        probe = new FakeProbe();
        LastJournal = journal;
        return new RepairEngine(registry, pack, journal, new FakeBackup(), probe, new FakeClock(),
                                roots ?? Build.Roots);
    }

    private static PackScenario Scenario(string id, string[] requires, params (string ruleId, bool manual)[] steps)
        => new()
        {
            Id = id, TitleFr = id, TitleEn = id, Requires = requires, BaseConfidence = 90,
            Playbook = steps.Select((s, i) => new PackStep
            {
                Step = i + 1, RuleId = s.ruleId, ManualOnly = s.manual,
                ReasonFr = s.manual ? "non fournissable" : null,
                ReasonEn = s.manual ? "cannot be supplied" : null,
            }).ToList(),
        };

    private static Finding[] Findings3() => new[]
    {
        Build.Finding("STEP1", @"C:\vpx\1"),
        Build.Finding("STEP2", @"C:\vpx\2"),
        Build.Finding("STEP3", @"C:\vpx\3"),
    };

    private static (FakeFs, IRepairEngine, InMemoryRepairJournal) ThreeStepPlaybook(string? failOn)
    {
        var fs = new FakeFs();
        fs.AddFile(@"C:\vpx\1", "a");
        fs.AddFile(@"C:\vpx\2", "b");
        fs.AddFile(@"C:\vpx\3", "c");
        if (failOn is not null) fs.FailWriteOn.Add(failOn);

        var pack = new KnowledgePack("2026.08",
            new[]
            {
                Build.Rule("s1", "STEP1", "scripted"),
                Build.Rule("s2", "STEP2", "scripted"),
                Build.Rule("s3", "STEP3", "scripted"),
            },
            new[] { Scenario("PLAYBOOK", new[] { "STEP1", "STEP2", "STEP3" },
                             ("s1", false), ("s2", false), ("s3", false)) });

        var eng = Engine(fs, pack, new RepairActionRegistry(new ScriptedAction(fs)),
                         out var journal, out _);
        return (fs, eng, journal);
    }

    private static string Text(FakeFs fs, string path)
        => fs.Files.TryGetValue(path, out var b) ? System.Text.Encoding.UTF8.GetString(b) : "<absent>";
}
