using PincabToolbox.Core.Models;
using PincabToolbox.Core.Scanning;
using PincabToolbox.Core.Services;

namespace PincabToolbox.Core.Tests;

/// <summary>Pure orphan-detection decision.</summary>
public static class PlaylistIntegrityInspectorTests
{
    public static void Test_ValidPlayListId_NotOrphan()
    {
        var details = new[] { ("1", "10", (string?)null) };
        var orphans = PlaylistIntegrityInspector.FindOrphanGameIds(details, new[] { "10" });
        Assert.Equal(0, orphans.Count);
    }

    public static void Test_InvalidPlayListId_IsOrphan()
    {
        var details = new[] { ("1", "999", (string?)null) };
        var orphans = PlaylistIntegrityInspector.FindOrphanGameIds(details, new[] { "10" });
        Assert.Equal(1, orphans.Count);
        Assert.Equal("1", orphans[0]);
    }

    public static void Test_NullGameId_Skipped()
    {
        var details = new[] { ((string?)null, "999", (string?)null) };
        var orphans = PlaylistIntegrityInspector.FindOrphanGameIds(details, new[] { "10" });
        Assert.Equal(0, orphans.Count);
    }

    public static void Test_NullPlayListId_Skipped()
    {
        var details = new[] { ("1", (string?)null, (string?)null) };
        var orphans = PlaylistIntegrityInspector.FindOrphanGameIds(details, new[] { "10" });
        Assert.Equal(0, orphans.Count);
    }

    public static void Test_IsFav2_ExcludedEvenIfInvalid()
    {
        // isFav=2 is the built-in "global favorites" pseudo-playlist -- never flagged.
        var details = new[] { ("1", "999", (string?)"2") };
        var orphans = PlaylistIntegrityInspector.FindOrphanGameIds(details, new[] { "10" });
        Assert.Equal(0, orphans.Count);
    }

    public static void Test_IsFavZeroOrNull_StillChecked()
    {
        var details = new[] { ("1", "999", (string?)"0"), ("2", "888", (string?)null) };
        var orphans = PlaylistIntegrityInspector.FindOrphanGameIds(details, new[] { "10" });
        Assert.Equal(2, orphans.Count);
    }

    public static void Test_DuplicateOrphanGameIds_DedupedInResult()
    {
        // Same game orphaned via two different bad playlist rows -- reported once.
        var details = new[] { ("1", "999", (string?)null), ("1", "998", (string?)null) };
        var orphans = PlaylistIntegrityInspector.FindOrphanGameIds(details, new[] { "10" });
        Assert.Equal(1, orphans.Count);
    }

    public static void Test_CaseInsensitivePlaylistIdMatch()
    {
        var details = new[] { ("1", "ABC", (string?)null) };
        var orphans = PlaylistIntegrityInspector.FindOrphanGameIds(details, new[] { "abc" });
        Assert.Equal(0, orphans.Count);
    }
}

/// <summary>End-to-end scanner behaviour, with the three table reads injected.</summary>
public static class PopperPlaylistScannerTests
{
    private static ScanContext CtxWithDb(string? dbPath = "/x/PUPDatabase.db")
    {
        var layout = new InstallLayout { RootPath = "/x", PupDatabasePath = dbPath };
        return new ScanContext { Layout = layout, Profile = Fixtures.Profile() };
    }

    private static List<string?[]> Rows(params string?[][] rows) => rows.ToList();

    public static void Test_NoPupDatabasePath_Silent()
    {
        var ctx = CtxWithDb(null);
        var scanner = new PopperPlaylistScanner(
            _ => Rows(new string?[] { "10" }),
            _ => Rows(new string?[] { "1", "999", null }),
            _ => Rows(new string?[] { "1", "Foo" }));
        Assert.Equal(0, scanner.Scan(ctx).ToList().Count);
    }

    public static void Test_PlaylistsUnreadable_Silent()
    {
        var ctx = CtxWithDb();
        var scanner = new PopperPlaylistScanner(
            _ => null,
            _ => Rows(new string?[] { "1", "999", null }),
            _ => null);
        Assert.Equal(0, scanner.Scan(ctx).ToList().Count);
    }

