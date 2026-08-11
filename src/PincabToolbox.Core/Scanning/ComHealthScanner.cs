using System.Text.RegularExpressions;
using PincabToolbox.Core.Models;
using PincabToolbox.Core.Services;

namespace PincabToolbox.Core.Scanning;

/// <summary>
/// LOT A (spec 10/08) — reads the Windows COM registration health of the three components every
/// pincab depends on (VPinMAME.Controller, B2S.Server, FlexDMD.FlexDMD), plus one weak-evidence
/// component kept at <see cref="Severity.Note"/> only (PinUpPlayer.PinDisplay). Nothing in the 26
/// scanners this lot follows reads a single COM registration — this is the #1 theme of the whole
/// community research pass ("ActiveX component can't create object", "Library not registered",
/// "Registered FlexDMD does not match your install path").
///
/// <para>
/// Three dimensions, all already available elsewhere in the project and deliberately reused, not
/// reimplemented (spec §2 "infrastructure réutilisable"): <b>required?</b> —
/// <see cref="ScriptAnalyzer.AnalyzeRomUsage"/> for VPinMAME/B2S, the profile's own "flexdmd"
/// <c>ScriptSignature</c> regex for FlexDMD (never a hand-typed duplicate of
/// <see cref="DependencyScanner"/>'s private regex); <b>present?</b> —
/// <see cref="Core.Profiles.Profile.BinaryRoles"/> + <see cref="LayoutDetector.FindFilesByPattern"/>,
/// exactly like <see cref="DependencyScanner"/>; <b>registered?</b> —
/// <see cref="ComRegistrationProbe"/>, in both the 32-bit and 64-bit views separately (spec's
/// "piège n°1").
/// </para>
///
/// <para>
/// <c>PinUpPlayer.PinDisplay</c> has no confirmed binary role and no script signal a table could
/// "require" it through — its ProgID comes from a single, unrecoupled external source (spec §5
/// A.2 table). Per the spec's own top-level rule ("aucun identifiant non recoupé n'ouvre un
/// finding de sévérité supérieure à Note"), every finding this scanner could produce for it is
/// clamped to <see cref="Severity.Note"/>, and — because it has no "required" signal —
/// <c>COM_NOT_REGISTERED</c> structurally never fires for it (that finding's own formula requires
/// "required by a table", which is always false here).
/// </para>
/// </summary>
public sealed class ComHealthScanner : IScanner
{
    public string Id => "com";
    public string Name => "COM Registration Health";

    private readonly Func<string, ComRegistryView, (bool Succeeded, ComRegistration? Registration)> _probe;
    private readonly Func<string, bool> _fileExists;

    public ComHealthScanner(
        Func<string, ComRegistryView, (bool, ComRegistration?)>? probe = null,
        Func<string, bool>? fileExists = null)
    {
        _probe = probe ?? ComRegistrationProbe.TryProbe;
        _fileExists = fileExists ?? File.Exists;
    }

    private sealed record ComponentSpec(string ProgId, string[] Roles, bool RequiredByATable, Severity SeverityCap);

