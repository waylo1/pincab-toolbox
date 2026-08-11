using PincabToolbox.Core.Models;

namespace PincabToolbox.Repair;

/// <summary>Read-only context handed to plan computation. No writes happen here.</summary>
public sealed record RepairContext
{
    /// <summary>Detected install roots. Used for containment (ADR-005).</summary>
    public required IReadOnlyList<string> InstallRoots { get; init; }

    public required Finding Finding { get; init; }

    public InstallLayout? Layout { get; init; }
}

/// <summary>
/// A repair CAPABILITY — code, held in a closed registry.
/// The Knowledge Pack may compose these; it may never define a new one (ADR-005).
/// </summary>
public interface IRepairAction
{
    string ActionId { get; }
    ChangeKind Kind { get; }

    /// <summary>
    /// Technical truth about reversibility. Overrides what the pack declares:
    /// if the action says no, the rule cannot say yes.
    /// </summary>
    bool IsReversibleByNature { get; }

    ValidationResult ValidateParameters(IReadOnlyDictionary<string, string> parameters);

    /// <summary>
    /// PURE — no side effect. This is the dry-run. The same PlannedChange is later
    /// consumed by Execute, so the preview can never diverge from the act.
    /// </summary>
    IReadOnlyList<PlannedChange> Plan(RepairContext context,
                                      IReadOnlyDictionary<string, string> parameters);

    /// <summary>
    /// Is the finding STILL true? Called at preflight, right before writing.
    /// A scan is a snapshot; the world may have moved.
    /// </summary>
    bool StillApplies(RepairContext context);

    ExecutionResult Execute(PlannedChange change);
    ExecutionResult Revert(PlannedChange change);
}

/// <summary>
/// CLOSED registry of capabilities, populated at compile time only.
/// An unknown ActionId makes the rule fall back to ManualOnly, without a noisy error.
/// </summary>
public interface IRepairActionRegistry
{
    bool TryGet(string actionId, out IRepairAction action);
    IReadOnlyCollection<string> KnownActionIds { get; }
}

public sealed class RepairActionRegistry : IRepairActionRegistry
{
    private readonly Dictionary<string, IRepairAction> _actions = new(StringComparer.Ordinal);

    public RepairActionRegistry(params IRepairAction[] actions)
    {
        foreach (var a in actions) _actions[a.ActionId] = a;
    }

    public bool TryGet(string actionId, out IRepairAction action)
        => _actions.TryGetValue(actionId, out action!);

    public IReadOnlyCollection<string> KnownActionIds => _actions.Keys;
}

/// <summary>
/// Everything the engine needs to know about the outside world, behind an interface.
/// Without this the preflight is untestable — you would have to really launch VPX
/// and really fill a disk.
/// </summary>
public interface IEnvironmentProbe
{
    /// <summary>Processes that forbid writing: VPinballX, PinUpPlayer, PinUpMenu, VPinMAME…</summary>
    IReadOnlyList<string> RunningBlockingProcesses();

    /// <summary>Bytes free on the volume that will hold the backup.</summary>
    long FreeBackupSpaceBytes();

    bool CanWriteTo(string target);
}

/// <summary>Injectable clock — otherwise journal entries are not verifiable in tests.</summary>
public interface ISystemClock
{
    DateTimeOffset UtcNow { get; }
}

public sealed class SystemClock : ISystemClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}

/// <summary>
/// Minimal process control surface. Exists so actions that terminate a stray process
/// (<see cref="Actions.KillZombiePinUpDisplayAction"/>) stay testable without really killing
/// anything, and so the engine can be exercised on any OS.
/// </summary>
public interface IProcessControl
{
    /// <summary>True if a process with this name (no extension) is currently running.</summary>
    bool IsRunning(string processName);

    /// <summary>Full executable path of the first running process with this name, or null.</summary>
    string? PathOf(string processName);

    /// <summary>
    /// Attempts to terminate every running process with this name. Returns true when none of
    /// them are running afterwards (including when none were running to begin with).
    /// </summary>
    bool Kill(string processName);
}

/// <summary>
/// Minimal Windows default-playback-device surface, behind an interface for the same reason as
/// every other system-touching abstraction here: <see cref="Actions.SetDefaultAudioDeviceAction"/>
/// must be testable without touching real audio hardware.
/// </summary>
public interface IAudioDeviceControl
{
    /// <summary>Id of the current default playback device, or null if unknown.</summary>
    string? GetDefaultPlaybackDeviceId();

    /// <summary>
    /// First playback device whose name contains <paramref name="nameContains"/>
    /// (case-insensitive), or null if none matches.
    /// </summary>
    string? FindPlaybackDeviceId(string nameContains);

    /// <summary>Sets the default playback device. Returns false on any failure (never throws).</summary>
    bool SetDefaultPlaybackDevice(string deviceId);
}

/// <summary>
/// Result of a single, argument-free launch of an external executable
/// (<see cref="Actions.RegisterComComponentAction"/>, LOT I). Deliberately reports only whether
/// the process STARTED and whether it returned within the timeout — never its stdout/exit-code
/// semantics, which are tool-specific and not something this engine can interpret safely.
/// </summary>
public sealed record ProcessLaunchResult(bool Started, bool TimedOut, int? ExitCode, string? Error)
{
    public static ProcessLaunchResult Ok(int exitCode) => new(true, false, exitCode, null);
    public static ProcessLaunchResult TimedOutResult() => new(true, true, null, null);
    public static ProcessLaunchResult Failed(string error) => new(false, false, null, error);
}

/// <summary>
/// LOT I (spec 10/08) — the ONLY way this project ever runs a foreign executable. Deliberately
/// narrow: one path in, zero arguments, one timeout out. No shell, no <c>cmd /c</c>, no string
/// interpolation of a command line — <see cref="RealProcessLauncher"/> calls
/// <c>Process.Start</c> directly on the exact path handed to it. Confinement (whitelist +
/// canonical-path containment) is the CALLER's job (<see cref="Actions.RegisterComComponentAction"/>);
/// this interface exists only so that logic is testable without ever actually spawning a process.
/// </summary>
public interface IProcessLauncher
{
    ProcessLaunchResult Launch(string exePath, TimeSpan timeout);
}

/// <summary>
/// Whether THIS process currently holds an elevated (administrator) token — checked at the moment
/// of use, never assumed from the static <c>app.manifest</c> (which requests <c>asInvoker</c>: the
/// app never auto-elevates, but a user can still right-click "Run as administrator" by hand, which
/// this interface must be able to see). LOT I rule 6: a repair that needs admin rights must say so
/// plainly and never attempt a surprise elevation.
/// </summary>
public interface IElevationProbe
{
    bool IsCurrentProcessElevated();
}

/// <summary>
/// Minimal file surface used by actions. Exists so actions stay testable without
/// touching a real disk — and so the engine can be exercised on any OS.
/// </summary>
public interface IFileSystem
{
    bool FileExists(string path);
    bool DirectoryExists(string path);
    IReadOnlyList<string> GetFiles(string directory);
    IReadOnlyList<string> GetDirectories(string directory);

    byte[] ReadAllBytes(string path);
    void WriteAllBytes(string path, byte[] content);

    void DeleteFile(string path);
    void MoveFile(string source, string destination);
    void MoveDirectory(string source, string destination);
    void CreateDirectory(string path);

    /// <summary>Windows alternate data stream (Mark of the Web). False on non-Windows.</summary>
    bool HasZoneIdentifier(string path);
    void RemoveZoneIdentifier(string path);
    void AddZoneIdentifier(string path);
}
