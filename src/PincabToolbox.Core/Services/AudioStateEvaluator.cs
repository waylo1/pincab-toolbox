namespace PincabToolbox.Core.Services;

/// <summary>
/// Pure decision: does a default-playback-device friendly name look like a display/HDMI audio
/// output rather than dedicated speakers? A simple, auditable substring match against the
/// vendor/generic names Windows assigns to GPU-driven HDMI/DisplayPort audio endpoints — not a
/// guess at the user's intent, just the observable fact of what the current default is named
/// (FIELD-LOG 2026-07-29, Pincab Passion: "aléatoirement au démarrage l'audio par défaut passe
/// sur l'HDMI").
///
/// Deliberately does NOT check "no endpoint enabled" or "volume at zero" (both mentioned in the
/// original audit fiche, §4-D1) — those need a broader COM surface (endpoint enumeration/state,
/// the volume interface) this pass keeps out of scope; see <see cref="AudioEndpointReader"/>'s
/// header. A future pass can extend this once that surface is added.
/// </summary>
public static class AudioStateEvaluator
{
    /// <summary>Name fragments Windows commonly assigns to GPU-driven HDMI/DisplayPort audio endpoints (case-insensitive).</summary>
    private static readonly string[] ScreenAudioMarkers =
    {
        "hdmi", "display audio", "nvidia high definition audio", "amd high definition audio",
        "intel(r) display audio", "displayport",
    };

    /// <summary>True when <paramref name="defaultDeviceName"/> looks like a screen/HDMI audio output.</summary>
    public static bool LooksLikeScreenOutput(string? defaultDeviceName)
    {
        if (string.IsNullOrWhiteSpace(defaultDeviceName)) return false;
        foreach (var marker in ScreenAudioMarkers)
        {
            if (defaultDeviceName.Contains(marker, StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }
}
