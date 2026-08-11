using PincabToolbox.Core.Models;
using PincabToolbox.Core.Scanning;
using PincabToolbox.Core.Services;

namespace PincabToolbox.Core.Tests;

/// <summary>
/// LOT B (spec 10/08) — pure decision tests for <see cref="ChainBitnessScanner.Evaluate"/>, plus a
/// couple of end-to-end <see cref="ChainBitnessScanner.Scan"/> tests. Mirrors the style of
/// <see cref="ComHealthScannerTests"/> (LOT A).
/// </summary>
public static class ChainBitnessScannerTests
{
    private static readonly Bitness[] X64Only = { Bitness.X64 };
    private static readonly Bitness[] X86Only = { Bitness.X86 };
    private static readonly Bitness[] Both = { Bitness.X86, Bitness.X64 };

    private static IReadOnlyDictionary<string, bool> Required(bool b2s = false, bool flexdmd = false) =>
        new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase) { ["b2s"] = b2s, ["flexdmd"] = flexdmd };

    private static IReadOnlySet<(string Role, Bitness Bitness)> PresentAt(params (string, Bitness)[] items) =>
        new HashSet<(string, Bitness)>(items);

    // ───────────────────────── Evaluate — CHAIN_BITNESS_GAP ─────────────────────────

    public static void Test_Required_MissingAtInstalledBitness_EmitsGap()
    {
        var findings = ChainBitnessScanner.Evaluate(
            Required(b2s: true), PresentAt(), X64Only, "chain-bitness");
        Assert.Equal(1, findings.Count(f => f.Code == "CHAIN_BITNESS_GAP"));
        Assert.Equal(Severity.Warning, findings.Single().Severity);
    }

    public static void Test_Required_PresentAtInstalledBitness_Silent()
    {
        var findings = ChainBitnessScanner.Evaluate(
            Required(b2s: true), PresentAt(("b2s", Bitness.X64)), X64Only, "chain-bitness");
        Assert.Equal(0, findings.Count);
    }

    public static void Test_NotRequired_NeverEmits_EvenIfAbsent()
    {
        var findings = ChainBitnessScanner.Evaluate(
            Required(b2s: false), PresentAt(), X64Only, "chain-bitness");
        Assert.Equal(0, findings.Count);
    }

    public static void Test_PresentOnlyAtNonInstalledBitness_StillGapsForInstalledOne()
    {
        // b2s exists as a 32-bit binary, but the only VPX installed is 64-bit — that copy cannot help it.
        var findings = ChainBitnessScanner.Evaluate(
            Required(b2s: true), PresentAt(("b2s", Bitness.X86)), X64Only, "chain-bitness");
        Assert.Equal(1, findings.Count(f => f.Code == "CHAIN_BITNESS_GAP"));
    }

    public static void Test_UnknownBitness_NeverEmitsGap()
    {
        var findings = ChainBitnessScanner.Evaluate(
            Required(b2s: true), PresentAt(), new[] { Bitness.Unknown }, "chain-bitness");
        Assert.Equal(0, findings.Count);
    }

    public static void Test_BothVpxBitnessesInstalled_OnlyOneCovered_GapsOnlyForMissingOne()
    {
        var findings = ChainBitnessScanner.Evaluate(
            Required(flexdmd: true), PresentAt(("flexdmd", Bitness.X64)), Both, "chain-bitness");
        Assert.Equal(1, findings.Count(f => f.Code == "CHAIN_BITNESS_GAP"));
        Assert.Contains("32-bit", findings.Single().EnglishText);
    }

    public static void Test_BothVpxBitnessesInstalled_BothCovered_NoGap()
    {
        var findings = ChainBitnessScanner.Evaluate(
            Required(b2s: true, flexdmd: true),
            PresentAt(("b2s", Bitness.X86), ("b2s", Bitness.X64), ("flexdmd", Bitness.X86), ("flexdmd", Bitness.X64)),
            Both, "chain-bitness");
        Assert.Equal(0, findings.Count);
    }

    public static void Test_BothComponentsMissing_EmitsOneGapEach()
    {
        var findings = ChainBitnessScanner.Evaluate(
            Required(b2s: true, flexdmd: true), PresentAt(), X64Only, "chain-bitness");
        Assert.Equal(2, findings.Count);
        Assert.Equal(1, findings.Count(f => f.Subject == "B2S Backglass Server"));
        Assert.Equal(1, findings.Count(f => f.Subject == "FlexDMD"));
    }

    // ───────────────────────── End-to-end Scan() ─────────────────────────

    public static void Test_Scan_NoVpxExecutables_Silent()
    {
        var layout = new InstallLayout { RootPath = "/install" };
        var ctx = new ScanContext { Layout = layout, Profile = Fixtures.Profile() };
        var scanner = new ChainBitnessScanner();
        var findings = scanner.Scan(ctx).ToList();
        Assert.Equal(0, findings.Count);
    }

    public static void Test_Scan_NoTables_Silent()
    {
        // A measurable VPX exists but no table references b2s/flexdmd -> nothing required -> silent.
        var vpx = Path.Combine(Path.GetTempPath(), "VPinballX64_" + Guid.NewGuid().ToString("N") + ".exe");
        File.WriteAllBytes(vpx, new byte[] { 0x4D, 0x5A }); // not a real PE -> Bitness.Unknown, harmless either way
        try
        {
            var layout = new InstallLayout { RootPath = Path.GetTempPath() };
            layout.VpxExecutables.Add(vpx);
            var ctx = new ScanContext { Layout = layout, Profile = Fixtures.Profile() };
            var scanner = new ChainBitnessScanner();
            var findings = scanner.Scan(ctx).ToList();
            Assert.Equal(0, findings.Count);
        }
        finally { File.Delete(vpx); }
    }
}
