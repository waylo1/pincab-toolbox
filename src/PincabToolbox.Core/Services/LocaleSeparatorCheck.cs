namespace PincabToolbox.Core.Services;

/// <summary>
/// Pure decision: is a decimal separator string something other than "." — the separator VPX
/// table scripts and physics parsing generally assume. Audit §4-G1: a known FR-market pain point
/// (Windows' French locale uses "," as the decimal separator).
///
/// <para>
/// Reads via <see cref="System.Globalization.CultureInfo.CurrentCulture"/> (see
/// <c>LocaleSeparatorScanner</c>) rather than the audit fiche's literal suggestion of a direct
/// HKCU\Control Panel\International\sDecimal registry read — CultureInfo reflects the exact same
/// effective, live fact (what separator the running process actually uses to parse/format
/// numbers) through pure BCL, no P/Invoke needed, and is simpler and safer. A deliberate, logged
/// deviation from the fiche's wording (FIELD-LOG, 06/08) — not a change of what is being checked.
/// </para>
/// </summary>
public static class LocaleSeparatorCheck
{
    public static bool IsNonStandard(string? decimalSeparator) =>
        !string.IsNullOrEmpty(decimalSeparator) && decimalSeparator != ".";
}
