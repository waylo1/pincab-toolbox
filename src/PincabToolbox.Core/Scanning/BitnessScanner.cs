using PincabToolbox.Core.Models;
using PincabToolbox.Core.Profiles;
using PincabToolbox.Core.Services;

namespace PincabToolbox.Core.Scanning;

/// <summary>
/// Inventories the bitness (32/64-bit) of every role-relevant binary and flags
/// hybrid installs — the #1 source of breakage during the VPX 64-bit transition.
/// Read-only: reports, never modifies.
/// </summary>
public sealed class BitnessScanner : IScanner
{
    public string Id => "bitness";
    public string Name => "Bitness Doctor";

    public IEnumerable<Finding> Scan(ScanContext ctx)
    {
        var found = new List<(BinaryRole role, string path, Bitness bits)>();

        foreach (var role in ctx.Profile.BinaryRoles)
        {
            var searchRoots = ResolveScope(ctx.Layout, role.Scope);
            foreach (var root in searchRoots)
            {
                foreach (var file in LayoutDetector.FindFilesByPattern(root, role.Pattern, 4))
                {
                    var bits = PeInspector.GetBitness(file);
                    found.Add((role, file, bits));
                }
            }
        }

        // Deduplicate by path (scopes may overlap)
        var unique = found
            .GroupBy(f => f.path, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToList();

        if (unique.Count == 0)
        {
            yield return new Finding
            {
                Code = "BITNESS_NOTHING_FOUND", Severity = Severity.Info, Category = Id,
                EnglishText = "No known binaries found to analyse.",
            };
            yield break;
        }

        foreach (var (role, path, bits) in unique)
        {
            yield return new Finding
            {
                Code = "BITNESS_INVENTORY", Severity = Severity.Info, Category = Id,
                Subject = Path.GetFileName(path), FilePath = path,
                Args = new[] { Path.GetFileName(path), Render(bits), role.Role },
                EnglishText = $"{Path.GetFileName(path)} — {Render(bits)} ({role.Role}).",
            };
        }

        // Cross-checks: main exe vs VPinMAME COM server.
        var mains = unique.Where(u => u.role.Role == "main-exe" && u.bits != Bitness.Unknown).ToList();
        bool has32Main = mains.Any(m => m.bits == Bitness.X86);
        bool has64Main = mains.Any(m => m.bits == Bitness.X64);

        bool hasVpm32 = unique.Any(u => u.role.Role is "vpinmame" && u.bits == Bitness.X86);
        bool hasVpm64 = unique.Any(u => u.role.Role is "vpinmame64" || (u.role.Role is "vpinmame" && u.bits == Bitness.X64));

        if (has64Main && !hasVpm64 && hasVpm32)
        {
            yield return new Finding
            {
                Code = "BITNESS_MISMATCH_VPM", Severity = Severity.Critical, Category = Id,
                Subject = "VPinMAME",
                EnglishText = "A 64-bit Visual Pinball executable is installed but only a 32-bit VPinMAME.dll was found. " +
                              "64-bit VPX cannot use the 32-bit COM server — ROM tables will fail.",
                FixHint = "Install and register the 64-bit VPinMAME (VPinMAME64.dll) for the 64-bit VPX, or launch the 32-bit VPX for these tables.",
            };
        }

        // Symmetric case: a 32-bit VPX with only a 64-bit VPinMAME. The 32-bit executable
        // cannot load the 64-bit COM server either — ROM tables will fail all the same.
        if (has32Main && !hasVpm32 && hasVpm64)
        {
            yield return new Finding
            {
                Code = "BITNESS_MISMATCH_VPM32", Severity = Severity.Critical, Category = Id,
                Subject = "VPinMAME",
                EnglishText = "A 32-bit Visual Pinball executable is installed but only a 64-bit VPinMAME.dll was found. " +
                              "32-bit VPX cannot use the 64-bit COM server — ROM tables will fail.",
                FixHint = "Install and register the 32-bit VPinMAME (VPinMAME.dll) for the 32-bit VPX, or launch the 64-bit VPX for these tables.",
            };
        }

        if (has32Main && has64Main)
        {
            yield return new Finding
            {
                Code = "BITNESS_HYBRID_INSTALL", Severity = Severity.Warning, Category = Id,
                Subject = "Installation",
                EnglishText = "Both 32-bit and 64-bit Visual Pinball executables are present. Hybrid installs work but every " +
                              "plugin (dmddevice, B2S, FlexDMD) must exist in BOTH bitnesses — this scan lists what you have.",
            };
        }

        bool hasDmd32 = unique.Any(u => u.role.Role == "dmddevice");
        bool hasDmd64 = unique.Any(u => u.role.Role == "dmddevice64");
        if (has64Main && hasDmd32 && !hasDmd64)
        {
            yield return new Finding
            {
                Code = "BITNESS_DMD64_MISSING", Severity = Severity.Warning, Category = Id,
                Subject = "dmddevice",
                EnglishText = "64-bit VPX found but no dmddevice64.dll — external DMDs will not work from the 64-bit VPinMAME.",
                FixHint = "Download the 64-bit dmddevice64.dll from the open-source Freezy dmd-extensions releases and place it next to VPinMAME64.",
            };
        }
    }

    private static string Render(Bitness b) => b switch
    {
        Bitness.X86 => "32-bit",
        Bitness.X64 => "64-bit",
        Bitness.Arm64 => "ARM64",
        _ => "unknown",
    };

    private static IEnumerable<string> ResolveScope(InstallLayout layout, string scope) => scope switch
    {
        "root" => new[] { layout.RootPath },
        "vpinmame" => layout.VPinMameDir is null ? Array.Empty<string>() : new[] { layout.VPinMameDir },
        "tables" => layout.TablesDir is null ? Array.Empty<string>() : new[] { layout.TablesDir },
        _ => new[] { layout.RootPath },
    };
}
