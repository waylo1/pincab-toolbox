using PincabToolbox.Core.Models;
using PincabToolbox.Core.Scanning;
using PincabToolbox.Core.Services;

namespace PincabToolbox.Core.Tests;

/// <summary>
/// LOT A.4 (spec 10/08) — pure decision tests for <see cref="ComHealthScanner.EvaluateComponent"/>
/// and <see cref="ComHealthScanner.EvaluateVpinmameNotRegistered"/>. Per the spec's own
/// instruction: "Écrire le test unitaire de ce cas [sonde en échec] en premier, avant
/// l'implémentation" — kept first in this file even though the implementation already exists,
/// so it stays the first thing a reader (or a future editor) sees.
/// </summary>
public static class ComHealthScannerTests
{
    private const string ProgId = "VPinMAME.Controller";
    private static readonly Bitness[] NoBitness = Array.Empty<Bitness>();
    private static readonly Bitness[] X64Only = { Bitness.X64 };
    private static readonly Bitness[] X86Only = { Bitness.X86 };

    private static ComRegistration Reg(ComRegistryView view, string path = "/install/VPinMAME/VPinMAME.dll")
        => new() { ProgId = ProgId, Clsid = "{GUID}", ServerPath = path, View = view };

    // ───────────────────────── VPINMAME_NOT_REGISTERED (A.3) — the Critical ─────────────────────────

    public static void Test_VpinmameNotRegistered_ProbeFailed_NeverEmitsCritical()
    {
        // THE test that protects against the costliest false positive: a registry read failure
        // must NEVER degrade into a Critical "not registered" claim, no matter what the other
        // three conditions look like.
        var f = ComHealthScanner.EvaluateVpinmameNotRegistered(
            view32: null, view64: null,
            probeSucceeded: false,
            binaryPresentUnderRoot: true,
            binaryPathUnderRoot: "/install/VPinMAME/VPinMAME.dll",
            requiredByATable: true,
            installedVpxBitnesses: X64Only,
            category: "com");
        Assert.Equal(null, f);
    }

    public static void Test_VpinmameNotRegistered_AllFourConditionsTrue_EmitsCritical()
    {
        var f = ComHealthScanner.EvaluateVpinmameNotRegistered(
            view32: null, view64: null,
            probeSucceeded: true,
            binaryPresentUnderRoot: true,
            binaryPathUnderRoot: "/install/VPinMAME/VPinMAME.dll",
            requiredByATable: true,
            installedVpxBitnesses: X64Only,
            category: "com");
        Assert.NotNull(f);
        Assert.Equal("VPINMAME_NOT_REGISTERED", f!.Code);
        Assert.Equal(Severity.Critical, f.Severity);
    }

    public static void Test_VpinmameNotRegistered_BinaryAbsent_NoFinding()
    {
        var f = ComHealthScanner.EvaluateVpinmameNotRegistered(
            null, null, probeSucceeded: true, binaryPresentUnderRoot: false, binaryPathUnderRoot: null,
            requiredByATable: true, installedVpxBitnesses: X64Only, category: "com");
        Assert.Equal(null, f);
    }

    public static void Test_VpinmameNotRegistered_RegisteredInOneView_NoFinding()
    {
        var f = ComHealthScanner.EvaluateVpinmameNotRegistered(
            Reg(ComRegistryView.Registry32), null, probeSucceeded: true, binaryPresentUnderRoot: true,
            binaryPathUnderRoot: "/x", requiredByATable: true, installedVpxBitnesses: X64Only, category: "com");
        Assert.Equal(null, f);
    }

    public static void Test_VpinmameNotRegistered_NotRequiredByAnyTable_NoFinding()
    {
        var f = ComHealthScanner.EvaluateVpinmameNotRegistered(
            null, null, probeSucceeded: true, binaryPresentUnderRoot: true, binaryPathUnderRoot: "/x",
            requiredByATable: false, installedVpxBitnesses: X64Only, category: "com");
        Assert.Equal(null, f);
    }

    // ───────────────────────── EvaluateComponent — COM_NOT_REGISTERED ─────────────────────────

