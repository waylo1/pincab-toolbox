namespace PincabToolbox.Core.Services;

/// <summary>
/// Pure parsing + geometry for the B2S Backglass Server's <c>ScreenRes.txt</c> / <c>&lt;table&gt;.res</c>
/// file format (audit §4/C1, handoff §3/C1) — decides whether a declared backglass position actually
/// lands on a connected monitor.
///
/// <para>
/// <b>Format verified against primary sources before writing a single line of parsing code</b> (the
/// shipped <c>ScreenResTemplate.txt</c>, the official vpinball/b2s-backglass wiki, and its Changelog —
/// not guessed): a plain-text file, one value per non-comment line (<c>#</c> to end of line), where
/// lines 1-2 are the playfield resolution, 3-4 the backglass width/height, 5 the screen selector, 6-7
/// the backglass X/Y **relative to that screen's own top-left corner**. Lines 8 onward cover the DMD
/// and an optional "Background" image and are out of this scanner's scope — see the two deliberate
/// scope cuts below.
/// </para>
///
/// <para>
/// <b>Scope cut #1 — pre-2.0.0 files without a <c>#&#160;V2</c> marker are refused outright.</b> The
/// format has a real, documented landmine: on an old file, when the B2S "Background" display option is
/// turned on, the Backglass block (lines 3-4/6-7) and the Background block (lines 13-16) **silently
/// swap meaning** — and nothing in the file itself says which case applies (that flag lives in a
/// different file, <c>B2STableSettings.xml</c>'s <c>StartBackground</c>, and even that is a global
/// default, not reliably per-table). B2S Server 2.0.0 fixed this by writing a <c># V2...</c> comment
/// line (anywhere in the file, not necessarily first) to pin the meaning going forward. Rather than
/// cross-reference a second file to resolve an ambiguity that may not even be resolvable per-table,
/// this parser trusts ONLY files carrying that marker and returns null for everything else — the exact
/// "biais silence" this whole session has applied to every other scanner, just applied one file-format
/// version earlier than usual.
/// </para>
///
/// <para>
/// <b>Scope cut #2 — DMD position (lines 10-11) is not checked, only the backglass.</b> Research
/// surfaced a genuine, unresolved conflict between the documented origin for these two fields
/// ("relative to the backglass screen") and the only real worked example available, which only
/// produces a sane on-screen position if read as relative to the **backglass window's own position**
/// instead. Two independent sources never agreed, so encoding either reading as fact would risk
/// exactly the false positive this whole handoff is built to avoid. Backglass position alone (lines
/// 6-7) has no such conflict — template, wiki prose, and worked examples all agree — so it's the only
/// geometry this analyzer trusts. Logged in FIELD-LOG, not silently dropped.
/// </para>
///
/// <para>
/// <b>Scope cut #3 — <c>B2STableSettings.xml</c> is not read by this Doctor at all</b> (contrary to the
/// handoff's original assumption that it also carries position data). Every real example available —
/// two independent per-table fragments and one full file, across two different wiki pages — is
/// exclusively toggles (log flags, skip-frame counts, DualMode, HideXxx); none carry x/y/width/height.
/// All backglass geometry lives in ScreenRes.txt/.res. Acting on verified reality rather than the
/// original (incorrect) premise, consistent with how B2's altsound.csv schema was verified rather than
/// assumed earlier this session.
/// </para>
/// </summary>
public static class ScreenTopologyAnalyzer
{
    /// <summary>A parsed, not-yet-resolved backglass placement from one ScreenRes.txt/.res file.</summary>
    /// <param name="Selector">Line 5 verbatim: a bare integer (\\.\DISPLAYn), "@NNNN" (absolute X), or "=N" (Nth monitor left-to-right, 1-based).</param>
    /// <param name="X">Backglass X, relative to the selected screen's own top-left corner.</param>
    /// <param name="Y">Backglass Y, relative to the selected screen's own top-left corner.</param>
    public readonly record struct BackglassPlacement(string Selector, int X, int Y, int Width, int Height);