    public IEnumerable<Finding> Scan(ScanContext ctx)
    {
        var root = ctx.Layout.RootPath;

        bool anyUsesController = false, anyUsesB2S = false, anyUsesFlexDmd = false;
        var flexPattern = ctx.Profile.ScriptSignatures.FirstOrDefault(s =>
            string.Equals(s.Id, "flexdmd", StringComparison.OrdinalIgnoreCase))?.Regex;
        Regex? flexRegex = null;
        if (!string.IsNullOrWhiteSpace(flexPattern))
        {
            try { flexRegex = new Regex(flexPattern, RegexOptions.Compiled, TimeSpan.FromSeconds(2)); }
            catch { flexRegex = null; }   // a broken profile regex must never crash the scan — silence for this signal
        }

        foreach (var table in ctx.Tables.Values)
        {
            ctx.Cancellation.ThrowIfCancellationRequested();
            if (table.Script is null) continue;

            var rom = ScriptAnalyzer.AnalyzeRomUsage(table.Script);
            if (rom.UsesController) anyUsesController = true;
            if (rom.UsesB2S) anyUsesB2S = true;

            if (flexRegex is not null)
            {
                try { if (flexRegex.IsMatch(table.Script)) anyUsesFlexDmd = true; }
                catch (RegexMatchTimeoutException) { /* skip this table's FlexDMD signal, never fail the scan */ }
            }
        }

        var installedVpxBitnesses = ctx.Layout.VpxExecutables
            .Select(PeInspector.GetBitness)
            .Where(b => b != Bitness.Unknown)
            .Distinct()
            .ToList();

        var components = new[]
        {
            new ComponentSpec("VPinMAME.Controller", new[] { "vpinmame", "vpinmame64" }, anyUsesController, Severity.Warning),
            new ComponentSpec("B2S.Server", new[] { "b2s" }, anyUsesB2S, Severity.Warning),
            new ComponentSpec("FlexDMD.FlexDMD", new[] { "flexdmd" }, anyUsesFlexDmd, Severity.Warning),
            new ComponentSpec("PinUpPlayer.PinDisplay", Array.Empty<string>(), false, Severity.Note),
        };

        foreach (var comp in components)
        {
            ctx.Cancellation.ThrowIfCancellationRequested();

            var (succ32, reg32) = SafeProbe(comp.ProgId, ComRegistryView.Registry32);
            var (succ64, reg64) = SafeProbe(comp.ProgId, ComRegistryView.Registry64);

            var binaryPath = FindBinaryPath(ctx, comp.Roles);
            var binaryPresent = binaryPath is not null;

            foreach (var f in EvaluateComponent(
                comp.ProgId, reg32, succ32, reg64, succ64,
                binaryPresent, root, comp.RequiredByATable,
                installedVpxBitnesses, Id, comp.SeverityCap, _fileExists))
            {
                yield return f;
            }

            // VPINMAME_NOT_REGISTERED (spec A.3, D-3) — the only Critical this lot may add, and
            // it reuses the SAME two probe results already computed above (never a second read).
            if (comp.ProgId == "VPinMAME.Controller")
            {
                var critical = EvaluateVpinmameNotRegistered(
                    reg32, reg64, succ32 && succ64, binaryPresent, binaryPath,
                    comp.RequiredByATable, installedVpxBitnesses, Id);
                if (critical is not null) yield return critical;
            }
        }
    }

    private (bool, ComRegistration?) SafeProbe(string progId, ComRegistryView view)
    {
        try { return _probe(progId, view); }
        catch { return (false, null); }   // a probe delegate must never take the whole scan down
    }

    private static string? FindBinaryPath(ScanContext ctx, IReadOnlyList<string> roles)
    {
        if (roles.Count == 0) return null;
        foreach (var role in roles)
        {
            foreach (var br in ctx.Profile.BinaryRoles)
            {
                if (!string.Equals(br.Role, role, StringComparison.OrdinalIgnoreCase)) continue;
                var scopeRoot = br.Scope switch
                {
                    "vpinmame" => ctx.Layout.VPinMameDir,
                    "tables" => ctx.Layout.TablesDir,
                    "root" => ctx.Layout.RootPath,
                    _ => ctx.Layout.RootPath,
                };
                if (scopeRoot is null) continue;
                var hit = LayoutDetector.FindFilesByPattern(scopeRoot, br.Pattern, 5).FirstOrDefault();
                if (hit is not null) return hit;
            }
        }
        return null;
    }

