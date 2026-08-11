using System.Text.RegularExpressions;
using PincabToolbox.Core.Models;
using PincabToolbox.Core.Services;

namespace PincabToolbox.Core.Scanning;

/// <summary>
/// LOT B (spec 10/08) — closes the exact gap <see cref="BitnessScanner"/> already names in its
/// own <c>BITNESS_HYBRID_INSTALL</c> text: "every plugin (dmddevice, B2S, FlexDMD) must exist in
/// BOTH bitnesses — this scan LISTS what you have". <see cref="BitnessScanner"/> inventories;
/// this VERIFIES the pairing B2S and FlexDMD never got — the P0 "64 bit and 32 bit are different
/// ecosystems" theme (≥5 independent Reddit/VPUniverse discussions, 2023→April 2025).
///
/// <para>
/// A NEW scanner, deliberately — <see cref="BitnessScanner"/> is not touched (spec §3.1 rule 5).
/// dmddevice is left out on purpose: <see cref="BitnessScanner"/>'s existing
/// <c>BITNESS_DMD64_MISSING</c> already covers the 32→64 direction for that one component, and
/// duplicating it here would double-report the same fact under two codes.
/// </para>
///
/// <para>
/// Bitness is always MEASURED (<see cref="PeInspector.GetBitness"/>) on the real file bytes —
/// never inferred from a file name like "dmddevice64.dll" (spec's explicit trap: a `64` in a name
/// is a convention, not a fact). A component's "required" signal reuses
/// <see cref="ScriptAnalyzer.AnalyzeRomUsage"/> (B2S) and the profile's own "flexdmd"
/// <c>ScriptSignature</c> regex (FlexDMD) — the same reusable signals <see cref="ComHealthScanner"/>
/// (LOT A) already established, not a new duplicate regex.
/// </para>
/// </summary>
public sealed class ChainBitnessScanner : IScanner
{
    public string Id => "chain-bitness";
    public string Name => "Chain Bitness Doctor";

    private static readonly (string Role, string Label)[] TrackedComponents =
    {
        ("b2s", "B2S Backglass Server"),
        ("flexdmd", "FlexDMD"),
    };

    public IEnumerable<Finding> Scan(ScanContext ctx)
    {
        var installedVpxBitnesses = ctx.Layout.VpxExecutables
            .Select(PeInspector.GetBitness)
            .Where(b => b == Bitness.X86 || b == Bitness.X64)
            .Distinct()
            .ToList();
        if (installedVpxBitnesses.Count == 0) yield break;   // no measurably-bitness VPX -> nothing to cross-check

        bool anyUsesB2S = false, anyUsesFlexDmd = false;
        var flexPattern = ctx.Profile.ScriptSignatures.FirstOrDefault(s =>
            string.Equals(s.Id, "flexdmd", StringComparison.OrdinalIgnoreCase))?.Regex;
        Regex? flexRegex = null;
        if (!string.IsNullOrWhiteSpace(flexPattern))
        {
            try { flexRegex = new Regex(flexPattern, RegexOptions.Compiled, TimeSpan.FromSeconds(2)); }
            catch { flexRegex = null; }
        }

        foreach (var table in ctx.Tables.Values)
        {
            ctx.Cancellation.ThrowIfCancellationRequested();
            if (table.Script is null) continue;
            var rom = ScriptAnalyzer.AnalyzeRomUsage(table.Script);
            if (rom.UsesB2S) anyUsesB2S = true;
            if (flexRegex is not null)
            {
                try { if (flexRegex.IsMatch(table.Script)) anyUsesFlexDmd = true; }
                catch (RegexMatchTimeoutException) { }
            }
        }

        var requiredByRole = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase)
        {
            ["b2s"] = anyUsesB2S,
            ["flexdmd"] = anyUsesFlexDmd,
        };

        var presentAt = new HashSet<(string Role, Bitness Bitness)>();
        foreach (var (role, _) in TrackedComponents)
        {
            foreach (var br in ctx.Profile.BinaryRoles)
            {
                if (!string.Equals(br.Role, role, StringComparison.OrdinalIgnoreCase)) continue;
                var scopeRoot = ResolveScope(ctx.Layout, br.Scope);
                if (scopeRoot is null) continue;

                foreach (var file in LayoutDetector.FindFilesByPattern(scopeRoot, br.Pattern, 5))
                {
                    ctx.Cancellation.ThrowIfCancellationRequested();
                    var bits = PeInspector.GetBitness(file);   // measured, never inferred from the name
                    if (bits == Bitness.X86 || bits == Bitness.X64) presentAt.Add((role, bits));
                }
            }
        }

        foreach (var f in Evaluate(requiredByRole, presentAt, installedVpxBitnesses, Id))
            yield return f;
    }

    /// <summary>Pure decision, testable without touching disk.</summary>
    public static IReadOnlyList<Finding> Evaluate(
        IReadOnlyDictionary<string, bool> requiredByRole,
        IReadOnlySet<(string Role, Bitness Bitness)> presentAt,
        IReadOnlyList<Bitness> installedVpxBitnesses,
        string category)
    {
        var findings = new List<Finding>();

        foreach (var bitness in installedVpxBitnesses.Distinct())
        {
            if (bitness != Bitness.X86 && bitness != Bitness.X64) continue;   // CHAIN_BITNESS_UNKNOWN: never emitted, per spec

            foreach (var (role, label) in TrackedComponents)
            {
                if (!requiredByRole.TryGetValue(role, out var required) || !required) continue;
                if (presentAt.Contains((role, bitness))) continue;

                var bLabel = bitness == Bitness.X86 ? "32-bit" : "64-bit";
                findings.Add(new Finding
                {
                    Code = "CHAIN_BITNESS_GAP", Severity = Severity.Warning, Category = category,
                    Subject = label,
                    Args = new[] { label, bLabel },
                    EnglishText = $"A {bLabel} Visual Pinball is installed and at least one table needs " +
                                  $"{label}, but no {bLabel} {label} binary was found under this install — " +
                                  $"it will fail to load from the {bLabel} process.",
                    FixHint = $"Install the {bLabel} build of {label} alongside the {bLabel} Visual Pinball.",
                });
            }
        }

        return findings;
    }

    private static string? ResolveScope(InstallLayout layout, string scope) => scope switch
    {
        "root" => layout.RootPath,
        "vpinmame" => layout.VPinMameDir,
        "tables" => layout.TablesDir,
        _ => layout.RootPath,
    };
}
