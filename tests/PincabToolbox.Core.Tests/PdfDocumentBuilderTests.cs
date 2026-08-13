using System.Text;
using PincabToolbox.Core.Reporting;

namespace PincabToolbox.Core.Tests;

public static class HelveticaMetricsTests
{
    public static void Test_Space_Is_Narrower_Than_Capital_M()
    {
        Assert.True(HelveticaMetrics.CharWidth1000(' ') < HelveticaMetrics.CharWidth1000('M'));
    }

    public static void Test_TextWidth_Scales_Linearly_With_FontSize()
    {
        var at10 = HelveticaMetrics.TextWidth("Hello", 10);
        var at20 = HelveticaMetrics.TextWidth("Hello", 20);
        Assert.True(Math.Abs(at20 - at10 * 2) < 0.001, "doubling font size should double the width");
    }

    public static void Test_Unmapped_Char_Falls_Back_To_Default_Width()
    {
        Assert.Equal(556, HelveticaMetrics.CharWidth1000('Ā'));
    }
}

public static class PdfTextTests
{
    public static void Test_Transliterate_Replaces_Checkmark()
    {
        Assert.Equal("OK found", PdfText.Transliterate("✓ found"));
    }

    public static void Test_Transliterate_Replaces_Arrow()
    {
        Assert.Equal("A -> B", PdfText.Transliterate("A → B"));
    }

    public static void Test_Transliterate_Leaves_Plain_Text_Untouched()
    {
        Assert.Equal("ROM_MISSING critical", PdfText.Transliterate("ROM_MISSING critical"));
    }

    public static void Test_EncodeLiteral_Ascii_Passthrough()
    {
        var bytes = PdfText.EncodeLiteral("ROM_OK");
        Assert.Equal("ROM_OK", Encoding.ASCII.GetString(bytes));
    }

    public static void Test_EncodeLiteral_Escapes_Parens_And_Backslash()
    {
        var bytes = PdfText.EncodeLiteral("(hi)\\");
        Assert.Equal(@"\(hi\)\\", Encoding.ASCII.GetString(bytes));
    }

    public static void Test_EncodeLiteral_Maps_Latin1_Accents_Directly()
    {
        var bytes = PdfText.EncodeLiteral("café");
        // 'é' is U+00E9, which is also its WinAnsi byte value (0xE9) — the whole point of the
        // Latin-1-range fast path in EncodeChar. byte[] has no value equality, hence SequenceEqual.
        Assert.True(bytes.SequenceEqual(new byte[] { (byte)'c', (byte)'a', (byte)'f', 0xE9 }));
    }

    public static void Test_EncodeLiteral_Maps_EmDash_Through_The_WinAnsi_Special_Block()
    {
        // U+2014 (em dash) is NOT byte 0x2014 truncated — WinAnsi puts it at 0x97, the whole
        // reason WinAnsiSpecial exists instead of a blind cast.
        var bytes = PdfText.EncodeLiteral("—");
        Assert.True(bytes.SequenceEqual(new byte[] { 0x97 }));
    }

    public static void Test_EncodeLiteral_Unrepresentable_Char_Falls_Back_To_Question_Mark()
    {
        var bytes = PdfText.EncodeLiteral("😀");   // an emoji, two UTF-16 surrogate chars
        Assert.True(bytes.SequenceEqual(new byte[] { (byte)'?', (byte)'?' }));
    }

    public static void Test_Wrap_Keeps_A_Short_Line_On_One_Line()
    {
        var lines = PdfText.Wrap("short text", 500, 10);
        Assert.Equal(1, lines.Count);
        Assert.Equal("short text", lines[0]);
    }

    public static void Test_Wrap_Breaks_At_A_Word_Boundary_When_Too_Wide()
    {
        var text = "one two three four five six seven eight nine ten";
        var lines = PdfText.Wrap(text, 100, 10);
        Assert.True(lines.Count > 1, "long text at a narrow width must wrap onto more than one line");
        foreach (var line in lines)
            Assert.True(HelveticaMetrics.TextWidth(line, 10) <= 100 + 0.01, $"line \"{line}\" overflows the max width");
    }

    public static void Test_Wrap_Rejoins_To_The_Same_Words_In_Order()
    {
        var text = "alpha beta gamma delta epsilon";
        var lines = PdfText.Wrap(text, 90, 10);
        Assert.Equal(text, string.Join(" ", lines));
    }

    public static void Test_Wrap_Hard_Breaks_A_Single_Word_Wider_Than_The_Line()
    {
        var longWord = new string('x', 200);
        var lines = PdfText.Wrap(longWord, 100, 10);
        Assert.True(lines.Count > 1, "a word wider than the max width must still be split");
        foreach (var line in lines)
            Assert.True(HelveticaMetrics.TextWidth(line, 10) <= 100 + 0.01, $"hard-broken piece \"{line}\" overflows the max width");
        Assert.Equal(longWord, string.Concat(lines));
    }

    public static void Test_Wrap_Empty_String_Yields_One_Blank_Line()
    {
        var lines = PdfText.Wrap("", 200, 10);
        Assert.Equal(1, lines.Count);
        Assert.Equal("", lines[0]);
    }
}

