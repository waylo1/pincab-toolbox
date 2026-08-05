using PincabToolbox.Core.Models;
using PincabToolbox.Repair;
using PincabToolbox.Repair.Actions;

namespace PincabToolbox.Repair.Demo;

/// <summary>
/// Bac à sable Repair — l'équivalent du mode démo du Scanner.
///
/// Fabrique une fausse installation dans le dossier temporaire, y reproduit de vraies pannes,
/// puis lance le VRAI moteur Repair dessus : plan, préflight, sauvegarde, application, annulation.
/// Ne touche jamais à une installation réelle : tout se passe sous %TEMP%.
///
///   dotnet run --project tools/PincabToolbox.Repair.Demo
///   dotnet run --project tools/PincabToolbox.Repair.Demo -- --keep     (garde le bac à sable)
/// </summary>
public static class Program
{
    private static int _failures;
    private static bool _keep;

    public static int Main(string[] args)
    {
        _keep = args.Contains("--keep");
        var sandbox = Path.Combine(Path.GetTempPath(),
            "PincabToolbox-RepairDemo", DateTime.Now.ToString("yyyyMMdd-HHmmss"));

        Title("Bac à sable Repair");
        Console.WriteLine($"  Dossier      : {sandbox}");
        Console.WriteLine($"  Système      : {(OperatingSystem.IsWindows() ? "Windows — marqueurs Windows réels" : "non-Windows — le scénario « DLL bloquée » sera annoncé comme non reproductible")}");
        Console.WriteLine("  Aucune installation réelle n'est touchée.\n");

        try
        {
            Scenario1_BlockedDll(sandbox);
            Scenario2_UnzippedRom(sandbox);
            Scenario3_FreeVersusLicensed(sandbox);
            Scenario4_PreflightRefusal(sandbox);
            Scenario5_Undo(sandbox);
            Scenario6_QuarantineOrphanedMedia(sandbox);
            Scenario7_AudioDeviceComSmokeTest();
        }
        finally
        {
            if (_keep) Console.WriteLine($"\n  Bac à sable conservé : {sandbox}");
            else TryDelete(sandbox);
        }

        Console.WriteLine();
        if (_failures == 0)
        {
            Ok("Tous les scénarios se sont comportés comme prévu.");
            return 0;
        }
        Bad($"{_failures} scénario(s) n'ont pas fait ce qui était attendu.");
        return 1;
    }

    // ─────────────────────── 1. DLL bloquée → automatique ───────────────────────

    private static void Scenario1_BlockedDll(string root)
    {
        Title("1. DLL bloquée par Windows");

        var (fs, dir) = NewInstall(root, "blocked-dll");
        var dll = Path.Combine(dir, "VPinMAME", "VPinMAME.dll");
        Write(fs, dll, "contenu binaire de la DLL");
        fs.AddZoneIdentifier(dll);

        if (!fs.HasZoneIdentifier(dll))
        {
            Skip("le marqueur « Mark of the Web » n'existe pas sur ce système.");
            Console.WriteLine("       Sur ton PC Windows, ce scénario s'exécutera pour de vrai.");
            return;
        }

        var (engine, journal) = NewEngine(fs, dir);
        var finding = Finding("BLOCKED_DLL", dll, "security");

        var plan = engine.Plan("demo", new[] { finding }, licensed: true);
        var item = plan.Items[0];

        Step($"Mode calculé      : {item.Mode}   (confiance 98 + réversible)");
        Step($"Ce qui va changer : {item.Changes[0].Before}  →  {item.Changes[0].After}");

        var result = engine.Apply(Select(plan));

        Check("le fichier est débloqué", !fs.HasZoneIdentifier(dll));
        Check("le contenu est intact", Read(fs, dll) == "contenu binaire de la DLL");
        Check("aucune récupération manuelle nécessaire", !result.RecoveryRequired);
        Check("la vérification confirme que le problème a disparu",
              engine.Verify(plan).Values.All(v => v));

        engine.Undo(plan.PlanId);
        Check("l'annulation remet le marqueur", fs.HasZoneIdentifier(dll));

        Journal(journal, plan.PlanId);
    }

    // ─────────────────────── 2. ROM décompressée → confirmation ───────────────────────

