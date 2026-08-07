using PincabToolbox.Core.Models;
using PincabToolbox.Core.Services;

namespace PincabToolbox.Core.Scanning;

/// <summary>
/// Audits each table's supporting assets: backglass (.directb2s), PinUP Popper
/// database entry, and PUP-Pack folder. Read-only (SQLite opened in ReadOnly mode).
/// </summary>
public sealed class CompletenessScanner : IScanner
{
    public string Id => "completeness";
    public string Name => "Install Auditor";

    public IEnumerable<Finding> Scan(ScanContext ctx)
    {
        if (ctx.Layout.TablesDir is null) yield break;

        var popperGames = LoadPopperGames(ctx.Layout.PupDatabasePath);

        foreach (var (path, table) in ctx.Tables)
        {
            ctx.Cancellation.ThrowIfCancellationRequested();
            var baseName = Path.GetFileNameWithoutExtension(path);

            // Backglass
            var b2s = Path.Combine(ctx.Layout.TablesDir, baseName + ".directb2s");
            if (!File.Exists(b2s))
            {
                yield return new Finding
                {
                    Code = "B2S_MISSING", Severity = Severity.Warning, Category = Id,
                    Subject = baseName, FilePath = path,
                    Args = new[] { baseName },
                    EnglishText = $"'{baseName}' has no .directb2s backglass file next to the table.",
                    FixHint = "If you use a backglass screen, download the matching .directb2s and place it in the tables folder with the exact same base name.",
                };
            }

            // Popper registration
            if (popperGames is not null)
            {
                bool known = popperGames.Contains(baseName);
                if (!known)
                {
                    yield return new Finding
                    {
                        Code = "POPPER_NOT_REGISTERED", Severity = Severity.Info, Category = Id,
                        Subject = baseName, FilePath = path,
                        Args = new[] { baseName },
                        EnglishText = $"'{baseName}' is not registered in PinUP Popper — it will not appear in the frontend.",
                    };
                }
            }

            // PUP-Pack (keyed by ROM name)
            if (ctx.Layout.PupVideosDir is not null && table.Script is not null)
            {
                var rom = ScriptAnalyzer.AnalyzeRomUsage(table.Script);
                if (rom.UsesController && rom.Primary is not null)
                {
                    var pupDir = Path.Combine(ctx.Layout.PupVideosDir, rom.Primary);
                    bool hasPup = Directory.Exists(pupDir) && Directory.EnumerateFileSystemEntries(pupDir).Any();
                    if (hasPup)
                    {
                        yield return new Finding
                        {
                            Code = "PUPPACK_PRESENT", Severity = Severity.Ok, Category = Id,
                            Subject = baseName,
                            Args = new[] { baseName, rom.Primary },
                            EnglishText = $"'{baseName}' has a PUP-Pack ({rom.Primary}).",
                        };
                    }
                }
            }
        }

        // Orphan / misnamed backglasses: B2S loads a .directb2s only when its base name matches
        // the .vpx exactly. A stray file (typo, leftover from a removed table) is a very common
        // reason a backglass "doesn't show" while the file is right there. Deterministic — we only
        // report files that match NO table, so there are no false positives.
        var tableNames = new HashSet<string>(
            ctx.Tables.Keys.Select(Path.GetFileNameWithoutExtension)!,
            StringComparer.OrdinalIgnoreCase);
        IEnumerable<string> b2sFiles;
        try { b2sFiles = Directory.EnumerateFiles(ctx.Layout.TablesDir, "*.directb2s", SearchOption.TopDirectoryOnly); }
        catch { b2sFiles = Array.Empty<string>(); }
        foreach (var file in b2sFiles)
        {
            ctx.Cancellation.ThrowIfCancellationRequested();
            var b2sName = Path.GetFileNameWithoutExtension(file);
            if (tableNames.Contains(b2sName)) continue;
            yield return new Finding
            {
                Code = "B2S_ORPHAN", Severity = Severity.Info, Category = Id,
                Subject = b2sName, FilePath = file,
                Args = new[] { b2sName },
                EnglishText = $"Backglass '{b2sName}.directb2s' has no table with a matching name — " +
                              "B2S loads backglasses by exact base name, so this one is ignored.",
                FixHint = "If this backglass belongs to a table, rename it to the table's exact base name (matching the .vpx). Otherwise it is a leftover you can remove.",
            };
        }

        // Frontend media: a registered game with no wheel image looks blank in the PinUP Popper
        // wheel. Only runs when a POPMedia folder exists (installs without media configured aren't
        // spammed) and is summarized into a single finding to avoid flooding a big collection.
        if (ctx.Layout.PopMediaDir is not null)
        {
            var gameNames = LoadPopperGameNames(ctx.Layout.PupDatabasePath);
            if (gameNames is not null && gameNames.Count > 0)
            {
                var wheelStems = CollectWheelStems(ctx.Layout.PopMediaDir);
                var missingWheel = gameNames
                    .Where(g => !wheelStems.Contains(g))
                    .OrderBy(g => g, StringComparer.OrdinalIgnoreCase)
                    .ToList();
                if (missingWheel.Count > 0)
                {
                    yield return new Finding
                    {
                        Code = "POPPER_MEDIA_MISSING", Severity = Severity.Info, Category = Id,
                        Subject = missingWheel.Count == 1 ? missingWheel[0] : $"{missingWheel.Count} games",
                        Args = new[] { missingWheel.Count.ToString(), gameNames.Count.ToString(), string.Join(", ", missingWheel.Take(8)) },
                        EnglishText = $"{missingWheel.Count} of {gameNames.Count} registered game(s) have no wheel image under POPMedia — they will look blank in the PinUP Popper wheel.",
                        FixHint = "Add a wheel image named exactly like the game (its Popper GameName) under POPMedia\\<emulator>\\Wheel, or re-run the Popper media import.",
                    };
                }
            }
        }

        if (ctx.Layout.PupDatabasePath is null)
        {
            yield return new Finding
            {
                Code = "POPPER_DB_NOT_FOUND", Severity = Severity.Info, Category = Id,
                EnglishText = "PinUP Popper database not found — frontend checks skipped.",
            };
        }
    }

