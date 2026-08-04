namespace PincabToolbox.Core.Models;

/// <summary>
/// Resolved locations of a virtual pinball installation.
/// All members are null when not found; scanners degrade gracefully.
/// </summary>
public sealed class InstallLayout
{
    public required string RootPath { get; init; }

    /// <summary>Folder containing .vpx tables.</summary>
    public string? TablesDir { get; set; }

    /// <summary>All Visual Pinball executables found (VPinballX.exe, VPinballX64.exe, VPinballX_GL64.exe…).</summary>
    public List<string> VpxExecutables { get; } = new();

    /// <summary>VPinMAME folder (contains roms/, nvram/, VPinMAME.dll…).</summary>
    public string? VPinMameDir { get; set; }

    /// <summary>ROM zips folder.</summary>
    public string? RomsDir { get; set; }

    /// <summary>VPMAlias.txt path when present.</summary>
    public string? AliasFilePath { get; set; }

    /// <summary>PinUP Popper SQLite database path (PUPDatabase.db).</summary>
    public string? PupDatabasePath { get; set; }

    /// <summary>PinUP Popper media root (POPMedia).</summary>
    public string? PopMediaDir { get; set; }

    /// <summary>PinUP Popper PUP-Packs folder (PUPVideos).</summary>
    public string? PupVideosDir { get; set; }

    public List<string> VpxTables { get; } = new();
}
