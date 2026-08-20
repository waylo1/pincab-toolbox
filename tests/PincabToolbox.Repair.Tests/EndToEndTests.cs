using PincabToolbox.Core.Models;
using PincabToolbox.Repair;
using PincabToolbox.Repair.Actions;

namespace PincabToolbox.Repair.Tests;

/// <summary>
/// The whole chain on the REAL shipped pack: JSON → rules → registry → plan → apply → undo.
/// If this passes, data and code agree.
/// </summary>
public static class EndToEndTests
{
    private static string PackPath()
    {
        var dir = AppContext.BaseDirectory;
        for (var i = 0; i < 8 && dir is not null; i++)
        {
            var p = Path.Combine(dir, "knowledge", "pack-2026.08.json");
            if (File.Exists(p)) return p;
            dir = Path.GetDirectoryName(dir.TrimEnd(Path.DirectorySeparatorChar));
        }
        throw new FileNotFoundException("knowledge/pack-2026.08.json not found from " + AppContext.BaseDirectory);
    }

    private static (FakeFs, IRepairEngine, InMemoryRepairJournal) Real()
    {
        var warnings = new List<string>();
        var pack = KnowledgePack.Load(File.ReadAllText(PackPath()), warnings);
        if (warnings.Count > 0) throw new Exception("shipped pack has warnings: " + string.Join("; ", warnings));

        var fs = new FakeFs();
        var registry = new RepairActionRegistry(new UnblockFileAction(fs), new RestoreRomArchiveAction(fs), new RepositionDmdAction(fs));
        var journal = new InMemoryRepairJournal();
        var eng = new RepairEngine(registry, pack, journal, new FakeBackup(), new FakeProbe(),
                                   new FakeClock(), Build.Roots);
        return (fs, eng, journal);
    }

    public static void Test_ShippedPack_LoadsWithoutWarnings()
    {
        var (_, _, _) = Real();   // throws if the shipped pack is not clean
    }

    public static void Test_ShippedPack_EveryActionIdExistsInTheRegistry()
    {
        var pack = KnowledgePack.Load(File.ReadAllText(PackPath()));
        var fs = new FakeFs();
        var registry = new RepairActionRegistry(new UnblockFileAction(fs), new RestoreRomArchiveAction(fs));

        foreach (var code in new[] { "BLOCKED_DLL", "ROM_UNZIPPED" })
        {
            var rule = pack.RuleFor(code);
            A.True(rule is not null, $"{code} must have a rule");
            A.True(registry.TryGet(rule!.ActionId, out _), $"{code}: action {rule.ActionId} must be compiled in");
        }
    }

