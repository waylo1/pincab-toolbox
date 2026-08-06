using PincabToolbox.Core.Models;
using PincabToolbox.Core.Services;

namespace PincabToolbox.Core.Scanning;

/// <summary>
/// Notes when Windows display scaling (DPI) for the current user is not the 100% baseline —
/// audit §4-C2: a known cause of a backglass or table window rendering truncated or offset on
/// some cabs, though whether THIS install is actually affected depends on factors this scan
/// cannot see (which renderer, per-app DPI overrides…) — hence <see cref="Severity.Note"/>
/// (ADR-010 Doctrine): state the reading, not a verdict.
/// </summary>
public sealed class DpiScalingScanner : IScanner
{
    public string Id => "dpi-scaling";
    public string Name => "DPI Scaling";

    private readonly Func<uint?> _getAppliedDpi;

    /// <param name="getAppliedDpi">Returns the current AppliedDPI registry reading, or null when unavailable. Defaults to a real registry read.</param>
    public DpiScalingScanner(Func<uint?>? getAppliedDpi = null)
    {
        _getAppliedDpi = getAppliedDpi ?? DpiRegistry.TryGetAppliedDpi;
    }

    public IEnumerable<Finding> Scan(ScanContext ctx)
    {
        ctx.Cancellation.ThrowIfCancellationRequested();

        uint? dpi;
        try { dpi = _getAppliedDpi(); }
        catch { return Array.Empty<Finding>(); }

        if (!DpiScalingEvaluator.IsNonStandard(dpi)) return Array.Empty<Finding>();

        var percent = DpiScalingEvaluator.Percent(dpi!.Value);
        return new[]
        {
            new Finding
            {
                Code = "DPI_SCALING_NONSTANDARD", Severity = Severity.Note, Category = Id,
                Subject = $"{percent}%",
                Args = new[] { percent.ToString() },
                EnglishText = $"Windows display scaling for this user is set to {percent}% rather than 100%. This is a known cause of a backglass or table window rendering truncated or offset on some cabs — worth checking your display if you notice that.",
                FixHint = "In Windows Display Settings, set 'Scale' back to 100% for the cab's monitors, or check that Visual Pinball / B2S are running DPI-aware if you keep scaling above 100%.",
            }
        };
    }
}
