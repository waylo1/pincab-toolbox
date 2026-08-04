using PincabToolbox.Core.Models;
using PincabToolbox.Core.Services;

namespace PincabToolbox.Core.Scanning;

/// <summary>
/// Detects the "PinUpDisplay.exe left running after a table closes" zombie reported on the
/// VPForums troubleshooting guide (FIELD-LOG 2026-07-29): the process stays alive and blocks the
/// next launch until it is killed by hand in Task Manager. Cheap and safe candidate for Repair
/// (terminate a stray process) — this scanner only reports it; RepairMode/Contracts is not
/// touched here.
///
/// A live-process check, not a file scan — matches <see cref="DiskSpaceScanner"/> in kind
/// (system state, not install content).
/// </summary>
public sealed class PinupDisplayZombieScanner : IScanner
{
    public string Id => "process";
    public string Name => "Stuck Processes";

    /// <summary>Any of these actually running means a table is active — not a zombie.</summary>
    public static readonly string[] ActiveTableProcessNames =
        { "VPinballX", "VPinballX64", "VPinballX_GL64" };

    public IEnumerable<Finding> Scan(ScanContext ctx)
    {
        var displayRunning = ProcessProbe.IsRunning("PinUpDisplay");
        if (!displayRunning) yield break;

        var tableRunning = ActiveTableProcessNames.Any(ProcessProbe.IsRunning);
        var finding = Evaluate(displayRunning, tableRunning, ProcessProbe.TryGetExecutablePath("PinUpDisplay"), Id);
        if (finding is not null) yield return finding;
    }

    /// <summary>Pure decision, testable without a real process list.</summary>
    public static Finding? Evaluate(bool displayRunning, bool tableRunning, string? exePath, string category)
    {
        if (!displayRunning || tableRunning) return null;

        return new Finding
        {
            Code = "PINUP_DISPLAY_ZOMBIE", Severity = Severity.Warning, Category = category,
            Subject = "PinUpDisplay.exe",
            FilePath = exePath,
            EnglishText = "PinUpDisplay.exe is still running with no table currently active — a leftover from a "
                        + "previous session. It can block the next table from launching until it is closed.",
            FixHint = "Close PinUpDisplay.exe from Task Manager before relaunching a table.",
        };
    }
}
