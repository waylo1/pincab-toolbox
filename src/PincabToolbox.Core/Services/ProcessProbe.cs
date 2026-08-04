namespace PincabToolbox.Core.Services;

/// <summary>
/// Live process presence check, by name (no extension), case-insensitive.
///
/// Used to detect the "PinUpDisplay.exe stays alive after a table closes" zombie reported on
/// VPForums (FIELD-LOG 2026-07-29): the process itself isn't a file on disk, so the usual
/// static-scan approach doesn't apply — this is the one Core check that looks at live OS state
/// beyond disk space (DiskSpaceScanner already does the same kind of thing).
///
/// <see cref="System.Diagnostics.Process"/> enumeration works cross-platform in .NET, but a
/// locked-down machine (or a non-Windows OS during development) can still throw — every caller
/// must degrade gracefully, never crash a scan.
/// </summary>
public static class ProcessProbe
{
    public static bool IsRunning(string processName)
    {
        try { return System.Diagnostics.Process.GetProcessesByName(processName).Length > 0; }
        catch { return false; }   // unknown must never be reported as "yes it's stuck"
    }

    /// <summary>
    /// Full path of the executable for the first running process with this name, or null when
    /// not running, or when the path cannot be read (permissions, process exited meanwhile…).
    /// </summary>
    public static string? TryGetExecutablePath(string processName)
    {
        try
        {
            var procs = System.Diagnostics.Process.GetProcessesByName(processName);
            foreach (var p in procs)
            {
                try
                {
                    var path = p.MainModule?.FileName;
                    if (!string.IsNullOrWhiteSpace(path)) return path;
                }
                catch { /* try the next candidate */ }
                finally { p.Dispose(); }
            }
            return null;
        }
        catch { return null; }
    }
}
