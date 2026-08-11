using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;

namespace PincabToolbox.Core.Services;

/// <summary>
/// Which registry view was read. A separate, project-local enum rather than
/// <c>Microsoft.Win32.Registry</c>'s <c>RegistryView</c> — Core is zero-external-dependency by
/// design (see the .csproj comment), and every other registry reader here
/// (<see cref="VpinmameRegistry"/>, <see cref="SerialPortRegistry"/>, <see cref="DpiRegistry"/>,
/// <see cref="VpinmameKeyProbe"/>) already hand-rolls advapi32 P/Invoke for the same reason —
/// this follows that convention rather than introducing the only BCL registry dependency in the
/// codebase for one new probe.
/// </summary>
public enum ComRegistryView { Registry32, Registry64 }

/// <summary>A resolved COM registration, read from ONE specific registry view.</summary>
public sealed record ComRegistration
{
    public required string ProgId { get; init; }
    public required string Clsid { get; init; }
    public required string ServerPath { get; init; }
    public required ComRegistryView View { get; init; }
}

/// <summary>
/// Read-only probe of the Windows COM class registration chain
/// (<c>HKEY_CLASSES_ROOT\&lt;progId&gt;\CLSID</c> → <c>HKEY_CLASSES_ROOT\CLSID\{GUID}\InprocServer32</c>,
/// falling back to <c>LocalServer32</c>). Never writes. Spec LOT A.1 (10/08).
///
/// <para>
/// <b>Piège n°1 de ce lot, cité explicitement dans la spec</b>: on ne lit jamais
/// <c>HKEY_CLASSES_ROOT</c> "tout court" — sur un Windows 64 bits, un composant COM 32 bits
/// s'enregistre dans une arborescence séparée (Wow6432Node), donc la vue obtenue dépendrait de
/// l'architecture du PROCESSUS APPELANT plutôt que de ce qui est réellement enregistré. Les deux
/// vues sont lues séparément ici via les drapeaux <c>KEY_WOW64_32KEY</c>/<c>KEY_WOW64_64KEY</c> —
/// l'équivalent P/Invoke de <c>RegistryView.Registry32</c>/<c>Registry64</c>.
/// </para>
/// </summary>
public static class ComRegistrationProbe
{
    private static readonly IntPtr HkeyClassesRoot = new(unchecked((int)0x80000000));
    private const int KEY_READ = 0x20019;
    private const int KEY_WOW64_32KEY = 0x0200;
    private const int KEY_WOW64_64KEY = 0x0100;
    private const int ERROR_FILE_NOT_FOUND = 2;

    /// <summary>
    /// Resolves a ProgID's CLSID + server path in ONE specific registry view. Returns null both
    /// when the chain is genuinely incomplete (not registered) AND when the read could not be
    /// completed at all (access denied, non-Windows, unexpected exception) — same convention as
    /// every other probe in this file (<see cref="VpinmameRegistry.TryGetRomPath"/> etc.): "null
    /// means I don't know", the caller must not treat it as "confirmed absent". Never throws.
    /// </summary>
    /// <remarks>
    /// Use <see cref="TryProbe"/> instead when the caller needs to tell "confirmed not
    /// registered" apart from "could not read the registry at all" — <see cref="Scanning.ComHealthScanner"/>'s
    /// <c>VPINMAME_NOT_REGISTERED</c> Critical (spec A.3) depends on exactly that distinction: a
    /// registry read failure must never be reported as "not registered".
    /// </remarks>
    public static ComRegistration? TryResolve(string progId, ComRegistryView view) => TryProbe(progId, view).Registration;

    /// <summary>
    /// Same resolution as <see cref="TryResolve"/>, but reports whether the read itself
    /// succeeded. A registry key genuinely not existing (Win32 error 2, ERROR_FILE_NOT_FOUND) is
    /// a SUCCESSFUL read with a negative result — Windows confidently told us the ProgID is not
    /// registered in this view. Any other outcome (access denied, a locked/corrupt hive,
    /// non-Windows, an unexpected exception) is a FAILED read, and callers must never render that
    /// as "not registered". Never throws.
    /// </summary>
    public static (bool Succeeded, ComRegistration? Registration) TryProbe(string progId, ComRegistryView view)
    {
        if (!OperatingSystem.IsWindows()) return (false, null);
        try { return ReadWindows(progId, view); }
        catch { return (false, null); }
    }

