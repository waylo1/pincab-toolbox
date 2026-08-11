using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace PincabToolbox.Repair;

/// <summary>Real file system. The only place in Repair that touches System.IO directly.</summary>
public sealed class RealFileSystem : IFileSystem
{
    public bool FileExists(string path) => File.Exists(path);
    public bool DirectoryExists(string path) => Directory.Exists(path);

    public IReadOnlyList<string> GetFiles(string directory)
        => Directory.Exists(directory) ? Directory.GetFiles(directory) : Array.Empty<string>();

    public IReadOnlyList<string> GetDirectories(string directory)
        => Directory.Exists(directory) ? Directory.GetDirectories(directory) : Array.Empty<string>();

    public byte[] ReadAllBytes(string path) => File.ReadAllBytes(path);

    public void WriteAllBytes(string path, byte[] content)
    {
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        File.WriteAllBytes(path, content);
    }

    public void DeleteFile(string path) => File.Delete(path);
    public void MoveFile(string source, string destination) => File.Move(source, destination);
    public void MoveDirectory(string source, string destination) => Directory.Move(source, destination);
    public void CreateDirectory(string path) => Directory.CreateDirectory(path);

    // ── Mark of the Web ────────────────────────────────────────────────
    // NTFS alternate data stream. Windows-only; a no-op elsewhere so the engine
    // stays testable and runnable on any OS.

    private static bool IsWindows => OperatingSystem.IsWindows();

    public bool HasZoneIdentifier(string path)
        => IsWindows && File.Exists(path + ":Zone.Identifier");

    public void RemoveZoneIdentifier(string path)
    {
        if (!IsWindows) return;
        var stream = path + ":Zone.Identifier";
        if (File.Exists(stream)) File.Delete(stream);
    }

    public void AddZoneIdentifier(string path)
    {
        if (!IsWindows) return;
        File.WriteAllText(path + ":Zone.Identifier", "[ZoneTransfer]\r\nZoneId=3\r\n");
    }
}

/// <summary>
/// Real environment probe. Process names are matched without extension, case-insensitively.
/// </summary>
public sealed class RealEnvironmentProbe : IEnvironmentProbe
{
    /// <summary>
    /// Anything that holds the install's files open. Writing while one of these runs
    /// can corrupt files, so the engine refuses rather than warns.
    /// </summary>
    public static readonly string[] BlockingProcessNames =
    {
        "VPinballX", "VPinballX64", "VPinballX_GL64",
        "PinUpPlayer", "PinUpMenu", "PinUpDisplay", "PinUpPacksEditor",
        "VPinMAME", "VPinMAMETest", "B2SBackglassServerEXE", "DOFLinxMSFS",
    };

    private readonly string _backupVolumePath;

    public RealEnvironmentProbe(string backupVolumePath) => _backupVolumePath = backupVolumePath;

    public IReadOnlyList<string> RunningBlockingProcesses()
    {
        var found = new List<string>();
        foreach (var name in BlockingProcessNames)
        {
            try
            {
                if (Process.GetProcessesByName(name).Length > 0) found.Add(name);
            }
            catch
            {
                // Enumerating processes can fail on locked-down machines.
                // Treat as "unknown", not as "clear" — but do not block on it either:
                // the write-access check remains the real safety net.
            }
        }
        return found;
    }

    public long FreeBackupSpaceBytes()
    {
        try
        {
            var root = Path.GetPathRoot(Path.GetFullPath(_backupVolumePath));
            if (string.IsNullOrEmpty(root)) return long.MaxValue;
            return new DriveInfo(root).AvailableFreeSpace;
        }
        catch { return long.MaxValue; }   // unknown: do not block on a probe failure
    }

    public bool CanWriteTo(string target)
    {
        try
        {
            if (File.Exists(target))
                return !new FileInfo(target).IsReadOnly;

            var dir = Path.GetDirectoryName(target);
            return string.IsNullOrEmpty(dir) || Directory.Exists(dir);
        }
        catch { return false; }
    }
}

