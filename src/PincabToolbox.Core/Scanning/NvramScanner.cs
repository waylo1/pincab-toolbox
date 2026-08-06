using PincabToolbox.Core.Models;
using PincabToolbox.Core.Services;

namespace PincabToolbox.Core.Scanning;

/// <summary>
/// Flags a zero-byte <c>VPinMAME/nvram/*.nv</c> file — the saved-state file VPinMAME could not write
/// a single byte to (a crash mid-write, a full disk, a bad shutdown), leaving the table unable to
/// read its state back and likely to boot to a black screen or freeze instead of starting fresh.
///
/// <para>
/// Thin I/O over <see cref="NvramInspector"/>: the enumerator is injected (delegate with a real-disk
/// default), same constructor-injection shape as <see cref="Scanning.VpxVersionScanner"/>, so the
/// decision path is fully testable without touching a real folder.
/// </para>
///
/// <para>
/// Severity is Warning: deterministic (a file's size is a fact, not a guess) but scoped to the one
/// ROM whose save state is lost, not the whole install — it does not meet the "will break the cab"
/// bar reserved for Critical.
/// </para>
/// </summary>
public sealed class NvramScanner : IScanner
{
    public string Id => "nvram";
    public string Name => "NVRAM Integrity";

    private readonly Func<string, IEnumerable<(string FileName, long SizeBytes)>> _enumerate;

    /// <param name="enumerator">
    /// Lists (fileName, sizeBytes) for every <c>.nv</c> file in the given nvram folder. Defaults to a
    /// real directory scan; injected in tests. Returning an empty sequence (folder absent/empty) is a
    /// normal case, not a failure.
    /// </param>
    public NvramScanner(Func<string, IEnumerable<(string FileName, long SizeBytes)>>? enumerator = null)
        => _enumerate = enumerator ?? EnumerateDisk;

    public IEnumerable<Finding> Scan(ScanContext ctx)
    {
        if (ctx.Layout.VPinMameDir is null) yield break;
        var nvramDir = Path.Combine(ctx.Layout.VPinMameDir, "nvram");

        IEnumerable<(string FileName, long SizeBytes)> files;
        try { files = _enumerate(nvramDir).ToList(); }
        catch { yield break; } // unreadable folder → silence, never a false positive

        foreach (var name in NvramInspector.FindEmpty(files))
        {
            var rom = Path.GetFileNameWithoutExtension(name);
            yield return new Finding
            {
                Code = "NVRAM_EMPTY", Severity = Severity.Warning, Category = Id,
                Subject = rom, FilePath = Path.Combine(nvramDir, name),
                Args = new[] { rom },
                EnglishText = $"The NVRAM save file for '{rom}' is empty (0 bytes) — VPinMAME can't read any saved state from it, so this table is likely to boot to a black screen or freeze instead of starting fresh.",
                FixHint = "Delete the empty .nv file and launch the table once — VPinMAME recreates it with defaults on a clean boot. If you have a backup .nv from before it broke, restore that instead to keep your high scores.",
            };
        }
    }

    private static IEnumerable<(string FileName, long SizeBytes)> EnumerateDisk(string nvramDir)
    {
        if (!Directory.Exists(nvramDir)) return Array.Empty<(string, long)>();
        return Directory.EnumerateFiles(nvramDir, "*.nv", SearchOption.TopDirectoryOnly)
            .Select(p => (Path.GetFileName(p), new FileInfo(p).Length));
    }
}
