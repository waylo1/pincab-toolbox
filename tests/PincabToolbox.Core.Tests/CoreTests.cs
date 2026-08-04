using PincabToolbox.Core.Models;
using PincabToolbox.Core.Profiles;
using PincabToolbox.Core.Scanning;
using PincabToolbox.Core.Services;
using PincabToolbox.Core.Vpx;

namespace PincabToolbox.Core.Tests;

public static class Fixtures
{
    public static string Dir
    {
        get
        {
            var d = Environment.GetEnvironmentVariable("FIXTURES_DIR");
            if (d is not null && Directory.Exists(d)) return d;
            // walk up from bin to tests/fixtures/out
            var cur = AppContext.BaseDirectory;
            for (int i = 0; i < 8; i++)
            {
                var candidate = Path.Combine(cur, "tests", "fixtures", "out");
                if (Directory.Exists(candidate)) return candidate;
                cur = Path.GetDirectoryName(cur) ?? cur;
            }
            throw new DirectoryNotFoundException("fixtures/out not found — run make_fixtures.py first.");
        }
    }

    public static string F(params string[] parts) => Path.Combine(new[] { Dir }.Concat(parts).ToArray());

    public static Profile Profile()
    {
        var cur = AppContext.BaseDirectory;
        for (int i = 0; i < 8; i++)
        {
            var candidate = Path.Combine(cur, "profiles", "vpx-popper.json");
            if (File.Exists(candidate)) return Profiles.Profile.Load(candidate);
            cur = Path.GetDirectoryName(cur) ?? cur;
        }
        throw new FileNotFoundException("profiles/vpx-popper.json not found.");
    }
}

public static class CompoundFileTests
{
    public static void Test_Opens_Fixture_And_Reads_Script()
    {
        var table = VpxReader.Read(Fixtures.F("simple.vpx"));
        Assert.Equal(null, table.Error);
        Assert.NotNull(table.Script);
        Assert.Contains("cGameName = \"afm_113b\"", table.Script);
        Assert.Contains("Table1_Init", table.Script);
    }

    public static void Test_Reads_TableInfo_Strings()
    {
        var table = VpxReader.Read(Fixtures.F("simple.vpx"));
        Assert.Equal("Simple Table", table.TableName);
        Assert.Equal("1.2.3", table.TableVersion);
        Assert.Equal("Tester", table.AuthorName);
    }

    public static void Test_Rejects_Garbage()
    {
        var tmp = Path.Combine(Path.GetTempPath(), "not_a_cfb.vpx");
        File.WriteAllBytes(tmp, new byte[600]);
        var table = VpxReader.Read(tmp);
        Assert.NotNull(table.Error);
        File.Delete(tmp);
    }

    public static void Test_ExtractScript_Handles_Plain_Biff()
    {
        var script = "Sub X()\r\nEnd Sub";
        var biff = BitConverter.GetBytes(4).Concat("CODE"u8.ToArray())
            .Concat(BitConverter.GetBytes(script.Length))
            .Concat(System.Text.Encoding.Latin1.GetBytes(script))
            .Concat(BitConverter.GetBytes(4)).Concat("ENDB"u8.ToArray())
            .ToArray();
        Assert.Equal(script, VpxReader.ExtractScript(biff));
    }

    public static void Test_DecodeInfoBytes_Utf16_And_Ansi()
    {
        Assert.Equal("1.2", VpxReader.DecodeInfoBytes(System.Text.Encoding.Unicode.GetBytes("1.2")));
        Assert.Equal("1.2", VpxReader.DecodeInfoBytes(System.Text.Encoding.Latin1.GetBytes("1.2")));
    }
}

public static class ScriptAnalyzerTests
{
    public static void Test_Const_GameName()
    {
        var r = ScriptAnalyzer.AnalyzeRomUsage("Const cGameName = \"afm_113b\"\nSet c = CreateObject(\"VPinMAME.Controller\")");
        Assert.True(r.UsesController);
        Assert.Equal("afm_113b", r.Primary);
    }

    public static void Test_Assignment_And_Multiple_Candidates()
    {
        var script = """
            If UseModded Then
                cGameName = "mm_mod"
            Else
                cGameName = "mm_109c"
            End If
            Set Controller = CreateObject("VPinMAME.Controller")
            """;
        var r = ScriptAnalyzer.AnalyzeRomUsage(script);
        Assert.Equal(2, r.Candidates.Count);
    }

    public static void Test_Em_Table_No_Controller()
    {
        var r = ScriptAnalyzer.AnalyzeRomUsage("Sub Table1_Init()\nEnd Sub");
        Assert.False(r.UsesController);
        Assert.Equal(0, r.Candidates.Count);
    }

    public static void Test_B2S_Backglass_Is_Not_A_Rom_Signal()
    {
        // KPI#1 false positive: an original/homebrew table with a B2S backglass and a game name,
        // but no VPinMAME controller, must NOT read as "needs a ROM". UsesController is the
        // VPinMAME signal only; B2S is tracked separately. (FIELD-LOG 2026-07-30)
        var script = "Set B2SController = CreateObject(\"B2S.Server\")\nB2SController.GameName = \"gotg\"";
        var r = ScriptAnalyzer.AnalyzeRomUsage(script);
        Assert.False(r.UsesController, "B2S.Server alone is not a VPinMAME ROM signal");
        Assert.True(r.UsesB2S, "B2S.Server usage should still be detected");
    }

    public static void Test_Vpinmame_Controller_Is_A_Rom_Signal()
    {
        var r = ScriptAnalyzer.AnalyzeRomUsage("Set c = CreateObject(\"VPinMAME.Controller\")");
        Assert.True(r.UsesController);
        Assert.False(r.UsesB2S);
    }

    // ---------------------------------------------------------------------------------------
    // Commented-out VPinMAME boilerplate — the KPI#1 FP source that survived the first fix.
    // Gregg's "criticals I think are originals without a ROM" (FB, 2026-08-03).
    // ---------------------------------------------------------------------------------------

    public static void Test_CommentedOut_Controller_Is_Not_A_Rom_Signal()
    {
        // The template an original was built from, with the ROM plumbing commented out.
        var script = """
            Const cGameName = "myoriginal"
            ' Set Controller = CreateObject("VPinMAME.Controller")
            ' Controller.GameName = cGameName
            Set B2S = CreateObject("B2S.Server")
            """;
        var r = ScriptAnalyzer.AnalyzeRomUsage(script);
        Assert.False(r.UsesController, "a commented-out CreateObject is dead code, not a ROM signal");
        Assert.True(r.UsesB2S, "the live B2S line must still be seen");
    }

    public static void Test_Rem_Commented_Controller_Is_Not_A_Rom_Signal()
    {
        var r = ScriptAnalyzer.AnalyzeRomUsage("REM Set c = CreateObject(\"VPinMAME.Controller\")");
        Assert.False(r.UsesController, "REM comments are comments too");
    }

    public static void Test_Rem_Inside_A_Word_Is_Not_A_Comment()
    {
        // "REMOVE"/"PREMIUM" must not truncate a live line.
        var r = ScriptAnalyzer.AnalyzeRomUsage(
            "REMOVEME = 1 : Set c = CreateObject(\"VPinMAME.Controller\")");
        Assert.True(r.UsesController, "REM only comments when it stands alone as a word");
    }

    public static void Test_Apostrophe_Inside_A_String_Does_Not_Start_A_Comment()
    {
        // "Rocky & Bullwinkle's" — an apostrophe inside a literal must not kill the rest of the line.
        var r = ScriptAnalyzer.AnalyzeRomUsage(
            "TableName = \"Rocky & Bullwinkle's\" : Set c = CreateObject(\"VPinMAME.Controller\")");
        Assert.True(r.UsesController, "an apostrophe inside a string literal is not a comment");
    }

    public static void Test_Live_Controller_After_A_Commented_Line_Still_Counts()
    {
        var script = """
            ' Set Controller = CreateObject("VPinMAME.Controller")
            Set Controller = CreateObject("VPinMAME.Controller")
            Const cGameName = "mm_109c"
            """;
        var r = ScriptAnalyzer.AnalyzeRomUsage(script);
        Assert.True(r.UsesController, "stripping comments must not hide the live declaration");
        Assert.Equal("mm_109c", r.Primary);
    }

