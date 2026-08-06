using PincabToolbox.Core.Models;
using PincabToolbox.Core.Scanning;
using PincabToolbox.Core.Services;
using PincabToolbox.Core.Vpx;

namespace PincabToolbox.Core.Tests;

/// <summary>Pure CSV parsing / sample extraction.</summary>
public static class AltSoundManifestLinterTests
{
    private const string ValidCsv =
        "\"ID\",\"CHANNEL\",\"DUCK\",\"GAIN\",\"LOOP\",\"STOP\",\"NAME\",\"FNAME\"\n" +
        "\"1\",\"1\",\"0\",\"1\",\"0\",\"0\",\"Boom\",\"boom1.wav\"\n" +
        "\"2\",\"1\",\"0\",\"1\",\"0\",\"0\",\"Bell\",\"bell1.ogg\"\n";

    public static void Test_ValidCsv_ExtractsSamples()
    {
        var samples = AltSoundManifestLinter.ExtractReferencedSamples(ValidCsv);
        Assert.Equal(2, samples.Count);
        Assert.True(samples.Contains("boom1.wav"));
        Assert.True(samples.Contains("bell1.ogg"));
    }

    public static void Test_QuotedFieldWithComma_ParsedCorrectly()
    {
        var csv =
            "\"ID\",\"CHANNEL\",\"DUCK\",\"GAIN\",\"LOOP\",\"STOP\",\"NAME\",\"FNAME\"\n" +
            "\"1\",\"1\",\"0\",\"1\",\"0\",\"0\",\"Boom, Loud\",\"boom1.wav\"\n";
        var samples = AltSoundManifestLinter.ExtractReferencedSamples(csv);
        Assert.Equal(1, samples.Count);
        Assert.Equal("boom1.wav", samples[0]);
    }

    public static void Test_EscapedQuoteInField_ParsedCorrectly()
    {
        var csv =
            "\"ID\",\"CHANNEL\",\"DUCK\",\"GAIN\",\"LOOP\",\"STOP\",\"NAME\",\"FNAME\"\n" +
            "\"1\",\"1\",\"0\",\"1\",\"0\",\"0\",\"Rock \"\"n\"\" Roll\",\"rock1.wav\"\n";
        var samples = AltSoundManifestLinter.ExtractReferencedSamples(csv);
        Assert.Equal(1, samples.Count);
        Assert.Equal("rock1.wav", samples[0]);
    }

    public static void Test_MissingFnameColumn_ReturnsEmpty()
    {
        var csv = "\"ID\",\"CHANNEL\",\"NAME\"\n\"1\",\"1\",\"Boom\"\n";
        var samples = AltSoundManifestLinter.ExtractReferencedSamples(csv);
        Assert.Equal(0, samples.Count);
    }

    public static void Test_EmptyFile_ReturnsEmpty()
    {
        var samples = AltSoundManifestLinter.ExtractReferencedSamples("");
        Assert.Equal(0, samples.Count);
    }

    public static void Test_BlankFname_Skipped()
    {
        var csv =
            "\"ID\",\"CHANNEL\",\"DUCK\",\"GAIN\",\"LOOP\",\"STOP\",\"NAME\",\"FNAME\"\n" +
            "\"1\",\"1\",\"0\",\"1\",\"0\",\"0\",\"Placeholder\",\"\"\n" +
            "\"2\",\"1\",\"0\",\"1\",\"0\",\"0\",\"Bell\",\"bell1.ogg\"\n";
        var samples = AltSoundManifestLinter.ExtractReferencedSamples(csv);
        Assert.Equal(1, samples.Count);
        Assert.Equal("bell1.ogg", samples[0]);
    }

