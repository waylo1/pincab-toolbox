using System.Globalization;
using PincabToolbox.Core.Models;
using PincabToolbox.Core.Services;

namespace PincabToolbox.Core.Scanning;

/// <summary>
/// Notes when the current culture's decimal separator isn't "." — audit §4-G1, a documented
/// FR-market pain point (Windows French locale defaults to ","). <see cref="Severity.Note"/>
/// (ADR-010 Doctrine): whether this actually breaks a given script/config depends on how that
/// script parses numbers, which this scan does not verify per-table — this states the OS-level
/// fact only.
/// </summary>
public sealed class LocaleSeparatorScanner : IScanner
{
    public string Id => "locale-separator";
    public string Name => "Decimal Separator";

    private readonly Func<string?> _getDecimalSeparator;

    /// <param name="getDecimalSeparator">Returns the current decimal separator. Defaults to CultureInfo.CurrentCulture.</param>
    public LocaleSeparatorScanner(Func<string?>? getDecimalSeparator = null)
    {
        _getDecimalSeparator = getDecimalSeparator ?? (() => CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator);
    }

    public IEnumerable<Finding> Scan(ScanContext ctx)
    {
        ctx.Cancellation.ThrowIfCancellationRequested();

        string? sep;
        try { sep = _getDecimalSeparator(); }
        catch { return Array.Empty<Finding>(); }

        if (!LocaleSeparatorCheck.IsNonStandard(sep)) return Array.Empty<Finding>();

        return new[]
        {
            new Finding
            {
                Code = "LOCALE_DECIMAL_SEPARATOR", Severity = Severity.Note, Category = Id,
                Subject = sep!,
                Args = new[] { sep! },
                EnglishText = $"This Windows user's decimal separator is '{sep}' rather than '.'. Some VPX table scripts and physics/config parsing assume a dot, and can misbehave under a comma-decimal locale — a known pain point for French-language Windows installs.",
                FixHint = "In Windows Region settings, you can set 'Decimal symbol' to '.' under additional number formatting — some pincab owners run their cab account with English (United States) number formatting specifically to avoid this class of issue.",
            }
        };
    }
}