    public static void Test_Trailing_Comment_On_A_Live_Line_Is_Trimmed_Only()
    {
        var r = ScriptAnalyzer.AnalyzeRomUsage(
            "Set c = CreateObject(\"VPinMAME.Controller\") ' the real one");
        Assert.True(r.UsesController);
    }
}

/// <summary>
/// Mod/variant recognition. Two independent reports on 2026-08-03 (Chad Greenaway, Gregg) plus
/// FD's earlier renaming report all reduce to the same thing: a derivative matches the base
/// table by name+year and then gets compared against a version it will never have.
/// </summary>
public static class TableVariantDetectorTests
{
    public static void Test_Mod_Suffix_Is_Detected()
    {
        Assert.Equal("MOD", TableVariantDetector.DetectDerivativeMarker(
            "Medieval Madness (Williams 1997) MOD 1.2"));
    }

    public static void Test_Bigus_Is_Detected_Both_Spellings()
    {
        Assert.True(TableVariantDetector.IsDerivative("Attack From Mars (Bally 1995) Bigus 2.0"));
        Assert.True(TableVariantDetector.IsDerivative("Attack From Mars (Bally 1995) Biggus 2.0"));
    }

    public static void Test_Parenthesised_Mod_Tag_Is_Detected()
    {
        // The shape Gregg reported: a "(MOD)" tag of its own.
        Assert.True(TableVariantDetector.IsDerivative("Some Table (Stern 2015) (MOD)"));
    }

    public static void Test_Plain_Table_Is_Not_A_Derivative()
    {
        Assert.False(TableVariantDetector.IsDerivative("Medieval Madness (Williams 1997)"));
        Assert.False(TableVariantDetector.IsDerivative("Attack From Mars (Bally 1995) 3.0"));
    }

    public static void Test_Marker_Must_Be_A_Whole_Token()
    {
        // The expensive mistake is the false positive: it silently hides a real update.
        Assert.False(TableVariantDetector.IsDerivative("Modern Times (Gottlieb 1965)"));
        Assert.False(TableVariantDetector.IsDerivative("Bigger Bang (Original 2021)"));
        Assert.False(TableVariantDetector.IsDerivative("The Model Shop (Original 2020)"));
    }

    public static void Test_Manufacturer_Group_Is_Never_Searched()
    {
        // A manufacturer or author field must not be able to trip the detector.
        Assert.False(TableVariantDetector.IsDerivative("Some Table (MOD Industries 1999)"));
    }

    public static void Test_Empty_Name_Is_Not_A_Derivative()
    {
        Assert.False(TableVariantDetector.IsDerivative(""));
        Assert.False(TableVariantDetector.IsDerivative("   "));
    }
}

/// <summary>
/// End-to-end behaviour of the mod filter Chad asked for: the base table still gets its update
/// notice, the mod does not, and the omission is stated rather than silent.
/// </summary>
public static class UpdateWatcherModTests
{
    private static List<VpsGame> Db()
    {
        var g = new VpsGame { Id = "vps-mm", Name = "Medieval Madness", Year = 1997 };
        g.TableFiles.Add(new VpsTableFile { Id = "f1", Version = "3.0", TableFormat = "VPX" });
        return new List<VpsGame> { g };
    }

    private static List<Finding> Scan(params (string name, string version)[] tables)
    {
        var tmp = Path.Combine(Path.GetTempPath(), "pt_uw_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tmp);
        try
        {
            var layout = new InstallLayout { RootPath = tmp, TablesDir = tmp };
            var ctx = new ScanContext { Layout = layout, Profile = Fixtures.Profile() };
            foreach (var (name, version) in tables)
            {
                var p = Path.Combine(tmp, name + ".vpx");
                ctx.Tables[p] = new VpxTableData { FilePath = p, Script = "", TableVersion = version };
            }
            return new UpdateWatcherScanner(Db()).Scan(ctx).ToList();
        }
        finally { Directory.Delete(tmp, true); }
    }

    public static void Test_Base_Table_Still_Reported_Outdated()
    {
        var f = Scan(("Medieval Madness (Williams 1997)", "1.2"));
        Assert.True(f.Any(x => x.Code == "UPDATE_AVAILABLE"),
            "the mod filter must not suppress genuine updates");
    }

    public static void Test_Mod_Is_Not_Reported_Outdated()
    {
        var f = Scan(("Medieval Madness (Williams 1997) MOD 1.2", "1.2"));
        Assert.False(f.Any(x => x.Code == "UPDATE_AVAILABLE"),
            "a mod versions independently — comparing it to the base table is meaningless");
    }

    public static void Test_Skipped_Mods_Are_Counted_Not_Silently_Dropped()
    {
        var f = Scan(
            ("Medieval Madness (Williams 1997)", "1.2"),
            ("Medieval Madness (Williams 1997) Bigus 1.0", "1.0"));

        var summary = f.Single(x => x.Code == "VPS_MATCH_SUMMARY");
        Assert.Equal("1", summary.Args[2]);
        Assert.True(summary.EnglishText.Contains("mods/variants"),
            "an unexplained omission is indistinguishable from a bug");
    }

    public static void Test_Update_Finding_Carries_The_Vps_Game_Id()
    {
        var f = Scan(("Medieval Madness (Williams 1997)", "1.2"))
            .Single(x => x.Code == "UPDATE_AVAILABLE");
        Assert.Equal("vps-mm", f.Args[4]);
    }
}

/// <summary>Direct VPS link (Chad Greenaway, FIELD-LOG 2026-08-03) — off unless configured.</summary>
public static class VpsGameLinkTests
{
    public static void Test_No_Template_Yields_No_Direct_Link()
    {
        var src = new UpdateSource { SiteUrl = "https://example.org" };
        Assert.True(src.GameUrl("abc123") is null, "a wrong link is worse than no link");
    }

    public static void Test_Template_Substitutes_The_Game_Id()
    {
        var src = new UpdateSource { GameUrlTemplate = "https://example.org/game/{id}" };
        Assert.Equal("https://example.org/game/abc123", src.GameUrl("abc123"));
    }

    public static void Test_Game_Id_Is_Url_Escaped()
    {
        var src = new UpdateSource { GameUrlTemplate = "https://example.org/game/{id}" };
        Assert.Equal("https://example.org/game/a%20b", src.GameUrl("a b"));
    }

    public static void Test_Missing_Id_Yields_No_Link()
    {
        var src = new UpdateSource { GameUrlTemplate = "https://example.org/game/{id}" };
        Assert.True(src.GameUrl(null) is null, "no id, no link");
        Assert.True(src.GameUrl("") is null, "no id, no link");
    }
}

/// <summary>
/// The blocked-DLL check was the only scanner with no test of its own — and it is the detection
/// behind the one repair action the field has confirmed twice (VPForums + Pincab Passion). The
/// NTFS stream read itself cannot run here, so the two decisions it feeds are tested directly.
/// </summary>
public static class BlockedFileTests
{
    public static void Test_Core_Plugins_Are_Critical()
    {
        foreach (var n in new[] { "VPinMAME.dll", "dmddevice64.dll", "B2SBackglassServer.dll", "flexdmd.dll", "dof.dll" })
            Assert.Equal(Severity.Critical, BlockedFileScanner.SeverityFor(n));
    }

    public static void Test_Classification_Is_Case_Insensitive_And_Path_Tolerant()
    {
        Assert.Equal(Severity.Critical, BlockedFileScanner.SeverityFor(@"C:\vpx\VPINMAME.DLL"));
        Assert.Equal(Severity.Critical, BlockedFileScanner.SeverityFor("/mnt/vpx/vpinmame.dll"));
    }

    public static void Test_Other_Dlls_Are_Only_Warnings()
    {
        Assert.Equal(Severity.Warning, BlockedFileScanner.SeverityFor("SomeRandomPlugin.dll"));
        Assert.Equal(Severity.Warning, BlockedFileScanner.SeverityFor("vpinmame_helper.dll"));
    }

