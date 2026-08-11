using PincabToolbox.Core.Services;

namespace PincabToolbox.Repair.Actions;

/// <summary>
/// LOT I (spec 10/08) — launches a component's OWN registration tool to address
/// <c>COM_NOT_REGISTERED</c>, <c>VPINMAME_NOT_REGISTERED</c> or <c>COM_BITNESS_GAP</c>
/// (<see cref="Core.Scanning.ComHealthScanner"/>). Decision D-2: this project never writes the
/// Windows registry itself — it runs the registration tool the component already ships with.
///
/// <para>
/// Running a foreign executable is a class of capability this project has never had before, and
/// the spec holds it to seven mandatory rules (§5 LOT I), each enforced by a specific piece of this
/// class — see the comment on each member. <b>Rule 1</b> Plan()'s hardcoded whitelist.
/// <b>Rule 2</b> Plan()'s canonical-path resolution, independently re-checked by the engine's own
/// containment gate (ADR-005) since <see cref="Kind"/> is not exempted from it.
/// <b>Rule 3</b> <see cref="IProcessLauncher"/> never accepts arguments — see its own header.
/// <b>Rule 4</b> Plan()'s <see cref="PeInspector"/> gate. <b>Rule 5</b> the launcher's mandatory
/// timeout. <b>Rule 6</b> Execute()'s elevation gate. <b>Rule 7</b> <see cref="IsReversibleByNature"/>.
/// </para>
///
/// <para>
/// <b>Deliberately NOT wired into any live Rule yet.</b> This class is fully built and unit-tested
/// for everything that does not require a real Windows machine, and is deliberately kept OUT of
/// <c>knowledge/pack-2026.08.json</c>'s <c>repairRules</c> — so <see cref="Engine.RepairEngine.Plan"/>
/// never produces a <see cref="PlannedChange"/> through it in production, regardless of whether it
/// sits in a <see cref="RepairActionRegistry"/> — until its two remaining unknowns are validated
/// for real, not assumed:
/// </para>
/// <list type="number">
/// <item>That the registration tool actually lives alongside the component's DLL, for all three
/// whitelisted tools, on a real install. <see cref="Core.Models.Finding.FilePath"/> for these three
/// codes is the component's DLL (see <c>ComHealthScanner</c>), not the tool — Plan() derives the
/// tool's expected path from that DLL's directory, an assumption nobody has confirmed on a real
/// cab.</item>
/// <item>How each tool actually behaves when launched with zero arguments. VPinMAME's own
/// <c>Setup.exe</c> in particular is known to be an interactive GUI installer, not a silent
/// registrar — "repair applied" would be a misleading claim if the user still has to click
/// something themselves inside a window this app just opened for them.</item>
/// </list>
/// <para>
/// Same precedent as <see cref="SetDefaultAudioDeviceAction"/>: built and tested, held out of the
/// closed registry's live wiring until proven on a real cab. See TRANSMISSION.md.
/// </para>
/// </summary>
public sealed class RegisterComComponentAction : IRepairAction
{
    /// <summary>
    /// Rule 1 — strict whitelist, hardcoded, NEVER built from scan data. Keyed by the exact ProgID
    /// <see cref="Core.Scanning.ComHealthScanner"/> already puts in <c>Finding.Subject</c> for
    /// every finding this action could ever apply to.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, string> ToolByProgId =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["FlexDMD.FlexDMD"] = "FlexDMDUI.exe",
            ["B2S.Server"] = "B2SBackglassServerRegisterApp.exe",
            ["VPinMAME.Controller"] = "Setup.exe",
        };

    /// <summary>Rule 5 — mandatory. See <see cref="Engine.RealProcessLauncher"/> for what "timeout" means here.</summary>
    public static readonly TimeSpan LaunchTimeout = TimeSpan.FromSeconds(20);

    private readonly IProcessLauncher _launcher;
    private readonly IElevationProbe _elevation;
    private readonly Func<string, bool> _fileExists;
    private readonly Func<string, string> _fullPath;
    private readonly Func<string, ComRegistryView, (bool Succeeded, ComRegistration? Registration)> _probe;

    public RegisterComComponentAction(
        IProcessLauncher launcher,
        IElevationProbe elevation,
        Func<string, bool>? fileExists = null,
        Func<string, string>? fullPath = null,
        Func<string, ComRegistryView, (bool, ComRegistration?)>? probe = null)
    {
        _launcher = launcher;
        _elevation = elevation;
        _fileExists = fileExists ?? File.Exists;
        _fullPath = fullPath ?? Path.GetFullPath;
        _probe = probe ?? ComRegistrationProbe.TryProbe;
    }

    public string ActionId => "register_com_component";
    public ChangeKind Kind => ChangeKind.ComReregistration;

    /// <summary>
    /// Rule 7 — always false. The prior registration cannot be reliably restored (it is often
    /// already broken or stale, which is usually WHY the finding fired in the first place).
    /// </summary>
    public bool IsReversibleByNature => false;

    public ValidationResult ValidateParameters(IReadOnlyDictionary<string, string> parameters) => ValidationResult.Ok;

    /// <summary>
    /// Rules 1, 2 and 4 — every one fail-closed. An unrecognised ProgID, a missing tool file, or a
    /// file that does not read back as a valid PE plans NOTHING: this action never guesses.
    /// </summary>
    public IReadOnlyList<PlannedChange> Plan(RepairContext ctx, IReadOnlyDictionary<string, string> parameters)
    {
        var progId = ctx.Finding.Subject;
        if (progId is null || !ToolByProgId.TryGetValue(progId, out var toolName))
            return Array.Empty<PlannedChange>();

        // Finding.FilePath is the component's DLL, not the registration tool (see class header,
        // unknown #1) — the tool is assumed to live in the same directory.
        var dllPath = ctx.Finding.FilePath;
        if (string.IsNullOrWhiteSpace(dllPath)) return Array.Empty<PlannedChange>();

        string toolPath;
        try
        {
            var dir = Path.GetDirectoryName(_fullPath(dllPath));
            if (string.IsNullOrEmpty(dir)) return Array.Empty<PlannedChange>();

            // Rule 2 — canonical absolute path, resolved HERE, before any check ever runs, so a
            // ".." folder name could not smuggle the tool outside the install. toolName is the
            // hardcoded literal from the whitelist above, never data from the scan. The engine's
            // own containment gate (ADR-005) independently re-checks this same path at Preflight —
            // ChangeKind.ComReregistration is not exempted from it (see RepairEngine.Preflight).
            toolPath = _fullPath(Path.Combine(dir, toolName));
        }
        catch { return Array.Empty<PlannedChange>(); }

        if (!_fileExists(toolPath)) return Array.Empty<PlannedChange>();

        // Rule 4 — must be a real, bitness-readable PE before it is ever considered launchable.
        if (PeInspector.GetBitness(toolPath) == Bitness.Unknown) return Array.Empty<PlannedChange>();

        return new[]
        {
            new PlannedChange
            {
                ActionId = ActionId,
                Kind = Kind,
                Target = toolPath,
                // Not restore data (rule 7) — a trace of what was observed, recorded by the
                // journal the same way every other change's Before/After is.
                Before = $"observed before repair: '{progId}' not resolving to a working registration",
                After = $"registration tool launched: {toolName}",
                Reversible = false,
            }
        };
    }

    /// <summary>
    /// Engine rule "a scan is a snapshot" — re-probes the SAME two registry views the scanner
    /// itself reads. Same silence-on-failure posture as
    /// <see cref="Core.Scanning.ComHealthScanner.EvaluateVpinmameNotRegistered"/>: if either probe
    /// cannot be trusted, this returns false, never true on a guess.
    /// </summary>
    public bool StillApplies(RepairContext ctx)
    {
        var progId = ctx.Finding.Subject;
        if (progId is null || !ToolByProgId.ContainsKey(progId)) return false;

        var (succ32, reg32) = SafeProbe(progId, ComRegistryView.Registry32);
        var (succ64, reg64) = SafeProbe(progId, ComRegistryView.Registry64);
        if (!succ32 || !succ64) return false;   // an unreadable registry says nothing, never "still broken"
        return reg32 is null && reg64 is null;
    }

    private (bool, ComRegistration?) SafeProbe(string progId, ComRegistryView view)
    {
        try { return _probe(progId, view); } catch { return (false, null); }
    }

    /// <summary>
    /// Rule 6 — elevation is checked HERE, at the moment of use, never assumed from
    /// <c>app.manifest</c> alone. Refuses cleanly with a message the UI can show verbatim, rather
    /// than attempting a launch that would fail deep inside the third-party tool with no clear
    /// signal why.
    /// </summary>
    public ExecutionResult Execute(PlannedChange change)
    {
        if (!_elevation.IsCurrentProcessElevated())
            return ExecutionResult.Fail(
                "this repair needs administrator rights — close Pincab Toolbox and reopen it via " +
                "\"Run as administrator\", then try again");

        var result = _launcher.Launch(change.Target, LaunchTimeout);
        if (!result.Started)
            return ExecutionResult.Fail(result.Error ?? "could not start the registration tool");

        // "Launched successfully" is the only claim this makes. Several whitelisted tools are
        // interactive GUI installers the user may still need to act inside (class header, unknown
        // #2) — this action cannot know whether the registration itself is now fixed. The next
        // scan's StillApplies() is the real verification, same as every other repair (Verify()).
        return ExecutionResult.Ok;
    }

    /// <summary>Rule 7 — matches <see cref="IsReversibleByNature"/>.</summary>
    public ExecutionResult Revert(PlannedChange change)
        => ExecutionResult.Fail("not reversible — the tool's own registration cannot be undone by this app; "
                               + "re-run its registration tool again if you need to point it elsewhere");
}
