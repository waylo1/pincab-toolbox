namespace PincabToolbox.Core.Services;

/// <summary>
/// Pure decision for PUPDatabase.db playlist-membership integrity (audit §4/F1). Given every
/// <c>PlayListDetails</c> row (the junction table between <c>Games</c> and <c>Playlists</c>,
/// confirmed by cross-referencing NailBuster's own PinUP Popper wiki SQL snippets and independent
/// forum posts — schema not previously documented anywhere in this repo) and the full set of real
/// <c>Playlists.PlayListID</c> values, finds the rows whose <c>PlayListID</c> doesn't resolve to a
/// real playlist. Per community reports (vpforums.org #50896), deleting a playlist from Popper's UI
/// removes only the <c>Playlists</c> row — the <c>PlayListDetails</c> rows are left behind pointing at
/// nothing — and the resulting orphaned reference freezes the Popper frontend menu when opened.
/// </summary>
public static class PlaylistIntegrityInspector
{
    /// <param name="details">Every PlayListDetails row as (GameID, PlayListID, isFav).</param>
    /// <param name="validPlaylistIds">Every real Playlists.PlayListID value.</param>
    /// <returns>Distinct GameID values that reference a non-existent playlist.</returns>
    public static List<string> FindOrphanGameIds(
        IEnumerable<(string? GameId, string? PlayListId, string? IsFav)> details,
        IReadOnlyCollection<string> validPlaylistIds)
    {
        var valid = new HashSet<string>(validPlaylistIds, StringComparer.OrdinalIgnoreCase);
        var orphans = new List<string>();

        foreach (var row in details)
        {
            if (string.IsNullOrWhiteSpace(row.GameId)) continue;
            if (string.IsNullOrWhiteSpace(row.PlayListId)) continue;

            // isFav=2 marks a row as belonging to the built-in "global favorites" pseudo-playlist
            // (confirmed via NailBuster's own wiki query "where pd.isFav=2") — not a user-created
            // Playlists row, so it has no reason to resolve there. Excluded rather than risking a
            // false positive on a convention this research couldn't fully pin down either way.
            if (row.IsFav == "2") continue;

            if (valid.Contains(row.PlayListId)) continue;
            orphans.Add(row.GameId);
        }

        return orphans.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }
}
