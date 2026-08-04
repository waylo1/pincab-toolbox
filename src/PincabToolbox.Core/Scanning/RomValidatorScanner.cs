using PincabToolbox.Core.Models;
using PincabToolbox.Core.Services;

namespace PincabToolbox.Core.Scanning;

/// <summary>
/// For every table: extracts the required ROM set from the script and checks the
/// roms folder (including VPMAlias resolution). 100% local — never downloads anything.
/// </summary>
public sealed class RomValidatorScanner : IScanner
{
    public string Id => "rom";
    public string Name => "ROM Validator";

    public IEnumerable<Finding> Scan(ScanContext ctx)
    {
        if (ctx.Layout.TablesDir is null)
        {
            yield return new Finding
            {
                Code = "TABLES_DIR_NOT_FOUND", Severity = Severity.Warning, Category = Id,
                EnglishText = "No tables folder found under the selected root — is this a Visual Pinball installation?",
            };
            yield break;
        }

        if (ctx.Layout.RomsDir is null)
        {
            yield return new Finding
            {
                Code = "ROMS_DIR_NOT_FOUND", Severity = Severity.Warning, Category = Id,
                EnglishText = "VPinMAME roms folder not found — ROM checks skipped.",
            };
            yield break;
        }

        foreach (var (path, table) in ctx.Tables)
        {
            ctx.Cancellation.ThrowIfCancellationRequested();
            var name = Path.GetFileNameWithoutExtension(path);

            if (table.Error is not null || table.Script is null)
            {
                yield return new Finding
                {
                    Code = "SCRIPT_UNREADABLE", Severity = Severity.Warning, Category = Id,
                    Subject = name, FilePath = path,
                    Args = new[] { name, table.Error ?? "no script found" },
                    EnglishText = $"Could not read the script of '{name}' ({table.Error ?? "no script found"}).",
                };
                continue;
            }

            var rom = ScriptAnalyzer.AnalyzeRomUsage(table.Script);

            // A VPinMAME ROM is required only when the script drives VPinMAME. A table that
            // names a game but merely opens a B2S backglass (originals/homebrew — Guardians of
            // the Galaxy, Harry Potter homebrew…) declares no VPinMAME ROM. Flagging those
            // ROM_MISSING/critical was the KPI#1 false positive (FIELD-LOG 2026-07-29/07-30).
            //
            // UsesController is the ONLY thing that opens ROM validation. The guard used to read
            // `!UsesController && !UsesB2S`, which let a B2S-only table into the lookup and relied
            // on a downstream `else if` to route it back out — so B2S was still, structurally, an
            // entry signal equivalent to the controller. It came back out labelled ROM_OK whenever
            // the named set happened to exist in the roms folder ("ROM found" for a table that
            // drives no ROM), and any future edit inside the lookup block could have reopened the
            // critical FP. The decision now lives in one place. (FIELD-LOG 2026-08-03.)
            if (!rom.UsesController || rom.Candidates.Count == 0)
            {
                yield return new Finding
                {
                    Code = "ROM_NOT_REQUIRED", Severity = Severity.Ok, Category = Id,
                    Subject = name, FilePath = path,
                    Args = new[] { name },
                    EnglishText = rom.UsesB2S
                        ? $"'{name}' uses a B2S backglass but does not drive VPinMAME — treated as an original/homebrew table (no ROM required)."
                        : $"'{name}' does not require a ROM (original/EM table).",
                };
                continue;
            }

            // A table is satisfied when ANY of its candidate ROM names resolves.
            var missing = new List<string>();
            string? satisfiedBy = null;
            foreach (var candidate in rom.Candidates)
            {
                if (ctx.RomSets.Contains(candidate)) { satisfiedBy = candidate; break; }
                if (ctx.Aliases.TryGetValue(candidate, out var target) && ctx.RomSets.Contains(target))
                {
                    satisfiedBy = $"{candidate} → {target} (alias)";
                    break;
                }
                missing.Add(candidate);
            }

            if (satisfiedBy is not null)
            {
                yield return new Finding
                {
                    Code = "ROM_OK", Severity = Severity.Ok, Category = Id,
                    Subject = name, FilePath = path,
                    Args = new[] { name, satisfiedBy },
                    EnglishText = $"'{name}' ROM found: {satisfiedBy}.",
                };
            }
            else
            {
                var primary = rom.Primary!;

                // Precise sub-case: the ROM is actually present, but as an unzipped folder.
                // VPinMAME loads ROMs from .zip archives, so an extracted folder won't be found —
                // a confusing "missing ROM" when the files are right there. Deterministic.
                var unzipped = rom.Candidates.FirstOrDefault(c =>
                    Directory.Exists(Path.Combine(ctx.Layout.RomsDir, c)));
                if (unzipped is not null)
                {
                    yield return new Finding
                    {
                        Code = "ROM_UNZIPPED", Severity = Severity.Warning, Category = Id,
                        Subject = name, FilePath = path,
                        Args = new[] { name, unzipped },
                        EnglishText = $"'{name}' ROM is present as an unzipped folder '{unzipped}' — VPinMAME loads ROMs from .zip archives, so it won't be found.",
                        FixHint = $"Compress the '{unzipped}' folder back into '{unzipped}.zip' inside the roms folder (do not zip an extra parent folder around it).",
                    };
                    continue;
                }

                yield return new Finding
                {
                    Code = "ROM_MISSING", Severity = Severity.Critical, Category = Id,
                    Subject = name, FilePath = path,
                    Args = new[] { name, primary + ".zip", string.Join(", ", missing) },
                    EnglishText = $"'{name}' will not start: ROM '{primary}.zip' is missing from the roms folder" +
                                  (missing.Count > 1 ? $" (alternatives also missing: {string.Join(", ", missing.Skip(1))})" : "") + ".",
                    FixHint = $"Place '{primary}.zip' into {ctx.Layout.RomsDir} (exact file name, keep it zipped).",
                };
            }
        }
    }
}
