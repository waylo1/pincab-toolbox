using PincabToolbox.Core.Models;
using PincabToolbox.Repair;
using PincabToolbox.Repair.Actions;

namespace PincabToolbox.Repair.Tests;

/// <summary>
/// The whole chain on the REAL shipped pack: JSON → rules → registry → plan → apply → undo.
/// If this passes, data and code agree.
/// </summary>
public static class EndToEndTests
{
    private static string PackPath()
    {
        var dir = AppContext.BaseDirectory;
        for (var i = 0; i < 8 && dir is not null; i++)
        {
            var p = Path.Combine(dir, "knowledge", "pack-2026.08.json");
            if (File.Exists(p)) return p;
            dir = Path.GetDirectoryName(dir.TrimEnd(Path.DirectorySeparatorChar));
        }
        throw new FileNotFoundException("knowledge/pack-2026.08.json not found from " + AppContext.BaseDirectory);
    }

    private static (FakeFs, IRepairEngine, InMemoryRepairJournal) Real()
    {
        var warnings = new List<string>();
        var pack = KnowledgePack.Load(File.ReadAllText(PackPath()), warnings);
        if (warnings.Count > 0) throw new Exception("shipped pack has warnings: " + string.Join("; ", warnings));

        var fs = new FakeFs();
        var registry = new RepairActionRegistry(new UnblockFileAction(fs), new RestoreRomArchiveAction(fs));
        var journal = new InMemoryRepairJournal();
        var eng = new RepairEngine(registry, pack, journal, new FakeBackup(), new FakeProbe(),
                                   new FakeClock(), Build.Roots);
        return (fs, eng, journal);
    }

    public static void Test_ShippedPack_LoadsWithoutWarnings()
    {
        var (_, _, _) = Real();   // throws if the shipped pack is not clean
    }

    public static void Test_ShippedPack_EveryActionIdExistsInTheRegistry()
    {
        var pack = KnowledgePack.Load(File.ReadAllText(PackPath()));
        var fs = new FakeFs();
        var registry = new RepairActionRegistry(new UnblockFileAction(fs), new RestoreRomArchiveAction(fs));

        foreach (var code in new[] { "BLOCKED_DLL", "ROM_UNZIPPED" })
        {
            var rule = pack.RuleFor(code);
            A.True(rule is not null, $"{code} must have a rule");
            A.True(registry.TryGet(rule!.ActionId, out _), $"{code}: action {rule.ActionId} must be compiled in");
        }
    }

    /// <summary>Knowledge.cs marks it AutoFixable, but no action ships in v1 — see ADR-007.</summary>
    public static void Test_ShippedPack_PopperRegistrationIsManualInV1()
    {
        var pack = KnowledgePack.Load(File.ReadAllText(PackPath()));
        A.True(pack.RuleFor("POPPER_NOT_REGISTERED") is null,
            "no automatic rule ships for the Popper database in v1");
    }

    public static void Test_EndToEnd_BlockedDllIsPlannedAppliedAndUndone()
    {
        var (fs, eng, journal) = Real();
        fs.AddFile(@"C:\vpx\VPinMAME\VPinMAME.dll", "BINARY");
        fs.Blocked.Add(@"C:\vpx\VPinMAME\VPinMAME.dll");

        var plan = eng.Plan("scan-1",
            new[] { Build.Finding("BLOCKED_DLL", @"C:\vpx\VPinMAME\VPinMAME.dll", "security") }, licensed: true);

        A.Equal(RepairMode.Automatic, plan.Items[0].Mode, "confidence 98 + reversible → automatic");

        var res = eng.Apply(Build.Select(plan));
        A.False(fs.HasZoneIdentifier(@"C:\vpx\VPinMAME\VPinMAME.dll"), "unblocked");
        A.Equal("BINARY", System.Text.Encoding.UTF8.GetString(fs.ReadAllBytes(@"C:\vpx\VPinMAME\VPinMAME.dll")),
            "content untouched");
        A.False(res.RecoveryRequired, "clean run");

        eng.Undo(plan.PlanId);
        A.True(fs.HasZoneIdentifier(@"C:\vpx\VPinMAME\VPinMAME.dll"), "fully reversible");
    }

    public static void Test_EndToEnd_UnzippedRomNeedsConfirmationNotAutomatic()
    {
        var (fs, eng, _) = Real();
        fs.AddDir(@"C:\vpx\roms");
        fs.AddDir(@"C:\vpx\roms\mm_109c");
        fs.AddFile(@"C:\vpx\roms\mm_109c\u1.bin", "D1");

        var plan = eng.Plan("scan-1",
            new[] { Build.Finding("ROM_UNZIPPED", @"C:\vpx\roms\mm_109c", "rom") }, licensed: true);

        A.Equal(RepairMode.ConfirmationRequired, plan.Items[0].Mode,
            "confidence 88 stays below the automatic threshold");

        eng.Apply(Build.Select(plan));
        A.True(fs.FileExists(@"C:\vpx\roms\mm_109c.zip"), "archive restored");
    }

    /// <summary>The migration playbook cannot be fully automated — and must say so up front.</summary>
    public static void Test_EndToEnd_MigrationScenarioIsPartialAndHonest()
    {
        var (fs, eng, _) = Real();
        fs.AddFile(@"C:\vpx\VPinMAME\VPinMAME.dll");
        fs.Blocked.Add(@"C:\vpx\VPinMAME\VPinMAME.dll");
        fs.AddFile(@"C:\vpx\VPinMAME\VPinMAME32.dll");
        fs.AddFile(@"C:\vpx\VPinMAME\dmddevice.dll");

        var plan = eng.Plan("scan-1", new[]
        {
            Build.Finding("BLOCKED_DLL", @"C:\vpx\VPinMAME\VPinMAME.dll", "security"),
            Build.Finding("BITNESS_MISMATCH_VPM", @"C:\vpx\VPinMAME\VPinMAME32.dll"),
            Build.Finding("BITNESS_DMD64_MISSING", @"C:\vpx\VPinMAME\dmddevice.dll"),
        }, licensed: true);

        var scenario = plan.Items.FirstOrDefault(i => i.TargetCode == "MIGRATION_32_TO_64_INCOMPLETE");
        A.True(scenario is not null, "the scenario fires when both bitness codes are present");
        A.Equal(Completeness.Partial, scenario!.Completeness, "it is partial");
        A.Equal(2, scenario.Missing.Count, "two steps cannot be automated");
        A.True(scenario.Missing.All(m => !string.IsNullOrWhiteSpace(m.MessageEn)), "and each says why");

        // The two bitness findings have no rule of their own → manual, never silently ignored.
        foreach (var code in new[] { "BITNESS_MISMATCH_VPM", "BITNESS_DMD64_MISSING" })
            A.Equal(RepairMode.ManualOnly, plan.Items.First(i => i.TargetCode == code).Mode, code);
    }
}
