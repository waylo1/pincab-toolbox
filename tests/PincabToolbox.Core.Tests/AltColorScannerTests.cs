using PincabToolbox.Core.Models;
using PincabToolbox.Core.Scanning;
using PincabToolbox.Core.Services;
using PincabToolbox.Core.Vpx;

namespace PincabToolbox.Core.Tests;

/// <summary>Pure pair-completeness decision.</summary>
public static class AltColorInspectorTests
{
    public static void Test_VniPlusPal_IsComplete()
    {
        Assert.True(AltColorInspector.IsComplete(new[] { ".vni", ".pal" }));
    }

    public static void Test_SerumPlusPal_IsComplete()
    {
        Assert.True(AltColorInspector.IsComplete(new[] { ".crz", ".pal" }));
    }

    public static void Test_AllExtensionsTogether_StillComplete()
    {
        Assert.True(AltColorInspector.IsComplete(new[] { ".vni", ".pal", ".crz", ".txt" }));
    }

    public static void Test_PalOnly_IsIncomplete()
    {
        Assert.False(AltColorInspector.IsComplete(new[] { ".pal" }));
    }

    public static void Test_VniOnly_IsIncomplete()
    {
        Assert.False(AltColorInspector.IsComplete(new[] { ".vni" }));
    }

    public static void Test_SerumOnly_IsIncomplete()
    {
        Assert.False(AltColorInspector.IsComplete(new[] { ".crz" }));
    }

    public static void Test_UnrelatedFilesOnly_IsIncomplete()
    {
        Assert.False(AltColorInspector.IsComplete(new[] { ".txt", ".readme" }));
    }

    public static void Test_Empty_IsIncomplete()
    {
        Assert.False(AltColorInspector.IsComplete(Array.Empty<string>()));
    }
}

/// <summary>End-to-end scanner behaviour, with the folder listing injected.</summary>
public static class AltColorScannerTests
{
    private static ScanContext CtxWithRomTable(string romName, string? vpinmameDir = "/x/VPinMAME")
    {
        var layout = new InstallLayout { RootPath = "/x", VPinMameDir = vpinmameDir };
        var ctx = new ScanContext { Layout = layout, Profile = Fixtures.Profile() };
        ctx.Tables["Foo.vpx"] = new VpxTableData
        {
            FilePath = "Foo.vpx",
            Script = $"Const cGameName = \"{romName}\"\nSet c = CreateObject(\"VPinMAME.Controller\")",
        };
        return ctx;
    }

    public static void Test_NoVPinMameDir_Silent()
    {
        var ctx = CtxWithRomTable("afm_113b", vpinmameDir: null);
        var findings = new AltColorScanner(_ => new[] { ".vni" }).Scan(ctx).ToList();
        Assert.Equal(0, findings.Count);
    }

    public static void Test_RomNotRequired_NeverQueried()
    {
        // No table uses a ROM at all (EM table) — the reader must not even be called.
        var layout = new InstallLayout { RootPath = "/x", VPinMameDir = "/x/VPinMAME" };
        var ctx = new ScanContext { Layout = layout, Profile = Fixtures.Profile() };
        ctx.Tables["Foo.vpx"] = new VpxTableData { FilePath = "Foo.vpx", Script = "Sub Table1_Init()\nEnd Sub" };
        var scanner = new AltColorScanner(_ => throw new InvalidOperationException("must not be called"));
        var findings = scanner.Scan(ctx).ToList(); // must not throw
        Assert.Equal(0, findings.Count);
    }

    public static void Test_RequiredRom_NoColorizationFiles_Silent()
    {
        var ctx = CtxWithRomTable("afm_113b");
        var findings = new AltColorScanner(_ => Array.Empty<string>()).Scan(ctx).ToList();
        Assert.Equal(0, findings.Count);
    }

    public static void Test_RequiredRom_CompletePair_Silent()
    {
        var ctx = CtxWithRomTable("afm_113b");
        var findings = new AltColorScanner(_ => new[] { ".vni", ".pal" }).Scan(ctx).ToList();
        Assert.False(findings.Any(f => f.Code == "ALTCOLOR_INCOMPLETE"));
    }

    public static void Test_RequiredRom_IncompletePair_Warns()
    {
        var ctx = CtxWithRomTable("afm_113b");
        var findings = new AltColorScanner(_ => new[] { ".pal" }).Scan(ctx).ToList();
        Assert.Equal(1, findings.Count(f => f.Code == "ALTCOLOR_INCOMPLETE"));
        var f = findings.Single(f => f.Code == "ALTCOLOR_INCOMPLETE");
        Assert.Equal(Severity.Warning, f.Severity);
        Assert.Equal("afm_113b", f.Subject);
    }

    public static void Test_ListExtensionsThrows_Silent()
    {
        var ctx = CtxWithRomTable("afm_113b");
        var findings = new AltColorScanner(_ => throw new UnauthorizedAccessException()).Scan(ctx).ToList();
        Assert.Equal(0, findings.Count);
    }

    public static void Test_QueriesTheRomSpecificFolder()
    {
        // The reader must be called with a path ending exactly in the required ROM's folder name.
        string? seenPath = null;
        var ctx = CtxWithRomTable("afm_113b");
        new AltColorScanner(p => { seenPath = p; return Array.Empty<string>(); }).Scan(ctx).ToList();
        Assert.NotNull(seenPath);
        Assert.True(seenPath!.Replace('\\', '/').EndsWith("altcolor/afm_113b"));
    }

    public static void Test_MultipleIncompleteRoms_AllReported()
    {
        var layout = new InstallLayout { RootPath = "/x", VPinMameDir = "/x/VPinMAME" };
        var ctx = new ScanContext { Layout = layout, Profile = Fixtures.Profile() };
        ctx.Tables["A.vpx"] = new VpxTableData { FilePath = "A.vpx", Script = "Const cGameName = \"afm_113b\"\nCreateObject(\"VPinMAME.Controller\")" };
        ctx.Tables["B.vpx"] = new VpxTableData { FilePath = "B.vpx", Script = "Const cGameName = \"mm_109c\"\nCreateObject(\"VPinMAME.Controller\")" };
        var findings = new AltColorScanner(_ => new[] { ".pal" }).Scan(ctx).ToList();
        Assert.Equal(2, findings.Count(f => f.Code == "ALTCOLOR_INCOMPLETE"));
    }
}