    private static void Scenario2_UnzippedRom(string root)
    {
        Title("2. ROM décompressée en dossier");

        var (fs, dir) = NewInstall(root, "unzipped-rom");
        var romFolder = Path.Combine(dir, "VPinMAME", "roms", "mm_109c");
        Write(fs, Path.Combine(romFolder, "mm_u1.bin"), "DONNEES ROM 1");
        Write(fs, Path.Combine(romFolder, "mm_u2.bin"), "DONNEES ROM 2");

        var (engine, journal) = NewEngine(fs, dir);
        var plan = engine.Plan("demo", new[] { Finding("ROM_UNZIPPED", romFolder, "rom") }, licensed: true);
        var item = plan.Items[0];

        Step($"Mode calculé      : {item.Mode}   (confiance 88 → sous le seuil automatique)");
        Step($"Ce qui va changer : {item.Changes[0].Before}  →  {item.Changes[0].After}");

        engine.Apply(Select(plan));

        var zip = romFolder + ".zip";
        Check("l'archive .zip a été créée", fs.FileExists(zip));
        Check("l'archive est une vraie archive lisible", ZipContains(fs, zip, "mm_u1.bin", "mm_u2.bin"));
        Check("le dossier d'origine est mis de côté, PAS supprimé",
              fs.DirectoryExists(romFolder + RestoreRomArchiveAction.ParkedSuffix));

        engine.Undo(plan.PlanId);
        Check("l'annulation restaure le dossier", fs.DirectoryExists(romFolder));
        Check("l'annulation retire l'archive", !fs.FileExists(zip));
        Check("les fichiers d'origine sont intacts",
              Read(fs, Path.Combine(romFolder, "mm_u1.bin")) == "DONNEES ROM 1");

        Journal(journal, plan.PlanId);
    }

    // ─────────────────────── 3. Ce que voit le gratuit vs le payant ───────────────────────

    private static void Scenario3_FreeVersusLicensed(string root)
    {
        Title("3. Ce que voit le Scanner gratuit, et ce qu'ajoute Repair  (ADR-006)");

        var (fs, dir) = NewInstall(root, "free-vs-paid");
        var romFolder = Path.Combine(dir, "VPinMAME", "roms", "afm_113b");
        Write(fs, Path.Combine(romFolder, "afm.bin"), "DONNEES");

        var (engine, _) = NewEngine(fs, dir);
        var finding = Finding("ROM_UNZIPPED", romFolder, "rom");

        var free = engine.Plan("demo", new[] { finding }, licensed: false).Items[0];
        var paid = engine.Plan("demo", new[] { finding }, licensed: true).Items[0];

        Console.WriteLine("\n  ┌─ Sans licence ─────────────────────────────────────────┐");
        Console.WriteLine($"  │ ROM décompressée");
        Console.WriteLine($"  │   ✓ Réparable automatiquement");
        Console.WriteLine($"  │   ✓ Sauvegarde avant modification : {Yes(free.Summary!.BackupPlanned)}");
        Console.WriteLine($"  │   ✓ Réversible : {Yes(free.Summary.FullyReversible)}");
        Console.WriteLine($"  │   ⏱ Durée estimée : {Duration(free.Summary.EstimatedDuration)}");
        Console.WriteLine($"  │   [ Réparer ]  🔒 Repair");
        Console.WriteLine("  └────────────────────────────────────────────────────────┘");

        Console.WriteLine("\n  ┌─ Avec licence ─────────────────────────────────────────┐");
        foreach (var c in paid.Changes)
            Console.WriteLine($"  │ {Short(c.Target)}\n  │   {c.Before}  →  {c.After}");
        Console.WriteLine("  └────────────────────────────────────────────────────────┘\n");

        Check("sans licence, aucun chemin ni valeur ne fuite", free.Changes.Count == 0);
        Check("mais le résumé est bien là", free.Summary is not null);
        Check("et il dit la vérité sur le nombre d'écritures",
              free.Summary!.ChangeCount == paid.Changes.Count);
        Check("avec licence, le détail apparaît", paid.Changes.Count > 0);
    }

    // ─────────────────────── 4. Refus au préflight ───────────────────────