    /// <summary>
    /// 14/08/2026 (extension, "si c'est une valeur produit on le fait"): every one of the 51 codes
    /// Knowledge.cs documents Impact/Cause for now ALSO has a pack entry with player/explanation/
    /// verification text, in all three languages. Started at 7 codes for the initial wiring; this
    /// asserts the full set so a future code added to Knowledge.cs without a matching pack entry
    /// fails loudly here instead of silently shipping a code with no detail-panel richness.
    /// </summary>
    public static void Test_ShippedPack_AllFiftyOneKnownCodesExposeEntryInAllThreeLanguages()
    {
        var pack = KnowledgePack.Load(File.ReadAllText(PackPath()));
        var knownCodes = new[]
        {
            "ROM_MISSING", "BITNESS_MISMATCH_VPM", "BITNESS_DMD64_MISSING", "BLOCKED_DLL",
            "B2S_MISSING", "POPPER_NOT_REGISTERED", "COMPAT_SIGNATURE", "LOW_DISK_SPACE",
            "SCANNER_ERROR", "COMPAT_MIN_VERSION", "ALTCOLOR_INCOMPLETE", "ALTSOUND_SAMPLE_MISSING",
            "DISPLAY_OFFSCREEN", "BROKEN_JUNCTION", "B2S_MALFORMED", "POPPER_ORPHAN_PLAYLIST",
            "NVRAM_EMPTY", "VPMALIAS_LOOP", "VPX_VERSION_OUTDATED", "UPDATE_AVAILABLE",
            "BITNESS_MISMATCH_VPM32", "ROM_UNZIPPED", "POPPER_MEDIA_MISSING", "B2S_ORPHAN",
            "B2S_SERVER_MISSING", "FLEXDMD_MISSING", "BITNESS_HYBRID_INSTALL", "SCRIPT_UNREADABLE",
            "TABLES_DIR_NOT_FOUND", "ROMS_DIR_NOT_FOUND", "PINUP_DISPLAY_ZOMBIE",
            "DISPLAY_SETUP_INCOMPLETE", "ORPHANED_MEDIA_FILE", "VPT_LEGACY_PRESENT",
            "AUDIO_DEFAULT_SUSPECT", "DPI_SCALING_NONSTANDARD", "DMD_COM_PORT_NOT_FOUND",
            "LOCALE_DECIMAL_SEPARATOR", "VPINMAME_CONFIG_PHANTOM", "COM_NOT_REGISTERED",
            "COM_STALE_PATH", "COM_BITNESS_GAP", "VPINMAME_NOT_REGISTERED", "CHAIN_BITNESS_GAP",
            "DMD_POSITION_OFFSCREEN", "NVRAM_FOLDER_NOT_WRITABLE", "COM_PATH_OUTSIDE_INSTALL",
            "DMD_VIRTUAL_DISABLED", "ALTSOUND_PRESENT_NOT_ENABLED", "ALTCOLOR_PRESENT_NOT_ENABLED",
            "SCREENRES_UNPARSED",
        };
        A.Equal(51, knownCodes.Length, "sanity: this list must track Knowledge.cs's 51 entries");

        foreach (var code in knownCodes)
        {
            var e = pack.EntryFor(code);
            A.True(e is not null, $"{code}: must have a pack entry");
            A.True(!string.IsNullOrWhiteSpace(e!.PlayerFr), $"{code}: playerFr");
            A.True(!string.IsNullOrWhiteSpace(e.PlayerEn), $"{code}: playerEn");
            A.True(!string.IsNullOrWhiteSpace(e.PlayerEs), $"{code}: playerEs");
            A.True(!string.IsNullOrWhiteSpace(e.ExplanationFr), $"{code}: explanationFr");
            A.True(!string.IsNullOrWhiteSpace(e.ExplanationEn), $"{code}: explanationEn");
            A.True(!string.IsNullOrWhiteSpace(e.ExplanationEs), $"{code}: explanationEs");
            A.True(!string.IsNullOrWhiteSpace(e.VerificationFr), $"{code}: verificationFr");
            A.True(!string.IsNullOrWhiteSpace(e.VerificationEn), $"{code}: verificationEn");
            A.True(!string.IsNullOrWhiteSpace(e.VerificationEs), $"{code}: verificationEs");
        }
    }

    /// <summary>Knowledge.cs marks it AutoFixable, but no action ships in v1 — see ADR-007.</summary>
    public static void Test_ShippedPack_PopperRegistrationIsManualInV1()
    {
        var pack = KnowledgePack.Load(File.ReadAllText(PackPath()));
        A.True(pack.RuleFor("POPPER_NOT_REGISTERED") is null,
            "no automatic rule ships for the Popper database in v1");
    }

    public static void Test_EndToEnd_BlockedDllIsPlannedAppliedAndUndone()
    {
        var (fs, eng, journal) = Real();
        fs.AddFile(@"C:\vpx\VPinMAME\VPinMAME.dll", "BINARY");
        fs.Blocked.Add(@"C:\vpx\VPinMAME\VPinMAME.dll");

        var plan = eng.Plan("scan-1",
            new[] { Build.Finding("BLOCKED_DLL", @"C:\vpx\VPinMAME\VPinMAME.dll", "security") }, licensed: true);

        A.Equal(RepairMode.Automatic, plan.Items[0].Mode, "confidence 98 + reversible → automatic");

        var res = eng.Apply(Build.Select(plan));
        A.False(fs.HasZoneIdentifier(@"C:\vpx\VPinMAME\VPinMAME.dll"), "unblocked");
        A.Equal("BINARY", System.Text.Encoding.UTF8.GetString(fs.ReadAllBytes(@"C:\vpx\VPinMAME\VPinMAME.dll")),
            "content untouched");
        A.False(res.RecoveryRequired, "clean run");

        eng.Undo(plan.PlanId);
        A.True(fs.HasZoneIdentifier(@"C:\vpx\VPinMAME\VPinMAME.dll"), "fully reversible");
    }

