namespace PincabToolbox.Core.Services;

/// <summary>
/// Pure well-formedness check for .directb2s files (audit §4/H2).
///
/// <para>
/// <b>Format verified by research before writing a single line of parsing code</b> — including the
/// DirectB2S Designer's own exporter source (`CreateDirectB2SFile()` in b2s-backglass/b2s-designer,
/// a plain `XmlDocument.Save`), the B2S Backglass Server's own loader (a plain `XmlDocument.Load` +
/// `SelectSingleNode("DirectB2SData")`), and an independent, real-collection-tested third-party parser
/// (francisdb/rust-directb2s, used by vpxtool). All three agree: a genuine .directb2s is always plain
/// XML, root element <c>DirectB2SData</c>. No confirmed real-world "OLE-compressed" variant was found
/// anywhere — despite the handoff's note that one might exist and that this repo's own
/// <see cref="PincabToolbox.Core.Vpx.CompoundFileReader"/> (used for .vpx) "exists if needed". See the
/// scope note on <see cref="Scanning.DirectB2sScanner"/> for how that conditional is honoured without
/// guessing a stream layout nobody could confirm.
/// </para>
/// </summary>
public static class DirectB2SValidator
{
    /// <summary>
    /// True when the bytes parse as well-formed XML. Deliberately does not require any specific root
    /// element — an unusual-but-syntactically-valid file is not this check's business, only a
    /// genuinely broken one (bias to silence on anything short of an actual parse failure).
    /// </summary>
    public static bool IsWellFormedXml(byte[] bytes)
    {
        if (bytes.Length == 0) return false;
        try
        {
            using var stream = new MemoryStream(bytes);
            using var reader = System.Xml.XmlReader.Create(stream);
            while (reader.Read()) { }
            return true;
        }
        catch { return false; }
    }

    /// <summary>
    /// True when the bytes start with the Microsoft Compound File Binary Format signature
    /// (<c>D0 CF 11 E0 A1 B1 1A E1</c>) — the same container .vpx itself uses. No genuine DirectB2S
    /// writer is confirmed to ever emit this, but a file that has it is a recognizably DIFFERENT
    /// format, not evidence of corruption — see scope note on <see cref="Scanning.DirectB2sScanner"/>.
    /// </summary>
    public static bool LooksLikeCompoundFile(byte[] bytes)
    {
        if (bytes.Length < 8) return false;
        return bytes[0] == 0xD0 && bytes[1] == 0xCF && bytes[2] == 0x11 && bytes[3] == 0xE0 &&
               bytes[4] == 0xA1 && bytes[5] == 0xB1 && bytes[6] == 0x1A && bytes[7] == 0xE1;
    }
}
