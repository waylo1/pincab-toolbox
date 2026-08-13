using System.Globalization;
using System.Text;

namespace PincabToolbox.Core.Reporting;

/// <summary>
/// Widths (in 1/1000 em units, standard AFM convention) for the built-in Helvetica base-14 font.
/// Used only to word-wrap text before laying it out on a page — not for pixel-perfect typesetting,
/// so the small set of WinAnsi-range characters not individually listed fall back to a reasonable
/// average (556, the width of a lowercase Latin letter in Helvetica) rather than needing every one
/// of the 256 codepoints enumerated by hand.
/// </summary>
public static class HelveticaMetrics
{
    private static readonly Dictionary<char, int> Ascii = new()
    {
        [' '] = 278, ['!'] = 278, ['"'] = 355, ['#'] = 556, ['$'] = 556, ['%'] = 889, ['&'] = 667,
        ['\''] = 191, ['('] = 333, [')'] = 333, ['*'] = 389, ['+'] = 584, [','] = 278, ['-'] = 333,
        ['.'] = 278, ['/'] = 278,
        ['0'] = 556, ['1'] = 556, ['2'] = 556, ['3'] = 556, ['4'] = 556, ['5'] = 556, ['6'] = 556,
        ['7'] = 556, ['8'] = 556, ['9'] = 556,
        [':'] = 278, [';'] = 278, ['<'] = 584, ['='] = 584, ['>'] = 584, ['?'] = 556, ['@'] = 1015,
        ['A'] = 667, ['B'] = 667, ['C'] = 722, ['D'] = 722, ['E'] = 667, ['F'] = 611, ['G'] = 778,
        ['H'] = 722, ['I'] = 278, ['J'] = 500, ['K'] = 667, ['L'] = 556, ['M'] = 833, ['N'] = 722,
        ['O'] = 778, ['P'] = 667, ['Q'] = 778, ['R'] = 722, ['S'] = 667, ['T'] = 611, ['U'] = 722,
        ['V'] = 667, ['W'] = 944, ['X'] = 667, ['Y'] = 667, ['Z'] = 611,
        ['['] = 278, ['\\'] = 278, [']'] = 278, ['^'] = 469, ['_'] = 556, ['`'] = 333,
        ['a'] = 556, ['b'] = 556, ['c'] = 500, ['d'] = 556, ['e'] = 556, ['f'] = 278, ['g'] = 556,
        ['h'] = 556, ['i'] = 222, ['j'] = 222, ['k'] = 500, ['l'] = 222, ['m'] = 833, ['n'] = 556,
        ['o'] = 556, ['p'] = 556, ['q'] = 556, ['r'] = 333, ['s'] = 500, ['t'] = 278, ['u'] = 556,
        ['v'] = 500, ['w'] = 722, ['x'] = 500, ['y'] = 500, ['z'] = 500,
        ['{'] = 334, ['|'] = 260, ['}'] = 334, ['~'] = 584,
    };

    /// <summary>Handful of WinAnsi characters this product's own strings actually use (accents,
    /// French quotes, dashes…) whose Helvetica width differs enough from the 556 default to be
    /// worth listing — see the WinAnsi encoding table in <see cref="PdfText"/> for the full set
    /// this exporter can place on a page at all.</summary>
    private static readonly Dictionary<char, int> Extended = new()
    {
        ['œ'] = 944, ['Œ'] = 944, ['—'] = 1000, ['–'] = 556, ['…'] = 1000, ['•'] = 350,
        ['«'] = 556, ['»'] = 556, ['·'] = 278, ['×'] = 584,
    };

    private const int DefaultWidth = 556;

    public static int CharWidth1000(char c)
    {
        if (Ascii.TryGetValue(c, out var w)) return w;
        if (Extended.TryGetValue(c, out var w2)) return w2;
        return DefaultWidth;
    }

    public static double TextWidth(string text, double fontSize)
    {
        var total = 0;
        foreach (var c in text) total += CharWidth1000(c);
        return total / 1000.0 * fontSize;
    }
}

