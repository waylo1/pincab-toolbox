using System.Text;
using PincabToolbox.Core.Models;
using PincabToolbox.Core.Scanning;
using PincabToolbox.Core.Services;

namespace PincabToolbox.Core.Tests;

/// <summary>Pure well-formedness / container-signature checks.</summary>
public static class DirectB2SValidatorTests
{
    private const string WellFormedXml =
        "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" +
        "<DirectB2SData Version=\"1.2\">\n" +
        "  <GameName Value=\"afm_113b\"/>\n" +
        "</DirectB2SData>\n";

    private const string MalformedXml = // missing closing tag
        "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" +
        "<DirectB2SData Version=\"1.2\">\n" +
        "  <GameName Value=\"afm_113b\"/>\n";

    public static void Test_WellFormedXml_ReturnsTrue()
    {
        Assert.True(DirectB2SValidator.IsWellFormedXml(Encoding.UTF8.GetBytes(WellFormedXml)));
    }

    public static void Test_MalformedXml_UnclosedTag_ReturnsFalse()
    {
        Assert.False(DirectB2SValidator.IsWellFormedXml(Encoding.UTF8.GetBytes(MalformedXml)));
    }

    public static void Test_EmptyBytes_ReturnsFalse()
    {
        Assert.False(DirectB2SValidator.IsWellFormedXml(Array.Empty<byte>()));
    }

    public static void Test_NotXmlAtAll_ReturnsFalse()
    {
        Assert.False(DirectB2SValidator.IsWellFormedXml(new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 }));
    }

    public static void Test_XmlWithUtf8Bom_StillWellFormed()
    {
        var bom = Encoding.UTF8.GetPreamble();
        var body = Encoding.UTF8.GetBytes(WellFormedXml);
        var withBom = new byte[bom.Length + body.Length];
        Array.Copy(bom, withBom, bom.Length);
        Array.Copy(body, 0, withBom, bom.Length, body.Length);
        Assert.True(DirectB2SValidator.IsWellFormedXml(withBom));
    }

    public static void Test_CompoundFileSignature_Detected()
    {
        var bytes = new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1, 0x00, 0x00 };
        Assert.True(DirectB2SValidator.LooksLikeCompoundFile(bytes));
    }

    public static void Test_PlainXmlBytes_NotDetectedAsCompoundFile()
    {
        Assert.False(DirectB2SValidator.LooksLikeCompoundFile(Encoding.UTF8.GetBytes(WellFormedXml)));
    }

    public static void Test_ShortBuffer_NotDetectedAsCompoundFile()
    {
        Assert.False(DirectB2SValidator.LooksLikeCompoundFile(new byte[] { 0xD0, 0xCF, 0x11 }));
    }
}

/// <summary>End-to-end scanner behaviour, with directory listing + file reads injected.</summary>
public static class DirectB2sScannerTests
{
    private const string WellFormedXml =
        "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<DirectB2SData Version=\"1.2\"><GameName Value=\"afm_113b\"/></DirectB2SData>";
    private const string MalformedXml =
        "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<DirectB2SData Version=\"1.2\"><GameName Value=\"afm_113b\"/>";
    private static readonly byte[] CfbBytes = { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1, 0x00, 0x00 };

    private static ScanContext CtxWithTablesDir(string tablesDir = "/x/Tables")
    {
        var layout = new InstallLayout { RootPath = "/x", TablesDir = tablesDir };
        return new ScanContext { Layout = layout, Profile = Fixtures.Profile() };
    }

    public static void Test_NoTablesDir_Silent()
    {
        var layout = new InstallLayout { RootPath = "/x", TablesDir = null };
        var ctx = new ScanContext { Layout = layout, Profile = Fixtures.Profile() };
        var findings = new DirectB2sScanner(_ => new[] { "/x/Foo.directb2s" }, _ => Encoding.UTF8.GetBytes(MalformedXml)).Scan(ctx).ToList();
        Assert.Equal(0, findings.Count);
    }

    public static void Test_ListFilesThrows_Silent()
    {
        var ctx = CtxWithTablesDir();
        var findings = new DirectB2sScanner(_ => throw new UnauthorizedAccessException(), _ => Encoding.UTF8.GetBytes(MalformedXml)).Scan(ctx).ToList();
        Assert.Equal(0, findings.Count);
    }

    public static void Test_NoDirectB2sFiles_Silent()
    {
        var ctx = CtxWithTablesDir();
        var findings = new DirectB2sScanner(_ => Array.Empty<string>(), _ => null).Scan(ctx).ToList();
        Assert.Equal(0, findings.Count);
    }

    public static void Test_WellFormedFile_Silent()
    {
        var ctx = CtxWithTablesDir();
        var findings = new DirectB2sScanner(_ => new[] { "/x/Tables/Foo.directb2s" }, _ => Encoding.UTF8.GetBytes(WellFormedXml)).Scan(ctx).ToList();
        Assert.Equal(0, findings.Count);
    }

    public static void Test_MalformedFile_Warns()
    {
        var ctx = CtxWithTablesDir();
        var findings = new DirectB2sScanner(_ => new[] { "/x/Tables/Foo.directb2s" }, _ => Encoding.UTF8.GetBytes(MalformedXml)).Scan(ctx).ToList();
        Assert.Equal(1, findings.Count(f => f.Code == "B2S_MALFORMED"));
        var f = findings.Single(f => f.Code == "B2S_MALFORMED");
        Assert.Equal(Severity.Warning, f.Severity);
        Assert.Equal("Foo.directb2s", f.Subject);
    }

    public static void Test_EmptyFile_Warns()
    {
        var ctx = CtxWithTablesDir();
        var findings = new DirectB2sScanner(_ => new[] { "/x/Tables/Foo.directb2s" }, _ => Array.Empty<byte>()).Scan(ctx).ToList();
        Assert.Equal(1, findings.Count(f => f.Code == "B2S_MALFORMED"));
    }

    public static void Test_CompoundFileSignature_SilentNotClaimedBroken()
    {
        var ctx = CtxWithTablesDir();
        var findings = new DirectB2sScanner(_ => new[] { "/x/Tables/Foo.directb2s" }, _ => CfbBytes).Scan(ctx).ToList();
        Assert.Equal(0, findings.Count);
    }

    public static void Test_ReadBytesThrows_SkippedNotCrashed()
    {
        var ctx = CtxWithTablesDir();
        var findings = new DirectB2sScanner(_ => new[] { "/x/Tables/Foo.directb2s" }, _ => throw new IOException("locked")).Scan(ctx).ToList();
        Assert.Equal(0, findings.Count);
    }

    public static void Test_UnreadableFile_NullBytes_Silent()
    {
        var ctx = CtxWithTablesDir();
        var findings = new DirectB2sScanner(_ => new[] { "/x/Tables/Foo.directb2s" }, _ => null).Scan(ctx).ToList();
        Assert.Equal(0, findings.Count);
    }

    public static void Test_MultipleMalformedFiles_AllReported()
    {
        var ctx = CtxWithTablesDir();
        var findings = new DirectB2sScanner(
            _ => new[] { "/x/Tables/A.directb2s", "/x/Tables/B.directb2s" },
            _ => Encoding.UTF8.GetBytes(MalformedXml)
        ).Scan(ctx).ToList();
        Assert.Equal(2, findings.Count(f => f.Code == "B2S_MALFORMED"));
    }
}