    public static void Test_DuplicateIds_BothFnamesExtracted()
    {
        // Rows sharing an ID is normal (engine random-picks a variant) — the linter itself doesn't
        // dedupe; that policy call belongs to the scanner.
        var csv =
            "\"ID\",\"CHANNEL\",\"DUCK\",\"GAIN\",\"LOOP\",\"STOP\",\"NAME\",\"FNAME\"\n" +
            "\"1\",\"1\",\"0\",\"1\",\"0\",\"0\",\"Boom A\",\"boomA.wav\"\n" +
            "\"1\",\"1\",\"0\",\"1\",\"0\",\"0\",\"Boom B\",\"boomB.wav\"\n";
        var samples = AltSoundManifestLinter.ExtractReferencedSamples(csv);
        Assert.Equal(2, samples.Count);
        Assert.True(samples.Contains("boomA.wav"));
        Assert.True(samples.Contains("boomB.wav"));
    }

    public static void Test_ShortRow_Skipped()
    {
        var csv =
            "\"ID\",\"CHANNEL\",\"DUCK\",\"GAIN\",\"LOOP\",\"STOP\",\"NAME\",\"FNAME\"\n" +
            "\"1\",\"1\"\n" + // malformed / truncated row — fewer fields than the header
            "\"2\",\"1\",\"0\",\"1\",\"0\",\"0\",\"Bell\",\"bell1.ogg\"\n";
        var samples = AltSoundManifestLinter.ExtractReferencedSamples(csv);
        Assert.Equal(1, samples.Count);
        Assert.Equal("bell1.ogg", samples[0]);
    }

    public static void Test_CaseInsensitiveHeader_Matches()
    {
        var csv = "\"id\",\"channel\",\"fname\"\n\"1\",\"1\",\"boom1.wav\"\n";
        var samples = AltSoundManifestLinter.ExtractReferencedSamples(csv);
        Assert.Equal(1, samples.Count);
        Assert.Equal("boom1.wav", samples[0]);
    }
}

