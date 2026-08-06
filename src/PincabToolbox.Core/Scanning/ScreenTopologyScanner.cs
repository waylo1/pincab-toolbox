using PincabToolbox.Core.Models;
using PincabToolbox.Core.Services;

namespace PincabToolbox.Core.Scanning;

/// <summary>
/// Flags a backglass window positioned entirely off every connected monitor — invisible even though
/// B2S Backglass Server loads it without error (audit §4/C1). Deliberately fills the gap
/// <see cref="DisplaySetupScanner"/> left on purpose: that scanner only ever counted connected
/// displays, because resolving actual window position requires parsing B2S's own ScreenRes.txt/.res
/// format, which is exactly what <see cref="ScreenTopologyAnalyzer"/> now does.
///
/// <para>
/// Checks two things, both via the same pure geometry: (1) the global <c>ScreenRes.txt</c> in the
/// tables folder, which applies to every table without its own override — evaluated once, not once
/// per table, so one broken shared file doesn't flood the report with duplicates; (2) each table's own
/// <c>&lt;table&gt;.res</c> override, when present, evaluated independently since it's genuinely
/// table-specific.
/// </para>
///
/// <para>
/// Silent whenever it cannot measure honestly: no monitors enumerable (non-Windows, API failure), no
/// ScreenRes/.res file, a file without the required <c># V2</c> marker (see
/// <see cref="ScreenTopologyAnalyzer"/> scope cut #1), or an unresolvable screen selector. See
/// <see cref="ScreenTopologyAnalyzer"/> for the full research trail and the two further scope cuts
/// (DMD position and B2STableSettings.xml both deliberately out of scope).
/// </para>
/// </summary>
public sealed class ScreenTopologyScanner : IScanner
{
    public string Id => "screentopology";
    public string Name => "Screen Topology";

    private readonly Func<string, string?> _readText;
    private readonly Func<IReadOnlyList<MonitorRect>?> _getMonitors;

    /// <param name="readText">Given a ScreenRes.txt/.res path, returns its text, or null when missing/unreadable. Defaults to a real file read.</param>
    /// <param name="getMonitors">Returns every connected monitor's rectangle, or null when unmeasurable. Defaults to <see cref="MonitorTopologyProbe.TryGetMonitorRects"/>.</param>
    public ScreenTopologyScanner(Func<string, string?>? readText = null, Func<IReadOnlyList<MonitorRect>?>? getMonitors = null)
    {
        _readText = readText ?? (p => File.Exists(p) ? File.ReadAllText(p) : null);
        _getMonitors = getMonitors ?? MonitorTopologyProbe.TryGetMonitorRects;
    }

    public IEnumerable<Finding> Scan(ScanContext ctx)
    {
        if (ctx.Layout.TablesDir is null) yield break;

        IReadOnlyList<MonitorRect>? monitors;
        try { monitors = _getMonitors(); } catch { monitors = null; }
        if (monitors is null || monitors.Count == 0) yield break; // can't measure honestly -> silence

        var globalPath = Path.Combine(ctx.Layout.TablesDir, "ScreenRes.txt");
        var globalFinding = Evaluate(globalPath, "ScreenRes.txt", monitors);
        if (globalFinding is not null) yield return globalFinding;

        foreach (var path in ctx.Tables.Keys.OrderBy(p => p, StringComparer.OrdinalIgnoreCase))
        {
            ctx.Cancellation.ThrowIfCancellationRequested();
            var baseName = Path.GetFileNameWithoutExtension(path);
            var resPath = Path.Combine(ctx.Layout.TablesDir, baseName + ".res");
            var finding = Evaluate(resPath, baseName, monitors);
            if (finding is not null) yield return finding;
        }
    }

    private Finding? Evaluate(string path, string subject, IReadOnlyList<MonitorRect> monitors)
    {
        string? text;
        try { text = _readText(path); } catch { return null; }
        if (text is null) return null;

        var placement = ScreenTopologyAnalyzer.ParseBackglassPlacement(text);
        if (placement is null) return null;

        var screen = ScreenTopologyAnalyzer.ResolveScreen(placement.Value.Selector, monitors);
        if (screen is null) return null;

        var absX = screen.Value.X + placement.Value.X;
        var absY = screen.Value.Y + placement.Value.Y;
        if (!ScreenTopologyAnalyzer.IsOffScreen(absX, absY, placement.Value.Width, placement.Value.Height, monitors))
            return null;

        return new Finding
        {
            Code = "DISPLAY_OFFSCREEN", Severity = Severity.Warning, Category = Id,
            Subject = subject, FilePath = path,
            Args = new[] { subject },
            EnglishText = $"The backglass position defined in '{Path.GetFileName(path)}' falls entirely outside every connected monitor — it will render invisible even though the file itself loads without error.",
            FixHint = "Re-run B2S_ScreenResIdentifier (or your ScreenRes editor) with all monitors connected in their normal cab layout, then re-save — a stale ScreenRes.txt/.res after a monitor or GPU change is the most common cause.",
        };
    }
}