    public static void Test_Component_NotRegistered_Required_Present_ProbeOk_Warns()
    {
        var findings = ComHealthScanner.EvaluateComponent(
            ProgId, null, true, null, true,
            binaryPresentUnderRoot: true, rootPath: "/install",
            requiredByATable: true, installedVpxBitnesses: NoBitness,
            category: "com", severityCap: Severity.Warning,
            binaryPath: "/install/VPinMAME/VPinMAME.dll");
        Assert.Equal(1, findings.Count(f => f.Code == "COM_NOT_REGISTERED"));
        Assert.Equal(Severity.Warning, findings.Single(f => f.Code == "COM_NOT_REGISTERED").Severity);
        // 20/08 — Repair (RegisterComComponentAction) needs this to derive the registration tool's
        // directory; without it the fix could never wire up even once the pack rule exists.
        Assert.Equal("/install/VPinMAME/VPinMAME.dll", findings.Single(f => f.Code == "COM_NOT_REGISTERED").FilePath,
            "FilePath must carry the component's own binary path through to Repair");
    }

    public static void Test_Component_NotRegistered_ButNotRequired_Silent()
    {
        var findings = ComHealthScanner.EvaluateComponent(
            ProgId, null, true, null, true,
            binaryPresentUnderRoot: true, rootPath: "/install",
            requiredByATable: false, installedVpxBitnesses: NoBitness,
            category: "com", severityCap: Severity.Warning);
        Assert.Equal(0, findings.Count);
    }

    public static void Test_Component_NotRegistered_ProbeFailed_Silent()
    {
        var findings = ComHealthScanner.EvaluateComponent(
            ProgId, null, false, null, true,   // 32-bit probe failed
            binaryPresentUnderRoot: true, rootPath: "/install",
            requiredByATable: true, installedVpxBitnesses: NoBitness,
            category: "com", severityCap: Severity.Warning);
        Assert.Equal(0, findings.Count(f => f.Code == "COM_NOT_REGISTERED"));
    }

    // ───────────────────────── EvaluateComponent — COM_STALE_PATH ─────────────────────────

    public static void Test_Component_RegisteredToMissingFile_StalePath()
    {
        var findings = ComHealthScanner.EvaluateComponent(
            ProgId, Reg(ComRegistryView.Registry64, "/gone/VPinMAME.dll"), true, null, true,
            binaryPresentUnderRoot: false, rootPath: "/install",
            requiredByATable: false, installedVpxBitnesses: NoBitness,
            category: "com", severityCap: Severity.Warning,
            fileExists: _ => false);
        Assert.Equal(1, findings.Count(f => f.Code == "COM_STALE_PATH"));
        Assert.Equal(Severity.Warning, findings.Single(f => f.Code == "COM_STALE_PATH").Severity);
    }

    // ───────────────────────── EvaluateComponent — COM_PATH_OUTSIDE_INSTALL ─────────────────────────

    public static void Test_Component_RegisteredOutsideRoot_WithLocalCopy_Note()
    {
        var findings = ComHealthScanner.EvaluateComponent(
            ProgId, Reg(ComRegistryView.Registry64, "/other-install/VPinMAME.dll"), true, null, true,
            binaryPresentUnderRoot: true, rootPath: "/install",
            requiredByATable: false, installedVpxBitnesses: NoBitness,
            category: "com", severityCap: Severity.Warning,
            fileExists: _ => true);
        Assert.Equal(1, findings.Count(f => f.Code == "COM_PATH_OUTSIDE_INSTALL"));
        Assert.Equal(Severity.Note, findings.Single(f => f.Code == "COM_PATH_OUTSIDE_INSTALL").Severity);
    }

    public static void Test_Component_RegisteredOutsideRoot_WithoutLocalCopy_Silent()
    {
        // Legitimate multi-install elsewhere on the machine — nothing local to compare against.
        var findings = ComHealthScanner.EvaluateComponent(
            ProgId, Reg(ComRegistryView.Registry64, "/other-install/VPinMAME.dll"), true, null, true,
            binaryPresentUnderRoot: false, rootPath: "/install",
            requiredByATable: false, installedVpxBitnesses: NoBitness,
            category: "com", severityCap: Severity.Warning,
            fileExists: _ => true);
        Assert.Equal(0, findings.Count);
    }