/// <summary>End-to-end scanner behaviour, with CSV read + sample existence injected.</summary>
public static class AltSoundScannerTests
{
    private const string TwoSampleCsv =
        "\"ID\",\"CHANNEL\",\"DUCK\",\"GAIN\",\"LOOP\",\"STOP\",\"NAME\",\"FNAME\"\n" +
        "\"1\",\"1\",\"0\",\"1\",\"0\",\"0\",\"Boom\",\"boom1.wav\"\n" +
        "\"2\",\"1\",\"0\",\"1\",\"0\",\"0\",\"Bell\",\"bell1.ogg\"\n";

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
        var findings = new AltSoundScanner(_ => TwoSampleCsv, _ => true).Scan(ctx).ToList();
        Assert.Equal(0, findings.Count);
    }

    public static void Test_RomNotRequired_NeverQueried()
    {
        var layout = new InstallLayout { RootPath = "/x", VPinMameDir = "/x/VPinMAME" };
        var ctx = new ScanContext { Layout = layout, Profile = Fixtures.Profile() };
        ctx.Tables["Foo.vpx"] = new VpxTableData { FilePath = "Foo.vpx", Script = "Sub Table1_Init()\nEnd Sub" };
        var scanner = new AltSoundScanner(
            _ => throw new InvalidOperationException("must not be called"),
            _ => throw new InvalidOperationException("must not be called"));
        var findings = scanner.Scan(ctx).ToList(); // must not throw
        Assert.Equal(0, findings.Count);
    }

    public static void Test_NoAltsoundCsv_Silent()
    {
        var ctx = CtxWithRomTable("afm_113b");
        var findings = new AltSoundScanner(_ => null, _ => true).Scan(ctx).ToList();
        Assert.Equal(0, findings.Count);
    }

    public static void Test_AllSamplesPresent_Silent()
    {
        var ctx = CtxWithRomTable("afm_113b");
        var findings = new AltSoundScanner(_ => TwoSampleCsv, _ => true).Scan(ctx).ToList();
        Assert.False(findings.Any(f => f.Code == "ALTSOUND_SAMPLE_MISSING"));
    }

    public static void Test_MissingSamples_WarnsWithCounts()
    {
        var ctx = CtxWithRomTable("afm_113b");
        // boom1.wav present, bell1.ogg missing.
        var findings = new AltSoundScanner(_ => TwoSampleCsv, p => p.Replace('\\', '/').EndsWith("boom1.wav")).Scan(ctx).ToList();
        Assert.Equal(1, findings.Count(f => f.Code == "ALTSOUND_SAMPLE_MISSING"));
        var f = findings.Single(f => f.Code == "ALTSOUND_SAMPLE_MISSING");
        Assert.Equal(Severity.Warning, f.Severity);
        Assert.Equal("afm_113b", f.Subject);
        Assert.Equal("afm_113b", f.Args[0]);
        Assert.Equal("1", f.Args[1]); // 1 missing
        Assert.Equal("2", f.Args[2]); // 2 total
    }

    public static void Test_ReadCsvThrows_Silent()
    {
        var ctx = CtxWithRomTable("afm_113b");
        var findings = new AltSoundScanner(_ => throw new UnauthorizedAccessException(), _ => true).Scan(ctx).ToList();
        Assert.Equal(0, findings.Count);
    }

    public static void Test_FileExistsThrows_SkippedNotCounted()
    {
        var ctx = CtxWithRomTable("afm_113b");
        // boom1.wav's existence check throws (treated as present, never guessed missing);
        // bell1.ogg genuinely missing — the real defect must still surface.
        var findings = new AltSoundScanner(_ => TwoSampleCsv, p =>
        {
            if (p.Replace('\\', '/').EndsWith("boom1.wav")) throw new IOException("locked");
            return false;
        }).Scan(ctx).ToList();
        Assert.Equal(1, findings.Count(f => f.Code == "ALTSOUND_SAMPLE_MISSING"));
        var f = findings.Single(f => f.Code == "ALTSOUND_SAMPLE_MISSING");
        Assert.Equal("1", f.Args[1]); // only bell1.ogg counted as missing
    }

    public static void Test_QueriesTheRomSpecificCsvPath()
    {
        string? seenPath = null;
        var ctx = CtxWithRomTable("afm_113b");
        new AltSoundScanner(p => { seenPath = p; return null; }, _ => true).Scan(ctx).ToList();
        Assert.NotNull(seenPath);
        Assert.True(seenPath!.Replace('\\', '/').EndsWith("altsound/afm_113b/altsound.csv"));
    }

    public static void Test_MultipleIncompleteRoms_AllReported()
    {
        var layout = new InstallLayout { RootPath = "/x", VPinMameDir = "/x/VPinMAME" };
        var ctx = new ScanContext { Layout = layout, Profile = Fixtures.Profile() };
        ctx.Tables["A.vpx"] = new VpxTableData { FilePath = "A.vpx", Script = "Const cGameName = \"afm_113b\"\nCreateObject(\"VPinMAME.Controller\")" };
        ctx.Tables["B.vpx"] = new VpxTableData { FilePath = "B.vpx", Script = "Const cGameName = \"mm_109c\"\nCreateObject(\"VPinMAME.Controller\")" };
        var findings = new AltSoundScanner(_ => TwoSampleCsv, _ => false).Scan(ctx).ToList();
        Assert.Equal(2, findings.Count(f => f.Code == "ALTSOUND_SAMPLE_MISSING"));
    }

    public static void Test_DuplicateSampleAcrossRows_CountedOnce()
    {
        var csv =
            "\"ID\",\"CHANNEL\",\"DUCK\",\"GAIN\",\"LOOP\",\"STOP\",\"NAME\",\"FNAME\"\n" +
            "\"1\",\"1\",\"0\",\"1\",\"0\",\"0\",\"Boom A\",\"boom1.wav\"\n" +
            "\"1\",\"1\",\"0\",\"1\",\"0\",\"0\",\"Boom A dup\",\"boom1.wav\"\n";
        var ctx = CtxWithRomTable("afm_113b");
        var findings = new AltSoundScanner(_ => csv, _ => false).Scan(ctx).ToList();
        var f = findings.Single(f => f.Code == "ALTSOUND_SAMPLE_MISSING");
        Assert.Equal("1", f.Args[1]); // missing
        Assert.Equal("1", f.Args[2]); // total — the repeated FNAME counts once
    }
}