    /// <summary>Builds the same minimal MZ/PE header shape as RegisterComComponentActionTests / make_fixtures.py.</summary>
    private static void WriteMinimalPe(string path, ushort machine)
    {
        var header = new byte[64];
        header[0] = (byte)'M'; header[1] = (byte)'Z';
        BitConverter.GetBytes(0x40).CopyTo(header, 0x3C);
        var tail = new byte[4 + 2 + 58];
        tail[0] = (byte)'P'; tail[1] = (byte)'E';
        BitConverter.GetBytes(machine).CopyTo(tail, 4);
        File.WriteAllBytes(path, header.Concat(tail).ToArray());
    }

    /// <summary>
    /// 20/08 — wires <c>register_com_component</c> for the first time (VPINMAME_NOT_REGISTERED,
    /// COM_NOT_REGISTERED, COM_BITNESS_GAP — see FIELD-LOG.md 2026-08-20 and the pack entries
    /// themselves for why COM_STALE_PATH stays out). Proves the whole chain works end to end on
    /// the REAL shipped pack, not just that the JSON parses: a real Finding shaped exactly like
    /// <c>ComHealthScanner.EvaluateVpinmameNotRegistered</c> produces it, through a real temp-disk
    /// VPinMAME.dll + Setup.exe (RegisterComComponentAction reads real disk, not FakeFs — same
    /// reason RegisterComComponentActionTests does this instead of using Real()'s FakeFs registry).
    /// </summary>
    public static void Test_EndToEnd_VpinmameNotRegistered_RealPack_PlansConfirmationRequired()
    {
        var dir = Path.Combine(Path.GetTempPath(), "pincab-e2e-com-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            var dll = Path.Combine(dir, "VPinMAME.dll");
            File.WriteAllText(dll, "x");
            WriteMinimalPe(Path.Combine(dir, "Setup.exe"), 0x8664);

            var pack = KnowledgePack.Load(File.ReadAllText(PackPath()));
            var registry = new RepairActionRegistry(
                new RegisterComComponentAction(new FakeProcessLauncher(), new FakeElevatedProcessLauncher()));
            var eng = new RepairEngine(registry, pack, new InMemoryRepairJournal(), new FakeBackup(),
                new FakeProbe(), new FakeClock(), new[] { dir });

            var finding = new Finding
            {
                Code = "VPINMAME_NOT_REGISTERED", Severity = Severity.Critical, Category = "com",
                Subject = "VPinMAME.Controller", FilePath = dll, EnglishText = "x",
            };

            var planLicensed = eng.Plan("scan-1", new[] { finding }, licensed: true);
            A.Equal(1, planLicensed.Items[0].Changes.Count, "the pack rule + real tool on disk must plan a real change");
            A.Equal(RepairMode.ConfirmationRequired, planLicensed.Items[0].Mode,
                "confidence 80, never-reversible by nature (rule 7) → confirmation, never silently automatic");

            var planUnlicensed = eng.Plan("scan-1", new[] { finding }, licensed: false);
            A.Equal(RepairMode.Locked, planUnlicensed.Items[0].Mode, "ADR-006 — a real fix exists, licence required to see/apply it");
        }
        finally { Directory.Delete(dir, true); }
    }

