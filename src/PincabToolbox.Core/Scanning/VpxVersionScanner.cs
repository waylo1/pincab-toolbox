using System.Diagnostics;
using System.Text.RegularExpressions;
using PincabToolbox.Core.Models;
using PincabToolbox.Core.Services;

namespace PincabToolbox.Core.Scanning;

/// <summary>
/// Compares the VPX version a table DECLARES it needs (the same <c>requires-vpx-version</c> script
/// signature the Compatibility Linter reads) against the newest VPX version actually installed, and
/// raises a Warning only when the install genuinely falls short.
///
/// <para>
/// This is the comparison <c>CompatibilityScanner</c> deliberately does NOT make: it reports the
/// declared minimum as a neutral <c>COMPAT_MIN_VERSION</c> Info because, on its own, a declaration is
/// not a defect — flagging every table that merely names a version once flipped a healthy 2090-table
/// collection to grade F (FIELD-LOG 2026-07-30). The missing half was the installed version. Now that
/// we can read it, an <em>actual</em> shortfall (installed &lt; required) is a real, actionable problem
/// and is surfaced — exactly the follow-up that scanner's own comment anticipated.
/// </para>
///
/// <para>
/// Same false-positive discipline as the rest of the Scanner, applied without compromise:
/// installed version unreadable (no VPX exe, no version resource, unparseable) → silent;
/// installed &gt;= required → silent; only a strict shortfall on the NEWEST installed VPX is reported.
/// A missing installed version can never produce a finding. This is a new file: no existing scanner is
/// touched, and the declared-version regex stays owned by the profile, so there is a single source of
/// truth shared with the Compatibility Linter.
/// </para>
///
/// <para>
/// Severity is <see cref="Severity.Warning"/>, deliberately not Critical. The requirement is a heuristic
/// string in a table comment, not a machine-verified contract; a false Critical would tank the health
/// score (uncapped −15 each) and hijack the "FIX THIS FIRST" banner — the precise asymmetric damage of
/// the July-30 incident. Warning honestly says "likely to misbehave, update recommended" without that
/// blast radius. If field returns later show these tables hard-fail, the single
/// <see cref="VpxVersionComparer.IsOutdated"/> call below is the one place to raise it — once calibrated
/// on real data (PROJECT-BRAIN §7.4), not guessed.
/// </para>
/// </summary>
public sealed class VpxVersionScanner : IScanner
{
    public string Id => "vpxversion";
    public string Name => "VPX Version Check";

    /// <summary>Profile signature id whose capture group 1 is the declared minimum VPX version.</summary>
    private const string RequiresVersionSignatureId = "requires-vpx-version";

    private readonly Func<string, string?> _readInstalledVersion;

    /// <param name="installedVersionReader">
    /// Reads a VPX executable's version string (product/file version). Defaults to a PE version-resource
    /// read; injected in tests so the decision path runs without a real Windows binary. (Same
    /// constructor-injection pattern as <see cref="UpdateWatcherScanner"/>.)
    /// </param>
    public VpxVersionScanner(Func<string, string?>? installedVersionReader = null)
        => _readInstalledVersion = installedVersionReader ?? ReadPeProductVersion;

    public IEnumerable<Finding> Scan(ScanContext ctx)
    {
        // Newest installed VPX version across every detected executable. If not a single one is
        // readable, we have nothing trustworthy to compare against — stay completely silent.
        if (!VpxVersionComparer.TryHighestInstalled(
                ctx.Layout.VpxExecutables.Select(_readInstalledVersion),
                out var installedMajor, out var installedMinor))
            yield break;

        // Reuse the profile's declared-version signature — never redefine the regex here, so this
        // scanner and the Compatibility Linter can never disagree about what a table "requires".
        var signature = ctx.Profile.ScriptSignatures
            .FirstOrDefault(s => s.Id == RequiresVersionSignatureId && !string.IsNullOrWhiteSpace(s.Regex));
        if (signature is null) yield break;

        Regex regex;
        try { regex = new Regex(signature.Regex, RegexOptions.Compiled, TimeSpan.FromSeconds(2)); }
        catch (ArgumentException) { yield break; } // a malformed profile regex is not the user's fault

        foreach (var (path, table) in ctx.Tables)
        {
            ctx.Cancellation.ThrowIfCancellationRequested();
            if (table.Script is null) continue;

            Match m;
            try { m = regex.Match(table.Script); }
            catch (RegexMatchTimeoutException) { continue; }
            if (!m.Success || m.Groups.Count <= 1) continue;

            if (!VpxVersionComparer.TryParseMajorMinor(m.Groups[1].Value, out var requiredMajor, out var requiredMinor))
                continue; // unparseable declaration → no honest verdict

            if (!VpxVersionComparer.IsOutdated(installedMajor, installedMinor, requiredMajor, requiredMinor))
                continue; // installed meets or exceeds requirement → silent

            var name = Path.GetFileNameWithoutExtension(path);
            var required = $"{requiredMajor}.{requiredMinor}";
            var installed = $"{installedMajor}.{installedMinor}";
            yield return new Finding
            {
                Code = "VPX_VERSION_OUTDATED", Severity = Severity.Warning, Category = Id,
                Subject = name, FilePath = path,
                Args = new[] { name, required, installed },
                EnglishText = $"'{name}' declares it needs Visual Pinball X {required}+, but the newest VPX installed is {installed} — this table may fail to load or run incorrectly until Visual Pinball X is updated.",
                FixHint = $"Update Visual Pinball X to {required} or newer (newest detected: {installed}). You can keep your current version alongside it — VPX builds can coexist, so other tables stay unaffected.",
            };
        }
    }

    /// <summary>
    /// Reads the product (or file) version string from a PE executable's version resource. Returns null
    /// on any failure — missing file, no version resource, or an I/O error — which the caller treats as
    /// "this executable's version is unknown". BCL only (no external dependency), works cross-platform.
    /// </summary>
    private static string? ReadPeProductVersion(string exePath)
    {
        try
        {
            var info = FileVersionInfo.GetVersionInfo(exePath);
            var version = info.ProductVersion;
            if (string.IsNullOrWhiteSpace(version)) version = info.FileVersion;
            return string.IsNullOrWhiteSpace(version) ? null : version;
        }
        catch
        {
            return null;
        }
    }
}
