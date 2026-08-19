using PincabToolbox.Core.Models;
using PincabToolbox.Core.Services;
using PincabToolbox.Repair.Actions;

namespace PincabToolbox.Repair.Tests;

/// <summary>
/// LOT I (spec 10/08), Rule 6 revised 19/08 — covers everything about
/// <see cref="RegisterComComponentAction"/> that does NOT require a real Windows machine: the
/// whitelist, the fail-closed Plan() gates (rules 1/2/4), the zero-argument launch contract (rule 3,
/// via <see cref="FakeProcessLauncher"/>), the adaptive elevation escalation (rule 6, via
/// <see cref="FakeElevatedProcessLauncher"/>) and the never-reversible contract (rule 7).
/// Real launch/UAC behaviour on a real cab is explicitly OUT of scope here — see the class's own
/// header comment for why this action is not wired into a live pack rule yet.
/// </summary>
public static class RegisterComComponentActionTests
{
    /// <summary>Builds the same minimal MZ/PE header shape as tests/fixtures/make_fixtures.py's build_pe.</summary>
    private static void WriteMinimalPe(string path, ushort machine)
    {
        var header = new byte[64];
        header[0] = (byte)'M'; header[1] = (byte)'Z';
        BitConverter.GetBytes(0x40).CopyTo(header, 0x3C);
        var tail = new byte[4 + 2 + 58];
        tail[0] = (byte)'P'; tail[1] = (byte)'E';
        BitConverter.GetBytes(machine).CopyTo(tail, 4);
        File.WriteAllBytes(path, header.Concat(tail).ToArray());
    }

