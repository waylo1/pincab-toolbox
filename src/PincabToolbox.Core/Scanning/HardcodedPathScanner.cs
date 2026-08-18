using PincabToolbox.Core.Models;
using PincabToolbox.Core.Services;

namespace PincabToolbox.Core.Scanning;

/// <summary>
/// A3 — Hardcoded-Path Linter (session prompt 18/08). Flags absolute, quoted Windows paths a table
/// script literally names (<see cref="HardcodedPathExtractor"/>) when the file they point to does
/// not exist on this machine — a table author's script that loads
/// <c>"C:\Users\someone-else\Sounds\click.wav"</c> works fine on their own PC and silently fails to
/// play/show that asset on anyone else's.
///
/// <para>
/// Real false-positive risk, called out explicitly in the handoff: the path COULD exist on this
/// exact machine (same username, coincidentally same folder layout) — checking
/// <see cref="File.Exists(string)"/> before ever flagging anything is exactly the mitigation for
/// that: a path that resolves here is, by definition, not a problem for this user, whoever wrote
/// the script. <see cref="Severity.Note"/> (ADR-010 Doctrine) either way — a genuinely absent
/// hardcoded path is a fact, not a guaranteed table failure (the referenced asset might be
/// decorative, or the script might already guard the load).
/// </para>
///
/// <para>
/// Summarized per table (one finding, not one per broken path) — the handoff's own instruction for
/// this item ("résumé par table, jamais une ligne par occurrence"), since a table can hard-code
/// several paths from the same foreign machine and a wall of near-duplicate findings would drown
/// the one useful signal.
/// </para>
/// </summary>
public sealed class HardcodedPathScanner : IScanner
{
    public string Id => "hardcoded-path";
    public string Name => "Hardcoded Path Linter";

    private readonly Func<string, bool> _fileExists;

    /// <param name="fileExists">Defaults to a real <see cref="File.Exists(string)"/>; injected in tests.</param>
    public HardcodedPathScanner(Func<string, bool>? fileExists = null)
        => _fileExists = fileExists ?? File.Exists;

    public IEnumerable<Finding> Scan(ScanContext ctx)
    {
        foreach (var (path, table) in ctx.Tables)
        {
            ctx.Cancellation.ThrowIfCancellationRequested();
            if (table.Script is null) continue;

            var candidates = HardcodedPathExtractor.ExtractAbsolutePaths(table.Script);
            if (candidates.Count == 0) continue;

            var broken = new List<string>();
            foreach (var p in candidates)
            {
                bool exists;
                try { exists = _fileExists(p); }
                catch { continue; } // unreadable → don't guess, skip this one path
                if (!exists) broken.Add(p);
            }
            if (broken.Count == 0) continue;

            var name = Path.GetFileNameWithoutExtension(path);
            yield return new Finding
            {
                Code = "SCRIPT_HARDCODED_PATH", Severity = Severity.Note, Category = Id,
                Subject = name, FilePath = path,
                Args = new[] { name, broken.Count.ToString(), broken[0] },
                EnglishText = $"'{name}' script references {broken.Count} absolute file path(s) that don't exist on this machine (e.g. \"{broken[0]}\") — these look hard-coded from another computer; anything the script loads from them will silently fail here.",
                FixHint = "Open the table's script and replace the hard-coded absolute path(s) with a path relative to the table, or copy the referenced file(s) to the exact path the script expects.",
            };
        }
    }
}