    public static void Test_Internet_And_Untrusted_Zones_Are_Blocked()
    {
        Assert.True(BlockedFileScanner.IsBlockedZone("[ZoneTransfer]\r\nZoneId=3\r\n"));
        Assert.True(BlockedFileScanner.IsBlockedZone("[ZoneTransfer]\r\nZoneId=4\r\n"));
    }

    public static void Test_Local_And_Trusted_Zones_Are_Not_Blocked()
    {
        // A domain-joined cab would otherwise light up on every file.
        foreach (var z in new[] { "0", "1", "2" })
            Assert.False(BlockedFileScanner.IsBlockedZone($"[ZoneTransfer]\r\nZoneId={z}\r\n"), $"zone {z}");
    }

    public static void Test_Absent_Or_Unparseable_Stream_Is_Not_Blocked()
    {
        Assert.False(BlockedFileScanner.IsBlockedZone(null));
        Assert.False(BlockedFileScanner.IsBlockedZone(""));
        Assert.False(BlockedFileScanner.IsBlockedZone("[ZoneTransfer]\r\nReferrerUrl=http://x\r\n"));
        Assert.False(BlockedFileScanner.IsBlockedZone("ZoneId=notanumber"));
    }

    public static void Test_Clean_Tree_Reports_The_All_Clear()
    {
        var tmp = Path.Combine(Path.GetTempPath(), "pt_blk_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tmp);
        try
        {
            File.WriteAllText(Path.Combine(tmp, "vpinmame.dll"), "x");   // present, not blocked
            var ctx = new ScanContext
            {
                Layout = new InstallLayout { RootPath = tmp },
                Profile = Fixtures.Profile(),
            };
            var f = new BlockedFileScanner().Scan(ctx).ToList();

            Assert.True(f.Any(x => x.Code == "BLOCKED_NONE" && x.Severity == Severity.Ok),
                "a clean install must say so explicitly, not stay silent");
            Assert.False(f.Any(x => x.Code == "BLOCKED_DLL"), "nothing is blocked here");
        }
        finally { Directory.Delete(tmp, true); }
    }

    public static void Test_Missing_Root_Yields_Nothing_Rather_Than_Throwing()
    {
        var ctx = new ScanContext
        {
            Layout = new InstallLayout { RootPath = Path.Combine(Path.GetTempPath(), "pt_absent_" + Guid.NewGuid().ToString("N")) },
            Profile = Fixtures.Profile(),
        };
        Assert.Equal(0, new BlockedFileScanner().Scan(ctx).Count());
    }
}

/// <summary>
/// Report rollup. The score was capped on 2026-07-30 but the report itself was still thousands of
/// rows on a large collection (FD: 2711 info lines) — the real readability problem.
/// </summary>
public static class RollupTests
{
    private static ScanReport Report(params (Severity sev, string code, int count)[] groups)
    {
        var r = new ScanReport { Layout = new InstallLayout { RootPath = "/x" } };
        foreach (var (sev, code, count) in groups)
            for (var i = 0; i < count; i++)
                r.Findings.Add(new Finding
                {
                    Code = code, Severity = sev, Category = "rom",
                    Subject = $"table{i}", EnglishText = code,
                });
        return r;
    }

    public static void Test_Repetitive_Ok_Findings_Collapse_To_One_Row()
    {
        var rolled = Report((Severity.Ok, "ROM_OK", 2038)).Rolled().ToList();

        Assert.Equal(1, rolled.Count);
        Assert.Equal(ScanReport.RollupCode, rolled[0].Code);
        Assert.Equal("2038", rolled[0].Args[0]);
        Assert.Equal("ROM_OK", rolled[0].Args[1]);
    }

    public static void Test_Criticals_Are_Never_Collapsed()
    {
        // 300 broken tables must look like 300 broken tables.
        var rolled = Report((Severity.Critical, "ROM_MISSING", 300)).Rolled().ToList();

        Assert.Equal(300, rolled.Count);
        Assert.False(rolled.Any(f => f.Code == ScanReport.RollupCode), "no critical may hide behind a count");
    }

    public static void Test_Small_Groups_Stay_Listed_Individually()
    {
        var rolled = Report((Severity.Info, "UPDATE_AVAILABLE", 4)).Rolled().ToList();
        Assert.Equal(4, rolled.Count);
        Assert.False(rolled.Any(f => f.Code == ScanReport.RollupCode), "4 rows are more useful than a count of 4");
    }

    public static void Test_Same_Code_Different_Severity_Is_Not_Merged()
    {
        // BLOCKED_DLL is Critical for a core plugin and Warning for anything else.
        var rolled = Report(
            (Severity.Critical, "BLOCKED_DLL", 6),
            (Severity.Warning, "BLOCKED_DLL", 6)).Rolled().ToList();

        Assert.Equal(6, rolled.Count(f => f.Severity == Severity.Critical));
        Assert.Equal(1, rolled.Count(f => f.Code == ScanReport.RollupCode));
    }

    public static void Test_Rollup_Keeps_Category_And_Severity_Of_Its_Members()
    {
        var g = Report((Severity.Info, "UPDATE_AVAILABLE", 50)).Rolled().Single();
        Assert.Equal(Severity.Info, g.Severity);
        Assert.Equal("rom", g.Category);
    }

    public static void Test_Nothing_Is_Lost_Ordered_Still_Has_Everything()
    {
        var r = Report((Severity.Ok, "ROM_OK", 2038), (Severity.Critical, "ROM_MISSING", 3));
        Assert.Equal(2041, r.Ordered().Count());
        Assert.Equal(4, r.Rolled().Count());   // 1 rollup + 3 criticals
    }

    public static void Test_Score_Is_Unaffected_By_Rollup()
    {
        // The rollup is a view. It must never change the diagnosis.
        var r = Report((Severity.Critical, "ROM_MISSING", 2), (Severity.Info, "UPDATE_AVAILABLE", 900));
        var before = r.Score;
        _ = r.Rolled().ToList();
        Assert.Equal(before, r.Score);
    }

    public static void Test_Threshold_Below_Two_Is_Clamped()
    {
        // A threshold of 1 or 0 would collapse single findings into "1 similar finding".
        var rolled = Report((Severity.Info, "UPDATE_AVAILABLE", 1)).Rolled(threshold: 0).ToList();
        Assert.Equal(1, rolled.Count);
        Assert.False(rolled.Any(f => f.Code == ScanReport.RollupCode), "a lone finding is never a group");
    }
}

public static class AliasFileTests
{
    public static void Test_Parse()
    {
        var tmp = Path.GetTempFileName();
        File.WriteAllText(tmp, "// c\n'quoted comment\nafm_mod,afm_113\n bad_line \nx,y\n");
        var map = AliasFile.Parse(tmp);
        Assert.Equal(2, map.Count);
        Assert.Equal("afm_113", map["afm_mod"]);
        File.Delete(tmp);
    }
}

public static class PeInspectorTests
{
    public static void Test_X86() => Assert.Equal(Bitness.X86, PeInspector.GetBitness(Fixtures.F("x86.exe")));
    public static void Test_X64() => Assert.Equal(Bitness.X64, PeInspector.GetBitness(Fixtures.F("x64.exe")));
    public static void Test_NotPe()
    {
        var tmp = Path.GetTempFileName();
        File.WriteAllText(tmp, "hello");
        Assert.Equal(Bitness.Unknown, PeInspector.GetBitness(tmp));
        File.Delete(tmp);
    }
}

public static class SqliteReaderTests
{
    public static void Test_Reads_Games_Table()
    {
        var rows = SqliteReader.TryReadTable(Fixtures.F("pup.db"), "Games", "GameName", "GameFileName");
        Assert.NotNull(rows);
        Assert.Equal(3, rows!.Count);
        Assert.True(rows.Any(r => r[0] == "Attack From Mars (Bally 1995)"));
    }

