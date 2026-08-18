namespace PincabToolbox.Core.Services;

/// <summary>
/// Pure name-matching for the A1 "Script Doctor" detection (session prompt 18/08, narrowed scope:
/// presence only — no version extraction/comparison, no fix). Copies of these VPX/VPinMAME shared
/// scripts sometimes get dropped directly into <c>Tables/</c> (a table author's zip bundled its
/// own copy, or a user extracted a table pack that included one) and, because VPX resolves a
/// shared script from wherever it finds one first, a local copy there silently shadows the
/// global/shared one for every table in the folder — not just the one it came with.
///
/// <para>
/// Deliberately narrow: only the four names the audit's terrain evidence names
/// (docs/AUDIT-Scanner-2026-08.md §A1, docs/HANDOFF-Sonnet5-scanners-2026-08.md). Whether a given
/// local copy is a problem depends on its version relative to the global one — that judgment is
/// explicitly out of scope for this session (ADR pending on the OSS-providable fix, per R3-e of the
/// handoff); this only states the fact that a local copy exists, at <c>Severity.Note</c>.
/// </para>
/// </summary>
public static class SharedScriptDetector
{
    public static readonly IReadOnlyList<string> KnownSharedScripts = new[]
    {
        "core.vbs", "controller.vbs", "VPMKeys.vbs", "nudge.vbs",
    };

    /// <summary>Case-insensitive match against the known shared-script file names.</summary>
    public static bool IsKnownSharedScript(string fileName) =>
        KnownSharedScripts.Any(n => string.Equals(n, fileName, StringComparison.OrdinalIgnoreCase));
}