    /// <summary>20/08 — reposition_dmd on the real shipped pack. Also exercises Apply()+Undo() end to end, unlike the register_com_component test above (that one truly cannot be undone).</summary>
    public static void Test_EndToEnd_DmdOffscreen_PlannedAppliedAndUndone()
    {
        var (fs, eng, _) = Real();
        fs.AddFile(@"C:\vpx\VPinMAME\dmddevice.ini",
            "[virtualdmd]\nenabled = true\nleft = 5000\ntop = 5000\nwidth = 1024\nheight = 256\n");

        var plan = eng.Plan("scan-1",
            new[] { Build.Finding("DMD_POSITION_OFFSCREEN", @"C:\vpx\VPinMAME\dmddevice.ini", "dmd-config") }, licensed: true);

        A.Equal(RepairMode.ConfirmationRequired, plan.Items[0].Mode, "confidence 85, below the 95 automatic threshold");

        eng.Apply(Build.Select(plan));
        var afterApply = System.Text.Encoding.UTF8.GetString(fs.ReadAllBytes(@"C:\vpx\VPinMAME\dmddevice.ini"));
        A.True(afterApply.Contains("left = 0"), "left rewritten on the real shipped pack + real engine");
        A.True(afterApply.Contains("top = 0"), "top rewritten too");

        eng.Undo(plan.PlanId);
        var afterUndo = System.Text.Encoding.UTF8.GetString(fs.ReadAllBytes(@"C:\vpx\VPinMAME\dmddevice.ini"));
        A.True(afterUndo.Contains("left = 5000"), "genuinely reversible — left back to the original offscreen value");
        A.True(afterUndo.Contains("top = 5000"), "top too");
    }

    public static void Test_EndToEnd_UnzippedRomNeedsConfirmationNotAutomatic()
    {
        var (fs, eng, _) = Real();
        fs.AddDir(@"C:\vpx\roms");
        fs.AddDir(@"C:\vpx\roms\mm_109c");
        fs.AddFile(@"C:\vpx\roms\mm_109c\u1.bin", "D1");

        var plan = eng.Plan("scan-1",
            new[] { Build.Finding("ROM_UNZIPPED", @"C:\vpx\roms\mm_109c", "rom") }, licensed: true);

        A.Equal(RepairMode.ConfirmationRequired, plan.Items[0].Mode,
            "confidence 88 stays below the automatic threshold");

        eng.Apply(Build.Select(plan));
        A.True(fs.FileExists(@"C:\vpx\roms\mm_109c.zip"), "archive restored");
    }

    /// <summary>The migration playbook cannot be fully automated — and must say so up front.</summary>
    public static void Test_EndToEnd_MigrationScenarioIsPartialAndHonest()
    {
        var (fs, eng, _) = Real();
        fs.AddFile(@"C:\vpx\VPinMAME\VPinMAME.dll");
        fs.Blocked.Add(@"C:\vpx\VPinMAME\VPinMAME.dll");
        fs.AddFile(@"C:\vpx\VPinMAME\VPinMAME32.dll");
        fs.AddFile(@"C:\vpx\VPinMAME\dmddevice.dll");

        var plan = eng.Plan("scan-1", new[]
        {
            Build.Finding("BLOCKED_DLL", @"C:\vpx\VPinMAME\VPinMAME.dll", "security"),
            Build.Finding("BITNESS_MISMATCH_VPM", @"C:\vpx\VPinMAME\VPinMAME32.dll"),
            Build.Finding("BITNESS_DMD64_MISSING", @"C:\vpx\VPinMAME\dmddevice.dll"),
        }, licensed: true);

        var scenario = plan.Items.FirstOrDefault(i => i.TargetCode == "MIGRATION_32_TO_64_INCOMPLETE");
        A.True(scenario is not null, "the scenario fires when both bitness codes are present");
        A.Equal(Completeness.Partial, scenario!.Completeness, "it is partial");
        A.Equal(2, scenario.Missing.Count, "two steps cannot be automated");
        A.True(scenario.Missing.All(m => !string.IsNullOrWhiteSpace(m.MessageEn)), "and each says why");

        // The two bitness findings have no rule of their own → manual, never silently ignored.
        foreach (var code in new[] { "BITNESS_MISMATCH_VPM", "BITNESS_DMD64_MISSING" })
            A.Equal(RepairMode.ManualOnly, plan.Items.First(i => i.TargetCode == code).Mode, code);
    }
}
