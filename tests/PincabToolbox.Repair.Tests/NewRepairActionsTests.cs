using PincabToolbox.Core.Models;
using PincabToolbox.Repair;
using PincabToolbox.Repair.Actions;

namespace PincabToolbox.Repair.Tests;

/// <summary>The three v0.2 Repair backlog actions (FIELD-LOG §2, TRANSMISSION 2026-08-03 chantier).</summary>
public static class NewRepairActionsTests
{
    // ═══════════════ kill_zombie_pinup_display (PINUP_DISPLAY_ZOMBIE) ═══════════════

    public static void Test_KillZombie_PlansNothingWhenNotRunning()
    {
        var proc = new FakeProcessControl();
        var a = new KillZombiePinUpDisplayAction(proc);
        A.Equal(0, a.Plan(Ctx("PINUP_DISPLAY_ZOMBIE", null), Empty).Count, "not running, nothing to do");
    }

    /// <summary>Fail CLOSED: no resolvable path means no target the engine can validate (ADR-005).</summary>
    public static void Test_KillZombie_PlansNothingWhenPathIsUnresolved()
    {
        var proc = new FakeProcessControl();
        proc.Running.Add("PinUpDisplay");
        var a = new KillZombiePinUpDisplayAction(proc);
        A.Equal(0, a.Plan(Ctx("PINUP_DISPLAY_ZOMBIE", null), Empty).Count, "no path, nothing planned");
    }

    public static void Test_KillZombie_PlansAndExecutesWhenRunningWithAResolvedPath()
    {
        var proc = new FakeProcessControl();
        proc.Running.Add("PinUpDisplay");
        var a = new KillZombiePinUpDisplayAction(proc);

        var change = a.Plan(Ctx("PINUP_DISPLAY_ZOMBIE", @"C:\popper\PinupSystem\PinUpDisplay.exe"), Empty).Single();
        A.Equal(@"C:\popper\PinupSystem\PinUpDisplay.exe", change.Target, "target is the resolved exe path");
        A.False(change.Reversible, "killing a process cannot be undone");

        A.True(a.Execute(change).Success, "execute");
        A.False(proc.Running.Contains("PinUpDisplay"), "process actually terminated");
        A.Equal(1, proc.KillCalls.Count, "kill called once");
    }

    public static void Test_KillZombie_RevertAlwaysFailsHonestly()
    {
        var proc = new FakeProcessControl();
        var a = new KillZombiePinUpDisplayAction(proc);
        var change = new PlannedChange
        {
            ActionId = a.ActionId, Kind = a.Kind, Target = @"C:\x\PinUpDisplay.exe",
            Before = "running", After = "terminated", Reversible = false,
        };
        A.False(a.Revert(change).Success, "there is no meaningful undo for a killed process");
    }

    public static void Test_KillZombie_StillApplies_TracksLiveProcessState()
    {
        var proc = new FakeProcessControl();
        var a = new KillZombiePinUpDisplayAction(proc);
        A.False(a.StillApplies(Ctx("PINUP_DISPLAY_ZOMBIE", null)), "not running: stale");
        proc.Running.Add("PinUpDisplay");
        A.True(a.StillApplies(Ctx("PINUP_DISPLAY_ZOMBIE", null)), "running: still applies");
    }

    // ═══════════════ set_default_audio_device (on-demand, not yet wired to a Finding) ═══════════════

    public static void Test_Audio_ValidateParameters_RequiresDeviceNameContains()
    {
        var a = new SetDefaultAudioDeviceAction(new FakeAudioDeviceControl());
        A.False(a.ValidateParameters(Empty).IsValid, "missing parameter must fail validation");
        A.True(a.ValidateParameters(Params(("deviceNameContains", "Speakers"))).IsValid, "present parameter is valid");
    }

    public static void Test_Audio_PlansNothingWhenTheDeviceIsNotPresent()
    {
        var audio = new FakeAudioDeviceControl { DefaultId = "hdmi-1" };
        var a = new SetDefaultAudioDeviceAction(audio);
        var plan = a.Plan(Ctx("X", null), Params(("deviceNameContains", "Speakers")));
        A.Equal(0, plan.Count, "device absent, nothing to do");
    }

    public static void Test_Audio_PlansNothingWhenThePreviousDefaultIsUnknown()
    {
        var audio = new FakeAudioDeviceControl();   // DefaultId left null: "unknown"
        audio.DevicesByName["Speakers (Realtek)"] = "spk-1";
        var a = new SetDefaultAudioDeviceAction(audio);
        var plan = a.Plan(Ctx("X", null), Params(("deviceNameContains", "Speakers")));
        A.Equal(0, plan.Count, "fail closed: no known previous state to promise as reversible");
    }

    public static void Test_Audio_PlansNothingWhenAlreadyDefault()
    {
        var audio = new FakeAudioDeviceControl { DefaultId = "spk-1" };
        audio.DevicesByName["Speakers (Realtek)"] = "spk-1";
        var a = new SetDefaultAudioDeviceAction(audio);
        var plan = a.Plan(Ctx("X", null), Params(("deviceNameContains", "Speakers")));
        A.Equal(0, plan.Count, "already the default, nothing to do");
    }

    public static void Test_Audio_RoundTripsExactly()
    {
        var audio = new FakeAudioDeviceControl { DefaultId = "hdmi-1" };
        audio.DevicesByName["Speakers (Realtek)"] = "spk-1";
        var a = new SetDefaultAudioDeviceAction(audio);

        var change = a.Plan(Ctx("X", null), Params(("deviceNameContains", "Speakers"))).Single();
        A.Equal("hdmi-1", change.Before, "previous default captured");
        A.Equal("spk-1", change.Target, "target device");
        A.True(change.Reversible, "reversible by construction");

        A.True(a.Execute(change).Success, "execute");
        A.Equal("spk-1", audio.DefaultId, "default changed");

        A.True(a.Revert(change).Success, "revert");
        A.Equal("hdmi-1", audio.DefaultId, "previous default restored");
    }