    public static void Test_Utf8_Accents()
    {
        var rows = SqliteReader.TryReadTable(Fixtures.F("pup.db"), "Games", "GameDisplay")!;
        Assert.True(rows.Any(r => r[0] == "Médiéval Madness"), "accented text must round-trip");
    }

    public static void Test_Overflow_Row()
    {
        var rows = SqliteReader.TryReadTable(Fixtures.F("pup.db"), "Games", "Notes")!;
        Assert.True(rows.Any(r => r[0] is not null && r[0]!.Length == 8000), "8000-char note exercises overflow pages");
    }

    public static void Test_Missing_Table_Returns_Null()
    {
        Assert.Equal(null, SqliteReader.TryReadTable(Fixtures.F("pup.db"), "NoSuchTable", "X"));
    }

    public static void Test_ParseColumns()
    {
        var cols = SqliteReader.ParseColumns(
            "CREATE TABLE t([GameID] INTEGER PRIMARY KEY, \"GameName\" TEXT, Notes TEXT, CHECK(GameID > 0), FOREIGN KEY(GameID) REFERENCES x(y))");
        Assert.Equal(3, cols.Count);
        Assert.Equal("GameID", cols[0]);
        Assert.Equal("GameName", cols[1]);
    }
}

public static class MyersDiffTests
{
    public static void Test_Equal_Texts()
    {
        var chunks = MyersDiff.DiffLines("a\nb", "a\nb");
        Assert.Equal(1, chunks.Count);
        Assert.Equal(DiffOp.Equal, chunks[0].Op);
    }

    public static void Test_Insert_Delete()
    {
        var chunks = MyersDiff.DiffLines("a\nb\nc", "a\nc");
        Assert.True(chunks.Any(c => c.Op == DiffOp.Delete && c.Length == 1));
        var chunks2 = MyersDiff.DiffLines("a\nc", "a\nb\nc");
        Assert.True(chunks2.Any(c => c.Op == DiffOp.Insert && c.Length == 1));
    }

    public static void Test_DiffService_Pairs_Modified()
    {
        var t1 = Path.Combine(Path.GetTempPath(), "s1.vbs");
        var t2 = Path.Combine(Path.GetTempPath(), "s2.vbs");
        File.WriteAllText(t1, "line1\nline2\nline3\n");
        File.WriteAllText(t2, "line1\nCHANGED\nline3\nADDED\n");
        var d = DiffService.DiffFiles(t1, t2);
        Assert.Equal(null, d.Error);
        Assert.Equal(1, d.ModifiedCount);
        Assert.Equal(1, d.InsertedCount);
        Assert.Equal(d.OldLines.Count, d.NewLines.Count);
        File.Delete(t1); File.Delete(t2);
    }

    public static void Test_Diff_Two_Vpx_Fixtures()
    {
        var d = DiffService.DiffFiles(Fixtures.F("simple.vpx"), Fixtures.F("simple_v2.vpx"));
        Assert.Equal(null, d.Error);
        Assert.True(d.InsertedCount >= 1, "v2 script has an extra line");
    }
}

public static class VpsDatabaseTests
{
    private const string SampleJson = """
        [
          {"id":"abc","name":"Attack From Mars","manufacturer":"Bally","year":1995,
           "tableFiles":[{"id":"t1","version":"1.2"},{"id":"t2","version":"2.0.1"}]},
          {"id":"def","name":"Medieval Madness","manufacturer":"Williams","year":1997,
           "tableFiles":[{"id":"t3","version":"0.9"}]}
        ]
        """;

    public static void Test_Parse()
    {
        var games = VpsDatabase.Parse(SampleJson);
        Assert.Equal(2, games.Count);
        Assert.Equal(2, games[0].TableFiles.Count);
    }

    public static void Test_Match_By_Name_And_Year()
    {
        var games = VpsDatabase.Parse(SampleJson);
        var g = VpsDatabase.Match(games, "Attack From Mars (Bally 1995) v1.2");
        Assert.NotNull(g);
        Assert.Equal("abc", g!.Id);
        Assert.Equal(null, VpsDatabase.Match(games, "Attack From Mars (Bally 1996)"));
    }

    public static void Test_CompareVersions()
    {
        Assert.True(VpsDatabase.CompareVersions("2.0.1", "1.9") > 0);
        Assert.True(VpsDatabase.CompareVersions("v1.2", "1.2") == 0);
        Assert.True(VpsDatabase.CompareVersions("1.10", "1.9") > 0);
        Assert.True(VpsDatabase.CompareVersions(null, "1.0") == 0);
    }

    public static void Test_ParseTableFileName()
    {
        var p = VpsDatabase.ParseTableFileName("Medieval Madness (Williams 1997) v2");
        Assert.NotNull(p);
        Assert.Equal("Medieval Madness", p!.Value.name);
        Assert.Equal(1997, p.Value.year);
    }

    /// <summary>Runs only when the real ~7MB VPS db has been placed in fixtures (never committed).</summary>
    public static void Test_Real_Vps_Db_If_Present()
    {
        var path = Path.Combine(Fixtures.Dir, "vpsdb.json");
        if (!File.Exists(path)) return; // graceful skip
        var games = VpsDatabase.Parse(File.ReadAllText(path));
        Assert.True(games.Count > 2000, $"expected >2000 games, got {games.Count}");

        var afm = VpsDatabase.Match(games, "Attack From Mars (Bally 1995) v1.2");
        Assert.NotNull(afm, "AFM must match against the real db");
        Assert.True(afm!.TableFiles.Count > 5);
        Assert.True(afm.TableFiles.Any(t => t.TableFormat == "VPX"), "AFM has VPX-format files");
    }
}

public static class EndToEndTests
{
    private static ScanReport RunScan()
    {
        var root = Fixtures.F("install");
        var profile = Fixtures.Profile();
        var engine = new ScanEngine()
            .Add(new RomValidatorScanner())
            .Add(new BitnessScanner())
            .Add(new CompletenessScanner())
            .Add(new CompatibilityScanner())
            .Add(new DependencyScanner())
            .Add(new DiskSpaceScanner())
            .Add(new LegacyTableScanner())
            .Add(new UpdateWatcherScanner(null));
        return engine.Run(root, profile);
    }

    public static void Test_Layout_Detected()
    {
        var report = RunScan();
        Assert.NotNull(report.Layout.TablesDir);
        Assert.NotNull(report.Layout.RomsDir);
        Assert.NotNull(report.Layout.PupDatabasePath);
        Assert.Equal(4, report.Layout.VpxTables.Count);
    }

    public static void Test_Rom_Findings()
    {
        var report = RunScan();
        Assert.True(report.Findings.Any(f => f.Code == "ROM_MISSING" && f.Subject.Contains("Medieval")),
            "Medieval Madness ROM must be reported missing");
        Assert.True(report.Findings.Any(f => f.Code == "ROM_OK" && f.Subject.Contains("Attack")),
            "AFM rom present");
        Assert.True(report.Findings.Any(f => f.Code == "ROM_OK" && f.EnglishText.Contains("alias")),
            "aliased table resolves through VPMAlias");
        Assert.True(report.Findings.Any(f => f.Code == "ROM_NOT_REQUIRED" && f.Subject.Contains("Original")),
            "EM table requires no ROM");
    }

    public static void Test_Bitness_Mismatch_Detected()
    {
        var report = RunScan();
        Assert.True(report.Findings.Any(f => f.Code == "BITNESS_MISMATCH_VPM" && f.Severity == Severity.Critical),
            "64-bit exe + 32-bit VPinMAME must be critical");
        Assert.True(report.Findings.Any(f => f.Code == "BITNESS_DMD64_MISSING"));
    }

    public static void Test_Completeness_Findings()
    {
        var report = RunScan();
        Assert.True(report.Findings.Any(f => f.Code == "B2S_MISSING" && f.Subject.Contains("Medieval")));
        Assert.False(report.Findings.Any(f => f.Code == "B2S_MISSING" && f.Subject.Contains("Attack")),
            "AFM has a backglass fixture");
        Assert.True(report.Findings.Any(f => f.Code == "POPPER_NOT_REGISTERED" && f.Subject.Contains("Original")),
            "Original Gem is not in PUPDatabase fixture");
    }