    private static string NewTempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), "pincab-registercom-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }

    private static Finding Finding(string progId, string? dllPath) => new()
    {
        Code = "COM_NOT_REGISTERED", Severity = Severity.Warning, Category = "com",
        Subject = progId, FilePath = dllPath, EnglishText = "x",
    };

    private static RegisterComComponentAction Action(
        FakeProcessLauncher? launcher = null, FakeElevatedProcessLauncher? elevatedLauncher = null,
        Func<string, ComRegistryView, (bool, ComRegistration?)>? probe = null)
        => new(launcher ?? new FakeProcessLauncher(), elevatedLauncher ?? new FakeElevatedProcessLauncher(), probe: probe);

    // ───────────────────────── whitelist (rule 1) ─────────────────────────

    public static void Test_ToolByProgId_MapsExactlyTheThreeWhitelistedComponents()
    {
        A.Equal(3, RegisterComComponentAction.ToolByProgId.Count, "spec §5 LOT I names exactly three tools");
        A.Equal("FlexDMDUI.exe", RegisterComComponentAction.ToolByProgId["FlexDMD.FlexDMD"], "FlexDMD's tool");
        A.Equal("B2SBackglassServerRegisterApp.exe", RegisterComComponentAction.ToolByProgId["B2S.Server"], "B2S's tool");
        A.Equal("Setup.exe", RegisterComComponentAction.ToolByProgId["VPinMAME.Controller"], "VPinMAME's tool");
    }

    // ───────────────────────── Plan() — fail-closed gates ─────────────────────────

    public static void Test_Plan_UnknownProgId_PlansNothing()
    {
        var dir = NewTempDir();
        try
        {
            var dll = Path.Combine(dir, "SomeOther.dll");
            File.WriteAllText(dll, "x");
            var ctx = new RepairContext { InstallRoots = new[] { dir }, Finding = Finding("Some.Other.ProgId", dll) };
            var changes = Action().Plan(ctx, new Dictionary<string, string>());
            A.Equal(0, changes.Count, "a ProgID outside the hardcoded whitelist must never be actioned");
        }
        finally { Directory.Delete(dir, true); }
    }

    public static void Test_Plan_MissingFilePath_PlansNothing()
    {
        var ctx = new RepairContext { InstallRoots = new[] { "/install" }, Finding = Finding("VPinMAME.Controller", null) };
        var changes = Action().Plan(ctx, new Dictionary<string, string>());
        A.Equal(0, changes.Count, "no DLL path means no directory to look for the tool in");
    }

    public static void Test_Plan_ToolNotPresentAlongsideDll_PlansNothing()
    {
        var dir = NewTempDir();
        try
        {
            var dll = Path.Combine(dir, "VPinMAME.dll");
            File.WriteAllText(dll, "x");   // Setup.exe deliberately NOT created
            var ctx = new RepairContext { InstallRoots = new[] { dir }, Finding = Finding("VPinMAME.Controller", dll) };
            var changes = Action().Plan(ctx, new Dictionary<string, string>());
            A.Equal(0, changes.Count, "the tool must actually exist on disk, never assumed present");
        }
        finally { Directory.Delete(dir, true); }
    }

    public static void Test_Plan_ToolPresentButNotAValidPe_PlansNothing()
    {
        var dir = NewTempDir();
        try
        {
            var dll = Path.Combine(dir, "VPinMAME.dll");
            File.WriteAllText(dll, "x");
            File.WriteAllText(Path.Combine(dir, "Setup.exe"), "not actually a PE file");
            var ctx = new RepairContext { InstallRoots = new[] { dir }, Finding = Finding("VPinMAME.Controller", dll) };
            var changes = Action().Plan(ctx, new Dictionary<string, string>());
            A.Equal(0, changes.Count, "rule 4 — must read back as a real PE with a known bitness before ever being launchable");
        }
        finally { Directory.Delete(dir, true); }
    }

    public static void Test_Plan_ValidWhitelistedTool_PlansOneNonReversibleChangeTargetingTheTool()
    {
        var dir = NewTempDir();
        try
        {
            var dll = Path.Combine(dir, "VPinMAME.dll");
            File.WriteAllText(dll, "x");
            var toolPath = Path.Combine(dir, "Setup.exe");
            WriteMinimalPe(toolPath, 0x8664);   // x64
            var ctx = new RepairContext { InstallRoots = new[] { dir }, Finding = Finding("VPinMAME.Controller", dll) };
            var changes = Action().Plan(ctx, new Dictionary<string, string>());

            A.Equal(1, changes.Count, "a whitelisted, present, valid-PE tool plans exactly one change");
            A.False(changes[0].Reversible, "rule 7 — never reversible");
            A.Equal(ChangeKind.ComReregistration, changes[0].Kind, "distinct ChangeKind for this LOT");
            A.True(Path.GetFullPath(changes[0].Target).Equals(Path.GetFullPath(toolPath), StringComparison.OrdinalIgnoreCase),
                "the target must be the resolved tool path, not the DLL");
        }
        finally { Directory.Delete(dir, true); }
    }

    public static void Test_Plan_DllPathWithTraversalSegments_TargetIsStillCanonical()
    {
        var dir = NewTempDir();
        try
        {
            var subDir = Path.Combine(dir, "sub");
            Directory.CreateDirectory(subDir);
            var dll = Path.Combine(dir, "VPinMAME.dll");
            File.WriteAllText(dll, "x");
            var toolPath = Path.Combine(dir, "Setup.exe");
            WriteMinimalPe(toolPath, 0x014C);

            // A DLL path containing a "sub/.." segment must resolve to the SAME canonical directory
            // as the plain path — rule 2's whole point.
            var messyDllPath = Path.Combine(subDir, "..", "VPinMAME.dll");
            var ctx = new RepairContext { InstallRoots = new[] { dir }, Finding = Finding("VPinMAME.Controller", messyDllPath) };
            var changes = Action().Plan(ctx, new Dictionary<string, string>());

            A.Equal(1, changes.Count, "traversal segments must still resolve to the real tool next to the real DLL");
            A.False(changes[0].Target.Contains(".."), "the planned target must be a clean, canonical path");
        }
        finally { Directory.Delete(dir, true); }
    }

    // ───────────────────────── StillApplies() — re-probe, silence on failure ─────────────────────────

    private static readonly ComRegistration SomeRegistration = new()
    {
        ProgId = "VPinMAME.Controller", Clsid = "{GUID}", ServerPath = "/x/VPinMAME.dll", View = ComRegistryView.Registry64,
    };

    public static void Test_StillApplies_UnknownProgId_ReturnsFalse()
    {
        var action = Action(probe: (_, _) => (true, null));
        var ctx = new RepairContext { InstallRoots = Array.Empty<string>(), Finding = Finding("Not.Whitelisted", "/x/y.dll") };
        A.False(action.StillApplies(ctx), "not one of the three whitelisted components");
    }

    public static void Test_StillApplies_EitherProbeFails_ReturnsFalse()
    {
        var action = Action(probe: (progId, view) => view == ComRegistryView.Registry32 ? (false, null) : (true, null));
        var ctx = new RepairContext { InstallRoots = Array.Empty<string>(), Finding = Finding("VPinMAME.Controller", "/x/VPinMAME.dll") };
        A.False(action.StillApplies(ctx), "an unreadable registry view must never be read as \"still broken\"");
    }

    public static void Test_StillApplies_BothViewsSucceedAndBothUnregistered_ReturnsTrue()
    {
        var action = Action(probe: (_, _) => (true, null));
        var ctx = new RepairContext { InstallRoots = Array.Empty<string>(), Finding = Finding("VPinMAME.Controller", "/x/VPinMAME.dll") };
        A.True(action.StillApplies(ctx), "both probes succeeded and found nothing registered — the problem is confirmed still there");
    }

    public static void Test_StillApplies_NowRegistered_ReturnsFalse()
    {
        var action = Action(probe: (_, view) => (true, view == ComRegistryView.Registry64 ? SomeRegistration : null));
        var ctx = new RepairContext { InstallRoots = Array.Empty<string>(), Finding = Finding("VPinMAME.Controller", "/x/VPinMAME.dll") };
        A.False(action.StillApplies(ctx), "the user (or another tool) may have already fixed it since the scan");
    }

    // ───────────────────────── Execute() — adaptive elevation (rule 6, revised 19/08), launch contract (rules 3/5) ─────────────────────────

    private static PlannedChange Change(string target) => new()
    {
        ActionId = "register_com_component", Kind = ChangeKind.ComReregistration,
        Target = target, Before = "b", After = "a", Reversible = false,
    };

    public static void Test_Execute_PlainLaunchSucceeds_NeverAttemptsElevation()
    {
        var launcher = new FakeProcessLauncher { Result = ProcessLaunchResult.Ok(0) };
        var elevatedLauncher = new FakeElevatedProcessLauncher();
        var action = Action(launcher, elevatedLauncher);

        var result = action.Execute(Change("/install/VPinMAME/Setup.exe"));

        A.True(result.Success, "a started, exited-cleanly launch is a success");
        A.Equal(1, launcher.Calls.Count, "exactly one plain launch attempt");
        A.Equal("/install/VPinMAME/Setup.exe", launcher.Calls[0].Path, "the launched path must be exactly the planned target — no substitution");
        A.True(launcher.Calls[0].Timeout > TimeSpan.Zero, "rule 5 — a timeout must always be passed");
        A.Equal(0, elevatedLauncher.Calls.Count, "rule 6 — no UAC prompt when the plain launch already worked");
    }

    public static void Test_Execute_ElevationRequired_EscalatesAndSucceeds()
    {
        var launcher = new FakeProcessLauncher { Result = ProcessLaunchResult.Failed("elevation required") };
        var elevatedLauncher = new FakeElevatedProcessLauncher { Result = ProcessLaunchResult.Ok(0) };
        var action = Action(launcher, elevatedLauncher);

        var result = action.Execute(Change("/install/VPinMAME/Setup.exe"));

        A.True(result.Success, "escalating to the elevated launcher and succeeding is still a success");
        A.Equal(1, elevatedLauncher.Calls.Count, "exactly one elevated retry, only after the plain launch reported elevation required");
        A.Equal("/install/VPinMAME/Setup.exe", elevatedLauncher.Calls[0].Path, "the elevated retry targets the exact same tool — no substitution");
    }

    public static void Test_Execute_ElevationRequired_UserDeclinesPrompt_FailsCalmly()
    {
        var launcher = new FakeProcessLauncher { Result = ProcessLaunchResult.Failed("elevation required") };
        var elevatedLauncher = new FakeElevatedProcessLauncher { Result = ProcessLaunchResult.Failed("elevation cancelled") };
        var action = Action(launcher, elevatedLauncher);

        var result = action.Execute(Change("/install/VPinMAME/Setup.exe"));

        A.False(result.Success, "a declined UAC prompt must not be reported as a success");
        A.True(result.Error!.Contains("nothing was changed", StringComparison.OrdinalIgnoreCase),
            "declining the prompt must read as calm and expected, never as a scary error");
    }

    public static void Test_Execute_LaunchFailsForOtherReason_NeverEscalates()
    {
        var launcher = new FakeProcessLauncher { Result = ProcessLaunchResult.Failed("file not found") };
        var elevatedLauncher = new FakeElevatedProcessLauncher();
        var action = Action(launcher, elevatedLauncher);

        var result = action.Execute(Change("/install/VPinMAME/Setup.exe"));

        A.False(result.Success, "a non-elevation failure must surface as a failure");
        A.Equal("file not found", result.Error, "the launcher's own reason must reach the caller unchanged");
        A.Equal(0, elevatedLauncher.Calls.Count, "only \"elevation required\" ever triggers the UAC escalation — no other failure does");
    }

    public static void Test_Execute_TimedOut_IsStillReportedAsStartedSuccessfully()
    {
        // A timeout means only "this app stopped waiting" (rule 5), not "the tool failed" — several
        // whitelisted tools are interactive GUI installers the user may still be using.
        var launcher = new FakeProcessLauncher { Result = ProcessLaunchResult.TimedOutResult() };
        var action = Action(launcher);

        var result = action.Execute(Change("/install/VPinMAME/Setup.exe"));

        A.True(result.Success, "a tool that is still open (e.g. waiting on the user) is not a failure");
    }

    // ───────────────────────── Revert() and reversibility (rule 7) ─────────────────────────

    public static void Test_IsReversibleByNature_IsAlwaysFalse()
        => A.False(Action().IsReversibleByNature, "rule 7 — this technical truth overrides anything a pack rule could declare");

    public static void Test_Revert_AlwaysFails()
    {
        var result = Action().Revert(Change("/install/VPinMAME/Setup.exe"));
        A.False(result.Success, "there is nothing to revert to — the prior registration state is not restore data");
    }
}
