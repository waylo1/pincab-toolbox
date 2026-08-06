namespace PincabToolbox.Core.Services;

/// <summary>
/// Pure decision for NTFS junction/symlink health (audit §4/G3): given the already-resolved facts
/// for one filesystem entry — is it a reparse point, and if so does its target still resolve — decides
/// whether it's broken. All the actual reparse-point I/O lives in
/// <see cref="Scanning.JunctionScanner"/>; this function has nothing left to guess about.
/// </summary>
public static class JunctionInspector
{
    public static bool IsBroken(bool isReparsePoint, bool targetExists) => isReparsePoint && !targetExists;
}
