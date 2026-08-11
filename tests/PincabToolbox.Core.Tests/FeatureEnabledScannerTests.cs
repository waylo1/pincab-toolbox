using PincabToolbox.Core.Models;
using PincabToolbox.Core.Scanning;

namespace PincabToolbox.Core.Tests;

/// <summary>
/// LOT D (spec 10/08) — pure decision tests for <see cref="FeatureEnabledScanner.EvaluateRom"/>,
/// plus a couple of end-to-end <see cref="FeatureEnabledScanner.Scan"/> tests.
/// </summary>
public static class FeatureEnabledScannerTests
{
    // ───────────────────────── EvaluateRom — ALTSOUND_PRESENT_NOT_ENABLED ─────────────────────────

    public static void Test_AltsoundPresent_ModeZero_EmitsNote()
    {
        var findings = FeatureEnabledScanner.EvaluateRom("afm_113b", altsoundPresentNonEmpty: true, soundMode: 0,
            altcolorComplete: false, dmdColorize: null, category: "feature-enabled");
        Assert.Equal(1, findings.Count(f => f.Code == "ALTSOUND_PRESENT_NOT_ENABLED"));
        Assert.Equal(Severity.Note, findings.Single(f => f.Code == "ALTSOUND_PRESENT_NOT_ENABLED").Severity);
    }

    public static void Test_AltsoundPresent_ModeNonZero_Silent()
    {
        var findings = FeatureEnabledScanner.EvaluateRom("afm_113b", altsoundPresentNonEmpty: true, soundMode: 1,
            altcolorComplete: false, dmdColorize: null, category: "feature-enabled");
        Assert.Equal(0, findings.Count(f => f.Code == "ALTSOUND_PRESENT_NOT_ENABLED"));
    }

    public static void Test_AltsoundPresent_ModeUnreadable_Silent_NeverAssumedOff()
    {
        var findings = FeatureEnabledScanner.EvaluateRom("afm_113b", altsoundPresentNonEmpty: true, soundMode: null,
            altcolorComplete: false, dmdColorize: null, category: "feature-enabled");
        Assert.Equal(0, findings.Count(f => f.Code == "ALTSOUND_PRESENT_NOT_ENABLED"));
    }

    public static void Test_AltsoundAbsent_ModeZero_Silent()
    {
        // Mode 0 with no pack installed at all is completely normal — nothing to flag.
        var findings = FeatureEnabledScanner.EvaluateRom("afm_113b", altsoundPresentNonEmpty: false, soundMode: 0,
            altcolorComplete: false, dmdColorize: null, category: "feature-enabled");
        Assert.Equal(0, findings.Count(f => f.Code == "ALTSOUND_PRESENT_NOT_ENABLED"));
    }

    // ───────────────────────── EvaluateRom — ALTCOLOR_PRESENT_NOT_ENABLED ─────────────────────────

    public static void Test_AltcolorComplete_ColorizeZero_EmitsNote()
    {
        var findings = FeatureEnabledScanner.EvaluateRom("mm_109c", altsoundPresentNonEmpty: false, soundMode: null,
            altcolorComplete: true, dmdColorize: 0, category: "feature-enabled");
        Assert.Equal(1, findings.Count(f => f.Code == "ALTCOLOR_PRESENT_NOT_ENABLED"));
        Assert.Equal(Severity.Note, findings.Single(f => f.Code == "ALTCOLOR_PRESENT_NOT_ENABLED").Severity);
    }

    public static void Test_AltcolorComplete_ColorizeNonZero_Silent()
    {
        var findings = FeatureEnabledScanner.EvaluateRom("mm_109c", altsoundPresentNonEmpty: false, soundMode: null,
            altcolorComplete: true, dmdColorize: 1, category: "feature-enabled");
        Assert.Equal(0, findings.Count(f => f.Code == "ALTCOLOR_PRESENT_NOT_ENABLED"));
    }

