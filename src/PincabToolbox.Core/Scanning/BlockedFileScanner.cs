using System.Text.RegularExpressions;
using PincabToolbox.Core.Models;

namespace PincabToolbox.Core.Scanning;

/// <summary>
/// Detects DLLs blocked by Windows ("Mark of the Web"). When a file is extracted from a
/// downloaded ZIP, Windows attaches a Zone.Identifier NTFS alternate data stream; DLLs marked
/// this way silently fail to load (VPinMAME, dmddevice, B2S, FlexDMD…), which is one of the most
/// common — and most invisible — causes of a pincab "that just doesn't work". Strictly read-only:
/// it only reads the stream, it never unblocks anything.
/// </summary>
public sealed class BlockedFileScanner : IScanner
{
    public string Id => "security";
    public string Name => "Blocked-file check";

    // Plugins whose blocking outright breaks tables → Critical; anything else → Warning.
    private static readonly string[] CriticalNames =
    {
        "vpinmame.dll", "vpinmame64.dll",
        "dmddevice.dll", "dmddevice64.dll",
        "b2sbackglassserver.dll",
        "flexdmd.dll", "flexdmd64.dll",
        "dof.dll", "directoutput.dll",
    };

    private static readonly Regex ZoneId = new(@"ZoneId=(\d+)", RegexOptions.Compiled);

    /// <summary>
    /// Severity for a blocked file, by name. Blocking one of the core plugins breaks tables
    /// outright; blocking anything else degrades something narrower.
    /// Pure and public so the classification is testable without an NTFS stream.
    /// </summary>
    /// <remarks>
    /// Splits on both separators by hand rather than using <see cref="Path.GetFileName(string)"/>:
    /// off Windows, <c>System.IO.Path</c> does not treat <c>\</c> as a separator, so a Windows path
    /// would come back whole and never match. Same convention as
    /// <c>FileBackupService.LastSegment</c> and <c>RepairEngine.ProcessNameFromPath</c> — the third
    /// time this trap has bitten, so it is worth stating plainly.
    /// </remarks>
    public static Severity SeverityFor(string fileName)
    {
        var cut = fileName.LastIndexOfAny(new[] { '/', '\\' });
        var bare = cut >= 0 ? fileName[(cut + 1)..] : fileName;
        return CriticalNames.Contains(bare.ToLowerInvariant()) ? Severity.Critical : Severity.Warning;
    }

    /// <summary>
    /// Decides whether a Zone.Identifier stream's contents mean "blocked". Zone 3 is Internet and
    /// zone 4 Untrusted — exactly the cases where Windows shows the Unblock checkbox. Zones 0–2
    /// (local machine, intranet, trusted) are not blocked and must not be reported, or every file
    /// on a domain-joined cab would light up.
    /// Pure and public so the rule is testable on any OS.
    /// </summary>
    public static bool IsBlockedZone(string? zoneIdentifierContent)
    {
        if (string.IsNullOrEmpty(zoneIdentifierContent)) return false;
        var m = ZoneId.Match(zoneIdentifierContent);
        return m.Success && int.TryParse(m.Groups[1].Value, out var z) && z >= 3;
    }

    public IEnumerable<Finding> Scan(ScanContext context)
    {
        var findings = new List<Finding>();
        var root = context.Layout.RootPath;
        var ct = context.Cancellation;
        if (string.IsNullOrEmpty(root) || !Directory.Exists(root))
            return findings;

        IEnumerable<string> dlls;
        try
        {
            dlls = Directory.EnumerateFiles(root, "*.dll", SearchOption.AllDirectories);
        }
        catch
        {
            return findings; // unreadable tree — skip silently
        }

        int blocked = 0;
        foreach (var dll in dlls)
        {
            ct.ThrowIfCancellationRequested();
            if (!IsBlocked(dll)) continue;

            blocked++;
            var name = Path.GetFileName(dll);
            findings.Add(new Finding
            {
                Code = "BLOCKED_DLL",
                Severity = SeverityFor(name),
                Category = Id,
                Subject = name,
                FilePath = dll,
                Args = new[] { name },
                EnglishText = $"“{name}” is blocked by Windows (downloaded file) — it may silently fail to load until you unblock it.",
                FixHint = "Right-click the file → Properties → tick “Unblock” → OK. Or in PowerShell: Unblock-File “<path>”",
            });
        }

        if (blocked == 0)
        {
            findings.Add(new Finding
            {
                Code = "BLOCKED_NONE",
                Severity = Severity.Ok,
                Category = Id,
                Subject = "Windows block check",
                EnglishText = "No Windows-blocked DLLs found.",
            });
        }

        return findings;
    }

    /// <summary>
    /// A file carries the "Mark of the Web" when it has a Zone.Identifier NTFS stream with
    /// ZoneId 3 (Internet) or 4 (Untrusted) — exactly the case where Windows shows the Unblock box.
    /// </summary>
    private static bool IsBlocked(string path)
    {
        try
        {
            return IsBlockedZone(File.ReadAllText(path + ":Zone.Identifier"));
        }
        catch
        {
            return false; // no stream / not NTFS / access denied → not blocked
        }
    }
}