/// <summary>
/// Real process control, by name (no extension). Used by
/// <see cref="Actions.KillZombiePinUpDisplayAction"/> — the surface is generic (any process
/// name), but the closed action registry (ADR-005) is what keeps a Knowledge Pack from ever
/// being able to name an arbitrary target here: only the hardcoded "PinUpDisplay" is ever passed.
/// </summary>
public sealed class RealProcessControl : IProcessControl
{
    public bool IsRunning(string processName) => Core.Services.ProcessProbe.IsRunning(processName);

    public string? PathOf(string processName) => Core.Services.ProcessProbe.TryGetExecutablePath(processName);

    public bool Kill(string processName)
    {
        try
        {
            var procs = Process.GetProcessesByName(processName);
            if (procs.Length == 0) return true;   // nothing running is a successful no-op

            var allDown = true;
            foreach (var p in procs)
            {
                try
                {
                    p.Kill();
                    p.WaitForExit(5000);
                    if (!p.HasExited) allDown = false;
                }
                catch { allDown = false; }
                finally { p.Dispose(); }
            }
            return allDown;
        }
        catch { return false; }
    }
}

/// <summary>
/// Real process launcher for LOT I (<see cref="Actions.RegisterComComponentAction"/>). The ONLY
/// place in this codebase that starts a foreign executable — direct <c>Process.Start</c>, no
/// shell, no arguments, ever. Deliberately does NOT kill the child on timeout: the three
/// whitelisted registration tools (VPinMAME's <c>Setup.exe</c> in particular) are ordinary GUI
/// installers, not silent CLI utilities — a user may need to click something inside the tool's own
/// window, and killing that window out from under them would be actively harmful, not safe. A
/// timeout here means only "this call stops waiting", never "the process is terminated" — the
/// app's own UI thread is what rule 5 protects from freezing, not the child process's lifetime.
/// </summary>
public sealed class RealProcessLauncher : IProcessLauncher
{
    public ProcessLaunchResult Launch(string exePath, TimeSpan timeout)
    {
        Process? process = null;
        try
        {
            process = Process.Start(new ProcessStartInfo
            {
                FileName = exePath,
                UseShellExecute = false,   // no shell, no interpolation — direct process creation
                WorkingDirectory = Path.GetDirectoryName(exePath) ?? "",
                // Deliberately no Arguments/ArgumentList set — rule 3: zero arguments, ever.
            });
            if (process is null) return ProcessLaunchResult.Failed("process failed to start");

            var exited = process.WaitForExit((int)Math.Clamp(timeout.TotalMilliseconds, 0, int.MaxValue));
            return exited ? ProcessLaunchResult.Ok(process.ExitCode) : ProcessLaunchResult.TimedOutResult();
        }
        catch (System.ComponentModel.Win32Exception win32)
        {
            // ERROR_ELEVATION_REQUIRED (740): the tool's own manifest demands admin and Windows
            // refused to start it at all — a clean signal, distinct from every other failure.
            return ProcessLaunchResult.Failed(
                win32.NativeErrorCode == 740 ? "elevation required" : win32.Message);
        }
        catch (Exception e)
        {
            return ProcessLaunchResult.Failed(e.Message);
        }
        finally
        {
            process?.Dispose();
        }
    }
}

