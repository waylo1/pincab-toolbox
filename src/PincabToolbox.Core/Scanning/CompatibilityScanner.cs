using System.Text.RegularExpressions;
using PincabToolbox.Core.Models;

namespace PincabToolbox.Core.Scanning;

/// <summary>
/// Applies the profile's script signatures to every table script (nFozzy physics,
/// declared minimum VPX version, FlexDMD/B2S usage…). Pure heuristics, honestly labelled.
/// </summary>
public sealed class CompatibilityScanner : IScanner
{
    public string Id => "compat";
    public string Name => "Compatibility Linter";

    public IEnumerable<Finding> Scan(ScanContext ctx)
    {
        var compiled = ctx.Profile.ScriptSignatures
            .Where(s => !string.IsNullOrWhiteSpace(s.Regex))
            .Select(s => (sig: s, regex: new Regex(s.Regex, RegexOptions.Compiled, TimeSpan.FromSeconds(2))))
            .ToList();

        foreach (var (path, table) in ctx.Tables)
        {
            ctx.Cancellation.ThrowIfCancellationRequested();
            if (table.Script is null) continue;
            var name = Path.GetFileNameWithoutExtension(path);

            foreach (var (sig, regex) in compiled)
            {
                Match m;
                try { m = regex.Match(table.Script); }
                catch (RegexMatchTimeoutException) { continue; }
                if (!m.Success) continue;

                if (sig.Id == "requires-vpx-version" && m.Groups.Count > 1)
                {
                    var requiredVersion = m.Groups[1].Value;
                    // Info, not Warning: we only read what the table *declares*, we never
                    // compare it to the VPX version actually installed — so we cannot honestly
                    // call this a defect. It's worth knowing, nothing is broken. Reporting it as
                    // Warning made every table in a large collection cost score points and flipped
                    // a healthy install to grade F (FIELD-LOG 2026-07-30 / FD report). If a future
                    // build learns the installed VPX version, an actual version < required
                    // mismatch could legitimately be raised to Warning/Critical then.
                    yield return new Finding
                    {
                        Code = "COMPAT_MIN_VERSION", Severity = Severity.Info, Category = Id,
                        Subject = name, FilePath = path,
                        Args = new[] { name, requiredVersion },
                        EnglishText = $"'{name}' declares it requires VPX {requiredVersion}+ — check your installed version before launching.",
                    };
                }
                else
                {
                    yield return new Finding
                    {
                        Code = "COMPAT_SIGNATURE", Severity = sig.Level == "warning" ? Severity.Warning : Severity.Info,
                        Category = Id, Subject = name, FilePath = path,
                        Args = new[] { name, sig.Meaning },
                        EnglishText = $"'{name}': {sig.Meaning}.",
                    };
                }
            }
        }
    }
}