public static class PdfDocumentBuilderTests
{
    public static void Test_Build_Starts_With_The_Pdf_Header()
    {
        var bytes = PdfDocumentBuilder.Build("Report", new[] { "line one" });
        var head = Encoding.Latin1.GetString(bytes, 0, 8);
        Assert.Equal("%PDF-1.4", head);
    }

    public static void Test_Build_Ends_With_Eof_Marker()
    {
        var bytes = PdfDocumentBuilder.Build("Report", new[] { "line one" });
        var text = Encoding.Latin1.GetString(bytes);
        Assert.True(text.TrimEnd().EndsWith("%%EOF"), "a well-formed PDF must end with %%EOF");
    }

    public static void Test_Build_Contains_A_Valid_Xref_Table()
    {
        var bytes = PdfDocumentBuilder.Build("Report", new[] { "line one", "line two" });
        var text = Encoding.Latin1.GetString(bytes);
        Assert.Contains("xref", text);
        Assert.Contains("trailer", text);
        Assert.Contains("startxref", text);
    }

    public static void Test_Build_Single_Short_Report_Is_One_Page()
    {
        var bytes = PdfDocumentBuilder.Build("Report", new[] { "one finding" });
        var text = Encoding.Latin1.GetString(bytes);
        var pageObjects = System.Text.RegularExpressions.Regex.Matches(text, @"/Type\s*/Page(?!s)").Count;
        Assert.Equal(1, pageObjects);
    }

    public static void Test_Build_Many_Lines_Spans_Multiple_Pages()
    {
        var lines = Enumerable.Range(0, 500).Select(i => $"finding number {i}").ToArray();
        var bytes = PdfDocumentBuilder.Build("Big Report", lines);
        var text = Encoding.Latin1.GetString(bytes);
        var pageObjects = System.Text.RegularExpressions.Regex.Matches(text, @"/Type\s*/Page(?!s)").Count;
        Assert.True(pageObjects > 1, "500 findings must not fit on a single A4 page");
    }

    public static void Test_Build_Kids_Count_Matches_Actual_Page_Object_Count()
    {
        var lines = Enumerable.Range(0, 500).Select(i => $"finding number {i}").ToArray();
        var bytes = PdfDocumentBuilder.Build("Big Report", lines);
        var text = Encoding.Latin1.GetString(bytes);
        var countMatch = System.Text.RegularExpressions.Regex.Match(text, @"/Type\s*/Pages\s*/Kids\s*\[(.*?)\]\s*/Count\s*(\d+)");
        Assert.True(countMatch.Success, "the /Pages object must declare /Kids and /Count");
        var declaredCount = int.Parse(countMatch.Groups[2].Value);
        var actualPageObjects = System.Text.RegularExpressions.Regex.Matches(text, @"/Type\s*/Page(?!s)").Count;
        Assert.Equal(actualPageObjects, declaredCount);
    }

    public static void Test_Build_Embeds_Plain_Ascii_Text_Verbatim()
    {
        var bytes = PdfDocumentBuilder.Build("Report", new[] { "UNIQUE_MARKER_TOKEN" });
        var text = Encoding.Latin1.GetString(bytes);
        Assert.Contains("UNIQUE_MARKER_TOKEN", text);
    }

    public static void Test_Build_Never_Emits_Encoding_Ascii_Question_Marks_For_Accents()
    {
        var bytes = PdfDocumentBuilder.Build("Rapport", new[] { "Testé à Paris" });
        var text = Encoding.Latin1.GetString(bytes);
        // If content bytes ever went through Encoding.ASCII instead of Latin1, 'é'/'à' would show
        // up as literal '?' inside the stream — this is the regression test for that exact bug.
        Assert.Contains("Testé à Paris", text);
    }

    public static void Test_Build_Transliterates_Decorative_Glyphs_Before_Embedding()
    {
        var bytes = PdfDocumentBuilder.Build("Report", new[] { "✓ Attack From Mars ROM found" });
        var text = Encoding.Latin1.GetString(bytes);
        Assert.Contains("OK Attack From Mars ROM found", text);
    }

    public static void Test_Build_Uses_Invariant_Culture_For_Numbers()
    {
        // A French culture ToString() would render "42,5" instead of "42.5" and corrupt every
        // coordinate/length in the content stream. Belt-and-braces regression guard.
        var original = System.Globalization.CultureInfo.CurrentCulture;
        try
        {
            System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.GetCultureInfo("fr-FR");
            var bytes = PdfDocumentBuilder.Build("Rapport", new[] { "une ligne" });
            var text = Encoding.Latin1.GetString(bytes);
            Assert.False(text.Contains(",5 Tf") || text.Contains(",28 "), "PDF numbers must use '.' regardless of thread culture");
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentCulture = original;
        }
    }

    public static void Test_Build_Empty_Report_Still_Produces_One_Valid_Page()
    {
        var bytes = PdfDocumentBuilder.Build("Empty Report", Array.Empty<string>());
        var text = Encoding.Latin1.GetString(bytes);
        Assert.Contains("%PDF-1.4", text);
        Assert.Contains("%%EOF", text);
        var pageObjects = System.Text.RegularExpressions.Regex.Matches(text, @"/Type\s*/Page(?!s)").Count;
        Assert.Equal(1, pageObjects);
    }
}
