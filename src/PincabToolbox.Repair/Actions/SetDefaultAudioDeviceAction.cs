namespace PincabToolbox.Repair.Actions;

/// <summary>
/// Sets the Windows default playback device to a caller-chosen device (matched by a substring
/// of its friendly name, e.g. "Speakers"). Answers the community fix for the default output
/// randomly resetting to an HDMI-connected display on boot (FIELD-LOG 2026-07-29, "Définir le
/// périphérique audio principal en ligne de commande" — community script used the third-party
/// NirCMD binary + a persistent Startup entry).
///
/// **Decision (2026-07-29, Maxime, see FIELD-LOG §2): on-demand only.** This action changes the
/// default device once, when Repair is run — it never installs a Startup script. If the device
/// resets again, the user re-runs Repair; nothing runs silently in the background.
///
/// **Not yet wired to a Finding.** Unlike the other actions, there is no reliable way to detect
/// statically that "the default device will reset" — it's a live runtime event, not something a
/// scan snapshot can see. The capability is coded, tested (against the abstraction) and ready,
/// but reaching it today would need a manual "Tools" trigger outside the Scan→Repair pipeline —
/// a UI decision left for Maxime, consistent with not wiring any Repair UI this session.
///
/// **Needs real-cab validation before release.** <see cref="RealAudioDeviceControl"/> talks to
/// Windows through the same undocumented `IPolicyConfig` COM interface NirCMD itself uses (there
/// is no public Win32 API for this) — well-established from Vista through Windows 10, but not
/// verifiable in a sandbox and not guaranteed on every Windows 11 build. See TRANSMISSION.md.
///
/// Reversible: the previous default is captured as <see cref="PlannedChange.Before"/> at plan
/// time and restored by <see cref="Revert"/>. If it cannot be determined, nothing is planned —
/// fail closed rather than declare a reversibility we cannot actually provide (ADR-006).
/// </summary>
public sealed class SetDefaultAudioDeviceAction : IRepairAction
{
    public const string DeviceNameContainsParam = "deviceNameContains";

    private readonly IAudioDeviceControl _audio;

    public SetDefaultAudioDeviceAction(IAudioDeviceControl audio) => _audio = audio;

    public string ActionId => "set_default_audio_device";
    public ChangeKind Kind => ChangeKind.AudioDeviceDefault;
    public bool IsReversibleByNature => true;

    public ValidationResult ValidateParameters(IReadOnlyDictionary<string, string> p)
        => p.TryGetValue(DeviceNameContainsParam, out var v) && !string.IsNullOrWhiteSpace(v)
            ? ValidationResult.Ok
            : ValidationResult.Fail($"missing required parameter '{DeviceNameContainsParam}'");

    public IReadOnlyList<PlannedChange> Plan(RepairContext ctx, IReadOnlyDictionary<string, string> p)
    {
        if (!p.TryGetValue(DeviceNameContainsParam, out var nameContains) || string.IsNullOrWhiteSpace(nameContains))
            return Array.Empty<PlannedChange>();

        var target = _audio.FindPlaybackDeviceId(nameContains);
        if (target is null) return Array.Empty<PlannedChange>();   // device not present: nothing to do

        var current = _audio.GetDefaultPlaybackDeviceId();
        if (current is null) return Array.Empty<PlannedChange>();  // fail closed: no known previous state to restore
        if (string.Equals(current, target, StringComparison.OrdinalIgnoreCase))
            return Array.Empty<PlannedChange>();                    // already the default

        return new[]
        {
            new PlannedChange
            {
                ActionId = ActionId,
                Kind = Kind,
                Target = target,
                Before = current,
                After = target,
                Reversible = true,
            }
        };
    }

    // No parameters are passed to StillApplies (interface limitation shared by every action).
    // Re-applying "set default to X" when X is already the default is harmless and idempotent,
    // so always proceeding to preflight is safe here — unlike a file write, there is no
    // double-move risk.
    public bool StillApplies(RepairContext ctx) => true;

    public ExecutionResult Execute(PlannedChange c)
        => _audio.SetDefaultPlaybackDevice(c.After)
            ? ExecutionResult.Ok
            : ExecutionResult.Fail("could not set the default playback device");

    public ExecutionResult Revert(PlannedChange c)
        => _audio.SetDefaultPlaybackDevice(c.Before)
            ? ExecutionResult.Ok
            : ExecutionResult.Fail("could not restore the previous default playback device");
}