    private static void Scenario4_PreflightRefusal(string root)
    {
        Title("4. Refus quand quelque chose tourne");

        var (fs, dir) = NewInstall(root, "preflight");
        var romFolder = Path.Combine(dir, "VPinMAME", "roms", "tz_94h");
        Write(fs, Path.Combine(romFolder, "tz.bin"), "DONNEES");

        var probe = new StubProbe { Blocking = { "VPinballX", "PinUpPlayer" } };
        var engine = Engine(fs, dir, probe, out _);

        var plan = Select(engine.Plan("demo", new[] { Finding("ROM_UNZIPPED", romFolder, "rom") }, true));
        var result = engine.Apply(plan);

        foreach (var b in result.Blockers) Console.WriteLine($"     ⛔ {b.MessageFr}");

        Check("rien n'a été écrit", !fs.FileExists(romFolder + ".zip"));
        Check("le dossier d'origine est intact", fs.DirectoryExists(romFolder));
        Check("le blocage est nommé", result.Blockers.Any(b => b.Code == "VPX_RUNNING"));

        // Espace disque insuffisant
        var engine2 = Engine(fs, dir, new StubProbe { FreeSpace = 1024 }, out _);
        var res2 = engine2.Apply(Select(engine2.Plan("demo",
            new[] { Finding("ROM_UNZIPPED", romFolder, "rom") }, true)));
        foreach (var b in res2.Blockers) Console.WriteLine($"     ⛔ {b.MessageFr}");
        Check("refus aussi quand il n'y a pas la place de sauvegarder",
              res2.Blockers.Any(b => b.Code == "NO_DISK_SPACE"));
    }

    // ─────────────────────── 5. Annulation de session ───────────────────────

    private static void Scenario5_Undo(string root)
    {
        Title("5. Tout annuler après coup");

        var (fs, dir) = NewInstall(root, "undo");
        var a = Path.Combine(dir, "VPinMAME", "roms", "rom_a");
        var b = Path.Combine(dir, "VPinMAME", "roms", "rom_b");
        Write(fs, Path.Combine(a, "a.bin"), "A");
        Write(fs, Path.Combine(b, "b.bin"), "B");

        var (engine, journal) = NewEngine(fs, dir);
        var plan = Select(engine.Plan("demo", new[]
        {
            Finding("ROM_UNZIPPED", a, "rom"),
            Finding("ROM_UNZIPPED", b, "rom"),
        }, licensed: true));

        engine.Apply(plan);
        Check("les deux ROM sont réparées", fs.FileExists(a + ".zip") && fs.FileExists(b + ".zip"));

        engine.Undo(plan.PlanId);
        Check("une seule annulation remet tout en état",
              fs.DirectoryExists(a) && fs.DirectoryExists(b)
              && !fs.FileExists(a + ".zip") && !fs.FileExists(b + ".zip"));

        var second = engine.Undo(plan.PlanId);
        Check("annuler deux fois n'est pas une erreur", second.Success);

        Console.WriteLine("\n  Journal exporté (anonymisé — prêt à coller sur un forum) :");
        foreach (var line in journal.ExportAnonymized(plan.PlanId).Split('\n').Take(6))
            if (line.Length > 0) Console.WriteLine($"     {line}");
    }

    // ─────────────────────── 6. Médias orphelins mis en quarantaine ───────────────────────

