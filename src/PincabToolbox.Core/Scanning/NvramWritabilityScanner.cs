using PincabToolbox.Core.Models;

namespace PincabToolbox.Core.Scanning;

/// <summary>
/// LOT G (spec 10/08) — a nvram folder Windows won't let VPinMAME write into produces a different
/// symptom than <see cref="NvramScanner"/>'s zero-byte files: scores and settings simply never save,
/// silently, table after table. Signal is real but thin (one direct citation in the research, P2 —
/// not rejected, per spec §3.0's "one real problem is still worth a Note").
///
/// <para>
/// A NEW scanner, deliberately — <see cref="NvramScanner"/> is not touched (spec §3.1 rule 5).
/// </para>
///
/// <para>
/// Tests with a REAL write (create then delete a temp file in the folder), never an ACL read — the
/// spec is explicit that Windows ACLs are too subtle to verdict reliably, while a write attempt is a
/// fact. If the folder itself can't be found, that is "don't know", not "not writable" — silence,
/// same as every other honesty-first check in this project.
/// </para>
///
/// <para>
/// Deliberately does not attempt any fix (no ACL changes) — the spec rejects that outright as a
/// repair (security-sensitive, hard to reverse). Detection only.
/// </para>
/// </summary>
public sealed class NvramWritabilityScanner : IScanner
{
    public string Id => "nvram-writable";
    public string Name => "NVRAM Folder Writability";

    private readonly Func<string, bool?> _canWrite;

    /// <param name="canWrite">
    /// Given the nvram folder path: true if a real write+delete succeeded, false if it genuinely
    /// failed, null when the folder doesn't exist or the test itself couldn't be attempted honestly.
    /// Defaults to a real disk write test.
    /// </param>
    public NvramWritabilityScanner(Func<string, bool?>? canWrite = null)
        => _canWrite = canWrite ?? DefaultCanWrite;

    public IEnumerable<Finding> Scan(ScanContext ctx)
    {
        if (ctx.Layout.VPinMameDir is null) yield break;
        var nvramDir = Path.Combine(ctx.Layout.VPinMameDir, "nvram");

        bool? writable;
        try { writable = _canWrite(nvramDir); }
        catch { writable = null; } // never let a probing failure become a false claim

        var finding = Evaluate(writable, nvramDir, Id);
        if (finding is not null) yield return finding;
    }

    /// <summary>Pure decision, testable without touching disk.</summary>
    public static Finding? Evaluate(bool? writable, string nvramDir, string category)
    {
        if (writable != false) return null; // true (writable) or null (couldn't determine) -> nothing to say

        return new Finding
        {
            Code = "NVRAM_FOLDER_NOT_WRITABLE", Severity = Severity.Warning, Category = category,
            Subject = "nvram", FilePath = nvramDir,
            EnglishText = "The VPinMAME nvram folder exists but a real write test to it failed — high scores and per-table settings will silently fail to save, table after table, with no error shown.",
            FixHint = "Check the nvram folder isn't marked read-only and that your Windows user account has write permission to it (right-click → Properties → Security).",
        };
    }

    private static bool? DefaultCanWrite(string dir)
    {
        if (!Directory.Exists(dir)) return null; // can't test what isn't there -> unknown, not "not writable"

        var probe = Path.Combine(dir, ".pincabtoolbox-write-test-" + Guid.NewGuid().ToString("N") + ".tmp");
        try
        {
            File.WriteAllText(probe, "x");
            return true;
        }
        catch
        {
            return false; // a real write genuinely failed — this is a fact, not a guess
        }
        finally
        {
            try { if (File.Exists(probe)) File.Delete(probe); } catch { /* best-effort cleanup only */ }
        }
    }
}
