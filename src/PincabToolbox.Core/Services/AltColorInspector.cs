namespace PincabToolbox.Core.Services;

/// <summary>
/// Pure decision for AltColor/Serum pair-completeness (audit §4/B1): given the lower-case file
/// extensions present in one ROM's <c>altcolor/&lt;rom&gt;/</c> folder, decides whether a full,
/// recognised colorization set is present.
///
/// <para>
/// Two independent complete forms exist side by side in the wild: the classic palette-based pair
/// (<c>.vni</c> + <c>.pal</c>) and the newer Serum pair (a <c>.cRZ</c> Serum file + <c>.pal</c>).
/// Complete when EITHER form is fully present — a table can be colorized with either engine.
/// </para>
///
/// <para>
/// An empty extension set is reported as "not complete" here, but the caller (the scanner) must
/// never surface that as a finding on its own: a ROM with zero colorization files simply never had
/// AltColor/Serum installed for it, which is a normal, unremarkable state for most ROMs — not a
/// defect. Only a folder that holds SOME colorization file yet still fails this check represents an
/// actual broken/partial install, and only the scanner has the context (folder had content or not)
/// to draw that line.
/// </para>
/// </summary>
public static class AltColorInspector
{
    public static bool IsComplete(IReadOnlyCollection<string> lowerCaseExtensions)
    {
        var hasPal = lowerCaseExtensions.Contains(".pal");
        var hasVni = lowerCaseExtensions.Contains(".vni");
        var hasSerum = lowerCaseExtensions.Contains(".crz");
        return (hasVni && hasPal) || (hasSerum && hasPal);
    }
}