    public static void Test_Compat_Findings()
    {
        var report = RunScan();
        Assert.True(report.Findings.Any(f => f.Code == "COMPAT_MIN_VERSION" && f.Args.Contains("10.8")),
            "declared 'requires VPX 10.8' must surface");
        // A declared min-version is Info, not Warning: we never compare it to the installed VPX,
        // so it is not a defect. Reporting it as Warning flipped large healthy collections to F
        // (FIELD-LOG 2026-07-30 / FD report). Lock the severity so it can't regress.
        Assert.True(report.Findings.All(f => f.Code != "COMPAT_MIN_VERSION" || f.Severity == Severity.Info),
            "COMPAT_MIN_VERSION must be Info (declarative note, not a defect)");
        Assert.True(report.Findings.Any(f => f.Code == "COMPAT_SIGNATURE" && f.EnglishText.Contains("nFozzy")));
    }

    public static void Test_Updates_Skipped_Offline()
    {
        var report = RunScan();
        Assert.True(report.Findings.Any(f => f.Code == "VPS_UNAVAILABLE"));
    }

    public static void Test_Dependency_B2S_Server_Missing_On_Fixture()
    {
        // The fixture install has a .directb2s (AFM) but no B2SBackglassServer.dll.
        var report = RunScan();
        Assert.True(report.Findings.Any(f => f.Code == "B2S_SERVER_MISSING" && f.Severity == Severity.Warning),
            "backglass present but no B2S server dll must warn");
    }

    public static void Test_No_False_FlexDmd_On_Fixture()
    {
        // No fixture script uses FlexDMD → the check must stay silent (low-false-positive rule).
        var report = RunScan();
        Assert.False(report.Findings.Any(f => f.Code == "FLEXDMD_MISSING"),
            "FlexDMD must not be flagged when no script uses it");
    }
}

public static class DependencyScannerTests
{
    public static void Test_FlexDmd_Missing_When_Script_Uses_It()
    {
        // Root exists (fixture install) and has no FlexDMD.dll; one synthetic table uses FlexDMD.
        var layout = new InstallLayout { RootPath = Fixtures.F("install") };
        var ctx = new ScanContext { Layout = layout, Profile = Fixtures.Profile() };
        ctx.Tables["Foo (Test 2024).vpx"] = new VpxTableData
        {
            FilePath = "Foo (Test 2024).vpx",
            Script = "Dim d\nSet d = CreateObject(\"FlexDMD.FlexDMD\")\n",
        };
        var findings = new DependencyScanner().Scan(ctx).ToList();
        Assert.True(findings.Any(f => f.Code == "FLEXDMD_MISSING" && f.Args.Contains("1")),
            "one table uses FlexDMD, dll absent → warn");
    }

    public static void Test_B2S_Server_Missing_From_Script_Signal()
    {
        // No backglass files (TablesDir null) but a script explicitly creates B2S.Server.
        var layout = new InstallLayout { RootPath = Fixtures.F("install") };
        var ctx = new ScanContext { Layout = layout, Profile = Fixtures.Profile() };
        ctx.Tables["Bar.vpx"] = new VpxTableData
        {
            FilePath = "Bar.vpx",
            Script = "Set b = CreateObject(\"B2S.Server\")\n",
        };
        var findings = new DependencyScanner().Scan(ctx).ToList();
        Assert.True(findings.Any(f => f.Code == "B2S_SERVER_MISSING"),
            "script uses B2S.Server, dll absent → warn");
    }

    public static void Test_No_Findings_When_Nothing_Needed()
    {
        // A script that uses neither component must produce no dependency findings.
        var layout = new InstallLayout { RootPath = Fixtures.F("install") };
        var ctx = new ScanContext { Layout = layout, Profile = Fixtures.Profile() };
        ctx.Tables["Plain.vpx"] = new VpxTableData { FilePath = "Plain.vpx", Script = "Sub Init()\nEnd Sub\n" };
        var findings = new DependencyScanner().Scan(ctx).ToList();
        Assert.False(findings.Any(f => f.Code == "FLEXDMD_MISSING"));
        Assert.False(findings.Any(f => f.Code == "B2S_SERVER_MISSING"));
    }
}

public static class CompletenessOrphanTests
{
    public static void Test_Orphan_Backglass_Flagged_But_Not_Matched_One()
    {
        var tmp = Directory.CreateTempSubdirectory().FullName;
        try
        {
            var tables = Path.Combine(tmp, "Tables");
            Directory.CreateDirectory(tables);
            File.WriteAllText(Path.Combine(tables, "Good Table (2020).directb2s"), "b2s");
            File.WriteAllText(Path.Combine(tables, "Stray Typo (2019).directb2s"), "b2s");

            var layout = new InstallLayout { RootPath = tmp };
            layout.TablesDir = tables;
            var ctx = new ScanContext { Layout = layout, Profile = Fixtures.Profile() };
            // Only "Good Table (2020)" exists as a table; the stray backglass matches nothing.
            ctx.Tables[Path.Combine(tables, "Good Table (2020).vpx")] =
                new VpxTableData { FilePath = "Good Table (2020).vpx", Script = "Sub I()\nEnd Sub" };

            var findings = new CompletenessScanner().Scan(ctx).ToList();
            Assert.True(findings.Any(f => f.Code == "B2S_ORPHAN" && f.Subject == "Stray Typo (2019)"),
                "the misnamed backglass must be flagged as an orphan");
            Assert.False(findings.Any(f => f.Code == "B2S_ORPHAN" && f.Subject == "Good Table (2020)"),
                "a correctly-named backglass must never be flagged");
        }
        finally { Directory.Delete(tmp, true); }
    }
}

public static class RomUnzippedTests
{
    public static void Test_Unzipped_Folder_Reported_Not_Missing()
    {
        var tmp = Directory.CreateTempSubdirectory().FullName;
        try
        {
            var roms = Path.Combine(tmp, "roms");
            Directory.CreateDirectory(Path.Combine(roms, "testrom")); // extracted folder, no .zip

            var layout = new InstallLayout { RootPath = tmp };
            layout.TablesDir = tmp;
            layout.RomsDir = roms;
            var ctx = new ScanContext { Layout = layout, Profile = Fixtures.Profile() };
            ctx.Tables["Foo.vpx"] = new VpxTableData
            {
                FilePath = "Foo.vpx",
                Script = "Const cGameName = \"testrom\"\nSet c = CreateObject(\"VPinMAME.Controller\")",
            };

            var findings = new RomValidatorScanner().Scan(ctx).ToList();
            Assert.True(findings.Any(f => f.Code == "ROM_UNZIPPED" && f.Args.Contains("testrom")),
                "an extracted ROM folder must be reported as unzipped");
            Assert.False(findings.Any(f => f.Code == "ROM_MISSING"),
                "it must not be reported as plainly missing");
        }
        finally { Directory.Delete(tmp, true); }
    }
}

public static class PopperMediaTests
{
    public static void Test_Missing_Wheel_Media_Summarized()
    {
        var names = SqliteReader.TryReadTable(Fixtures.F("pup.db"), "Games", "GameName")!
            .Select(r => r.Length > 0 ? r[0] : null)
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Select(v => v!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        Assert.True(names.Count >= 2, "fixture must have at least 2 registered games");

        var tmp = Directory.CreateTempSubdirectory().FullName;
        try
        {
            var tables = Path.Combine(tmp, "Tables");
            var wheel = Path.Combine(tmp, "POPMedia", "Visual Pinball X", "Wheel");
            Directory.CreateDirectory(tables);
            Directory.CreateDirectory(wheel);
            // Give exactly ONE game a wheel image; the rest are missing.
            File.WriteAllText(Path.Combine(wheel, names[0] + ".png"), "png");

            var layout = new InstallLayout { RootPath = tmp };
            layout.TablesDir = tables;
            layout.PupDatabasePath = Fixtures.F("pup.db");
            layout.PopMediaDir = Path.Combine(tmp, "POPMedia");
            var ctx = new ScanContext { Layout = layout, Profile = Fixtures.Profile() };

            var findings = new CompletenessScanner().Scan(ctx).ToList();
            var f = findings.FirstOrDefault(x => x.Code == "POPPER_MEDIA_MISSING");
            Assert.NotNull(f);
            Assert.Equal((names.Count - 1).ToString(), f!.Args[0]);
            Assert.False(f.Args[2].Contains(names[0]), "the game with a wheel must not be listed as missing");
        }
        finally { Directory.Delete(tmp, true); }
    }

    public static void Test_No_Media_Finding_When_No_PopMedia()
    {
        // No POPMedia dir → the media check must stay silent.
        var tmp = Directory.CreateTempSubdirectory().FullName;
        try
        {
            var tables = Path.Combine(tmp, "Tables");
            Directory.CreateDirectory(tables);
            var layout = new InstallLayout { RootPath = tmp };
            layout.TablesDir = tables;
            layout.PupDatabasePath = Fixtures.F("pup.db");
            var ctx = new ScanContext { Layout = layout, Profile = Fixtures.Profile() };

            var findings = new CompletenessScanner().Scan(ctx).ToList();
            Assert.False(findings.Any(f => f.Code == "POPPER_MEDIA_MISSING"),
                "media check must not run without a POPMedia folder");
        }
        finally { Directory.Delete(tmp, true); }
    }
}

public static class BitnessReverseTests
{
    public static void Test_Reverse_Mismatch_32main_Only_64vpm()
    {
        var tmp = Directory.CreateTempSubdirectory().FullName;
        try
        {
            var vpm = Path.Combine(tmp, "VPinMAME");
            Directory.CreateDirectory(vpm);
            File.Copy(Fixtures.F("x86.exe"), Path.Combine(tmp, "VPinballX.exe"));   // 32-bit main
            File.Copy(Fixtures.F("x64.exe"), Path.Combine(vpm, "VPinMAME64.dll"));  // 64-bit COM server only

            var layout = new InstallLayout { RootPath = tmp };
            layout.VPinMameDir = vpm;
            var ctx = new ScanContext { Layout = layout, Profile = Fixtures.Profile() };

            var findings = new BitnessScanner().Scan(ctx).ToList();
            Assert.True(findings.Any(f => f.Code == "BITNESS_MISMATCH_VPM32" && f.Severity == Severity.Critical),
                "32-bit VPX + only 64-bit VPinMAME must be critical");
            Assert.False(findings.Any(f => f.Code == "BITNESS_MISMATCH_VPM"),
                "the forward-direction finding must not also fire");
        }
        finally { Directory.Delete(tmp, true); }
    }
}

/// <summary>
/// Health-score behaviour. The rework (FIELD-LOG 2026-07-30 / FD's 2090-table report that
/// scored 0/100·F with 0 criticals) guarantees: Info/Ok never move the score, warning volume
/// alone can never drop below grade B, and real breakage (criticals) still drives it down.
/// These build a <see cref="ScanReport"/> directly — hermetic, no fixtures needed.
/// </summary>
public static class ScoreTests
{
    private static ScanReport Report(int criticals, int warnings, int infos = 0, int oks = 0)
    {
        var report = new ScanReport { Layout = new InstallLayout { RootPath = "test" } };
        void Add(int n, Severity s)
        {
            for (var i = 0; i < n; i++)
                report.Findings.Add(new Finding
                {
                    Code = $"{s}_{i}", Severity = s, Category = "test",
                    Subject = $"Table {i}", EnglishText = "synthetic",
                });
        }
        Add(criticals, Severity.Critical);
        Add(warnings, Severity.Warning);
        Add(infos, Severity.Info);
        Add(oks, Severity.Ok);
        return report;
    }