    public static void Test_DetailsUnreadable_Silent()
    {
        var ctx = CtxWithDb();
        var scanner = new PopperPlaylistScanner(
            _ => Rows(new string?[] { "10" }),
            _ => null,
            _ => null);
        Assert.Equal(0, scanner.Scan(ctx).ToList().Count);
    }

    public static void Test_NoOrphans_Silent()
    {
        var ctx = CtxWithDb();
        var scanner = new PopperPlaylistScanner(
            _ => Rows(new string?[] { "10" }),
            _ => Rows(new string?[] { "1", "10", null }),
            _ => Rows(new string?[] { "1", "Foo" }));
        Assert.Equal(0, scanner.Scan(ctx).ToList().Count);
    }

    public static void Test_OrphansFound_WarnsWithCount()
    {
        var ctx = CtxWithDb();
        var scanner = new PopperPlaylistScanner(
            _ => Rows(new string?[] { "10" }),
            _ => Rows(new string?[] { "1", "999", null }, new string?[] { "2", "999", null }),
            _ => Rows(new string?[] { "1", "Foo" }, new string?[] { "2", "Bar" }));
        var findings = scanner.Scan(ctx).ToList();
        Assert.Equal(1, findings.Count(f => f.Code == "POPPER_ORPHAN_PLAYLIST"));
        var f = findings.Single(f => f.Code == "POPPER_ORPHAN_PLAYLIST");
        Assert.Equal(Severity.Warning, f.Severity);
        Assert.Equal("2", f.Args[0]);
    }

    public static void Test_GameNameResolved_WhenGamesReadable()
    {
        var ctx = CtxWithDb();
        var scanner = new PopperPlaylistScanner(
            _ => Rows(new string?[] { "10" }),
            _ => Rows(new string?[] { "1", "999", null }),
            _ => Rows(new string?[] { "1", "Attack from Mars" }));
        var f = scanner.Scan(ctx).Single();
        Assert.True(f.Args[1].Contains("Attack from Mars"));
    }

    public static void Test_FallsBackToGameId_WhenGamesUnreadable()
    {
        var ctx = CtxWithDb();
        var scanner = new PopperPlaylistScanner(
            _ => Rows(new string?[] { "10" }),
            _ => Rows(new string?[] { "1", "999", null }),
            _ => null);
        var f = scanner.Scan(ctx).Single();
        Assert.True(f.Args[1].Contains("1"));
    }

    public static void Test_FavoritesPseudoPlaylist_NeverFlagged()
    {
        var ctx = CtxWithDb();
        var scanner = new PopperPlaylistScanner(
            _ => Rows(new string?[] { "10" }),
            _ => Rows(new string?[] { "1", "999", "2" }),
            _ => Rows(new string?[] { "1", "Foo" }));
        Assert.Equal(0, scanner.Scan(ctx).ToList().Count);
    }

    public static void Test_ReadPlaylistsThrows_Silent()
    {
        var ctx = CtxWithDb();
        var scanner = new PopperPlaylistScanner(
            _ => throw new IOException(),
            _ => Rows(new string?[] { "1", "999", null }),
            _ => null);
        Assert.Equal(0, scanner.Scan(ctx).ToList().Count);
    }

    public static void Test_ReadDetailsThrows_Silent()
    {
        var ctx = CtxWithDb();
        var scanner = new PopperPlaylistScanner(
            _ => Rows(new string?[] { "10" }),
            _ => throw new IOException(),
            _ => null);
        Assert.Equal(0, scanner.Scan(ctx).ToList().Count);
    }

    public static void Test_ReadGamesThrows_StillReportsWithFallback()
    {
        // The bonus name-lookup failing must not take the whole finding down with it.
        var ctx = CtxWithDb();
        var scanner = new PopperPlaylistScanner(
            _ => Rows(new string?[] { "10" }),
            _ => Rows(new string?[] { "1", "999", null }),
            _ => throw new IOException());
        var findings = scanner.Scan(ctx).ToList();
        Assert.Equal(1, findings.Count(f => f.Code == "POPPER_ORPHAN_PLAYLIST"));
    }
}