/// <summary>
/// Everything needed to place plain-text lines into a PDF's base-14 Helvetica font without any
/// external library: WinAnsiEncoding transliteration (the only encoding a PDF reader is guaranteed
/// to have for a non-embedded standard font), literal-string escaping, and greedy word-wrap.
/// </summary>
public static class PdfText
{
    /// <summary>
    /// WinAnsiEncoding byte for characters below U+0100 matches the Unicode code point directly —
    /// WinAnsi's upper half (0xA0-0xFF) is Latin-1. The 0x80-0x9F block is the exception: WinAnsi
    /// repurposes it for typographic characters (em dash, curly quotes, bullet, œ…) that live at
    /// completely different Unicode code points, so those need an explicit table. Anything with no
    /// entry here and no ASCII byte falls back to '?' rather than corrupting the byte stream — this
    /// exporter is for a diagnostic report, not a typesetting engine.
    /// </summary>
    private static readonly Dictionary<char, byte> WinAnsiSpecial = new()
    {
        ['€'] = 0x80, ['‚'] = 0x82, ['ƒ'] = 0x83, ['„'] = 0x84, ['…'] = 0x85, ['†'] = 0x86,
        ['‡'] = 0x87, ['ˆ'] = 0x88, ['‰'] = 0x89, ['Š'] = 0x8A, ['‹'] = 0x8B, ['Œ'] = 0x8C,
        ['Ž'] = 0x8E, ['‘'] = 0x91, ['’'] = 0x92, ['“'] = 0x93, ['”'] = 0x94,
        ['•'] = 0x95, ['–'] = 0x96, ['—'] = 0x97, ['˜'] = 0x98, ['™'] = 0x99, ['š'] = 0x9A,
        ['›'] = 0x9B, ['œ'] = 0x9C, ['ž'] = 0x9E, ['Ÿ'] = 0x9F,
    };

    /// <summary>
    /// Decorative glyphs this product's UI uses (severity/status icons) that have no place in
    /// WinAnsiEncoding at all. Swapped for an ASCII equivalent before layout so word-wrap measures
    /// the text that will actually appear, rather than measuring a glyph and printing '?' for it.
    /// </summary>
    private static readonly (string From, string To)[] Transliterations =
    {
        ("✓", "OK"), ("✕", "X"), ("▲", "!"), ("⚠", "!"), ("⚑", "[manual]"),
        ("✎", ""), ("⏱", ""), ("→", "->"), ("─", "-"),
    };

    public static string Transliterate(string text)
    {
        foreach (var (from, to) in Transliterations)
            if (text.Contains(from, StringComparison.Ordinal))
                text = text.Replace(from, to, StringComparison.Ordinal);
        return text;
    }

    private static byte EncodeChar(char c)
    {
        if (c < 0x80) return (byte)c;
        if (WinAnsiSpecial.TryGetValue(c, out var special)) return special;
        if (c is >= (char)0xA0 and <= (char)0xFF) return (byte)c;
        return (byte)'?';
    }

    /// <summary>Encodes and escapes text for use inside a PDF literal string, i.e. the bytes to
    /// place between the '(' and ')' of a Tj operand.</summary>
    public static byte[] EncodeLiteral(string text)
    {
        var bytes = new List<byte>(text.Length + 4);
        foreach (var c in text)
        {
            var b = EncodeChar(c);
            if (b is (byte)'(' or (byte)')' or (byte)'\\') bytes.Add((byte)'\\');
            bytes.Add(b);
        }
        return bytes.ToArray();
    }

    /// <summary>Greedy word-wrap to a maximum pixel width at the given font size. A single word
    /// wider than the whole line is hard-broken by character so it can never overflow the page.</summary>
    public static List<string> Wrap(string text, double maxWidth, double fontSize)
    {
        var lines = new List<string>();
        if (string.IsNullOrEmpty(text)) { lines.Add(""); return lines; }

        var words = text.Split(' ');
        var current = new StringBuilder();

        void FlushCurrent()
        {
            lines.Add(current.ToString());
            current.Clear();
        }

        foreach (var rawWord in words)
        {
            var word = rawWord;
            while (HelveticaMetrics.TextWidth(word, fontSize) > maxWidth)
            {
                // Hard-break a word that alone is wider than the page — long paths, mostly.
                var cut = word.Length;
                while (cut > 1 && HelveticaMetrics.TextWidth(word[..cut], fontSize) > maxWidth) cut--;
                var piece = word[..cut];
                if (current.Length > 0) { FlushCurrent(); }
                lines.Add(piece);
                word = word[cut..];
            }

            var candidate = current.Length == 0 ? word : current + " " + word;
            if (HelveticaMetrics.TextWidth(candidate, fontSize) > maxWidth && current.Length > 0)
            {
                FlushCurrent();
                current.Append(word);
            }
            else
            {
                current.Clear();
                current.Append(candidate);
            }
        }

        if (current.Length > 0 || lines.Count == 0) FlushCurrent();
        return lines;
    }
}