    public static void Test_AltcolorComplete_ColorizeUnreadable_Silent_NeverAssumedOff()
    {
        var findings = FeatureEnabledScanner.EvaluateRom("mm_109c", altsoundPresentNonEmpty: false, soundMode: null,
            altcolorComplete: true, dmdColorize: null, category: "feature-enabled");
        Assert.Equal(0, findings.Count(f => f.Code == "ALTCOLOR_PRESENT_NOT_ENABLED"));
    }

    public static void Test_AltcolorIncomplete_ColorizeZero_Silent()
    {
        // Incomplete set (spec: reuse AltColorInspector.IsComplete) -> AltColorScanner's own
        // ALTCOLOR_INCOMPLETE already owns that story, this scanner stays out of it.
        var findings = FeatureEnabledScanner.EvaluateRom("mm_109c", altsoundPresentNonEmpty: false, soundMode: null,
            altcolorComplete: false, dmdColorize: 0, category: "feature-enabled");
        Assert.Equal(0, findings.Count(f => f.Code == "ALTCOLOR_PRESENT_NOT_ENABLED"));
    }

    public static void Test_BothConditionsAtOnce_EmitsBoth()
    {
        var findings = FeatureEnabledScanner.EvaluateRom("tz_94h", altsoundPresentNonEmpty: true, soundMode: 0,
            altcolorComplete: true, dmdColorize: 0, category: "feature-enabled");
        Assert.Equal(2, findings.Count);
    }

    // ───────────────────────── End-to-end Scan() ─────────────────────────

    private static ScanContext MakeCtx(string rom, string script)
    {
        var layout = new InstallLayout { RootPath = "/install", VPinMameDir = "/install/VPinMAME" };
        var ctx = new ScanContext { Layout = layout, Profile = Fixtures.Profile() };
        ctx.Tables["/install/tables/table.vpx"] = new PincabToolbox.Core.Vpx.VpxTableData { FilePath = "/install/tables/table.vpx", Script = script };
        return ctx;
    }

    public static void Test_Scan_NoVPinMameDir_Silent()
    {
        var layout = new InstallLayout { RootPath = "/install" };
        var ctx = new ScanContext { Layout = layout, Profile = Fixtures.Profile() };
        var scanner = new FeatureEnabledScanner();
        Assert.Equal(0, scanner.Scan(ctx).Count());
    }

    public static void Test_Scan_NoTables_Silent()
    {
        var layout = new InstallLayout { RootPath = "/install", VPinMameDir = "/install/VPinMAME" };
        var ctx = new ScanContext { Layout = layout, Profile = Fixtures.Profile() };
        var scanner = new FeatureEnabledScanner();
        Assert.Equal(0, scanner.Scan(ctx).Count());
    }

    public static void Test_Scan_EndToEnd_AltsoundPresentModeOff_EmitsNote()
    {
        var ctx = MakeCtx("afm_113b", "cGameName = \"afm_113b\"\nSet Controller = CreateObject(\"VPinMAME.Controller\")");
        var scanner = new FeatureEnabledScanner(
            altsoundFolderHasFiles: _ => true,
            listAltcolorExtensions: _ => Array.Empty<string>(),
            getSoundMode: _ => 0,
            getDmdColorize: _ => null);
        var findings = scanner.Scan(ctx).ToList();
        Assert.Equal(1, findings.Count(f => f.Code == "ALTSOUND_PRESENT_NOT_ENABLED"));
    }

    public static void Test_Scan_EndToEnd_NoRomsRequired_Silent()
    {
        var ctx = MakeCtx("afm_113b", "' no controller, no rom usage here");
        var scanner = new FeatureEnabledScanner(
            altsoundFolderHasFiles: _ => true,
            listAltcolorExtensions: _ => new[] { ".vni", ".pal" },
            getSoundMode: _ => 0,
            getDmdColorize: _ => 0);
        Assert.Equal(0, scanner.Scan(ctx).Count());
    }
}
