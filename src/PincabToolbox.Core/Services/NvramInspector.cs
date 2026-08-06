namespace PincabToolbox.Core.Services;

/// <summary>
/// Pure decision for the NVRAM 0-byte check: given file name + size pairs already enumerated from a
/// <c>VPinMAME/nvram</c> folder, picks out every <c>.nv</c> file that is exactly zero bytes.
///
/// <para>
/// A 0-byte NVRAM file cannot hold the saved state VPinMAME expects on load, so the table boots to a
/// black screen or freezes instead of falling back to sane defaults the way a MISSING file would.
/// Deliberately narrower than "does the size match what this ROM's driver normally writes" — there is
/// no specs database of expected sizes per ROM in this project, so only the unambiguous 0-byte case is
/// reported (audit §4/H1). A non-zero size, however small or large, is never flagged.
/// </para>
/// </summary>
public static class NvramInspector
{
    public static IReadOnlyList<string> FindEmpty(IEnumerable<(string FileName, long SizeBytes)> files)
        => files.Where(f => f.SizeBytes == 0).Select(f => f.FileName).ToList();
}