/// <summary>
/// Builds a paginated PDF from plain text with zero external dependencies — the whole codebase is
/// built without a single NuGet package (see NuGet.Config), and a report export is no exception.
/// Uses only the built-in Helvetica base-14 font (guaranteed present in every PDF reader, no font
/// embedding needed) with WinAnsiEncoding. Deliberately dumb layout: one line in, one line out,
/// word-wrapped and paginated — no columns, no tables, no images. Good enough for a text report;
/// anything fancier would mean writing a much bigger PDF engine for a free scanner's export button.
/// </summary>
public static class PdfDocumentBuilder
{
    private const double PageWidth = 595.28;   // A4, points (1/72 in)
    private const double PageHeight = 841.89;
    private const double MarginX = 42;
    private const double MarginTop = 54;
    private const double MarginBottom = 44;
    private const double TitleFontSize = 16;
    private const double BodyFontSize = 9.5;
    private const double LineHeight = 12.5;
    private const double TitleGap = 22;

    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    private static string Num(double v) => v.ToString("0.##", Inv);

    /// <summary>
    /// Builds the PDF bytes. <paramref name="bodyLines"/> are logical lines — each one is
    /// word-wrapped and paginated independently; pass an empty string for a blank line between
    /// paragraphs. Every line (title included) goes through <see cref="PdfText.Transliterate"/>
    /// and WinAnsi encoding, so callers do not need to pre-sanitize anything.
    /// </summary>
    public static byte[] Build(string title, IReadOnlyList<string> bodyLines)
    {
        var maxWidth = PageWidth - 2 * MarginX;

        var wrapped = new List<string>();
        foreach (var raw in bodyLines)
        {
            // A logical line is exactly one visual line once wrapped — an embedded \r/\n would
            // silently desync that from the page budget computed below.
            var line = PdfText.Transliterate((raw ?? "").Replace("\r", "").Replace("\n", " "));
            wrapped.AddRange(PdfText.Wrap(line, maxWidth, BodyFontSize));
        }

        var linesPerPage = (int)((PageHeight - MarginTop - MarginBottom) / LineHeight);
        // First page loses a few body lines to the title block.
        var firstPageBudget = Math.Max(1, linesPerPage - (int)Math.Ceiling((TitleFontSize + TitleGap) / LineHeight));

        var pages = new List<List<string>>();
        var i = 0;
        var budget = firstPageBudget;
        while (i < wrapped.Count)
        {
            var take = Math.Min(budget, wrapped.Count - i);
            pages.Add(wrapped.GetRange(i, take));
            i += take;
            budget = linesPerPage;
        }
        if (pages.Count == 0) pages.Add(new List<string>());

        return Assemble(PdfText.Transliterate(title), pages);
    }

