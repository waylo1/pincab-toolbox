using System.IO.Compression;
using PincabToolbox.Core.Models;
using PincabToolbox.Repair;
using PincabToolbox.Repair.Actions;

namespace PincabToolbox.Repair.Tests;

/// <summary>The two shipped actions, and the Knowledge Pack loader.</summary>
public static class ActionAndPackTests
{
    // ═══════════════ unblock_file (BLOCKED_DLL) ═══════════════

    public static void Test_Unblock_PlansNothingWhenTheFileIsNotBlocked()
    {
        var fs = new FakeFs();
        fs.AddFile(@"C:\vpx\a.dll");
        var a = new UnblockFileAction(fs);
        A.Equal(0, a.Plan(Ctx(@"C:\vpx\a.dll"), Empty).Count, "no marker, nothing to do");
    }

    public static void Test_Unblock_PlansNothingWhenTheFileIsGone()
    {
        var fs = new FakeFs();
        var a = new UnblockFileAction(fs);
        A.Equal(0, a.Plan(Ctx(@"C:\vpx\ghost.dll"), Empty).Count, "missing file, nothing to do");
    }

    public static void Test_Unblock_RoundTripsExactly()
    {
        var fs = new FakeFs();
        fs.AddFile(@"C:\vpx\a.dll");
        fs.Blocked.Add(@"C:\vpx\a.dll");
        var a = new UnblockFileAction(fs);

        var change = a.Plan(Ctx(@"C:\vpx\a.dll"), Empty).Single();
        A.True(a.Execute(change).Success, "execute");
        A.False(fs.HasZoneIdentifier(@"C:\vpx\a.dll"), "unblocked");
        A.True(a.Revert(change).Success, "revert");
        A.True(fs.HasZoneIdentifier(@"C:\vpx\a.dll"), "marker restored — reversible for real");
    }

    public static void Test_Unblock_NeverTouchesFileContent()
    {
        var fs = new FakeFs();
        fs.AddFile(@"C:\vpx\a.dll", "PRECIOUS");
        fs.Blocked.Add(@"C:\vpx\a.dll");
        var a = new UnblockFileAction(fs);
        a.Execute(a.Plan(Ctx(@"C:\vpx\a.dll"), Empty).Single());
        A.Equal("PRECIOUS", System.Text.Encoding.UTF8.GetString(fs.ReadAllBytes(@"C:\vpx\a.dll")),
            "only the marker is removed");
    }

    // ═══════════════ restore_rom_archive (ROM_UNZIPPED) ═══════════════

    public static void Test_RestoreRom_ProducesARealReadableZip()
    {
        var fs = RomFolder();
        var a = new RestoreRomArchiveAction(fs);

        var change = a.Plan(Ctx(@"C:\vpx\roms\mm_109c"), Empty).Single();
        A.Equal(@"C:\vpx\roms\mm_109c.zip", change.Target, "archive path");
        A.True(a.Execute(change).Success, "execute");

        using var ms = new MemoryStream(fs.ReadAllBytes(@"C:\vpx\roms\mm_109c.zip"));
        using var zip = new ZipArchive(ms, ZipArchiveMode.Read);
        A.Sequence(new[] { "u1.bin", "u2.bin" }, zip.Entries.Select(e => e.Name).OrderBy(x => x),
            "both ROM files are inside");
        A.Equal("DATA1",
            new StreamReader(zip.GetEntry("u1.bin")!.Open()).ReadToEnd(), "content preserved");
    }

    /// <summary>Reversibility comes from parking the folder, never deleting it.</summary>
    public static void Test_RestoreRom_ParksTheFolderInsteadOfDeletingIt()
    {
        var fs = RomFolder();
        var a = new RestoreRomArchiveAction(fs);
        a.Execute(a.Plan(Ctx(@"C:\vpx\roms\mm_109c"), Empty).Single());

        A.False(fs.DirectoryExists(@"C:\vpx\roms\mm_109c"), "original path freed");
        A.True(fs.DirectoryExists(@"C:\vpx\roms\mm_109c" + RestoreRomArchiveAction.ParkedSuffix),
            "folder kept aside, not destroyed");
    }

    public static void Test_RestoreRom_RevertRestoresTheOriginalState()
    {
        var fs = RomFolder();
        var a = new RestoreRomArchiveAction(fs);
        var change = a.Plan(Ctx(@"C:\vpx\roms\mm_109c"), Empty).Single();

        a.Execute(change);
        A.True(a.Revert(change).Success, "revert");

        A.True(fs.DirectoryExists(@"C:\vpx\roms\mm_109c"), "folder back");
        A.False(fs.FileExists(@"C:\vpx\roms\mm_109c.zip"), "archive removed");
        A.Equal("DATA1", System.Text.Encoding.UTF8.GetString(fs.ReadAllBytes(@"C:\vpx\roms\mm_109c\u1.bin")),
            "files intact");
    }

