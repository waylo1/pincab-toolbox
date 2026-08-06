using PincabToolbox.Core.Models;
using PincabToolbox.Core.Services;

namespace PincabToolbox.Core.Scanning;

/// <summary>
/// Flags games in PinUP Popper's database whose playlist assignment points at a playlist that no
/// longer exists — an orphaned foreign key that community reports (vpforums.org #50896) describe as
/// freezing the Popper frontend menu when opened, most often left behind after a playlist is deleted
/// from the UI while games are still assigned to it (audit §4/F1).
///
/// <para>
/// <b>Schema verified by research before writing a single query</b> — not previously documented
/// anywhere in this repo. Confirmed via NailBuster's own PinUP Popper wiki (SQL snippets he posted
/// himself) and independent, converging forum threads: playlist membership is NOT a column on
/// <c>Games</c>, it's a many-to-many junction table <c>PlayListDetails</c> (columns <c>GameID</c>,
/// <c>PlayListID</c>, <c>isFav</c>), joined against <c>Playlists</c> (<c>PlayListID</c>). Read-only —
/// consistent with ADR-007 (SqliteReader is a read-only parser; writing is out of scope).
/// </para>
///
/// <para>
/// One real gap this research could not close: <c>Playlists</c>' own name column was never confirmed
/// anywhere, so this scanner does not attempt to display a playlist name — only the affected game(s).
/// If <see cref="SqliteReader"/> can't find a table or the expected columns, it degrades to returning
/// null/empty rather than throwing (see its own doc comment), so a wrong guess here would make this
/// scanner silently find nothing rather than misreport — safe, if not maximally useful; a smaller,
/// additive follow-up once/if the real name column is confirmed.
/// </para>
/// </summary>
public sealed class PopperPlaylistScanner : IScanner
{
    public string Id => "popperplaylist";
    public string Name => "PUPDatabase Playlist Integrity";

    private const int MaxExamplesShown = 8;

    private readonly Func<string, List<string?[]>?> _readPlaylists;
    private readonly Func<string, List<string?[]>?> _readDetails;
    private readonly Func<string, List<string?[]>?> _readGames;

    public PopperPlaylistScanner(
        Func<string, List<string?[]>?>? readPlaylists = null,
        Func<string, List<string?[]>?>? readDetails = null,
        Func<string, List<string?[]>?>? readGames = null)
    {
        _readPlaylists = readPlaylists ?? (db => SqliteReader.TryReadTable(db, "Playlists", "PlayListID"));
        _readDetails = readDetails ?? (db => SqliteReader.TryReadTable(db, "PlayListDetails", "GameID", "PlayListID", "isFav"));
        _readGames = readGames ?? (db => SqliteReader.TryReadTable(db, "Games", "GameID", "GameName"));
    }

    public IEnumerable<Finding> Scan(ScanContext ctx)
    {
        if (ctx.Layout.PupDatabasePath is null) yield break;
        var dbPath = ctx.Layout.PupDatabasePath;

        List<string?[]>? playlists;
        List<string?[]>? details;
        try { playlists = _readPlaylists(dbPath); } catch { playlists = null; }
        try { details = _readDetails(dbPath); } catch { details = null; }
        if (playlists is null || details is null) yield break; // unreadable / table not found -> silence

        var validIds = playlists
            .Select(r => r.Length > 0 ? r[0] : null)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id!)
            .ToList();

        var detailTuples = details.Select(r => (
            GameId: r.Length > 0 ? r[0] : null,
            PlayListId: r.Length > 1 ? r[1] : null,
            IsFav: r.Length > 2 ? r[2] : null));

        var orphanGameIds = PlaylistIntegrityInspector.FindOrphanGameIds(detailTuples, validIds);
        if (orphanGameIds.Count == 0) yield break;

        // Best-effort game-name resolution for a readable message; falls back to the raw GameID.
        Dictionary<string, string>? nameById = null;
        try
        {
            var games = _readGames(dbPath);
            if (games is not null)
            {
                nameById = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var row in games)
                {
                    var id = row.Length > 0 ? row[0] : null;
                    var name = row.Length > 1 ? row[1] : null;
                    if (!string.IsNullOrWhiteSpace(id) && !string.IsNullOrWhiteSpace(name))
                        nameById[id!] = name!;
                }
            }
        }
        catch { nameById = null; }

        var examples = orphanGameIds
            .Take(MaxExamplesShown)
            .Select(id => nameById is not null && nameById.TryGetValue(id, out var n) ? n : id)
            .ToList();

        yield return new Finding
        {
            Code = "POPPER_ORPHAN_PLAYLIST", Severity = Severity.Warning, Category = Id,
            Subject = $"{orphanGameIds.Count} game(s)",
            Args = new[] { orphanGameIds.Count.ToString(), string.Join(", ", examples) },
            EnglishText = $"{orphanGameIds.Count} game(s) in PinUP Popper's database are assigned to a playlist that no longer exists — this is known to freeze the Popper frontend menu when opened.",
            FixHint = "In PinUP Popper's admin tool, re-open and re-save each affected game's playlist assignment (or clear it), or recreate the missing playlist. This most often happens after a playlist is deleted from the UI while games are still assigned to it.",
        };
    }
}