    private static byte[] Assemble(string title, List<List<string>> pages)
    {
        var objects = new List<byte[]>();     // index 0 == object #1
        var offsets = new List<int>();

        int NewObjectSlot() { objects.Add(Array.Empty<byte>()); return objects.Count; }
        void SetObject(int number, string content) => objects[number - 1] = Encoding.ASCII.GetBytes(content);
        void SetObjectBytes(int number, byte[] content) => objects[number - 1] = content;

        var catalogNum = NewObjectSlot();
        var pagesNum = NewObjectSlot();
        var fontRegularNum = NewObjectSlot();
        var fontBoldNum = NewObjectSlot();

        SetObject(fontRegularNum, $"{fontRegularNum} 0 obj\n<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>\nendobj\n");
        SetObject(fontBoldNum, $"{fontBoldNum} 0 obj\n<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold /Encoding /WinAnsiEncoding >>\nendobj\n");

        var pageObjectNumbers = new List<int>();
        var contentObjectNumbers = new List<int>();
        for (var p = 0; p < pages.Count; p++)
        {
            pageObjectNumbers.Add(NewObjectSlot());
            contentObjectNumbers.Add(NewObjectSlot());
        }

        for (var p = 0; p < pages.Count; p++)
        {
            var pageNum = pageObjectNumbers[p];
            var contentNum = contentObjectNumbers[p];
            var content = BuildPageContent(title, pages[p], p == 0, p + 1);
            // Latin1: content can carry WinAnsi bytes up to 0xFF (see LiteralEscapedString) which
            // Encoding.ASCII would silently mangle into '?'.
            var contentBytes = Encoding.Latin1.GetBytes(content);
            var contentHeader = Encoding.Latin1.GetBytes($"{contentNum} 0 obj\n<< /Length {contentBytes.Length} >>\nstream\n");
            var contentFooter = Encoding.Latin1.GetBytes("\nendstream\nendobj\n");
            SetObjectBytes(contentNum, Concat(contentHeader, contentBytes, contentFooter));

            SetObject(pageNum,
                $"{pageNum} 0 obj\n<< /Type /Page /Parent {pagesNum} 0 R /MediaBox [0 0 {Num(PageWidth)} {Num(PageHeight)}] " +
                $"/Resources << /Font << /F1 {fontRegularNum} 0 R /F2 {fontBoldNum} 0 R >> >> /Contents {contentNum} 0 R >>\nendobj\n");
        }

        var kids = string.Join(" ", pageObjectNumbers.Select(n => $"{n} 0 R"));
        SetObject(pagesNum, $"{pagesNum} 0 obj\n<< /Type /Pages /Kids [{kids}] /Count {pageObjectNumbers.Count} >>\nendobj\n");
        SetObject(catalogNum, $"{catalogNum} 0 obj\n<< /Type /Catalog /Pages {pagesNum} 0 R >>\nendobj\n");

        var body = new MemoryStream();
        void Write(byte[] b) => body.Write(b, 0, b.Length);
        // Latin1, not ASCII: object headers/xref are pure ASCII so it makes no difference there,
        // but content-stream bytes can carry WinAnsi codes up to 0xFF (see LiteralEscapedString) —
        // Encoding.ASCII would silently replace every one of those with '?'.
        void WriteAscii(string s) => Write(Encoding.Latin1.GetBytes(s));

        WriteAscii("%PDF-1.4\n%\xE2\xE3\xCF\xD3\n");
        foreach (var obj in objects)
        {
            offsets.Add((int)body.Length);
            Write(obj);
        }
        var xrefOffset = (int)body.Length;

        WriteAscii($"xref\n0 {objects.Count + 1}\n0000000000 65535 f \n");
        foreach (var off in offsets)
            WriteAscii($"{off:0000000000} 00000 n \n");

        WriteAscii($"trailer\n<< /Size {objects.Count + 1} /Root {catalogNum} 0 R >>\nstartxref\n{xrefOffset}\n%%EOF");

        return body.ToArray();
    }

    private static byte[] Concat(params byte[][] parts)
    {
        var total = parts.Sum(p => p.Length);
        var result = new byte[total];
        var offset = 0;
        foreach (var p in parts) { Buffer.BlockCopy(p, 0, result, offset, p.Length); offset += p.Length; }
        return result;
    }

    private static string BuildPageContent(string title, List<string> lines, bool isFirstPage, int pageNumber)
    {
        var sb = new StringBuilder();
        var y = PageHeight - MarginTop;

        if (isFirstPage)
        {
            sb.Append("BT /F2 ").Append(Num(TitleFontSize)).Append(" Tf ")
              .Append(Num(MarginX)).Append(' ').Append(Num(y)).Append(" Td (")
              .Append(LiteralEscapedString(title)).Append(") Tj ET\n");
            y -= TitleGap;
        }

        // Td moves BY a delta from the current text position, it does not set an absolute one.
        // BT resets that position to (0,0), so the first Td of this block can use the real
        // (MarginX, y) as its delta; every line after that must move by (0, -LineHeight) only —
        // repeating (MarginX, y) here would walk the text further right and up each time.
        sb.Append("BT /F1 ").Append(Num(BodyFontSize)).Append(" Tf\n");
        var first = true;
        foreach (var line in lines)
        {
            if (first)
            {
                sb.Append(Num(MarginX)).Append(' ').Append(Num(y)).Append(" Td (");
                first = false;
            }
            else
            {
                sb.Append("0 ").Append(Num(-LineHeight)).Append(" Td (");
            }
            sb.Append(LiteralEscapedString(line)).Append(") Tj\n");
            y -= LineHeight;
        }
        sb.Append("ET\n");

        sb.Append("BT /F1 8 Tf ").Append(Num(MarginX)).Append(' ').Append(Num(MarginBottom - 22))
          .Append(" Td (").Append(LiteralEscapedString($"Pincab Toolbox - page {pageNumber}")).Append(") Tj ET\n");

        return sb.ToString();
    }

    /// <summary>String form of <see cref="PdfText.EncodeLiteral"/> for building the content stream
    /// as text — every byte it can produce is ASCII-range-safe once escaped, so round-tripping
    /// through a .NET string (Latin-1 code points 0x00-0xFF map 1:1 to char) is safe here.</summary>
    private static string LiteralEscapedString(string text)
    {
        var bytes = PdfText.EncodeLiteral(text);
        var chars = new char[bytes.Length];
        for (var i = 0; i < bytes.Length; i++) chars[i] = (char)bytes[i];
        return new string(chars);
    }
}
