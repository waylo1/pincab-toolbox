using PincabToolbox.Core.Models;
using PincabToolbox.Core.Scanning;
using PincabToolbox.Core.Services;
using PincabToolbox.Core.Vpx;

namespace PincabToolbox.Core.Tests;

/// <summary>Pure literal extraction.</summary>
public static class HardcodedPathExtractorTests
{
    public static void Test_ExtractsQuotedAbsolutePath()
    {
        var paths = HardcodedPathExtractor.ExtractAbsolutePaths(
            @"x = ""C:\Users\someone-else\Sounds\click.wav""");
        Assert.Equal(1, paths.Count);
        Assert.Equal(@"C:\Users\someone-else\Sounds\click.wav", paths[0]);
    }

    public static void Test_RelativePath_NotMatched()
    {
        var paths = HardcodedPathExtractor.ExtractAbsolutePaths(@"x = ""Sounds\click.wav""");
        Assert.Equal(0, paths.Count);
    }

    public static void Test_FolderPathWithoutExtension_NotMatched()
    {
        var paths = HardcodedPathExtractor.ExtractAbsolutePaths(@"x = ""C:\Users\someone-else\Sounds""");
        Assert.Equal(0, paths.Count);
    }

    public static void Test_CommentedOutReference_Ignored()
    {
        var paths = HardcodedPathExtractor.ExtractAbsolutePaths(
            "' x = \"C:\\Users\\someone-else\\click.wav\"\nDim y");
        Assert.Equal(0, paths.Count);
    }

    public static void Test_DuplicatePaths_Deduplicated()
    {
        var paths = HardcodedPathExtractor.ExtractAbsolutePaths(
            @"a = ""C:\a\b.wav""" + "\n" + @"b = ""C:\a\b.wav""");
        Assert.Equal(1, paths.Count);
    }

    public static void Test_MultipleDistinctPaths_AllExtracted()
    {
        var paths = HardcodedPathExtractor.ExtractAbsolutePaths(
            @"a = ""C:\a\b.wav""" + "\n" + @"b = ""D:\x\y.png""");
        Assert.Equal(2, paths.Count);
    }
}

/// <summary>End-to-end scanner behaviour, with the file-existence check injected.</summary>
public static class HardcodedPathScannerTests
{
    private static ScanContext Ctx() =>
        new() { Layout = new InstallLayout { RootPath = "/x" }, Profile = Fixtures.Profile() };

    public static void Test_PathExistsOnThisMachine_Silent()
    {
        var ctx = Ctx();
        ctx.Tables["Foo.vpx"] = new VpxTableData { FilePath = "Foo.vpx", Script = @"x = ""C:\a\b.wav""" };
        var scanner = new HardcodedPathScanner(_ => true);
        var findings = scanner.Scan(ctx).ToList();
        Assert.Equal(0, findings.Count);
    }

    public static void Test_PathAbsent_Notes_SummarizedPerTable()
    {
        var ctx = Ctx();
        ctx.Tables["Foo.vpx"] = new VpxTableData
        {
            FilePath = "Foo.vpx",
            Script = @"a = ""C:\a\b.wav""" + "\n" + @"b = ""C:\a\c.wav""",
        };
        var scanner = new HardcodedPathScanner(_ => false);
        var findings = scanner.Scan(ctx).ToList();
        Assert.Equal(1, findings.Count); // one finding for the table, not one per path
        var f = findings[0];
        Assert.Equal("SCRIPT_HARDCODED_PATH", f.Code);
        Assert.Equal(Severity.Note, f.Severity);
        Assert.Equal("2", f.Args[1]); // 2 broken paths
    }

    public static void Test_NoHardcodedPath_Silent()
    {
        var ctx = Ctx();
        ctx.Tables["Foo.vpx"] = new VpxTableData { FilePath = "Foo.vpx", Script = "Sub Table1_Init()\nEnd Sub" };
        var scanner = new HardcodedPathScanner(_ => false);
        var findings = scanner.Scan(ctx).ToList();
        Assert.Equal(0, findings.Count);
    }

    public static void Test_NullScript_Silent()
    {
        var ctx = Ctx();
        ctx.Tables["Foo.vpx"] = new VpxTableData { FilePath = "Foo.vpx", Script = null };
        var scanner = new HardcodedPathScanner(_ => false);
        var findings = scanner.Scan(ctx).ToList();
        Assert.Equal(0, findings.Count);
    }

    public static void Test_MultipleTables_OneFindingEach()
    {
        var ctx = Ctx();
        ctx.Tables["A.vpx"] = new VpxTableData { FilePath = "A.vpx", Script = @"x = ""C:\a\b.wav""" };
        ctx.Tables["B.vpx"] = new VpxTableData { FilePath = "B.vpx", Script = @"x = ""C:\a\c.wav""" };
        var scanner = new HardcodedPathScanner(_ => false);
        var findings = scanner.Scan(ctx).ToList();
        Assert.Equal(2, findings.Count(f => f.Code == "SCRIPT_HARDCODED_PATH"));
    }

    public static void Test_FileExistsThrows_SkipsThatPath_NeverThrows()
    {
        var ctx = Ctx();
        ctx.Tables["Foo.vpx"] = new VpxTableData { FilePath = "Foo.vpx", Script = @"x = ""C:\a\b.wav""" };
        var scanner = new HardcodedPathScanner(_ => throw new IOException("locked"));
        var findings = scanner.Scan(ctx).ToList(); // must not throw
        Assert.Equal(0, findings.Count);
    }
}
