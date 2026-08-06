using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace PincabToolbox.Core.Services;

/// <summary>
/// Reads the current Windows default playback device's friendly name — nothing else. A
/// deliberately narrow, read-only mirror of the COM surface <c>RealAudioDeviceControl</c> (in
/// PincabToolbox.Repair) uses to WRITE the default device: only
/// <c>IMMDeviceEnumerator.GetDefaultAudioEndpoint</c> + <c>IMMDevice.OpenPropertyStore</c> +
/// <c>IPropertyStore.GetValue</c> are declared here. <c>IPolicyConfig</c> (the undocumented
/// interface that actually changes the default device) is not declared at all in Core — this file
/// is structurally incapable of writing to the audio device, by construction, not by convention.
///
/// <para>
/// Duplicated here rather than shared with Repair because <c>PincabToolbox.Core</c> must not
/// depend on <c>PincabToolbox.Repair</c> (the dependency runs the other way — Repair reads Core's
/// Finding/ScanContext types). Carries the same Vista→Windows 10 COM-interface caveat as
/// RealAudioDeviceControl's header (unverifiable in a Linux sandbox, needs real-cab validation)
/// but a much smaller blast radius on failure: at worst a missing Finding, never a wrong device
/// change.
/// </para>
///
/// <para>
/// EnumAudioEndpoints and GetDevice are declared (as <c>IntPtr</c> stand-ins for their real
/// interface-typed parameters) purely to keep <see cref="IMMDeviceEnumerator"/>'s vtable slots in
/// the COM-mandated order — this reader never calls them. Only the two methods this reader
/// actually uses are given their real interface types.
/// </para>
/// </summary>
[SupportedOSPlatform("windows")]
public static class AudioEndpointReader
{
    /// <summary>Friendly name of the current default playback (render) device, or null when unavailable (non-Windows, no default device, COM failure…).</summary>
    public static string? TryGetDefaultPlaybackDeviceName()
    {
        if (!OperatingSystem.IsWindows()) return null;
        try { return ReadWindows(); }
        catch { return null; } // a COM interop that doesn't match this Windows build must never crash a scan
    }

    [SupportedOSPlatform("windows")]
    private static string? ReadWindows()
    {
        var enumerator = (IMMDeviceEnumerator)new MMDeviceEnumeratorComObject();
        enumerator.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia, out var device);
        device.OpenPropertyStore(0 /* STGM_READ */, out var store);
        var key = PKEY_Device_FriendlyName;
        store.GetValue(ref key, out var pv);
        try { return Marshal.PtrToStringUni(pv.pointerValue); }
        finally { PropVariantClear(ref pv); }
    }

    private static PROPERTYKEY PKEY_Device_FriendlyName => new()
    {
        fmtid = new Guid("a45c254e-df1c-4efd-8020-67d146a850e0"),
        pid = 14,
    };

    [DllImport("ole32.dll")]
    private static extern void PropVariantClear(ref PROPVARIANT pvar);

    // ── minimal read-only COM surface: mmdeviceapi.h subset only, no IPolicyConfig anywhere ──

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

    [ComImport, Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"),
     InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMMDeviceEnumerator
    {
        void EnumAudioEndpoints(EDataFlow dataFlow, int dwStateMask, out IntPtr devices); // unused; kept for vtable order
        void GetDefaultAudioEndpoint(EDataFlow dataFlow, ERole role, out IMMDevice endpoint);
        void GetDevice([MarshalAs(UnmanagedType.LPWStr)] string id, out IntPtr device); // unused; kept for vtable order
        void RegisterEndpointNotificationCallback(IntPtr client); // unused; kept for vtable order
        void UnregisterEndpointNotificationCallback(IntPtr client); // unused; kept for vtable order
    }

    [ComImport, Guid("D666063F-1587-4E43-81F1-B948E807363F"),
     InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMMDevice
    {
        void Activate(ref Guid iid, int dwClsCtx, IntPtr pActivationParams, out IntPtr ppInterface); // unused; kept for vtable order
        void OpenPropertyStore(int stgmAccess, out IPropertyStore properties);
        void GetId([MarshalAs(UnmanagedType.LPWStr)] out string id); // unused; kept for vtable order
        void GetState(out int state); // unused; kept for vtable order
    }

    [ComImport, Guid("886d8eeb-8cf2-4446-8d02-cdba1dbdcf99"),
     InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IPropertyStore
    {
        void GetCount(out uint count); // unused; kept for vtable order
        void GetAt(uint index, out PROPERTYKEY key); // unused; kept for vtable order
        void GetValue(ref PROPERTYKEY key, out PROPVARIANT value);
        void SetValue(ref PROPERTYKEY key, ref PROPVARIANT value); // unused; kept for vtable order
        void Commit(); // unused; kept for vtable order
    }
}
