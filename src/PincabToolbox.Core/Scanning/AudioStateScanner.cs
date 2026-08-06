using PincabToolbox.Core.Models;
using PincabToolbox.Core.Services;

namespace PincabToolbox.Core.Scanning;

/// <summary>
/// Notes when the CURRENT default Windows playback device looks like a display/HDMI audio output
/// rather than dedicated cab speakers — the state a pincab is often found in after Windows
/// randomly resets the default output on boot (FIELD-LOG 2026-07-29, Pincab Passion). This scanner
/// answers only "is that the case right now" — it deliberately does not, and cannot, predict a
/// future reset; that nuance is already on record (audit §4-D1) and is why this ships as a
/// <see cref="Severity.Note"/>, not a Warning (ADR-010 Doctrine): a screen output being the
/// default is a fact worth surfacing, not necessarily a defect — some cabs intentionally route
/// audio through a screen/soundbar over HDMI.
///
/// <para>
/// Detection-only. The Repair action that changes the default device
/// (<c>SetDefaultAudioDeviceAction</c> / <c>set_default_audio_device</c>) already exists but is
/// deliberately NOT wired to this finding's code yet — the action needs a target device name
/// substring (e.g. "Speakers") to switch TO, and guessing that name for an arbitrary cab would be
/// exactly the kind of unverified assumption this project avoids. Wiring it is a Knowledge Pack
/// <c>repairRules</c> decision left for Maxime (see FIELD-LOG, DÉCISIONS EN ATTENTE).
/// </para>
/// </summary>
public sealed class AudioStateScanner : IScanner
{
    public string Id => "audio-state";
    public string Name => "Audio Current-State";

    private readonly Func<string?> _getDefaultPlaybackDeviceName;

    /// <param name="getDefaultPlaybackDeviceName">Returns the current default playback device's friendly name, or null when unknown. Defaults to a real Core Audio (MMDevice) read.</param>
    public AudioStateScanner(Func<string?>? getDefaultPlaybackDeviceName = null)
    {
        _getDefaultPlaybackDeviceName = getDefaultPlaybackDeviceName ?? AudioEndpointReader.TryGetDefaultPlaybackDeviceName;
    }

    public IEnumerable<Finding> Scan(ScanContext ctx)
    {
        ctx.Cancellation.ThrowIfCancellationRequested();

        string? name;
        try { name = _getDefaultPlaybackDeviceName(); }
        catch { return Array.Empty<Finding>(); } // unreadable -> silence, never a false positive

        if (!AudioStateEvaluator.LooksLikeScreenOutput(name)) return Array.Empty<Finding>();

        return new[]
        {
            new Finding
            {
                Code = "AUDIO_DEFAULT_SUSPECT", Severity = Severity.Note, Category = Id,
                Subject = name!,
                Args = new[] { name! },
                EnglishText = $"The current default Windows playback device is '{name}' — its name suggests a display/HDMI audio output rather than dedicated speakers. This is a known spot for Windows to silently reset the default to on boot; worth checking it's what you intend.",
                FixHint = "If this isn't the audio output you want, set the default playback device back to your speakers in Windows Sound settings.",
            }
        };
    }
}