    public static void Test_RestoreRom_DoesNothingWhenTheArchiveAlreadyExists()
    {
        var fs = RomFolder();
        fs.AddFile(@"C:\vpx\roms\mm_109c.zip", "already here");
        var a = new RestoreRomArchiveAction(fs);
        A.Equal(0, a.Plan(Ctx(@"C:\vpx\roms\mm_109c"), Empty).Count, "nothing to do");
        A.False(a.StillApplies(Ctx(@"C:\vpx\roms\mm_109c")), "and the finding is stale");
    }

    // ═══════════════ Knowledge Pack loading ═══════════════

    private const string SamplePack = """
    {
      "schemaVersion": 1,
      "packVersion": "2026.08",
      "entries": [
        { "code": "BLOCKED_DLL",
          "repairRules": [
            { "id": "unblock-dll", "actionId": "unblock_file",
              "repairConfidence": 98, "reversible": true }
          ] }
      ],
      "scenarios": [
        { "id": "MIG", "titleFr": "Migration", "titleEn": "Migration",
          "requires": ["BLOCKED_DLL", "BITNESS_MISMATCH_VPM"],
          "baseConfidence": 90,
          "repairPlaybook": [ { "step": 1, "ruleId": "unblock-dll" } ] }
      ]
    }
    """;

    public static void Test_Pack_LoadsRulesAndScenarios()
    {
        var pack = KnowledgePack.Load(SamplePack);
        A.Equal("2026.08", pack.PackVersion, "version");
        A.Equal("unblock_file", pack.RuleFor("BLOCKED_DLL")!.ActionId, "rule by code");
        A.Equal("unblock-dll", pack.RuleById("unblock-dll")!.Id, "rule by id");
        A.Equal(1, pack.Scenarios.Count, "scenario loaded");
    }

    /// <summary>
    /// 14/08/2026 (Maxime, "si c'est une valeur produit on le fait"): the pack's plain-language
    /// player/explanation/verification text — previously parsed by nothing — is now exposed via
    /// EntryFor for the Écran 1 detail panel. impactFr/impactEn/causeFr/causeEn stay unread here
    /// on purpose (Knowledge.cs is the single source of truth for those two fields, see FIELD-LOG).
    /// </summary>
    public static void Test_Pack_LoadsEntryEditorialTextInAllThreeLanguages()
    {
        var pack = KnowledgePack.Load("""
        { "packVersion": "2026.08", "entries": [
          { "code": "BLOCKED_DLL",
            "playerFr": "fr-player", "playerEn": "en-player", "playerEs": "es-player",
            "explanationFr": "fr-expl", "explanationEn": "en-expl", "explanationEs": "es-expl",
            "verificationFr": "fr-verif", "verificationEn": "en-verif", "verificationEs": "es-verif",
            "impactFr": "should not be read here", "impactEn": "should not be read here",
            "repairRules": [] }
        ] }
        """);

        var entry = pack.EntryFor("BLOCKED_DLL");
        A.True(entry is not null, "entry loaded");
        A.Equal("fr-player", entry!.PlayerFr, "player fr");
        A.Equal("en-player", entry.PlayerEn, "player en");
        A.Equal("es-player", entry.PlayerEs, "player es");
        A.Equal("fr-expl", entry.ExplanationFr, "explanation fr");
        A.Equal("en-expl", entry.ExplanationEn, "explanation en");
        A.Equal("es-expl", entry.ExplanationEs, "explanation es");
        A.Equal("fr-verif", entry.VerificationFr, "verification fr");
        A.Equal("en-verif", entry.VerificationEn, "verification en");
        A.Equal("es-verif", entry.VerificationEs, "verification es");
    }

    /// <summary>An entry with only repairRules and no editorial text must not fabricate one — the
    /// detail panel's new sections stay hidden for the 44 codes the pack author hasn't written
    /// this content for yet, same ADR-005 tolerance as everything else in the pack.</summary>
    public static void Test_Pack_EntryWithNoEditorialTextStaysAbsent()
    {
        var pack = KnowledgePack.Load(SamplePack);
        A.True(pack.EntryFor("BLOCKED_DLL") is null, "no editorial text in SamplePack, so no entry");
        A.True(pack.EntryFor("UNKNOWN_CODE") is null, "unknown code, still no crash");
    }