/// <summary>
/// Real elevation check via the process's own security token — not the static
/// <c>app.manifest</c> (which only controls whether Windows AUTO-elevates on launch; a user can
/// still right-click "Run as administrator" by hand, which this DOES see). Hand-rolled P/Invoke
/// against <c>advapi32.dll</c>, same posture as every other native call in this codebase (zero
/// external dependency — see <c>RegistryReader</c> in Core for the precedent).
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class RealElevationProbe : IElevationProbe
{
    private const uint TOKEN_QUERY = 0x0008;
    private const int TokenElevation = 20;

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool OpenProcessToken(IntPtr processHandle, uint desiredAccess, out IntPtr tokenHandle);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool GetTokenInformation(
        IntPtr tokenHandle, int tokenInformationClass,
        out int tokenInformation, int tokenInformationLength, out int returnLength);

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetCurrentProcess();

    [DllImport("kernel32.dll")]
    private static extern bool CloseHandle(IntPtr handle);

    public bool IsCurrentProcessElevated()
    {
        if (!OperatingSystem.IsWindows()) return false;

        IntPtr token = IntPtr.Zero;
        try
        {
            if (!OpenProcessToken(GetCurrentProcess(), TOKEN_QUERY, out token)) return false;
            if (!GetTokenInformation(token, TokenElevation, out var elevation, sizeof(int), out _)) return false;
            return elevation != 0;
        }
        catch
        {
            // Unknown must never be reported as elevated — the whole point of this check is to
            // avoid a false "you have admin rights" that leads the caller to attempt a write that
            // then fails confusingly deep inside a third-party tool.
            return false;
        }
        finally
        {
            if (token != IntPtr.Zero) CloseHandle(token);
        }
    }
}