    private static void Scenario6_QuarantineOrphanedMedia(string root)
    {
        Title("6. Médias orphelins mis en quarantaine (quarantine_orphaned_media)");

        var (fs, dir) = NewInstall(root, "orphaned-media");
        var tablesDir = Path.Combine(dir, "Tables");
        var popMediaRoot = Path.Combine(dir, "PinUPSystem", "POPMedia");
        var wheelDir = Path.Combine(popMediaRoot, "Wheel");

        var keptTable = Path.Combine(tablesDir, "Medieval Madness (Williams 1997).vpx");
        Write(fs, keptTable, "table toujours installée");
        Write(fs, Path.Combine(wheelDir, "Medieval Madness (Williams 1997).png"), "wheel — table présente");
        var orphanMedia = Path.Combine(wheelDir, "Twilight Zone (Bally 1993).png");
        Write(fs, orphanMedia, "wheel — table supprimée depuis");

        var layout = new InstallLayout
        {
            RootPath = dir,
            TablesDir = tablesDir,
            PopMediaDir = popMediaRoot,
            PupVideosDir = Path.Combine(dir, "PinUPSystem", "PUPVideos"),
        };
        layout.VpxTables.Add(keptTable);

        var (engine, journal) = NewEngine(fs, dir, layout);
        var finding = Finding("ORPHANED_MEDIA_FILE", popMediaRoot, "media-orphan");
        var plan = engine.Plan("demo", new[] { finding }, licensed: true);
        var item = plan.Items[0];

        Step($"Mode calculé      : {item.Mode}   (confiance 85, réversible)");
        Check("exactement le fichier orphelin est planifié, pas le fichier légitime",
              item.Changes.Count == 1 && item.Changes[0].Before == orphanMedia);

        engine.Apply(Select(plan));

        Check("le fichier orphelin a été déplacé en quarantaine, pas supprimé",
              !fs.FileExists(orphanMedia)
              && fs.FileExists(Path.Combine(wheelDir, QuarantineOrphanedMediaAction.QuarantineFolderName,
                                             "Twilight Zone (Bally 1993).png")));
        Check("le fichier de la table toujours installée n'a pas bougé",
              fs.FileExists(Path.Combine(wheelDir, "Medieval Madness (Williams 1997).png")));

        engine.Undo(plan.PlanId);
        Check("l'annulation restaure le fichier orphelin à sa place d'origine", fs.FileExists(orphanMedia));

        Journal(journal, plan.PlanId);
    }

    // ─────────────────────── 7. Smoke-test COM audio (lecture seule) ───────────────────────

    private static void Scenario7_AudioDeviceComSmokeTest()
    {
        Title("7. Périphérique audio par défaut — smoke-test COM (set_default_audio_device)");

        if (!OperatingSystem.IsWindows())
        {
            Skip("Windows uniquement — l'interface COM IPolicyConfig n'existe pas ailleurs.");
            return;
        }

        Console.WriteLine("     Lecture seule volontaire : ce scénario ne CHANGE jamais ton");
        Console.WriteLine("     périphérique audio par défaut, il vérifie seulement que l'appel COM");
        Console.WriteLine("     ne plante pas sur ta machine. Tester le vrai SetDefaultPlaybackDevice()");
        Console.WriteLine("     changerait ton son pour de vrai — décision à prendre à la main, pas");
        Console.WriteLine("     automatiquement dans une démo.");

        try
        {
            var audio = new RealAudioDeviceControl();
            var current = audio.GetDefaultPlaybackDeviceId();
            Check("l'appel COM répond sans exception", true);
            Check("un périphérique par défaut a été trouvé", current is not null);
            Console.WriteLine($"     périphérique par défaut actuel : {(current is null ? "<aucun trouvé>" : current)}");
        }
        catch (Exception e)
        {
            Check($"l'appel COM répond sans exception (a levé : {e.GetType().Name}: {e.Message})", false);
        }
    }

    // ─────────────────────── plomberie ───────────────────────

    private static (RealFileSystem, string) NewInstall(string root, string name)
    {
        var dir = Path.Combine(root, name);
        Directory.CreateDirectory(Path.Combine(dir, "VPinMAME", "roms"));
        Directory.CreateDirectory(Path.Combine(dir, "Tables"));
        return (new RealFileSystem(), dir);
    }

    private static (IRepairEngine, InMemoryRepairJournal) NewEngine(RealFileSystem fs, string dir, InstallLayout? layout = null)
    {
        var e = Engine(fs, dir, new StubProbe(), out var j, layout);
        return (e, j);
    }

    private static IRepairEngine Engine(RealFileSystem fs, string dir, StubProbe probe,
                                        out InMemoryRepairJournal journal, InstallLayout? layout = null)
    {
        journal = new InMemoryRepairJournal();
        var registry = new RepairActionRegistry(
            new UnblockFileAction(fs),
            new RestoreRomArchiveAction(fs),
            new QuarantineOrphanedMediaAction(fs));
        var pack = LoadPack();
        var backupRoot = Path.Combine(Path.GetDirectoryName(dir)!, "_backups");
        return new RepairEngine(registry, pack, journal, new FileBackupService(fs, backupRoot),
                                probe, new SystemClock(), new[] { dir }, layout);
    }