    // ═══════════════ quarantine_orphaned_media (ORPHANED_MEDIA_FILE) ═══════════════

    public static void Test_Media_QuarantinesAFileMatchingNoInstalledTable()
    {
        var fs = MediaFs();
        var a = new QuarantineOrphanedMediaAction(fs);
        var changes = a.Plan(LayoutCtx(fs), Empty);

        var c = changes.Single(x => x.Before.Contains("RemovedTable"));
        // Targets are path-normalised to '/' internally (same convention as FileBackupService).
        A.Equal("C:/popper/PinupSystem/POPMedia/Wheel/_pctb-quarantine/RemovedTable.png", c.Target, "quarantine path");

        A.True(a.Execute(c).Success, "execute");
        A.False(fs.FileExists(@"C:\popper\PinupSystem\POPMedia\Wheel\RemovedTable.png"), "moved out of the way");
        A.True(fs.FileExists(c.Target), "and now sits in quarantine — not deleted");
    }

    public static void Test_Media_NeverFlagsAFileMatchingAnInstalledTable()
    {
        var fs = MediaFs();
        var a = new QuarantineOrphanedMediaAction(fs);
        var changes = a.Plan(LayoutCtx(fs), Empty);
        A.True(changes.All(c => !c.Before.Contains("Medieval Madness")), "installed table's media stays untouched");
    }

    public static void Test_Media_NeverFlagsDefaultNamedFiles()
    {
        var fs = MediaFs();
        var a = new QuarantineOrphanedMediaAction(fs);
        var changes = a.Plan(LayoutCtx(fs), Empty);
        A.True(changes.All(c => !c.Before.Contains("default")), "Popper's own fallback media is never touched");
    }

    /// <summary>
    /// Regression test for the community incident (FIELD-LOG 2026-07-29): a "(SCREENx)" suffixed
    /// file for a table that IS installed must never be quarantined.
    /// </summary>
    public static void Test_Media_NeverFlagsAScreenSuffixedFileOfAnInstalledTable()
    {
        var fs = MediaFs();
        var a = new QuarantineOrphanedMediaAction(fs);
        var changes = a.Plan(LayoutCtx(fs), Empty);
        A.True(changes.All(c => !c.Before.Contains("Medieval Madness (Williams 1997)01(SCREEN3)")),
            "per-screen loading video of an installed table must survive — this is the exact incident it must not repeat");
    }

    public static void Test_Media_RevertMovesTheFileBack()
    {
        var fs = MediaFs();
        var a = new QuarantineOrphanedMediaAction(fs);
        var c = a.Plan(LayoutCtx(fs), Empty).Single(x => x.Before.Contains("RemovedTable"));

        a.Execute(c);
        A.True(a.Revert(c).Success, "revert");
        A.True(fs.FileExists(@"C:\popper\PinupSystem\POPMedia\Wheel\RemovedTable.png"), "file back in place");
        A.False(fs.FileExists(c.Target), "quarantine copy gone");
    }

    public static void Test_Media_PlansNothingWithoutALayout()
    {
        var fs = new FakeFs();
        var a = new QuarantineOrphanedMediaAction(fs);
        var ctx = new RepairContext { InstallRoots = Build.Roots, Finding = Build.Finding("ORPHANED_MEDIA_FILE", null!), Layout = null };
        A.Equal(0, a.Plan(ctx, Empty).Count, "no layout, nothing to recompute from");
    }

    // ═══════════════ helpers ═══════════════

    private static readonly Dictionary<string, string> Empty = new();

    private static Dictionary<string, string> Params(params (string, string)[] kv)
        => kv.ToDictionary(x => x.Item1, x => x.Item2);

    private static RepairContext Ctx(string code, string? filePath) => new()
    {
        InstallRoots = Build.Roots,
        Finding = Build.Finding(code, filePath!),
    };

    private static RepairContext LayoutCtx(FakeFs fs)
    {
        var layout = new InstallLayout
        {
            RootPath = @"C:\popper",
            PopMediaDir = @"C:\popper\PinupSystem\POPMedia",
        };
        layout.VpxTables.Add(@"C:\vpx\Tables\Medieval Madness (Williams 1997).vpx");

        return new RepairContext
        {
            InstallRoots = Build.Roots,
            Finding = Build.Finding("ORPHANED_MEDIA_FILE", @"C:\popper\PinupSystem\POPMedia"),
            Layout = layout,
        };
    }

    /// <summary>A small POPMedia layout: one installed table, one orphan, one default, one screen-suffixed survivor.</summary>
    private static FakeFs MediaFs()
    {
        var fs = new FakeFs();
        fs.AddDir(@"C:\popper\PinupSystem\POPMedia");
        fs.AddDir(@"C:\popper\PinupSystem\POPMedia\Wheel");
        fs.AddFile(@"C:\popper\PinupSystem\POPMedia\Wheel\Medieval Madness (Williams 1997).png");
        fs.AddFile(@"C:\popper\PinupSystem\POPMedia\Wheel\Medieval Madness (Williams 1997)01(SCREEN3).png");
        fs.AddFile(@"C:\popper\PinupSystem\POPMedia\Wheel\default.png");
        fs.AddFile(@"C:\popper\PinupSystem\POPMedia\Wheel\RemovedTable.png");
        return fs;
    }
}