    [SupportedOSPlatform("windows")]
    private static (bool, ComRegistration?) ReadWindows(string progId, ComRegistryView view)
    {
        var wow64Flag = view == ComRegistryView.Registry32 ? KEY_WOW64_32KEY : KEY_WOW64_64KEY;

        var rcProgId = TryOpenAndReadDefault($@"{progId}\CLSID", wow64Flag, out var clsid);
        if (rcProgId == ERROR_FILE_NOT_FOUND) return (true, null);   // confidently not registered
        if (rcProgId != 0) return (false, null);                     // could not determine
        if (string.IsNullOrWhiteSpace(clsid)) return (true, null);   // key present but empty — not usably registered

        var (rcServer, serverPath) = ReadServerPath(clsid!, wow64Flag);
        if (rcServer == ERROR_FILE_NOT_FOUND) return (true, null);
        if (rcServer != 0) return (false, null);
        if (string.IsNullOrWhiteSpace(serverPath)) return (true, null);

        return (true, new ComRegistration { ProgId = progId, Clsid = clsid!, ServerPath = serverPath!, View = view });
    }

    /// <summary>
    /// Chain step 2: <c>InprocServer32</c>, falling back to <c>LocalServer32</c> — spec A.1:
    /// "si absent, essayer LocalServer32 — les deux existent selon le composant".
    /// </summary>
    [SupportedOSPlatform("windows")]
    private static (int rc, string? path) ReadServerPath(string clsid, int wow64Flag)
    {
        var rcInproc = TryOpenAndReadDefault($@"CLSID\{clsid}\InprocServer32", wow64Flag, out var inprocPath);
        if (rcInproc == 0 && !string.IsNullOrWhiteSpace(inprocPath)) return (0, inprocPath);
        if (rcInproc != 0 && rcInproc != ERROR_FILE_NOT_FOUND) return (rcInproc, null);

        var rcLocal = TryOpenAndReadDefault($@"CLSID\{clsid}\LocalServer32", wow64Flag, out var localPath);
        if (rcLocal == 0 && !string.IsNullOrWhiteSpace(localPath)) return (0, localPath);
        if (rcLocal != 0 && rcLocal != ERROR_FILE_NOT_FOUND) return (rcLocal, null);

        return (ERROR_FILE_NOT_FOUND, null);   // neither present — confidently no server registered
    }

    [SupportedOSPlatform("windows")]
    private static int TryOpenAndReadDefault(string subKey, int wow64Flag, out string? value)
    {
        value = null;
        var rc = RegOpenKeyEx(HkeyClassesRoot, subKey, 0, KEY_READ | wow64Flag, out var hKey);
        if (rc != 0) return rc;
        try { value = ReadDefaultStringValue(hKey); return 0; }
        finally { RegCloseKey(hKey); }
    }

    /// <summary>Reads the key's unnamed "(Default)" value — an empty string as the value NAME, per Win32 convention.</summary>
    [SupportedOSPlatform("windows")]
    private static string? ReadDefaultStringValue(IntPtr hKey)
    {
        const uint REG_SZ = 1, REG_EXPAND_SZ = 2;
        uint type = 0, cb = 0;
        if (RegQueryValueEx(hKey, "", IntPtr.Zero, ref type, null, ref cb) != 0) return null;
        if ((type != REG_SZ && type != REG_EXPAND_SZ) || cb == 0) return null;

        var data = new byte[cb];
        if (RegQueryValueEx(hKey, "", IntPtr.Zero, ref type, data, ref cb) != 0) return null;

        var s = Encoding.Unicode.GetString(data, 0, (int)cb).TrimEnd('\0').Trim();
        if (type == REG_EXPAND_SZ) s = Environment.ExpandEnvironmentVariables(s);
        return string.IsNullOrWhiteSpace(s) ? null : s;
    }

    [SupportedOSPlatform("windows")]
    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int RegOpenKeyEx(IntPtr hKey, string subKey, int options, int samDesired, out IntPtr phkResult);

    [SupportedOSPlatform("windows")]
    [DllImport("advapi32.dll", EntryPoint = "RegQueryValueExW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int RegQueryValueEx(IntPtr hKey, string valueName, IntPtr reserved, ref uint type, byte[]? data, ref uint cbData);

    [SupportedOSPlatform("windows")]
    [DllImport("advapi32.dll")]
    private static extern int RegCloseKey(IntPtr hKey);
}
