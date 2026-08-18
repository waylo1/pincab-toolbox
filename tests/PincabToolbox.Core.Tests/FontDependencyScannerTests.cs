using PincabToolbox.Core.Models;
using PincabToolbox.Core.Scanning;
using PincabToolbox.Core.Services;
using PincabToolbox.Core.Vpx;

namespace PincabToolbox.Core.Tests;

/// <summary>Pure literal extraction.</summary>
public static class FontReferenceExtractorTests
{
    public static void Test_ExtractsQuotedTtfLiteral()
    {
        var names = FontReferenceExtractor.ExtractTtfFileNames("LoadFont \"scoreboard.ttf\"");
        Assert.Equal(1, names.Count);
        Assert.Equal("scoreboard.ttf", names[0]);
    }

    public static void Test_StripsPathPrefix_KeepsBaseNameOnly()
    {
        var names = FontReferenceExtractor.ExtractTtfFileNames(@"x = ""Fonts\DMDCustom.ttf""");
        Assert.Equal(1, names.Count);
        Assert.Equal("DMDCustom.ttf", names[0]);
    }

    public static void Test_CommentedOutReference_Ignored()
    {
        var names = FontReferenceExtractor.ExtractTtfFileNames("' LoadFont \"scoreboard.ttf\"\nDim x");
        Assert.Equal(0, names.Count);
    }

    public static void Test_NoTtfReference_Empty()
    {
        var names = FontReferenceExtractor.ExtractTtfFileNames("Sub Table1_Init()\nEnd Sub");
        Assert.Equal(0, names.Count);
    }

    public static void Test_DuplicateReferences_Deduplicated()
    {
        var names = FontReferenceExtractor.ExtractTtfFileNames("a = \"x.ttf\"\nb = \"x.ttf\"\nc = \"X.TTF\"");
        Assert.Equal(1, names.Count);
    }

    public static void Test_BareExtensionOnly_Ambiguous_Skipped()
    {
        var names = FontReferenceExtractor.ExtractTtfFileNames("a = \".ttf\"");
        Assert.Equal(0, names.Count);
    }
}

/// <summary>End-to-end scanner behaviour, with the install-wide file search injected.</summary>
public static class FontDependencyScannerTests
{
    private static ScanContext Ctx()
    {
        var layout = new InstallLayout { RootPath = "/x" };
        return new ScanContext { Layout = layout, Profile = Fixtures.Profile() };
    }

    public static void Test_FontFoundUnderInstall_Silent()
    {
        var ctx = Ctx();
        ctx.Tables["Foo.vpx"] = new VpxTableData { FilePath = "Foo.vpx", Script = "x = \"scoreboard.ttf\"" };
        var scanner = new FontDependencyScanner((root, pattern, depth) => new[] { "/x/Fonts/scoreboard.ttf" });
        var findings = scanner.Scan(ctx).ToList();
        Assert.Equal(0, findings.Count);
    }

    public static void Test_FontNotFound_Notes()
    {
        var ctx = Ctx();
        ctx.Tables["Foo.vpx"] = new VpxTableData { FilePath = "Foo.vpx", Script = "x = \"scoreboard.ttf\"" };
        var scanner = new FontDependencyScanner((root, pattern, depth) => Array.Empty<string>());
        var findings = scanner.Scan(ctx).ToList();
        Assert.Equal(1, findings.Count(f => f.Code == "FONT_FILE_MISSING"));
        var f = findings.Single(f => f.Code == "FONT_FILE_MISSING");
        Assert.Equal(Severity.Note, f.Severity);
    }

    public static void Test_NoScriptReferencesFont_Silent()
    {
        var ctx = Ctx();
        ctx.Tables["Foo.vpx"] = new VpxTableData { FilePath = "Foo.vpx", Script = "Sub Table1_Init()\nEnd Sub" };
        var scanner = new FontDependencyScanner((root, pattern, depth) => Array.Empty<string>());
        var findings = scanner.Scan(ctx).ToList();
        Assert.Equal(0, findings.Count);
    }

    public static void Test_NullScript_Silent()
    {
        var ctx = Ctx();
        ctx.Tables["Foo.vpx"] = new VpxTableData { FilePath = "Foo.vpx", Script = null };
        var scanner = new FontDependencyScanner((root, pattern, depth) => Array.Empty<string>());
        var findings = scanner.Scan(ctx).ToList();
        Assert.Equal(0, findings.Count);
    }

    public static void Test_MultipleTablesMissingFonts_OneSummarizedFinding()
    {
        var ctx = Ctx();
        ctx.Tables["A.vpx"] = new VpxTableData { FilePath = "A.vpx", Script = "x = \"a.ttf\"" };
        ctx.Tables["B.vpx"] = new VpxTableData { FilePath = "B.vpx", Script = "x = \"b.ttf\"" };
        var scanner = new FontDependencyScanner((root, pattern, depth) => Array.Empty<string>());
        var findings = scanner.Scan(ctx).ToList();
        Assert.Equal(1, findings.Count); // summarized, not one per table
        Assert.Equal("2", findings[0].Args[0]); // 2 affected tables
        Assert.Equal("2", findings[0].Args[1]); // 2 distinct fonts
    }

    public static void Test_FindFilesThrows_SkipsThatFont_NeverThrows()
    {
        var ctx = Ctx();
        ctx.Tables["Foo.vpx"] = new VpxTableData { FilePath = "Foo.vpx", Script = "x = \"scoreboard.ttf\"" };
        var scanner = new FontDependencyScanner((root, pattern, depth) => throw new UnauthorizedAccessException("locked"));
        var findings = scanner.Scan(ctx).ToList(); // must not throw
        Assert.Equal(0, findings.Count);
    }

    public static void Test_NoRootPath_Silent()
    {
        var ctx = new ScanContext { Layout = new InstallLayout { RootPath = "" }, Profile = Fixtures.Profile() };
        ctx.Tables["Foo.vpx"] = new VpxTableData { FilePath = "Foo.vpx", Script = "x = \"scoreboard.ttf\"" };
        var scanner = new FontDependencyScanner((root, pattern, depth) => Array.Empty<string>());
        var findings = scanner.Scan(ctx).ToList();
        Assert.Equal(0, findings.Count);
    }
}