/// <summary>
/// Real default-playback-device control, for <see cref="Actions.SetDefaultAudioDeviceAction"/>.
///
/// Windows has no PUBLIC API to change the default audio endpoint — every tool that does it
/// (including the community's own NirCMD, FIELD-LOG 2026-07-29) goes through the same
/// undocumented <c>IPolicyConfig</c> COM interface used here. It has been stable from Vista
/// through Windows 10; Windows 10 2004+ and Windows 11 are known to sometimes require a
/// different interface layout (this is why several NirCMD-alternative tools ship version
/// -specific fallbacks). This implementation targets the original, most widely deployed layout
/// only. <b>Needs validation on Maxime's own cab before this ships</b> (see TRANSMISSION.md) —
/// every call is wrapped so a mismatch degrades to "could not set the device" rather than
/// crashing the app, but a COM interop that doesn't match reality cannot be caught by tests
/// running in a Linux sandbox, only by running it for real.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class RealAudioDeviceControl : IAudioDeviceControl
{
    private const int DEVICE_STATE_ACTIVE = 0x1;

    public string? GetDefaultPlaybackDeviceId()
    {
        if (!OperatingSystem.IsWindows()) return null;
        try
        {
            var enumerator = (IMMDeviceEnumerator)new MMDeviceEnumeratorComObject();
            enumerator.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia, out var device);
            device.GetId(out var id);
            return id;
        }
        catch { return null; }
    }

    public string? FindPlaybackDeviceId(string nameContains)
    {
        if (!OperatingSystem.IsWindows()) return null;
        try
        {
            var enumerator = (IMMDeviceEnumerator)new MMDeviceEnumeratorComObject();
            enumerator.EnumAudioEndpoints(EDataFlow.eRender, DEVICE_STATE_ACTIVE, out var collection);
            collection.GetCount(out var count);
            for (uint i = 0; i < count; i++)
            {
                collection.Item(i, out var device);
                var name = TryGetFriendlyName(device);
                if (name is null || !name.Contains(nameContains, StringComparison.OrdinalIgnoreCase)) continue;
                device.GetId(out var id);
                return id;
            }
            return null;
        }
        catch { return null; }
    }

    public bool SetDefaultPlaybackDevice(string deviceId)
    {
        if (!OperatingSystem.IsWindows()) return false;
        try
        {
            var policy = (IPolicyConfig)new PolicyConfigClientComObject();
            // All three roles, matching what the community's NirCMD-based script did — a table's
            // sound may be queried under any of them depending on the API it uses.
            policy.SetDefaultEndpoint(deviceId, ERole.eConsole);
            policy.SetDefaultEndpoint(deviceId, ERole.eMultimedia);
            policy.SetDefaultEndpoint(deviceId, ERole.eCommunications);
            return true;
        }
        catch { return false; }
    }

    private static string? TryGetFriendlyName(IMMDevice device)
    {
        try
        {
            device.OpenPropertyStore(0 /* STGM_READ */, out var store);
            var key = PKEY_Device_FriendlyName;
            store.GetValue(ref key, out var pv);
            try { return Marshal.PtrToStringUni(pv.pointerValue); }
            finally { PropVariantClear(ref pv); }
        }
        catch { return null; }
    }

    private static PROPERTYKEY PKEY_Device_FriendlyName => new()
    {
        fmtid = new Guid("a45c254e-df1c-4efd-8020-67d146a850e0"),
        pid = 14,
    };

    [DllImport("ole32.dll")]
    private static extern void PropVariantClear(ref PROPVARIANT pvar);

    // ── minimal COM surface: just enough of mmdeviceapi.h + the undocumented IPolicyConfig ──

    private enum EDataFlow { eRender = 0, eCapture = 1, eAll = 2 }
    private enum ERole { eConsole = 0, eMultimedia = 1, eCommunications = 2 }

    [StructLayout(LayoutKind.Sequential)]
    private struct PROPERTYKEY { public Guid fmtid; public int pid; }

    [StructLayout(LayoutKind.Explicit)]
    private struct PROPVARIANT
    {
        [FieldOffset(0)] public ushort vt;
        [FieldOffset(8)] public IntPtr pointerValue;
    }

    [ComImport, Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
    private class MMDeviceEnumeratorComObject { }

    [ComImport, Guid("870AF99C-171D-4F9E-AF0D-E63DF40C2BC9")]
    private class PolicyConfigClientComObject { }

    [ComImport, Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"),
     InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMMDeviceEnumerator
    {
        void EnumAudioEndpoints(EDataFlow dataFlow, int dwStateMask, out IMMDeviceCollection devices);
        void GetDefaultAudioEndpoint(EDataFlow dataFlow, ERole role, out IMMDevice endpoint);
        void GetDevice(string id, out IMMDevice device);
        void RegisterEndpointNotificationCallback(IntPtr client);
        void UnregisterEndpointNotificationCallback(IntPtr client);
    }

    [ComImport, Guid("0BD7A1BE-7A1A-44DB-8397-CC5392387B5E"),
     InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMMDeviceCollection
    {
        void GetCount(out uint count);
        void Item(uint index, out IMMDevice device);
    }

    [ComImport, Guid("D666063F-1587-4E43-81F1-B948E807363F"),
     InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMMDevice
    {
        void Activate(ref Guid iid, int dwClsCtx, IntPtr pActivationParams, out IntPtr ppInterface);
        void OpenPropertyStore(int stgmAccess, out IPropertyStore properties);
        void GetId([MarshalAs(UnmanagedType.LPWStr)] out string id);
        void GetState(out int state);
    }

    [ComImport, Guid("886d8eeb-8cf2-4446-8d02-cdba1dbdcf99"),
     InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IPropertyStore
    {
        void GetCount(out uint count);
        void GetAt(uint index, out PROPERTYKEY key);
        void GetValue(ref PROPERTYKEY key, out PROPVARIANT value);
        void SetValue(ref PROPERTYKEY key, ref PROPVARIANT value);
        void Commit();
    }

    /// <summary>The undocumented interface every default-audio-device tool relies on (Vista→Win10).</summary>
    [ComImport, Guid("f8679f50-850a-41cf-9c72-430f290290c8"),
     InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IPolicyConfig
    {
        void GetMixFormat(string id, out IntPtr format);
        void GetDeviceFormat(string id, bool bDefault, out IntPtr format);
        void ResetDeviceFormat(string id);
        void SetDeviceFormat(string id, IntPtr endpointFormat, IntPtr mixFormat);
        void GetProcessingPeriod(string id, bool bDefault, out long defaultPeriod, out long minimumPeriod);
        void SetProcessingPeriod(string id, long period);
        void GetShareMode(string id, out IntPtr mode);
        void SetShareMode(string id, IntPtr mode);
        void GetPropertyValue(string id, bool bFxStore, ref PROPERTYKEY key, out PROPVARIANT value);
        void SetPropertyValue(string id, bool bFxStore, ref PROPERTYKEY key, ref PROPVARIANT value);
        void SetDefaultEndpoint(string id, ERole role);
        void SetEndpointVisibility(string id, bool visible);
    }
}
