using System.IO;
using System.Text.Json;

namespace PincabToolbox.App;

/// <summary>
/// User preferences persisted between sessions in
/// %AppData%\PincabToolbox\settings.json. All access is best-effort:
/// any IO/parse failure silently falls back to defaults so the app never
/// fails to start because of a corrupt settings file.
/// </summary>
public sealed class Settings
{
    public double WindowWidth { get; set; }
    public double WindowHeight { get; set; }
    public double WindowLeft { get; set; } = double.NaN;
    public double WindowTop { get; set; } = double.NaN;
    public string? LastRoot { get; set; }
    public string? Lang { get; set; }
    public bool OnboardingSeen { get; set; }

    /// <summary>
    /// 20/08 — mode Débutant/Expert du panneau de détail d'un finding (voir
    /// <c>MainWindow.ListFindings_SelectionChanged</c>). Défaut à `true` (Expert), délibérément :
    /// avant ce champ, TOUT LE MONDE voyait déjà le détail complet — un défaut à `false` aurait
    /// silencieusement changé ce que voient les testeurs déjà en cours de test sans qu'ils l'aient
    /// demandé. Un nouvel utilisateur peut passer en Débutant lui-même ; ça ne bascule jamais tout
    /// seul.
    /// </summary>
    public bool ExpertMode { get; set; } = true;

    /// <summary>
    /// LOT H.4 (spec 10/08) — the Repair license key, pasted once by the user and re-verified
    /// (never trusted) on every Apply via <see cref="Repair.Licensing.LicenseVerifier"/>. Stored as
    /// plain text like the rest of this file: the key itself is a public, non-secret credential
    /// (its signature is what proves validity, not its confidentiality) — same posture as any
    /// software license key.
    /// </summary>
    public string? RepairLicenseKey { get; set; }

    private static string FilePath =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "PincabToolbox", "settings.json");

    public static Settings Load()
    {
        try
        {
            var p = FilePath;
            if (File.Exists(p))
                return JsonSerializer.Deserialize<Settings>(File.ReadAllText(p)) ?? new Settings();
        }
        catch { /* ignore — use defaults */ }
        return new Settings();
    }

    public void Save()
    {
        try
        {
            var p = FilePath;
            Directory.CreateDirectory(Path.GetDirectoryName(p)!);
            File.WriteAllText(p, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch { /* ignore — persistence is best-effort */ }
    }
}