    /// <summary>
    /// Pure decision for the A.2 finding family — testable without a real registry. Mirrors the
    /// shape of <see cref="DisplaySetupScanner.Evaluate"/> / <see cref="BlockedFileScanner.SeverityFor"/>.
    /// </summary>
    public static IReadOnlyList<Finding> EvaluateComponent(
        string progId,
        ComRegistration? view32, bool probe32Succeeded,
        ComRegistration? view64, bool probe64Succeeded,
        bool binaryPresentUnderRoot, string? rootPath,
        bool requiredByATable,
        IReadOnlyList<Bitness> installedVpxBitnesses,
        string category, Severity severityCap,
        Func<string, bool>? fileExists = null)
    {
        fileExists ??= File.Exists;
        var findings = new List<Finding>();
        var bothProbesSucceeded = probe32Succeeded && probe64Succeeded;

        if (view32 is null && view64 is null)
        {
            if (bothProbesSucceeded && binaryPresentUnderRoot && requiredByATable)
            {
                findings.Add(Cap(new Finding
                {
                    Code = "COM_NOT_REGISTERED", Severity = Severity.Warning, Category = category,
                    Subject = progId,
                    Args = new[] { progId },
                    EnglishText = $"'{progId}' is not registered in either the 32-bit or 64-bit COM registry, " +
                                  "but the matching component is present under this install and at least one " +
                                  "table needs it — it will fail with an error such as \"ActiveX component " +
                                  "can't create object\" or \"Library not registered (Exception from HRESULT: 0x8002801D)\".",
                    FixHint = "Run the component's own registration tool (its Setup.exe / registration app / regsvr32) as Administrator.",
                }, severityCap));
            }
            // absent from both views but not present/required, or the probe itself failed:
            // silence — never guess (spec §3.1 rule 3/4).
        }
        else
        {
            // Prefer validating the view matching an actually-installed VPX bitness (the one that
            // matters to this user); fall back to 64-bit, then whichever view is registered.
            var preferred =
                (installedVpxBitnesses.Contains(Bitness.X64) && view64 is not null) ? view64 :
                (installedVpxBitnesses.Contains(Bitness.X86) && view32 is not null) ? view32 :
                view64 ?? view32;

            bool exists;
            try { exists = fileExists(preferred!.ServerPath); } catch { exists = false; }

            if (!exists)
            {
                findings.Add(Cap(new Finding
                {
                    Code = "COM_STALE_PATH", Severity = Severity.Warning, Category = category,
                    Subject = progId, FilePath = preferred!.ServerPath,
                    Args = new[] { progId, preferred.ServerPath },
                    EnglishText = $"'{progId}' is registered but points to '{preferred.ServerPath}', which no " +
                                  "longer exists — a leftover registration from a previous install. Loading it will fail.",
                    FixHint = "Re-run the component's registration tool from its CURRENT location to overwrite the stale registration.",
                }, severityCap));
            }
            else if (!IsUnderRoot(preferred!.ServerPath, rootPath))
            {
                if (binaryPresentUnderRoot)
                {
                    findings.Add(Cap(new Finding
                    {
                        Code = "COM_PATH_OUTSIDE_INSTALL", Severity = Severity.Note, Category = category,
                        Subject = progId, FilePath = preferred.ServerPath,
                        Args = new[] { progId, preferred.ServerPath },
                        EnglishText = $"'{progId}' is registered, but to a copy outside this install " +
                                      $"('{preferred.ServerPath}') — this install also has its own copy of the " +
                                      "component. Tables run from here will actually load the registered (other) copy.",
                        FixHint = "If you meant to use THIS install's copy, re-run its registration tool from here.",
                    }, severityCap));
                }
                // else: a legitimate other install with nothing local to compare against — silence,
                // per spec A.2 (multi-install is not itself a defect).
            }
            else
            {
                findings.Add(Cap(new Finding
                {
                    Code = "COM_OK", Severity = Severity.Ok, Category = category,
                    Subject = progId, FilePath = preferred.ServerPath,
                    Args = new[] { progId },
                    EnglishText = $"'{progId}' is registered and points inside this install.",
                }, severityCap));
            }
        }

        // COM_BITNESS_GAP — independent of the family above; answers the research's P0 #2
        // ("64 bit and 32 bit are different ecosystems"). Measured, never guessed: only evaluated
        // for a bitness PeInspector actually confirmed on a real installed VPX executable.
        foreach (var bitness in installedVpxBitnesses.Distinct())
        {
            if (bitness != Bitness.X86 && bitness != Bitness.X64) continue;
            var thisView = bitness == Bitness.X86 ? view32 : view64;
            var otherView = bitness == Bitness.X86 ? view64 : view32;
            if (thisView is null && otherView is not null)
            {
                var label = bitness == Bitness.X86 ? "32-bit" : "64-bit";
                findings.Add(Cap(new Finding
                {
                    Code = "COM_BITNESS_GAP", Severity = Severity.Warning, Category = category,
                    Subject = progId,
                    Args = new[] { progId, label },
                    EnglishText = $"A {label} Visual Pinball is installed but '{progId}' is only registered in " +
                                  $"the OTHER bitness — the {label} process cannot use it. This is the classic " +
                                  "\"32-bit and 64-bit are different ecosystems\" failure.",
                    FixHint = $"Register the {label} build of this component, or launch the VPX build matching the bitness that IS registered.",
                }, severityCap));
            }
        }

        return findings;
    }

