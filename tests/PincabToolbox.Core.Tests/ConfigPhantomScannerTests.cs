using PincabToolbox.Core.Models;
using PincabToolbox.Core.Scanning;

namespace PincabToolbox.Core.Tests;

/// <summary>End-to-end scanner behaviour, with the registry + disk reads injected.</summary>
public static class ConfigPhantomScannerTests
{
    private static ScanContext Ctx(string? vpinmameDir = "/vpm")
    {
        var layout = new InstallLayout { RootPath = "/x", VPinMameDir = vpinmameDir };
        return new ScanContext { Layout = layout, Profile = Fixtures.Profile() };
    }

    public static void Test_BothPresent_Notes()
    {
        var findings = new ConfigPhantomScanner(() => true, _ => true).Scan(Ctx()).ToList();
        Assert.Equal(1, findings.Count(f => f.Code == "VPINMAME_CONFIG_PHANTOM"));
        Assert.Equal(Severity.Note, findings.Single().Severity);
    }

    public static void Test_OnlyIni_Silent()
    {
        var findings = new ConfigPhantomScanner(() => false, _ => true).Scan(Ctx()).ToList();
        Assert.Equal(0, findings.Count);
    }

    public static void Test_OnlyRegistry_Silent()
    {
        var findings = new ConfigPhantomScanner(() => true, _ => false).Scan(Ctx()).ToList();
        Assert.Equal(0, findings.Count);
    }

    public static void Test_Neither_Silent()
    {
        var findings = new ConfigPhantomScanner(() => false, _ => false).Scan(Ctx()).ToList();
        Assert.Equal(0, findings.Count);
    }

    public static void Test_NoVPinMameDir_Silent()
    {
        var findings = new ConfigPhantomScanner(() => true, _ => true).Scan(Ctx(vpinmameDir: null)).ToList();
        Assert.Equal(0, findings.Count);
    }

    public static void Test_FileCheckThrows_Silent()
    {
        var findings = new ConfigPhantomScanner(() => true, _ => throw new IOException()).Scan(Ctx()).ToList();
        Assert.Equal(0, findings.Count);
    }

    public static void Test_RegistryCheckThrows_Silent()
    {
        var findings = new ConfigPhantomScanner(() => throw new InvalidOperationException(), _ => true).Scan(Ctx()).ToList();
        Assert.Equal(0, findings.Count);
    }
}
