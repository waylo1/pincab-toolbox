using PincabToolbox.Core.Models;
using PincabToolbox.Core.Services;

namespace PincabToolbox.Core.Scanning;

/// <summary>
/// Notes when BOTH a VPinMAME registry configuration and a VPinMAME.ini file are present at once
/// — audit §4-E2: VPinMAME can be configured through either, and having both around invites
/// editing the one that isn't actually taking effect without noticing. <see cref="Severity.Note"/>
/// (ADR-010 Doctrine): the two co-existing is the fact we can confirm; which one VPinMAME actually
/// honors in every version/build is not something this scan verifies, so this stops short of
/// asserting a verdict about which wins.
/// </summary>
public sealed class ConfigPhantomScanner : IScanner
{
    public string Id => "config-phantom";
    public string Name => "Registry/INI Phantom Conflict";

    private readonly Func<bool> _registryKeyExists;
    private readonly Func<string, bool> _fileExists;

    /// <param name="registryKeyExists">Whether the VPinMAME registry key exists. Defaults to a real registry read.</param>
    /// <param name="fileExists">Given a path, whether it exists. Defaults to a real disk check.</param>
    public ConfigPhantomScanner(Func<bool>? registryKeyExists = null, Func<string, bool>? fileExists = null)
    {
        _registryKeyExists = registryKeyExists ?? VpinmameKeyProbe.KeyExists;
        _fileExists = fileExists ?? File.Exists;
    }

    public IEnumerable<Finding> Scan(ScanContext ctx)
    {
        ctx.Cancellation.ThrowIfCancellationRequested();

        if (ctx.Layout.VPinMameDir is null) return Array.Empty<Finding>();
        var iniPath = Path.Combine(ctx.Layout.VPinMameDir, "VPinMAME.ini");

        bool iniExists;
        try { iniExists = _fileExists(iniPath); }
        catch { return Array.Empty<Finding>(); }
        if (!iniExists) return Array.Empty<Finding>();

        bool keyExists;
        try { keyExists = _registryKeyExists(); }
        catch { return Array.Empty<Finding>(); }
        if (!keyExists) return Array.Empty<Finding>();

        return new[]
        {
            new Finding
            {
                Code = "VPINMAME_CONFIG_PHANTOM", Severity = Severity.Note, Category = Id,
                Subject = "VPinMAME.ini",
                FilePath = iniPath,
                EnglishText = "Both a VPinMAME registry configuration (HKCU\\Software\\Freeware\\Visual PinMame) and a VPinMAME.ini file were found. VPinMAME can be configured through either — if you're editing one and not seeing changes take effect, you may be editing the one that isn't currently in use.",
                FixHint = "If you rely on VPinMAME.ini, check its settings actually take effect; otherwise consider removing it to avoid ambiguity and keep the registry configuration as the single source.",
            }
        };
    }
}
