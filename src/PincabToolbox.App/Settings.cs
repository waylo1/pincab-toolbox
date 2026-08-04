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
