using System.Collections.Generic;
using System.Linq;
using PincabToolbox.Core.Models;

namespace PincabToolbox.Core.Diagnostics;

/// <summary>
/// Decision logic pulled out of MainWindow.xaml.cs's Brush-touching Build* methods so it can be
/// unit tested — started as a mini-slice of point 5 done early inside point 3 (TRANSMISSION 13/08),
/// moved here properly as part of point 5/6 itself (ADR-012: decision logic belongs in a testable
/// assembly, not App). MainWindow itself can't host this: it's one half of a partial class whose
/// other half is XAML-generated, so nothing inside it is testable in a sandbox without the Windows
/// Desktop SDK.
///
/// Every type here answers "what happened" (which finding won, what severity applies), never "how
/// does it look" — no Brush, no Loc text, no glyph. MainWindow.BuildChainRows/BuildTableRows call
/// these and do only the WPF-facing translation (Brush selection, localized text), which is a
/// couple of lines of table lookups with effectively no decision left in them to get wrong.
/// </summary>

// ---------------- causal chain (BuildChainRows) ----------------

/// <summary>One step of a scenario's causal chain, decided but not yet drawn. <see cref="IsCutPoint"/>
/// marks the FIRST good→bad transition — the same "✕→ in red, → everywhere else" rule the 11/08
/// mockup calls for, decided once here instead of inline in the loop that builds WPF rows.</summary>
public sealed record ChainRowPlan
{
    public required string Arrow { get; init; }
    public required bool IsCutPoint { get; init; }
    public required string Label { get; init; }
    public required string Status { get; init; }
    public required ChainTone Tone { get; init; }
}

public static class ChainRowPlanner
{
    public static List<ChainRowPlan> Plan(IReadOnlyList<ChainStepMatch> steps)
    {
        var rows = new List<ChainRowPlan>(steps.Count);
        for (var i = 0; i < steps.Count; i++)
        {
            var s = steps[i];
            var cut = i > 0 && steps[i - 1].Tone == ChainTone.Good && s.Tone == ChainTone.Bad;
            rows.Add(new ChainRowPlan
            {
                Arrow = i == 0 ? "" : cut ? "✕→" : "→",
                IsCutPoint = cut,
                Label = s.Label,
                Status = s.Status,
                Tone = s.Tone,
            });
        }
        return rows;
    }
}

// ---------------- "Tables analysées" columns (BuildTableRows) ----------------

public enum RomColumnStatus { Unknown, Ok, Missing, NotRequired, Unzipped }

/// <summary>Which of the (at most one) ROM_* findings for this table won, and the ROM name that
/// goes into the format string — the caller (MainWindow) owns the string/Brush, this owns the pick.</summary>
public sealed record RomColumnPlan
{
    public required RomColumnStatus Status { get; init; }
    public string? RomName { get; init; }
}

public enum B2sColumnStatus { Unknown, Present, Missing }

public sealed record B2sColumnPlan
{
    public required B2sColumnStatus Status { get; init; }
    /// <summary>Only meaningful when Status is Missing — the real measured severity of B2S_MISSING,
    /// never a guess (ADR-010).</summary>
    public Severity Severity { get; init; }
}

public enum FrontendColumnStatus { Unknown, Registered, NotRegistered }

public sealed record FrontendColumnPlan
{
    public required FrontendColumnStatus Status { get; init; }
    /// <summary>Only meaningful when Status is NotRegistered. Falls back to Info, never invented —
    /// the same "current pack is Info, not the mockup's alarming orange" call from 12/08.</summary>
    public Severity Severity { get; init; }
}

public static class TableRowPlanner
{
    private static readonly string[] RomCodes = { "ROM_OK", "ROM_MISSING", "ROM_NOT_REQUIRED", "ROM_UNZIPPED" };

    public static RomColumnPlan PlanRom(IReadOnlyList<Finding> tableFindings)
    {
        var f = tableFindings.FirstOrDefault(x => RomCodes.Contains(x.Code));
        if (f is null) return new RomColumnPlan { Status = RomColumnStatus.Unknown };

        var romName = f.Args.Count > 1 ? f.Args[1] : "";
        return f.Code switch
        {
            "ROM_OK" => new RomColumnPlan { Status = RomColumnStatus.Ok, RomName = romName },
            "ROM_MISSING" => new RomColumnPlan { Status = RomColumnStatus.Missing, RomName = romName },
            "ROM_NOT_REQUIRED" => new RomColumnPlan { Status = RomColumnStatus.NotRequired },
            _ => new RomColumnPlan { Status = RomColumnStatus.Unzipped },
        };
    }

    /// <summary>completenessFailed disables the whole column (a SCANNER_ERROR in the completeness
    /// category means B2S_MISSING itself could not be trusted for this scan) rather than reporting
    /// a false "present".</summary>
    public static B2sColumnPlan PlanB2s(IReadOnlyList<Finding> tableFindings, bool completenessFailed)
    {
        if (completenessFailed) return new B2sColumnPlan { Status = B2sColumnStatus.Unknown };

        var f = tableFindings.FirstOrDefault(x => x.Code == "B2S_MISSING");
        return f is null
            ? new B2sColumnPlan { Status = B2sColumnStatus.Present }
            : new B2sColumnPlan { Status = B2sColumnStatus.Missing, Severity = f.Severity };
    }

    /// <summary>popperRegistered null means the Popper database itself could not be read — Unknown,
    /// never a guessed Registered/NotRegistered (same "silence is not a measurement" rule as
    /// BuildComponentRows).</summary>
    public static FrontendColumnPlan PlanFrontend(string tableName, IReadOnlySet<string>? popperRegistered, IReadOnlyList<Finding> tableFindings)
    {
        if (popperRegistered is null) return new FrontendColumnPlan { Status = FrontendColumnStatus.Unknown };
        if (popperRegistered.Contains(tableName)) return new FrontendColumnPlan { Status = FrontendColumnStatus.Registered };

        var f = tableFindings.FirstOrDefault(x => x.Code == "POPPER_NOT_REGISTERED");
        return new FrontendColumnPlan { Status = FrontendColumnStatus.NotRegistered, Severity = f?.Severity ?? Severity.Info };
    }
}
