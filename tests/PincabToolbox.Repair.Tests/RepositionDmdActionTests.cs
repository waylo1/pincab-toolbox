using PincabToolbox.Core.Models;
using PincabToolbox.Repair.Actions;

namespace PincabToolbox.Repair.Tests;

/// <summary>
/// 20/08 — <see cref="RepositionDmdAction"/> (DMD_POSITION_OFFSCREEN). The rewrite logic itself
/// (<c>DmdDeviceIniParser.RewriteVirtualDmdPosition</c>) is tested in Core.Tests; this file covers
/// only what belongs to the action: Plan()'s fail-closed gates, Execute()/Revert() round-tripping
/// through <see cref="FakeFs"/>, and StillApplies().
/// </summary>
public static class RepositionDmdActionTests
{
    private const string IniPath = @"C:\vpx\VPinMAME\dmddevice.ini";

    private static Finding Finding(string? filePath) => new()
    {
        Code = "DMD_POSITION_OFFSCREEN", Severity = Severity.Warning, Category = "dmd-config",
        Subject = "virtualdmd", FilePath = filePath, EnglishText = "x",
    };

    private static RepairContext Ctx(string? filePath) => new()
    {
        InstallRoots = new[] { @"C:\vpx" }, Finding = Finding(filePath),
    };

    // ───────────────────────── Plan() — fail-closed gates ─────────────────────────

    public static void Test_Plan_NoFilePath_PlansNothing()
    {
        var fs = new FakeFs();
        var changes = new RepositionDmdAction(fs).Plan(Ctx(null), new Dictionary<string, string>());
        A.Equal(0, changes.Count, "no ini path, nothing to plan");
    }

    public static void Test_Plan_FileMissing_PlansNothing()
    {
        var fs = new FakeFs();
        var changes = new RepositionDmdAction(fs).Plan(Ctx(IniPath), new Dictionary<string, string>());
        A.Equal(0, changes.Count, "the ini must actually exist");
    }

    public static void Test_Plan_NoVirtualDmdSection_PlansNothing()
    {
        var fs = new FakeFs();
        fs.AddFile(IniPath, "[pin2dmd]\nenabled = true\n");
        var changes = new RepositionDmdAction(fs).Plan(Ctx(IniPath), new Dictionary<string, string>());
        A.Equal(0, changes.Count, "no [virtualdmd] section — never invent one");
    }

    public static void Test_Plan_AlreadyAtSafeDefault_PlansNothing()
    {
        var fs = new FakeFs();
        fs.AddFile(IniPath, "[virtualdmd]\nleft = 0\ntop = 0\n");
        var changes = new RepositionDmdAction(fs).Plan(Ctx(IniPath), new Dictionary<string, string>());
        A.Equal(0, changes.Count, "already (0,0) — nothing to fix");
    }

    public static void Test_Plan_OffscreenPosition_PlansOneChange()
    {
        var fs = new FakeFs();
        fs.AddFile(IniPath, "[virtualdmd]\nleft = 5000\ntop = 5000\nwidth = 1024\nheight = 256\n");
        var changes = new RepositionDmdAction(fs).Plan(Ctx(IniPath), new Dictionary<string, string>());

        A.Equal(1, changes.Count, "a genuinely offscreen position plans exactly one change");
        A.True(changes[0].Reversible, "unlike register_com_component, this one IS reversible");
        A.Equal(ChangeKind.IniWrite, changes[0].Kind, "distinct ChangeKind for this LOT");
        A.Equal(IniPath, changes[0].Target, "the ini itself, not some derived path");
        A.True(changes[0].Before.Contains("5000"), "Before must carry the exact old position");
        A.True(changes[0].After.Contains("0"), "After describes the safe default");
    }

    // ───────────────────────── StillApplies() ─────────────────────────

    public static void Test_StillApplies_StillOffscreen_ReturnsTrue()
    {
        var fs = new FakeFs();
        fs.AddFile(IniPath, "[virtualdmd]\nleft = 5000\ntop = 5000\n");
        A.True(new RepositionDmdAction(fs).StillApplies(Ctx(IniPath)), "still off every monitor, unchanged since the scan");
    }

    public static void Test_StillApplies_AlreadyFixed_ReturnsFalse()
    {
        var fs = new FakeFs();
        fs.AddFile(IniPath, "[virtualdmd]\nleft = 0\ntop = 0\n");
        A.False(new RepositionDmdAction(fs).StillApplies(Ctx(IniPath)), "someone (or Repair itself) already fixed it since the scan");
    }

    // ───────────────────────── Execute() / Revert() ─────────────────────────

    public static void Test_Execute_WritesZeroZero_KeepsWidthHeight()
    {
        var fs = new FakeFs();
        fs.AddFile(IniPath, "[virtualdmd]\nleft = 5000\ntop = 5000\nwidth = 1024\nheight = 256\n");
        var action = new RepositionDmdAction(fs);
        var change = action.Plan(Ctx(IniPath), new Dictionary<string, string>())[0];

        var result = action.Execute(change);

        A.True(result.Success, "sanity: must actually succeed");
        var written = System.Text.Encoding.UTF8.GetString(fs.ReadAllBytes(IniPath));
        A.True(written.Contains("left = 0"), "left rewritten on disk");
        A.True(written.Contains("top = 0"), "top rewritten on disk");
        A.True(written.Contains("width = 1024"), "width untouched on disk");
    }

    public static void Test_Revert_RestoresTheExactOriginalPosition()
    {
        var fs = new FakeFs();
        fs.AddFile(IniPath, "[virtualdmd]\nleft = 5000\ntop = -300\nwidth = 1024\nheight = 256\n");
        var action = new RepositionDmdAction(fs);
        var change = action.Plan(Ctx(IniPath), new Dictionary<string, string>())[0];
        action.Execute(change);

        var revertResult = action.Revert(change);

        A.True(revertResult.Success, "sanity: revert must succeed");
        var restored = System.Text.Encoding.UTF8.GetString(fs.ReadAllBytes(IniPath));
        A.True(restored.Contains("left = 5000"), "left restored to the exact original value");
        A.True(restored.Contains("top = -300"), "top restored to the exact original (negative) value");
    }

    public static void Test_Execute_FileGoneSincePlanning_FailsCleanly()
    {
        var fs = new FakeFs();
        fs.AddFile(IniPath, "[virtualdmd]\nleft = 5000\ntop = 5000\n");
        var action = new RepositionDmdAction(fs);
        var change = action.Plan(Ctx(IniPath), new Dictionary<string, string>())[0];

        fs.Files.Remove(IniPath);   // the file vanished between Plan and Apply

        var result = action.Execute(change);
        A.False(result.Success, "must fail cleanly, never throw or silently do nothing");
    }
}
