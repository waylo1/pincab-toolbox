using PincabToolbox.Core.Models;
using PincabToolbox.Core.Profiles;
using PincabToolbox.Core.Vpx;

namespace PincabToolbox.Core.Scanning;

/// <summary>Shared context passed to every scanner (tables are parsed once).</summary>
public sealed class ScanContext
{
    public required InstallLayout Layout { get; init; }
    public required Profile Profile { get; init; }

    /// <summary>Parsed table data, keyed by file path (populated by the engine).</summary>
    public Dictionary<string, VpxTableData> Tables { get; } = new();

    /// <summary>Set of ROM zip base names (lower case, no extension) present in the roms folder.</summary>
    public HashSet<string> RomSets { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>VPMAlias mappings.</summary>
    public Dictionary<string, string> Aliases { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public CancellationToken Cancellation { get; init; }
}

public interface IScanner
{
    /// <summary>Stable id, used as Finding.Category.</summary>
    string Id { get; }

    /// <summary>English display name.</summary>
    string Name { get; }

    IEnumerable<Finding> Scan(ScanContext context);
}