    /// <summary>
    /// Pure decision for <c>VPINMAME_NOT_REGISTERED</c> (spec A.3, decision D-3 — the first
    /// <see cref="Severity.Critical"/> added since the 03/08 scanner freeze). All four conditions
    /// are MEASURED, never assumed — in particular, <paramref name="probeSucceeded"/> false (a
    /// registry read failure of ANY kind) means this returns null, not a downgraded Warning: a
    /// Critical must never fire on "I couldn't check" (spec: "on ne dégrade pas en Warning, on se
    /// tait"). Write this test FIRST, per the spec's own instruction — see
    /// <c>ComHealthScannerTests.Test_VpinmameNotRegistered_ProbeFailed_NeverEmitsCritical</c>.
    /// </summary>
    public static Finding? EvaluateVpinmameNotRegistered(
        ComRegistration? view32, ComRegistration? view64,
        bool probeSucceeded, bool binaryPresentUnderRoot, string? binaryPathUnderRoot,
        bool requiredByATable, IReadOnlyList<Bitness> installedVpxBitnesses, string category)
    {
        // Condition 1: VPinMAME.dll (or VPinMAME64.dll) present under the scanned root.
        if (!binaryPresentUnderRoot) return null;
        // Condition 2: the registry read itself succeeded — an exception, an inaccessible hive,
        // or running off Windows must NEVER be reported as "not registered" (spec's own words:
        // "le finding n'est pas émis du tout").
        if (!probeSucceeded) return null;
        // Condition 3: VPinMAME.Controller absent from BOTH views.
        if (view32 is not null || view64 is not null) return null;
        // Condition 4: at least one scanned table actually needs a ROM controller.
        if (!requiredByATable) return null;

        var bitnessNote = installedVpxBitnesses.Count switch
        {
            0 => "",
            1 => $" ({(installedVpxBitnesses[0] == Bitness.X86 ? "32-bit" : installedVpxBitnesses[0] == Bitness.X64 ? "64-bit" : "unknown-bitness")} Visual Pinball installed)",
            _ => " (both 32-bit and 64-bit Visual Pinball installed)",
        };

        return new Finding
        {
            Code = "VPINMAME_NOT_REGISTERED", Severity = Severity.Critical, Category = category,
            Subject = "VPinMAME.Controller",
            FilePath = binaryPathUnderRoot,
            Args = Array.Empty<string>(),
            EnglishText = "VPinMAME.dll is present but VPinMAME.Controller is not registered in either the " +
                          "32-bit or 64-bit COM registry, and at least one table needs it" + bitnessNote +
                          " — every ROM-based table will fail to start (\"ActiveX component can't create " +
                          "object\" / \"Library not registered\"). This is almost always caused by copying a " +
                          "VPX install by hand without ever running VPinMAME's own Setup.exe.",
            FixHint = "Run VPinMAME's own Setup.exe (in the VPinMAME folder) as Administrator — it registers " +
                      "the COM component. This is the single most common fix for \"no ROM table starts\".",
        };
    }

    private static Finding Cap(Finding f, Severity cap) => f.Severity > cap ? f with { Severity = cap } : f;

    private static bool IsUnderRoot(string path, string? root)
    {
        if (string.IsNullOrEmpty(root)) return false;
        try
        {
            var full = Path.GetFullPath(path).TrimEnd('\\', '/');
            var fullRoot = Path.GetFullPath(root).TrimEnd('\\', '/');
            return full.Equals(fullRoot, StringComparison.OrdinalIgnoreCase)
                || full.StartsWith(fullRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
        }
        catch { return false; }
    }
}
