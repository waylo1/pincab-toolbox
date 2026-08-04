using PincabToolbox.Core.Models;
using PincabToolbox.Core.Profiles;
using PincabToolbox.Core.Services;
using PincabToolbox.Core.Vpx;

namespace PincabToolbox.Core.Scanning;

/// <summary>Orchestrates layout detection, shared parsing, and all scanners.</summary>
public sealed class ScanEngine
{
    private readonly List<IScanner> _scanners = new();

    public ScanEngine Add(IScanner scanner)
    {
        _scanners.Add(scanner);
        return this;
    }

    public IReadOnlyList<IScanner> Scanners => _scanners;

    public ScanReport Run(string rootPath, Profile profile,
        IProgress<string>? progress = null, CancellationToken ct = default)
    {
        var layout = LayoutDetector.Detect(rootPath, profile);
        var report = new ScanReport { Layout = layout, StartedAt = DateTimeOffset.Now };

        var ctx = new ScanContext { Layout = layout, Profile = profile, Cancellation = ct };

        // Shared prep: ROM set inventory
        if (layout.RomsDir is not null)
        {
            foreach (var zip in Directory.EnumerateFiles(layout.RomsDir, "*.zip", SearchOption.TopDirectoryOnly))
                ctx.RomSets.Add(Path.GetFileNameWithoutExtension(zip));
        }

        // Shared prep: aliases
        if (layout.AliasFilePath is not null)
            ctx.Aliases = AliasFile.Parse(layout.AliasFilePath);

        // Shared prep: parse every table once
        int i = 0;
        foreach (var table in layout.VpxTables)
        {
            ct.ThrowIfCancellationRequested();
            progress?.Report($"Reading table {++i}/{layout.VpxTables.Count}: {Path.GetFileName(table)}");
            ctx.Tables[table] = VpxReader.Read(table);
        }

        foreach (var scanner in _scanners)
        {
            ct.ThrowIfCancellationRequested();
            progress?.Report($"Running {scanner.Name}…");
            try
            {
                report.Findings.AddRange(scanner.Scan(ctx));
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                report.Findings.Add(new Finding
                {
                    Code = "SCANNER_ERROR",
                    Severity = Severity.Warning,
                    Category = scanner.Id,
                    Subject = scanner.Name,
                    Args = new[] { scanner.Name, ex.Message },
                    EnglishText = $"Scanner '{scanner.Name}' failed: {ex.Message}",
                });
            }
        }

        report.FinishedAt = DateTimeOffset.Now;
        return report;
    }
}