    /// <summary>A bad pack must never stop the free scanner from running.</summary>
    public static void Test_Pack_SkipsMalformedRulesInsteadOfCrashing()
    {
        var warnings = new List<string>();
        var pack = KnowledgePack.Load("""
        { "packVersion": "2026.08", "entries": [
          { "code": "A", "repairRules": [ { "id": "", "actionId": "x", "repairConfidence": 50 } ] },
          { "code": "B", "repairRules": [ { "id": "ok", "actionId": "y", "repairConfidence": 900 } ] },
          { "code": "C", "repairRules": [ { "id": "good", "actionId": "unblock_file",
                                            "repairConfidence": 98, "reversible": true } ] }
        ] }
        """, warnings);

        A.True(pack.RuleFor("A") is null, "rule without id skipped");
        A.True(pack.RuleFor("B") is null, "confidence out of range skipped");
        A.True(pack.RuleFor("C") is not null, "the valid rule still loads");
        A.Equal(2, warnings.Count, "and both problems are reported");
    }

    /// <summary>Anti-false-positive: coincident findings do not make a diagnosis.</summary>
    public static void Test_Pack_RejectsScenariosWithFewerThanTwoRequiredCodes()
    {
        var warnings = new List<string>();
        var pack = KnowledgePack.Load("""
        { "packVersion": "2026.08", "entries": [],
          "scenarios": [ { "id": "WEAK", "requires": ["ONLY_ONE"] } ] }
        """, warnings);
        A.Equal(0, pack.Scenarios.Count, "rejected");
        A.Equal(1, warnings.Count, "and reported");
    }

    public static void Test_Pack_EmptyPackMakesEverythingManual()
    {
        A.True(KnowledgePack.Empty.RuleFor("BLOCKED_DLL") is null, "no rule");
        A.Equal(RepairMode.ManualOnly,
            RepairModeResolver.Resolve(false, true, 100, true), "so: manual only");
    }

    // ═══════════════ helpers ═══════════════

    private static readonly Dictionary<string, string> Empty = new();

    private static RepairContext Ctx(string path) => new()
    {
        InstallRoots = Build.Roots,
        Finding = Build.Finding("X", path),
    };

    private static FakeFs RomFolder()
    {
        var fs = new FakeFs();
        fs.AddDir(@"C:\vpx\roms");
        fs.AddDir(@"C:\vpx\roms\mm_109c");
        fs.AddFile(@"C:\vpx\roms\mm_109c\u1.bin", "DATA1");
        fs.AddFile(@"C:\vpx\roms\mm_109c\u2.bin", "DATA2");
        return fs;
    }

    // ═══════════════ Chemin de sauvegarde ═══════════════

    /// <summary>
    /// The backup path is printed on the recovery screen. A path with ".." in it is the last
    /// thing someone needs when their install is in an in-between state.
    /// </summary>
    public static void Test_Backup_PathIsNormalisedForHumans()
    {
        var fs = new FakeFs();
        fs.AddFile(@"C:/vpx/a.dll", "x");
        var backup = new FileBackupService(fs, "C:/vpx/../_backups");

        var path = backup.Backup("plan-1", new RepairPlanItem
        {
            ItemId = "i1", TargetCode = "X", Mode = RepairMode.Automatic,
            Changes = new[] { new PlannedChange {
                ActionId = "a", Kind = ChangeKind.FileAttribute, Target = @"C:/vpx/a.dll",
                Before = "b", After = "c", Reversible = true } },
        });

        A.False(path.Contains(".."), $"no '..' may reach the user: {path}");
        A.Equal("C:/_backups/plan-1/i1", path, "normalised");
    }

    public static void Test_Backup_RestoresAFileFromTheManifest()
    {
        var fs = new FakeFs();
        fs.AddFile(@"C:/vpx/a.dll", "ORIGINAL");
        var backup = new FileBackupService(fs, "C:/backups");
        var item = new RepairPlanItem
        {
            ItemId = "i1", TargetCode = "X", Mode = RepairMode.Automatic,
            Changes = new[] { new PlannedChange {
                ActionId = "a", Kind = ChangeKind.FileAttribute, Target = @"C:/vpx/a.dll",
                Before = "b", After = "c", Reversible = true } },
        };

        backup.Backup("plan-1", item);
        fs.WriteAllBytes(@"C:/vpx/a.dll", System.Text.Encoding.UTF8.GetBytes("BROKEN"));

        A.True(backup.Restore("plan-1", "i1").Success, "restore succeeds");
        A.Equal("ORIGINAL", System.Text.Encoding.UTF8.GetString(fs.ReadAllBytes(@"C:/vpx/a.dll")),
            "the file is back to what it was");
    }
}