    // ───────────────────────── EvaluateComponent — bare .NET COM host (mscoree.dll) ─────────────────────────
    // 2026-08 field report (crrispy): B2S.Server and FlexDMD.FlexDMD are registered the standard
    // .NET way, InprocServer32's default is the bare filename "mscoree.dll" with no directory —
    // the generic CLR activation host, not a per-component binary. Before this fix, fileExists()
    // was asked about that bare name as-is, which only ever resolves relative to the app's own
    // working directory, so it always came back false and every healthy install got a false
    // COM_STALE_PATH ("no longer exists ... will fail"). These three tests protect the fix.

    public static void Test_Component_BareMscoreeHost_ResolvesAndExists_SilentNotStaleOrOutside()
    {
        var findings = ComHealthScanner.EvaluateComponent(
            "B2S.Server", Reg(ComRegistryView.Registry64, "mscoree.dll"), true, null, true,
            binaryPresentUnderRoot: true, rootPath: "/install",
            requiredByATable: false, installedVpxBitnesses: NoBitness,
            category: "com", severityCap: Severity.Warning,
            // Whatever directory it got resolved against (System32/SysWOW64, or left bare if
            // %windir% is unavailable in this environment), the filename itself is unmistakable.
            fileExists: p => p.EndsWith("mscoree.dll", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(0, findings.Count(f => f.Code == "COM_STALE_PATH"));
        Assert.Equal(0, findings.Count(f => f.Code == "COM_PATH_OUTSIDE_INSTALL"));
        Assert.Equal(0, findings.Count(f => f.Code == "COM_OK"));
    }

    public static void Test_Component_BareMscoreeHost_TrulyMissing_StillStalePath()
    {
        // A machine genuinely missing mscoree.dll (broken .NET Framework) must still be reported —
        // the fix only silences the false negative, it must never hide a real one.
        var findings = ComHealthScanner.EvaluateComponent(
            "B2S.Server", Reg(ComRegistryView.Registry64, "mscoree.dll"), true, null, true,
            binaryPresentUnderRoot: true, rootPath: "/install",
            requiredByATable: false, installedVpxBitnesses: NoBitness,
            category: "com", severityCap: Severity.Warning,
            fileExists: _ => false);
        Assert.Equal(1, findings.Count(f => f.Code == "COM_STALE_PATH"));
    }

    public static void Test_Component_FullPathMscoreeDll_NotSpecialCased()
    {
        // A ServerPath that already carries a directory (even one literally ending in
        // "mscoree.dll") is a different, already-correct case — untouched by this fix.
        var findings = ComHealthScanner.EvaluateComponent(
            "B2S.Server", Reg(ComRegistryView.Registry64, @"C:\Windows\System32\mscoree.dll"), true, null, true,
            binaryPresentUnderRoot: true, rootPath: "/install",
            requiredByATable: false, installedVpxBitnesses: NoBitness,
            category: "com", severityCap: Severity.Warning,
            fileExists: _ => true);
        Assert.Equal(1, findings.Count(f => f.Code == "COM_PATH_OUTSIDE_INSTALL"));
    }

    // ───────────────────────── EvaluateComponent — COM_OK ─────────────────────────

    public static void Test_Component_RegisteredInsideRoot_ExistingPath_Ok()
    {
        var findings = ComHealthScanner.EvaluateComponent(
            ProgId, Reg(ComRegistryView.Registry64, "/install/VPinMAME/VPinMAME.dll"), true, null, true,
            binaryPresentUnderRoot: true, rootPath: "/install",
            requiredByATable: false, installedVpxBitnesses: NoBitness,
            category: "com", severityCap: Severity.Warning,
            fileExists: _ => true);
        Assert.Equal(1, findings.Count(f => f.Code == "COM_OK"));
        Assert.Equal(Severity.Ok, findings.Single(f => f.Code == "COM_OK").Severity);
    }

    // ───────────────────────── EvaluateComponent — COM_BITNESS_GAP ─────────────────────────

    public static void Test_Component_X64VpxInstalled_RegisteredOnly32_BitnessGap()
    {
        var findings = ComHealthScanner.EvaluateComponent(
            ProgId, Reg(ComRegistryView.Registry32), true, null, true,
            binaryPresentUnderRoot: false, rootPath: "/install",
            requiredByATable: false, installedVpxBitnesses: X64Only,
            category: "com", severityCap: Severity.Warning,
            fileExists: _ => true,
            binaryPath: "/install/VPinMAME/VPinMAME64.dll");
        Assert.Equal(1, findings.Count(f => f.Code == "COM_BITNESS_GAP"));
        Assert.Equal(Severity.Warning, findings.Single(f => f.Code == "COM_BITNESS_GAP").Severity);
        Assert.Equal("/install/VPinMAME/VPinMAME64.dll", findings.Single(f => f.Code == "COM_BITNESS_GAP").FilePath,
            "same reason as COM_NOT_REGISTERED — Repair needs a real FilePath to derive the tool's directory");
    }

    public static void Test_Component_UnknownBitnessVpx_NeverBitnessGap()
    {
        // Bitness.Unknown must never be treated as a confirmed 32 or 64-bit install.
        var findings = ComHealthScanner.EvaluateComponent(
            ProgId, Reg(ComRegistryView.Registry32), true, null, true,
            binaryPresentUnderRoot: false, rootPath: "/install",
            requiredByATable: false, installedVpxBitnesses: new[] { Bitness.Unknown },
            category: "com", severityCap: Severity.Warning,
            fileExists: _ => true);
        Assert.Equal(0, findings.Count(f => f.Code == "COM_BITNESS_GAP"));
    }

    public static void Test_Component_X64VpxInstalled_RegisteredBothViews_NoBitnessGap()
    {
        var findings = ComHealthScanner.EvaluateComponent(
            ProgId, Reg(ComRegistryView.Registry32), true, Reg(ComRegistryView.Registry64), true,
            binaryPresentUnderRoot: true, rootPath: "/install",
            requiredByATable: false, installedVpxBitnesses: X64Only,
            category: "com", severityCap: Severity.Warning,
            fileExists: _ => true);
        Assert.Equal(0, findings.Count(f => f.Code == "COM_BITNESS_GAP"));
    }

    // ───────────────────────── PinUpPlayer.PinDisplay — severity cap ─────────────────────────

    public static void Test_Component_SeverityCappedToNote_EvenWhenFormulaSaysWarning()
    {
        // COM_STALE_PATH would normally be Warning — capped to Note for the single-source ProgID.
        var findings = ComHealthScanner.EvaluateComponent(
            "PinUpPlayer.PinDisplay", Reg(ComRegistryView.Registry64, "/gone.dll"), true, null, true,
            binaryPresentUnderRoot: false, rootPath: "/install",
            requiredByATable: false, installedVpxBitnesses: NoBitness,
            category: "com", severityCap: Severity.Note,
            fileExists: _ => false);
        Assert.Equal(1, findings.Count(f => f.Code == "COM_STALE_PATH"));
        Assert.Equal(Severity.Note, findings.Single(f => f.Code == "COM_STALE_PATH").Severity);
    }

    public static void Test_Component_OkSeverity_NeverRaisedByCap()
    {
        // The cap only ever lowers severity — COM_OK (already below Note) must stay Ok.
        var findings = ComHealthScanner.EvaluateComponent(
            "PinUpPlayer.PinDisplay", Reg(ComRegistryView.Registry64, "/install/x.dll"), true, null, true,
            binaryPresentUnderRoot: true, rootPath: "/install",
            requiredByATable: false, installedVpxBitnesses: NoBitness,
            category: "com", severityCap: Severity.Note,
            fileExists: _ => true);
        Assert.Equal(Severity.Ok, findings.Single(f => f.Code == "COM_OK").Severity);
    }

    // ───────────────────────── End-to-end Scan() ─────────────────────────

    public static void Test_Scan_UnreadableProbe_NeverThrows_NoFindings()
    {
        var layout = new InstallLayout { RootPath = "/install" };
        var ctx = new ScanContext { Layout = layout, Profile = Fixtures.Profile() };
        var scanner = new ComHealthScanner(probe: (_, _) => throw new InvalidOperationException("registry unavailable"));
        var findings = scanner.Scan(ctx).ToList();
        Assert.Equal(0, findings.Count);
    }

    public static void Test_Scan_NoTablesNoBinaries_Silent()
    {
        var layout = new InstallLayout { RootPath = "/install" };
        var ctx = new ScanContext { Layout = layout, Profile = Fixtures.Profile() };
        var scanner = new ComHealthScanner(probe: (_, _) => (true, null));
        var findings = scanner.Scan(ctx).ToList();
        Assert.Equal(0, findings.Count);
    }
}
