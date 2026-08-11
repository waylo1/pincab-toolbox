using PincabToolbox.Core.Models;
using PincabToolbox.Core.Scanning;

namespace PincabToolbox.Core.Tests;

/// <summary>LOT G (spec 10/08) — <see cref="NvramWritabilityScanner"/>.</summary>
public static class NvramWritabilityScannerTests
{
    // ───────────────────────── Evaluate — pure decision ─────────────────────────

    public static void Test_Evaluate_NotWritable_EmitsWarning()
    {
        var f = NvramWritabilityScanner.Evaluate(false, "/install/VPinMAME/nvram", "nvram-writable");
        Assert.NotNull(f);
        Assert.Equal("NVRAM_FOLDER_NOT_WRITABLE", f!.Code);
        Assert.Equal(Severity.Warning, f.Severity);
    }

    public static void Test_Evaluate_Writable_Silent()
    {
        Assert.Equal(null, NvramWritabilityScanner.Evaluate(true, "/x", "nvram-writable"));
    }

    public static void Test_Evaluate_Undetermined_Silent_NeverAssumedBroken()
    {
        // Folder doesn't exist, or the probe itself couldn't run -> "don't know", not "not writable".
        Assert.Equal(null, NvramWritabilityScanner.Evaluate(null, "/x", "nvram-writable"));
    }

    // ───────────────────────── Scan() with injected probe ─────────────────────────

    public static void Test_Scan_NoVPinMameDir_Silent()
    {
        var layout = new InstallLayout { RootPath = "/install" };
        var ctx = new ScanContext { Layout = layout, Profile = Fixtures.Profile() };
        var scanner = new NvramWritabilityScanner(canWrite: _ => false);
        Assert.Equal(0, scanner.Scan(ctx).Count());
    }

    public static void Test_Scan_ProbeReturnsFalse_EmitsFinding()
    {
        var layout = new InstallLayout { RootPath = "/install", VPinMameDir = "/install/VPinMAME" };
        var ctx = new ScanContext { Layout = layout, Profile = Fixtures.Profile() };
        var scanner = new NvramWritabilityScanner(canWrite: _ => false);
        var findings = scanner.Scan(ctx).ToList();
        Assert.Equal(1, findings.Count(f => f.Code == "NVRAM_FOLDER_NOT_WRITABLE"));
    }

    public static void Test_Scan_ProbeThrows_NeverThrows_Silent()
    {
        var layout = new InstallLayout { RootPath = "/install", VPinMameDir = "/install/VPinMAME" };
        var ctx = new ScanContext { Layout = layout, Profile = Fixtures.Profile() };
        var scanner = new NvramWritabilityScanner(canWrite: _ => throw new UnauthorizedAccessException());
        Assert.Equal(0, scanner.Scan(ctx).Count());
    }

    // ───────────────────────── Real disk (default probe) ─────────────────────────

    public static void Test_DefaultProbe_RealWritableFolder_ReportsWritable_NoFinding()
    {
        var tmp = Path.Combine(Path.GetTempPath(), "pt_nvram_" + Guid.NewGuid().ToString("N"));
        var nvramDir = Path.Combine(tmp, "VPinMAME", "nvram");
        Directory.CreateDirectory(nvramDir);
        try
        {
            var layout = new InstallLayout { RootPath = tmp, VPinMameDir = Path.Combine(tmp, "VPinMAME") };
            var ctx = new ScanContext { Layout = layout, Profile = Fixtures.Profile() };
            var scanner = new NvramWritabilityScanner(); // real disk write test
            var findings = scanner.Scan(ctx).ToList();
            Assert.Equal(0, findings.Count(f => f.Code == "NVRAM_FOLDER_NOT_WRITABLE"));

            // The probe must clean up after itself — no leftover temp file in the folder.
            Assert.Equal(0, Directory.EnumerateFiles(nvramDir).Count());
        }
        finally { Directory.Delete(tmp, true); }
    }

    public static void Test_DefaultProbe_MissingNvramFolder_Undetermined_NoFinding()
    {
        var tmp = Path.Combine(Path.GetTempPath(), "pt_nvram_missing_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tmp);
        try
        {
            var layout = new InstallLayout { RootPath = tmp, VPinMameDir = tmp }; // no "nvram" subfolder created
            var ctx = new ScanContext { Layout = layout, Profile = Fixtures.Profile() };
            var scanner = new NvramWritabilityScanner();
            Assert.Equal(0, scanner.Scan(ctx).Count());
        }
        finally { Directory.Delete(tmp, true); }
    }
}
