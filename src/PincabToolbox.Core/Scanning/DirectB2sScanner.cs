using PincabToolbox.Core.Models;
using PincabToolbox.Core.Services;

namespace PincabToolbox.Core.Scanning;

/// <summary>
/// Flags a <c>.directb2s</c> backglass file that isn't well-formed XML — B2S Backglass Server refuses
/// to load it outright ("not a valid directb2s backglass file"), so the backglass simply doesn't
/// appear, with no signal anywhere else in the install pointing at why (audit §4/H2).
///
/// <para>
/// <b>Scope note on the "compressed" variant the handoff flagged as possible.</b> Research (see
/// <see cref="DirectB2SValidator"/>) checked the DirectB2S Designer's own exporter source, the B2S
/// Backglass Server's own loader source, and an independent third-party parser exercised against real
/// user table collections — all three treat .directb2s as plain XML only, with no compression or OLE
/// container anywhere. No confirmed real-world sample or source reference for an OLE-compressed
/// variant turned up. Rather than silently drop the handoff's caution, this scanner still recognizes
/// the MS-CFB signature (the same one <see cref="PincabToolbox.Core.Vpx.CompoundFileReader"/> already
/// reads for .vpx) and treats a file starting with it as "a different, unrecognized format" rather
/// than "broken" — silence, not a guess either way. Actually decoding such a file would mean guessing
/// at an internal stream layout no source confirmed, which this session's whole discipline has
/// consistently refused to do (see B2's altsound.csv and C1's ScreenRes.txt research, both done before
/// writing a parser). If Maxime has a real sample that starts with that signature, decoding it
/// properly is a small additive follow-up once its actual internal shape is known.
/// </para>
///
/// <para>
/// Deliberately flags a 0-byte file too (not specially skipped) — same philosophy as
/// <see cref="NvramScanner"/>'s 0-byte NVRAM check: an empty file is unambiguously something
/// B2S cannot load, most commonly an interrupted download or save, and "0 bytes" is exactly the kind
/// of deterministic, zero-FP fact this whole session has been built to report.
/// </para>
/// </summary>
public sealed class DirectB2sScanner : IScanner
{
    public string Id => "directb2s";
    public string Name => "DirectB2S Integrity";

    private readonly Func<string, IReadOnlyCollection<string>> _listFiles;
    private readonly Func<string, byte[]?> _readBytes;

    /// <param name="listFiles">Given the tables folder, returns every .directb2s file path directly inside it. Defaults to a real directory listing.</param>
    /// <param name="readBytes">Given a file path, returns its raw bytes, or null when missing/unreadable. Defaults to a real file read.</param>
    public DirectB2sScanner(Func<string, IReadOnlyCollection<string>>? listFiles = null, Func<string, byte[]?>? readBytes = null)
    {
        _listFiles = listFiles ?? ListOnDisk;
        _readBytes = readBytes ?? ReadBytesOrNull;
    }

    public IEnumerable<Finding> Scan(ScanContext ctx)
    {
        if (ctx.Layout.TablesDir is null) yield break;

        IReadOnlyCollection<string> files;
        try { files = _listFiles(ctx.Layout.TablesDir); } catch { yield break; }

        foreach (var file in files)
        {
            ctx.Cancellation.ThrowIfCancellationRequested();

            byte[]? bytes;
            try { bytes = _readBytes(file); } catch { continue; } // unreadable -> silence
            if (bytes is null) continue;

            if (DirectB2SValidator.IsWellFormedXml(bytes)) continue; // healthy
            if (DirectB2SValidator.LooksLikeCompoundFile(bytes)) continue; // unrecognized alt container, not claimed broken

            var name = Path.GetFileName(file);
            yield return new Finding
            {
                Code = "B2S_MALFORMED", Severity = Severity.Warning, Category = Id,
                Subject = name, FilePath = file,
                Args = new[] { name },
                EnglishText = $"'{name}' is not well-formed XML — B2S Backglass Server will refuse to load it (typically \"not a valid directb2s backglass file\"), so this backglass will not appear at all.",
                FixHint = "Re-download or re-export this backglass — a truncated download or an interrupted save is the most common cause of a broken .directb2s.",
            };
        }
    }

    private static IReadOnlyCollection<string> ListOnDisk(string tablesDir)
        => Directory.Exists(tablesDir)
            ? Directory.EnumerateFiles(tablesDir, "*.directb2s", SearchOption.TopDirectoryOnly).ToList()
            : Array.Empty<string>();

    private static byte[]? ReadBytesOrNull(string path)
        => File.Exists(path) ? File.ReadAllBytes(path) : null;
}