    public static void Test_Score_Pristine_Is_APlus()
    {
        var r = Report(criticals: 0, warnings: 0);
        Assert.Equal(100, r.Score);
        Assert.Equal("A+", r.Grade);
    }

    public static void Test_Score_InfoAndOk_Are_Neutral()
    {
        // 2711 info + 1 ok was FD's exact tail — it must not cost a single point.
        var r = Report(criticals: 0, warnings: 0, infos: 2711, oks: 1);
        Assert.Equal(100, r.Score);
        Assert.Equal("A+", r.Grade);
    }

    public static void Test_Score_LargeHealthyCollection_NeverGradesF()
    {
        // The regression this whole session fixes: a big, clean library must not read as broken.
        var r = Report(criticals: 0, warnings: 500);
        Assert.True(r.Score >= 70, $"warning volume alone must stay >= grade B, got {r.Score}");
        Assert.False(r.Grade == "F", "warnings without a critical must never grade F");
    }

    public static void Test_Score_WarningPenalty_IsCapped()
    {
        // Beyond the cap, adding more warnings changes nothing — volume is not severity.
        Assert.Equal(Report(0, 71).Score, Report(0, 5000).Score);
        Assert.Equal("B", Report(0, 71).Grade);
    }

    public static void Test_Score_Criticals_StillDriveDown()
    {
        // Real breakage must still read badly — the fix must not neuter genuine criticals.
        Assert.True(Report(criticals: 5, warnings: 0).Score < 40, "5 criticals should be grade F territory");
        Assert.Equal("F", Report(criticals: 5, warnings: 0).Grade);
        Assert.True(Report(criticals: 1, warnings: 0).Score < 100, "one critical must cost points");
    }
}

/// <summary>
/// ROM validation false-positive guard (KPI#1). Hermetic — builds its own ScanContext, so it
/// never perturbs the shared install fixture. FIELD-LOG 2026-07-29/07-30: original/homebrew
/// tables with a B2S backglass (Guardians of the Galaxy, Harry Potter homebrew) were flagged
/// ROM_MISSING/critical because B2S.Server was mistaken for a VPinMAME ROM signal.
/// </summary>
public static class RomValidatorFpTests
{
    private static List<Finding> ScanOne(string script)
    {
        var tmp = Path.Combine(Path.GetTempPath(), "pt_rom_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tmp);
        try
        {
            var layout = new InstallLayout { RootPath = tmp, TablesDir = tmp, RomsDir = tmp };
            var ctx = new ScanContext { Layout = layout, Profile = Fixtures.Profile() };
            var vpxPath = Path.Combine(tmp, "Guardians of the Galaxy (Original 2023).vpx");
            ctx.Tables[vpxPath] = new VpxTableData { FilePath = vpxPath, Script = script };
            return new RomValidatorScanner().Scan(ctx).ToList();
        }
        finally { Directory.Delete(tmp, true); }
    }

    public static void Test_B2S_Original_Never_Reported_Missing()
    {
        var findings = ScanOne("Set B2S = CreateObject(\"B2S.Server\")\nB2S.GameName = \"gotg\"");
        Assert.False(findings.Any(f => f.Code == "ROM_MISSING"),
            "a B2S-only original must never be reported as a missing ROM (KPI#1 FP)");
        Assert.True(findings.Any(f => f.Code == "ROM_NOT_REQUIRED" && f.Severity == Severity.Ok),
            "it should read as no-ROM-required");
    }

    public static void Test_Real_Vpinmame_Table_Still_Reported_Missing()
    {
        // The fix must not neuter genuine missing-ROM detection.
        var findings = ScanOne("Const cGameName = \"mm_109c\"\nSet c = CreateObject(\"VPinMAME.Controller\")");
        Assert.True(findings.Any(f => f.Code == "ROM_MISSING" && f.Severity == Severity.Critical),
            "a real VPinMAME table with no ROM present must still be critical");
    }

    // ---------------------------------------------------------------------------------------
    // The KPI#1 guard above only covered `B2S.GameName = "..."` (the DirectGameName path, which
    // ScriptAnalyzer only reads when nothing else matched). The harder shape — a B2S-only table
    // that ALSO declares a first-class `Const cGameName` — was never asserted, and that is the
    // shape the entry checks below actually gate on. FIELD-LOG 2026-08-03.
    // ---------------------------------------------------------------------------------------

    /// <summary>
    /// B2S-only + a fully resolvable `Const cGameName`. This is the exact combination the
    /// original KPI#1 report described: the script names a game (so candidates resolve) but
    /// never creates a VPinMAME controller. It must read as "no ROM required", never critical.
    /// </summary>
    public static void Test_B2S_Only_With_Resolvable_ConstGameName_Is_Never_Critical()
    {
        var findings = ScanOne(
            "Const cGameName = \"gotg\"\nSet B2S = CreateObject(\"B2S.Server\")\nB2S.GameName = cGameName");

        Assert.False(findings.Any(f => f.Severity == Severity.Critical),
            "B2S-only + resolvable cGameName must never produce a critical (KPI#1)");
        Assert.False(findings.Any(f => f.Code == "ROM_MISSING"),
            "B2S-only + resolvable cGameName must never be reported as a missing ROM (KPI#1)");
        Assert.True(findings.Any(f => f.Code == "ROM_NOT_REQUIRED" && f.Severity == Severity.Ok),
            "it must read as no-ROM-required");
    }

    /// <summary>
    /// Same shape, but the named ROM happens to exist in the roms folder. Before the guard was
    /// tightened, a B2S-only original still entered ROM validation and came back out labelled
    /// ROM_OK — "ROM found" for a table that drives no ROM at all. Harmless in severity, wrong
    /// in substance, and it kept B2S wired into the ROM decision path.
    /// </summary>
    public static void Test_B2S_Only_Does_Not_Enter_Rom_Validation_Even_When_Rom_Exists()
    {
        var findings = ScanOneWithRom(
            "Const cGameName = \"afm_113b\"\nSet B2S = CreateObject(\"B2S.Server\")",
            romZipName: "afm_113b");

        Assert.True(findings.Any(f => f.Code == "ROM_NOT_REQUIRED"),
            "a B2S-only table must not be validated against the roms folder at all");
        Assert.False(findings.Any(f => f.Code == "ROM_OK"),
            "a name collision with a real ROM set must not relabel an original as a ROM table");
    }

    /// <summary>An unzipped ROM folder must still be caught — B2S presence must not mask it.</summary>
    public static void Test_Vpinmame_Table_With_B2S_Still_Reports_Unzipped_Rom()
    {
        var findings = ScanOneUnzipped(
            "Const cGameName = \"mm_109c\"\nSet c = CreateObject(\"VPinMAME.Controller\")\n" +
            "Set B2S = CreateObject(\"B2S.Server\")",
            romFolderName: "mm_109c");

        Assert.True(findings.Any(f => f.Code == "ROM_UNZIPPED"),
            "a real VPinMAME table that also opens B2S must still get the unzipped-ROM diagnosis");
    }

    /// <summary>
    /// Gregg's report, end to end (FB "Virtual Pinball and VPin Cab Builders", 2026-08-03): a
    /// genuine original, built from a ROM-table template whose VPinMAME plumbing was commented
    /// out rather than deleted, must not come back critical.
    /// </summary>
    public static void Test_Original_With_CommentedOut_Vpinmame_Boilerplate_Is_Not_Critical()
    {
        var findings = ScanOne("""
            Option Explicit
            Const cGameName = "myoriginal"
            ' Dim Controller
            ' Set Controller = CreateObject("VPinMAME.Controller")
            ' Controller.GameName = cGameName
            ' Controller.Run
            Set B2S = CreateObject("B2S.Server")
            """);

        Assert.False(findings.Any(f => f.Severity == Severity.Critical),
            "an original whose ROM boilerplate is commented out must never be critical");
        Assert.True(findings.Any(f => f.Code == "ROM_NOT_REQUIRED"),
            "it must read as no-ROM-required");
    }

    private static List<Finding> ScanOneWithRom(string script, string romZipName) =>
        ScanIn(script, dir =>
            File.WriteAllText(Path.Combine(dir, romZipName + ".zip"), "x"));

    private static List<Finding> ScanOneUnzipped(string script, string romFolderName) =>
        ScanIn(script, dir => Directory.CreateDirectory(Path.Combine(dir, romFolderName)));

    private static List<Finding> ScanIn(string script, Action<string> seedRoms)
    {
        var tmp = Path.Combine(Path.GetTempPath(), "pt_rom_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tmp);
        try
        {
            seedRoms(tmp);
            var layout = new InstallLayout { RootPath = tmp, TablesDir = tmp, RomsDir = tmp };
            var ctx = new ScanContext { Layout = layout, Profile = Fixtures.Profile() };
            foreach (var z in Directory.GetFiles(tmp, "*.zip"))
                ctx.RomSets.Add(Path.GetFileNameWithoutExtension(z));
            var vpxPath = Path.Combine(tmp, "Some Table (Original 2023).vpx");
            ctx.Tables[vpxPath] = new VpxTableData { FilePath = vpxPath, Script = script };
            return new RomValidatorScanner().Scan(ctx).ToList();
        }
        finally { Directory.Delete(tmp, true); }
    }
}

/// <summary>
/// Cross-drive roms resolution (FIELD-LOG 2026-07-30): FD's tables were on E: and VPinMAME on D:,
/// so no roms folder was found under the scanned root and ROM checks were skipped entirely. The
/// registry rompath is used as a fallback. Hermetic: the registry read no-ops on non-Windows, so
/// these drive the fallback through the injectable hint.
/// </summary>
public static class LayoutDetectorTests
{
    public static void Test_RomPath_On_Another_Drive_Is_Resolved_Via_Hint()
    {
        var root = Path.Combine(Path.GetTempPath(), "pt_root_" + Guid.NewGuid().ToString("N"));
        var otherDriveRoms = Path.Combine(Path.GetTempPath(), "pt_roms_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "Tables"));   // tables here, no roms under root
        Directory.CreateDirectory(otherDriveRoms);                 // roms "on another drive"
        try
        {
            var layout = LayoutDetector.Detect(root, Fixtures.Profile(), vpinmameRomPathHint: otherDriveRoms);
            Assert.Equal(otherDriveRoms, layout.RomsDir);
            // VPinMAME dir is derived as the parent of the registry roms path (VPinMAME\roms).
            Assert.Equal(Directory.GetParent(otherDriveRoms)!.FullName, layout.VPinMameDir);
        }
        finally
        {
            Directory.Delete(root, true);
            Directory.Delete(otherDriveRoms, true);
        }
    }

    public static void Test_Hint_Ignored_When_Not_An_Existing_Directory()
    {
        var root = Path.Combine(Path.GetTempPath(), "pt_root_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "Tables"));
        try
        {
            var layout = LayoutDetector.Detect(root, Fixtures.Profile(), vpinmameRomPathHint: @"Z:\does\not\exist");
            Assert.True(layout.RomsDir is null, "a non-existent hint must not be accepted as the roms folder");
        }
        finally { Directory.Delete(root, true); }
    }

    public static void Test_Registry_Read_Is_Null_Off_Windows()
    {
        // Documents the graceful-degradation contract the fallback relies on.
        if (!OperatingSystem.IsWindows())
            Assert.True(VpinmameRegistry.TryGetRomPath() is null, "must be null on non-Windows");
    }
}

public static class DiskSpaceTests
{
    public static void Test_Healthy_Free_Space_Is_Silent()
    {
        var f = DiskSpaceScanner.Evaluate("E:\\", 50L * 1024 * 1024 * 1024, "disk", DiskSpaceScanner.WarnThresholdBytes);
        Assert.True(f is null, "50 GB free must not warn");
    }

    public static void Test_Low_Free_Space_Warns()
    {
        var f = DiskSpaceScanner.Evaluate("E:\\", 1L * 1024 * 1024 * 1024, "disk", DiskSpaceScanner.WarnThresholdBytes);
        Assert.NotNull(f);
        Assert.Equal("LOW_DISK_SPACE", f!.Code);
        Assert.Equal(Severity.Warning, f.Severity);
        // Culture-robust: the value is formatted with the current culture ("1.0" en, "1,0" fr).
        Assert.Equal((1.0).ToString("0.0"), f.Args[1]);
    }
}

public static class LegacyTableTests
{
    public static void Test_Vpt_Files_Produce_Info_Finding()
    {
        var tmp = Path.Combine(Path.GetTempPath(), "pt_vpt_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tmp);
        try
        {
            File.WriteAllText(Path.Combine(tmp, "Old Table (VP9 2011).vpt"), "x");
            File.WriteAllText(Path.Combine(tmp, "Another Legacy.vpt"), "x");
            File.WriteAllText(Path.Combine(tmp, "Modern.vpx"), "x");
            var ctx = new ScanContext
            {
                Layout = new InstallLayout { RootPath = tmp, TablesDir = tmp },
                Profile = Fixtures.Profile(),
            };
            var findings = new LegacyTableScanner().Scan(ctx).ToList();
            Assert.Equal(1, findings.Count);
            Assert.Equal("VPT_LEGACY_PRESENT", findings[0].Code);
            Assert.Equal(Severity.Info, findings[0].Severity);
            Assert.Equal("2", findings[0].Args[0]);
        }
        finally { Directory.Delete(tmp, true); }
    }

    public static void Test_No_Vpt_Is_Silent()
    {
        var tmp = Path.Combine(Path.GetTempPath(), "pt_novpt_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tmp);
        try
        {
            File.WriteAllText(Path.Combine(tmp, "Modern.vpx"), "x");
            var ctx = new ScanContext
            {
                Layout = new InstallLayout { RootPath = tmp, TablesDir = tmp },
                Profile = Fixtures.Profile(),
            };
            Assert.Equal(0, new LegacyTableScanner().Scan(ctx).Count());
        }
        finally { Directory.Delete(tmp, true); }
    }
}

public static class PinupDisplayZombieTests
{
    public static void Test_Silent_When_Display_Not_Running()
    {
        var f = PinupDisplayZombieScanner.Evaluate(displayRunning: false, tableRunning: false, exePath: null, category: "process");
        Assert.True(f is null, "nothing to report when it isn't even running");
    }

    public static void Test_Silent_When_A_Table_Is_Actually_Active()
    {
        var f = PinupDisplayZombieScanner.Evaluate(displayRunning: true, tableRunning: true, exePath: @"C:\x\PinUpDisplay.exe", category: "process");
        Assert.True(f is null, "a table running means this is legitimate, not a zombie");
    }

    public static void Test_Warns_When_Left_Running_With_No_Table_Active()
    {
        var f = PinupDisplayZombieScanner.Evaluate(displayRunning: true, tableRunning: false, exePath: @"C:\x\PinUpDisplay.exe", category: "process");
        Assert.NotNull(f);
        Assert.Equal("PINUP_DISPLAY_ZOMBIE", f!.Code);
        Assert.Equal(Severity.Warning, f.Severity);
        Assert.Equal(@"C:\x\PinUpDisplay.exe", f.FilePath);
    }
}

public static class DisplaySetupTests
{
    public static void Test_Silent_Without_Multiscreen_Components()
    {
        var f = DisplaySetupScanner.Evaluate(connectedMonitors: 1, hasMultiScreenComponent: false, category: "display");
        Assert.True(f is null, "single screen is normal for a plain VPX-only install");
    }

    public static void Test_Silent_When_Monitor_Count_Cannot_Be_Measured()
    {
        var f = DisplaySetupScanner.Evaluate(connectedMonitors: null, hasMultiScreenComponent: true, category: "display");
        Assert.True(f is null, "unmeasurable must stay silent, never guess");
    }

    public static void Test_Silent_When_Enough_Screens_Are_Connected()
    {
        var f = DisplaySetupScanner.Evaluate(connectedMonitors: 3, hasMultiScreenComponent: true, category: "display");
        Assert.True(f is null, "3 screens for a b2s/DMD setup is exactly what's expected");
    }

    public static void Test_Flags_A_Single_Screen_With_Backglass_Or_Dmd_Present()
    {
        var f = DisplaySetupScanner.Evaluate(connectedMonitors: 1, hasMultiScreenComponent: true, category: "display");
        Assert.NotNull(f);
        Assert.Equal("DISPLAY_SETUP_INCOMPLETE", f!.Code);
        Assert.Equal(Severity.Info, f.Severity, "informative only — never a repair target (ADR-005)");
        Assert.Equal("1", f.Args[0]);
    }
}

public static class OrphanMediaMatcherTests
{
    private static readonly string[] Tables = { "Medieval Madness (Williams 1997)" };

    public static void Test_Exact_Match_Is_Not_Orphan()
        => Assert.True(!OrphanMediaMatcher.IsOrphan("Medieval Madness (Williams 1997)", Tables));

    public static void Test_Unrelated_Name_Is_Orphan()
        => Assert.True(OrphanMediaMatcher.IsOrphan("Some Removed Table", Tables));

    public static void Test_Default_Prefixed_Is_Never_Orphan()
        => Assert.True(!OrphanMediaMatcher.IsOrphan("default_wheel", Tables));

    /// <summary>
    /// Regression test for the community incident (FIELD-LOG 2026-07-29): the first version of a
    /// PowerShell cleanup script deleted per-screen loading videos of tables that WERE installed
    /// because it didn't recognise the "(SCREENx)" suffix as belonging to a known table.
    /// </summary>
    public static void Test_Screen_Suffixed_File_Of_An_Installed_Table_Is_Not_Orphan()
        => Assert.True(!OrphanMediaMatcher.IsOrphan("Medieval Madness (Williams 1997)01(SCREEN3)", Tables));

    public static void Test_Trailing_Numeric_Index_Does_Not_Defeat_A_Real_Match()
        => Assert.True(!OrphanMediaMatcher.IsOrphan("Medieval Madness (Williams 1997)02", Tables));

    public static void Test_Empty_Table_List_Never_Crashes()
        => Assert.True(OrphanMediaMatcher.IsOrphan("Anything", Array.Empty<string>()));
}

public static class OrphanedMediaScannerTests
{
    public static void Test_Reports_A_Count_Of_Orphaned_Files()
    {
        var tmp = Path.Combine(Path.GetTempPath(), "pt_media_" + Guid.NewGuid().ToString("N"));
        var wheel = Path.Combine(tmp, "Wheel");
        Directory.CreateDirectory(wheel);
        try
        {
            File.WriteAllText(Path.Combine(wheel, "Medieval Madness (Williams 1997).png"), "x");
            File.WriteAllText(Path.Combine(wheel, "Medieval Madness (Williams 1997)01(SCREEN3).png"), "x");
            File.WriteAllText(Path.Combine(wheel, "default.png"), "x");
            File.WriteAllText(Path.Combine(wheel, "RemovedTable.png"), "x");

            var layout = new InstallLayout { RootPath = tmp, PopMediaDir = tmp };
            layout.VpxTables.Add(Path.Combine(tmp, "..", "Tables", "Medieval Madness (Williams 1997).vpx"));
            var ctx = new ScanContext { Layout = layout, Profile = Fixtures.Profile() };

            var findings = new OrphanedMediaScanner().Scan(ctx).ToList();
            Assert.Equal(1, findings.Count);
            Assert.Equal("ORPHANED_MEDIA_FILE", findings[0].Code);
            Assert.Equal(Severity.Info, findings[0].Severity);
            Assert.Equal("1", findings[0].Args[0]);
        }
        finally { Directory.Delete(tmp, true); }
    }

    public static void Test_Silent_When_Nothing_Is_Orphaned()
    {
        var tmp = Path.Combine(Path.GetTempPath(), "pt_media_clean_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tmp);
        try
        {
            File.WriteAllText(Path.Combine(tmp, "default.png"), "x");
            var layout = new InstallLayout { RootPath = tmp, PopMediaDir = tmp };
            var ctx = new ScanContext { Layout = layout, Profile = Fixtures.Profile() };
            Assert.Equal(0, new OrphanedMediaScanner().Scan(ctx).Count());
        }
        finally { Directory.Delete(tmp, true); }
    }
}