    /// <summary>The distinct GameName values from the Popper Games table (canonical media keys).</summary>
    private static List<string>? LoadPopperGameNames(string? dbPath)
    {
        if (dbPath is null || !File.Exists(dbPath)) return null;
        var rows = SqliteReader.TryReadTable(dbPath, "Games", "GameName");
        if (rows is null) return null;

        var names = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in rows)
        {
            var v = row.Length > 0 ? row[0] : null;
            if (!string.IsNullOrWhiteSpace(v) && seen.Add(v)) names.Add(v);
        }
        return names;
    }

    /// <summary>File-name stems of every file living under any "Wheel" folder in the media tree.</summary>
    private static HashSet<string> CollectWheelStems(string popMediaDir)
    {
        var stems = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Same lazy-enumeration trap as BlockedFileScanner: a single
        // Directory.EnumerateDirectories(..., AllDirectories) is deferred, so wrapping only the
        // call in try/catch does not protect the foreach that actually walks the tree. Walk
        // directory-by-directory via LayoutDetector.SafeEnumerateDirs instead, each guarded by
        // its own try/catch, so one unreadable subtree is skipped rather than failing the whole
        // scanner.
        foreach (var dir in LayoutDetector.SafeEnumerateDirs(popMediaDir, int.MaxValue))
        {
            if (!string.Equals(Path.GetFileName(dir), "Wheel", StringComparison.OrdinalIgnoreCase))
                continue;

            string[] files;
            try { files = Directory.GetFiles(dir); }
            catch { continue; }
            foreach (var f in files) stems.Add(Path.GetFileNameWithoutExtension(f));
        }
        return stems;
    }

    /// <summary>Reads GameName + GameFileName from PUPDatabase.db via the built-in read-only SQLite reader.</summary>
    private static HashSet<string>? LoadPopperGames(string? dbPath)
    {
        if (dbPath is null || !File.Exists(dbPath)) return null;
        var rows = SqliteReader.TryReadTable(dbPath, "Games", "GameName", "GameFileName");
        if (rows is null) return null;

        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in rows)
        {
            foreach (var v in row)
            {
                if (!string.IsNullOrEmpty(v))
                {
                    names.Add(v);
                    names.Add(Path.GetFileNameWithoutExtension(v));
                }
            }
        }
        return names;
    }
}