    /// <summary>Charge le vrai pack livré. Ce n'est pas une maquette : c'est ce qui tournera chez l'utilisateur.</summary>
    private static IKnowledgePack LoadPack()
    {
        var dir = AppContext.BaseDirectory;
        for (var i = 0; i < 8 && dir is not null; i++)
        {
            var p = Path.Combine(dir, "knowledge", "pack-2026.08.json");
            if (File.Exists(p))
            {
                var warnings = new List<string>();
                var pack = KnowledgePack.Load(File.ReadAllText(p), warnings);
                foreach (var w in warnings) Console.WriteLine($"  ⚠ pack : {w}");
                return pack;
            }
            dir = Path.GetDirectoryName(dir.TrimEnd(Path.DirectorySeparatorChar));
        }
        Console.WriteLine("  ⚠ knowledge/pack-2026.08.json introuvable — pack vide, tout sera manuel.");
        return KnowledgePack.Empty;
    }

    private sealed class StubProbe : IEnvironmentProbe
    {
        public List<string> Blocking { get; } = new();
        public long FreeSpace { get; set; } = 10L * 1024 * 1024 * 1024;
        public IReadOnlyList<string> RunningBlockingProcesses() => Blocking;
        public long FreeBackupSpaceBytes() => FreeSpace;
        public bool CanWriteTo(string target) => true;
    }

    private static Finding Finding(string code, string path, string category) => new()
    {
        Code = code, Severity = Severity.Critical, Category = category,
        Subject = Path.GetFileName(path), FilePath = path, EnglishText = code,
    };

    private static RepairPlan Select(RepairPlan p)
        => p with { Items = p.Items.Select(i => i with { Selected = true }).ToList() };

    private static void Write(IFileSystem fs, string path, string content)
        => fs.WriteAllBytes(path, System.Text.Encoding.UTF8.GetBytes(content));

    private static string Read(IFileSystem fs, string path)
        => fs.FileExists(path) ? System.Text.Encoding.UTF8.GetString(fs.ReadAllBytes(path)) : "<absent>";

    private static bool ZipContains(IFileSystem fs, string zip, params string[] names)
    {
        try
        {
            using var ms = new MemoryStream(fs.ReadAllBytes(zip));
            using var archive = new System.IO.Compression.ZipArchive(ms);
            return names.All(n => archive.Entries.Any(e => e.Name == n));
        }
        catch { return false; }
    }

    private static void Journal(InMemoryRepairJournal j, string planId)
    {
        Console.WriteLine("     journal : " + string.Join(" → ",
            j.Read(planId).Select(e => e.Event.ToString())));
    }

    private static string Short(string path)
    {
        var parts = path.Split(Path.DirectorySeparatorChar, '/');
        return parts.Length <= 3 ? path : "…" + Path.DirectorySeparatorChar
             + string.Join(Path.DirectorySeparatorChar, parts[^3..]);
    }

    private static string Yes(bool b) => b ? "oui" : "non";

    private static string Duration(DurationBucket d) => d switch
    {
        DurationBucket.Seconds => "quelques secondes",
        DurationBucket.UnderAMinute => "moins d'une minute",
        _ => "quelques minutes",
    };

    private static void TryDelete(string path)
    {
        try { if (Directory.Exists(path)) Directory.Delete(path, recursive: true); }
        catch { /* le bac à sable est dans %TEMP%, Windows fera le ménage */ }
    }

    private static void Title(string s)
    {
        Console.WriteLine();
        Console.WriteLine("══ " + s + " " + new string('═', Math.Max(0, 60 - s.Length)));
    }

    private static void Step(string s) => Console.WriteLine($"     {s}");
    private static void Ok(string s) => Console.WriteLine($"  ✓ {s}");
    private static void Bad(string s) { Console.WriteLine($"  ✗ {s}"); }
    private static void Skip(string s) => Console.WriteLine($"  — ignoré : {s}");

    private static void Check(string what, bool ok)
    {
        Console.WriteLine(ok ? $"     ✓ {what}" : $"     ✗ {what}");
        if (!ok) _failures++;
    }
}