    /// <summary>
    /// Parses lines 1-7 of a ScreenRes.txt/.res file. Returns null unless an explicit "# V2" marker is
    /// present somewhere in the file (scope cut #1), the file has fewer than 7 non-comment/non-blank
    /// lines, any of the 4 required numeric fields fails to parse, or the declared size is degenerate
    /// (width or height &lt;= 0) — every case biased to silence rather than a guessed defect.
    /// </summary>
    public static BackglassPlacement? ParseBackglassPlacement(string text)
    {
        var lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');

        var hasV2Marker = lines.Any(l => l.TrimStart().StartsWith("# V2", StringComparison.OrdinalIgnoreCase));
        if (!hasV2Marker) return null;

        var values = new List<string>(7);
        foreach (var raw in lines)
        {
            var line = raw.Trim();
            if (line.Length == 0) continue;
            if (line.StartsWith("#", StringComparison.Ordinal)) continue;
            values.Add(line);
            if (values.Count == 7) break; // only lines 1-7 matter here (through Backglass X/Y)
        }
        if (values.Count < 7) return null;

        if (!int.TryParse(values[2], out var width) || width <= 0) return null;
        if (!int.TryParse(values[3], out var height) || height <= 0) return null;
        var selector = values[4].Trim();
        if (selector.Length == 0) return null;
        if (!int.TryParse(values[5], out var x)) return null;
        if (!int.TryParse(values[6], out var y)) return null;

        return new BackglassPlacement(selector, x, y, width, height);
    }

    /// <summary>
    /// Resolves a ScreenRes.txt screen selector against real enumerated monitors. Returns null on any
    /// unresolvable selector (unknown syntax, no matching monitor, out-of-range index) rather than
    /// guessing — an unresolvable selector means the geometry check below cannot run honestly.
    /// </summary>
    public static MonitorRect? ResolveScreen(string selector, IReadOnlyList<MonitorRect> monitors)
    {
        if (monitors.Count == 0) return null;
        selector = selector.Trim();

        if (selector.StartsWith("@", StringComparison.Ordinal))
        {
            if (!int.TryParse(selector.AsSpan(1), out var absX)) return null;
            foreach (var m in monitors) if (m.X == absX) return m;
            return null;
        }

        if (selector.StartsWith("=", StringComparison.Ordinal))
        {
            if (!int.TryParse(selector.AsSpan(1), out var index) || index < 1) return null;
            var sorted = monitors.OrderBy(m => m.X).ToList();
            return index <= sorted.Count ? sorted[index - 1] : null;
        }

        if (int.TryParse(selector, out var deviceNum))
        {
            foreach (var m in monitors)
            {
                if (ExtractTrailingDigits(m.DeviceName) == deviceNum) return m;
            }
        }

        return null;
    }

    /// <summary>
    /// True when the given rectangle (already resolved to absolute virtual-desktop coordinates) has
    /// zero overlap with every connected monitor — it would render fully invisible. Any overlap at
    /// all counts as visible: this deliberately never judges "on the wrong monitor" or "partially
    /// clipped", only total invisibility (handoff R3 — strict scope to the deterministic).
    /// </summary>
    public static bool IsOffScreen(int x, int y, int width, int height, IReadOnlyList<MonitorRect> monitors)
    {
        foreach (var m in monitors)
        {
            var overlapsX = x < m.X + m.Width && x + width > m.X;
            var overlapsY = y < m.Y + m.Height && y + height > m.Y;
            if (overlapsX && overlapsY) return false;
        }
        return true;
    }

    private static int? ExtractTrailingDigits(string deviceName)
    {
        var i = deviceName.Length;
        while (i > 0 && char.IsDigit(deviceName[i - 1])) i--;
        if (i == deviceName.Length) return null;
        return int.Parse(deviceName[i..]);
    }
}
