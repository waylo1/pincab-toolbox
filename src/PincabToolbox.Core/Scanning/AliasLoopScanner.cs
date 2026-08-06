using PincabToolbox.Core.Models;
using PincabToolbox.Core.Services;

namespace PincabToolbox.Core.Scanning;

/// <summary>
/// Flags a circular VPMAlias.txt chain (A -&gt; B -&gt; A) — VPinMAME resolves aliases by following the
/// mapping and a loop makes that resolution recurse forever, crashing with a stack overflow the
/// instant a table using that alias tries to load its ROM.
///
/// <para>
/// Zero I/O of its own: <see cref="ScanEngine"/> already parses <c>VPMAlias.txt</c> once via
/// <see cref="AliasFile"/> as shared prep (<c>ScanContext.Aliases</c>), the same map
/// <see cref="RomValidatorScanner"/> already reads. This scanner only asks the pure
/// <see cref="AliasGraph"/> whether that map contains a cycle — same "thin scanner over a pure
/// decision" shape as <see cref="Services.VpxVersionComparer"/>, just with no reader to inject
/// because there is nothing left to read.
/// </para>
///
/// <para>
/// Severity is Warning, not Critical: it is a real, deterministic defect (fact, not heuristic — a
/// cycle either exists in the file or it does not), but it only bites the specific ROM name(s)
/// caught in the loop, not the whole install, so it does not meet the "will break the cab" bar
/// <see cref="Severity.Critical"/> is reserved for.
/// </para>
/// </summary>
public sealed class AliasLoopScanner : IScanner
{
    public string Id => "aliasloop";
    public string Name => "VPMAlias Loop Check";

    public IEnumerable<Finding> Scan(ScanContext ctx)
    {
        foreach (var cycle in AliasGraph.FindCycles(ctx.Aliases))
        {
            var chain = string.Join(" -> ", cycle) + " -> " + cycle[0];
            yield return new Finding
            {
                Code = "VPMALIAS_LOOP", Severity = Severity.Warning, Category = Id,
                Subject = cycle[0], FilePath = ctx.Layout.AliasFilePath,
                Args = new[] { chain },
                EnglishText = $"VPMAlias.txt has a circular alias chain: {chain}. VPinMAME resolves aliases by following this mapping — a loop makes it recurse forever and crash with a stack overflow the moment a table needs this ROM name.",
                FixHint = "Open VPMAlias.txt and break the loop: make the last alias in the chain point directly to the real ROM set name instead of back to an earlier alias.",
            };
        }
    }
}
