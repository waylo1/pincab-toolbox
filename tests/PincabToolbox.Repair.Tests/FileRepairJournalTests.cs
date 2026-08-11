using PincabToolbox.Repair;

namespace PincabToolbox.Repair.Tests;

/// <summary>
/// LOT H.1 (spec 10/08) — the journal is what makes Undo survive closing the app. These tests
/// exercise real disk I/O (a temp directory per test, cleaned up after) rather than a fake — the
/// one thing worth actually proving here is that entries really land on disk and really reload,
/// which an in-memory fake could not tell us.
/// </summary>
public static class FileRepairJournalTests
{
    private static string NewTempRoot()
    {
        var dir = Path.Combine(Path.GetTempPath(), "pincab-journal-tests-" + Guid.NewGuid().ToString("N"));
        return dir;
    }

    private static JournalEntry Entry(string planId, JournalEvent ev, string? itemId = null, PlannedChange? change = null) => new()
    {
        AtUtc = new DateTimeOffset(2026, 8, 10, 12, 0, 0, TimeSpan.Zero),
        Event = ev,
        PlanId = planId,
        ItemId = itemId,
        Change = change,
    };

    public static void Test_Write_PersistsAcrossInstances()
    {
        var root = NewTempRoot();
        try
        {
            var j1 = new FileRepairJournal(root);
            j1.Write(Entry("plan-1", JournalEvent.PlanCreated));
            j1.Write(Entry("plan-1", JournalEvent.ChangeApplied, "item-1",
                new PlannedChange { ActionId = "a", Kind = ChangeKind.FileAttribute, Target = "C:/x", Before = "b", After = "a", Reversible = true }));

            // A fresh instance, as if the app had been closed and reopened, must see everything
            // the first instance wrote — this is the entire point of H.1.
            var j2 = new FileRepairJournal(root);
            var entries = j2.Read("plan-1");
            A.Equal(2, entries.Count, "reloaded journal should contain both entries written earlier");
            A.Equal(JournalEvent.PlanCreated, entries[0].Event, "first entry event");
            A.Equal(JournalEvent.ChangeApplied, entries[1].Event, "second entry event");

            var applied = j2.AppliedChanges("plan-1", "item-1");
            A.Equal(1, applied.Count, "AppliedChanges must be rebuilt from the reloaded entries");
            A.Equal("C:/x", applied[0].Target, "reloaded change target");
        }
        finally { TryDelete(root); }
    }

    public static void Test_Write_IsAppendedImmediately_NotBatchedAtPlanEnd()
    {
        // H.1: "écrit sur disque au fur et à mesure, pas en fin de plan" — verified by reading the
        // file directly (bypassing the journal object) after each individual Write.
        var root = NewTempRoot();
        try
        {
            var j = new FileRepairJournal(root);
            var filePath = Path.Combine(root, "journal.jsonl");

            j.Write(Entry("plan-2", JournalEvent.PlanCreated));
            A.True(File.Exists(filePath), "journal file must exist after the very first Write");
            var afterFirst = File.ReadAllLines(filePath).Length;
            A.Equal(1, afterFirst, "one Write must produce exactly one persisted line immediately");

            j.Write(Entry("plan-2", JournalEvent.PreflightPassed));
            var afterSecond = File.ReadAllLines(filePath).Length;
            A.Equal(2, afterSecond, "a second Write must append immediately, not wait for a 'plan completed' event");
        }
        finally { TryDelete(root); }
    }

    public static void Test_Load_SkipsCorruptLineButKeepsTheRest()
    {
        var root = NewTempRoot();
        try
        {
            Directory.CreateDirectory(root);
            var filePath = Path.Combine(root, "journal.jsonl");
            File.WriteAllLines(filePath, new[]
            {
                """{"AtUtc":"2026-08-10T12:00:00+00:00","Event":"PlanCreated","PlanId":"plan-3"}""",
                "{ this is not valid json at all",
                """{"AtUtc":"2026-08-10T12:01:00+00:00","Event":"PlanCompleted","PlanId":"plan-3"}""",
            });

            var j = new FileRepairJournal(root);
            var entries = j.Read("plan-3");
            A.Equal(2, entries.Count, "a corrupt line must be skipped, not lose the whole journal (H.1: never blocks a scan)");
        }
        finally { TryDelete(root); }
    }

    public static void Test_Load_MissingFile_DegradesToEmptyHistory()
    {
        var root = NewTempRoot();
        try
        {
            // Root directory does not even exist yet — must not throw on construction.
            var j = new FileRepairJournal(root);
            A.Equal(0, j.Read("anything").Count, "no file on disk means no history, not a crash");
        }
        finally { TryDelete(root); }
    }

    public static void Test_Write_ToUnwritableRoot_NeverThrows_InMemoryStillWorks()
    {
        // Point the journal at a path that cannot possibly be created as a directory (a file
        // masquerading as the parent) to force the disk append to fail, and confirm Write() still
        // returns normally and the in-memory copy (this run's own Undo) still has the entry.
        var root = NewTempRoot();
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(root)!);
            File.WriteAllText(root, "not a directory"); // 'root' now exists as a FILE, not a dir

            var j = new FileRepairJournal(root);
            j.Write(Entry("plan-4", JournalEvent.PlanCreated));

            A.True(j.LastWriteFailed, "a disk append that cannot possibly succeed must be reported, not silently pretended fine");
            A.Equal(1, j.Read("plan-4").Count, "in-memory copy must still have the entry even though the disk append failed");
        }
        finally { TryDelete(root); }
    }

    public static void Test_KnownPlanIds_ReturnsDistinctPlanIds_MostRecentFirst()
    {
        var root = NewTempRoot();
        try
        {
            var j = new FileRepairJournal(root);
            j.Write(Entry("plan-a", JournalEvent.PlanCreated));
            j.Write(Entry("plan-a", JournalEvent.PlanCompleted));
            j.Write(Entry("plan-b", JournalEvent.PlanCreated));

            var ids = j.KnownPlanIds();
            A.Sequence(new[] { "plan-b", "plan-a" }, ids, "known plan ids, most recent first, de-duplicated");
        }
        finally { TryDelete(root); }
    }

    private static void TryDelete(string root)
    {
        try
        {
            if (File.Exists(root)) File.Delete(root);
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
        catch { /* best-effort cleanup */ }
    }
}
