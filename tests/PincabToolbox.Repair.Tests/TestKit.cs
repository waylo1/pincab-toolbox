using PincabToolbox.Core.Models;
using PincabToolbox.Repair;

namespace PincabToolbox.Repair.Tests;

/// <summary>Minimal assertions — same spirit as the Core test project.</summary>
public static class A
{
    public static void True(bool c, string msg) { if (!c) throw new Exception(msg); }
    public static void False(bool c, string msg) { if (c) throw new Exception(msg); }

    public static void Equal<T>(T expected, T actual, string msg)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new Exception($"{msg} — expected <{expected}>, got <{actual}>");
    }

    public static void Sequence<T>(IEnumerable<T> expected, IEnumerable<T> actual, string msg)
    {
        var e = expected.ToList(); var a = actual.ToList();
        if (!e.SequenceEqual(a))
            throw new Exception($"{msg} — expected [{string.Join(", ", e)}], got [{string.Join(", ", a)}]");
    }
}

// ───────────────────────────── fakes ─────────────────────────────

public sealed class FakeClock : ISystemClock
{
    public DateTimeOffset UtcNow { get; set; } = new(2026, 7, 25, 12, 0, 0, TimeSpan.Zero);
}

public sealed class FakeProbe : IEnvironmentProbe
{
    public List<string> Blocking { get; } = new();
    public long FreeSpace { get; set; } = 10L * 1024 * 1024 * 1024;
    public HashSet<string> ReadOnly { get; } = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<string> RunningBlockingProcesses() => Blocking;
    public long FreeBackupSpaceBytes() => FreeSpace;
    public bool CanWriteTo(string target) => !ReadOnly.Contains(target);
}

/// <summary>In-memory file system. Directories are implicit from file paths.</summary>
public sealed class FakeFs : IFileSystem
{
    public Dictionary<string, byte[]> Files { get; } = new(StringComparer.OrdinalIgnoreCase);
    public HashSet<string> Dirs { get; } = new(StringComparer.OrdinalIgnoreCase);
    public HashSet<string> Blocked { get; } = new(StringComparer.OrdinalIgnoreCase);

    public HashSet<string> FailWriteOn { get; } = new(StringComparer.OrdinalIgnoreCase);
    public HashSet<string> FailRevertOn { get; } = new(StringComparer.OrdinalIgnoreCase);

    public void AddFile(string path, string content = "x")
    {
        Files[path] = System.Text.Encoding.UTF8.GetBytes(content);
        var d = Parent(path); if (d is not null) Dirs.Add(d);
    }

    public void AddDir(string path) => Dirs.Add(path);

    public bool FileExists(string p) => Files.ContainsKey(p);
    public bool DirectoryExists(string p) => Dirs.Contains(p);

    public IReadOnlyList<string> GetFiles(string dir)
        => Files.Keys.Where(k => string.Equals(Parent(k), dir, StringComparison.OrdinalIgnoreCase))
                     .OrderBy(k => k, StringComparer.Ordinal).ToList();

    public IReadOnlyList<string> GetDirectories(string dir)
        => Dirs.Where(d => string.Equals(Parent(d), dir, StringComparison.OrdinalIgnoreCase))
               .OrderBy(d => d, StringComparer.Ordinal).ToList();

    public byte[] ReadAllBytes(string p) => Files.TryGetValue(p, out var b) ? b : throw new FileNotFoundException(p);

    public void WriteAllBytes(string p, byte[] c)
    {
        if (FailWriteOn.Contains(p)) throw new IOException($"write refused: {p}");
        Files[p] = c;
        var d = Parent(p); if (d is not null) Dirs.Add(d);
    }

    public void DeleteFile(string p)
    {
        if (FailRevertOn.Contains(p)) throw new IOException($"cannot undo: {p}");
        Files.Remove(p);
    }

    public void MoveFile(string s, string d)
    {
        if (FailWriteOn.Contains(d)) throw new IOException($"write refused: {d}");
        Files[d] = Files[s]; Files.Remove(s);
    }

    public void MoveDirectory(string s, string d)
    {
        if (FailWriteOn.Contains(d)) throw new IOException($"write refused: {d}");
        if (FailRevertOn.Contains(d)) throw new IOException($"cannot undo: {d}");
        foreach (var f in Files.Keys.Where(k => k.StartsWith(s + "/", StringComparison.OrdinalIgnoreCase)).ToList())
        { Files[d + f[s.Length..]] = Files[f]; Files.Remove(f); }
        Dirs.Remove(s); Dirs.Add(d);
    }

    public void CreateDirectory(string p) => Dirs.Add(p);

    public bool HasZoneIdentifier(string p) => Blocked.Contains(p);

    public void RemoveZoneIdentifier(string p)
    {
        if (FailWriteOn.Contains(p)) throw new IOException($"write refused: {p}");
        Blocked.Remove(p);
    }

    public void AddZoneIdentifier(string p)
    {
        if (FailRevertOn.Contains(p)) throw new IOException($"cannot undo: {p}");
        Blocked.Add(p);
    }

    private static string? Parent(string path)
    {
        var i = path.LastIndexOfAny(new[] { '/', '\\' });
        return i <= 0 ? null : path[..i];
    }
}

public sealed class FakeProcessControl : IProcessControl
{
    public HashSet<string> Running { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, string> Paths { get; } = new(StringComparer.OrdinalIgnoreCase);
    public HashSet<string> RefuseKill { get; } = new(StringComparer.OrdinalIgnoreCase);
    public List<string> KillCalls { get; } = new();

    public bool IsRunning(string processName) => Running.Contains(processName);
    public string? PathOf(string processName) => Paths.TryGetValue(processName, out var p) ? p : null;

    public bool Kill(string processName)
    {
        KillCalls.Add(processName);
        if (RefuseKill.Contains(processName)) return false;
        Running.Remove(processName);
        return true;
    }
}

public sealed class FakeAudioDeviceControl : IAudioDeviceControl
{
    public string? DefaultId { get; set; }
    public Dictionary<string, string> DevicesByName { get; } = new(StringComparer.OrdinalIgnoreCase);
    public bool RefuseSet { get; set; }
    public List<string> SetCalls { get; } = new();

    public string? GetDefaultPlaybackDeviceId() => DefaultId;

    public string? FindPlaybackDeviceId(string nameContains)
        => DevicesByName.Where(kv => kv.Key.Contains(nameContains, StringComparison.OrdinalIgnoreCase))
                        .Select(kv => kv.Value).FirstOrDefault();

    public bool SetDefaultPlaybackDevice(string deviceId)
    {
        SetCalls.Add(deviceId);
        if (RefuseSet) return false;
        DefaultId = deviceId;
        return true;
    }
}

/// <summary>Backup that keeps a snapshot in memory — enough to assert ordering and restore.</summary>
public sealed class FakeBackup : IBackupService
{
    public List<string> Calls { get; } = new();
    public int PruneKeep { get; private set; } = -1;

    public string Backup(string planId, RepairPlanItem item)
    {
        Calls.Add($"{planId}/{item.ItemId}");
        return $"/backups/{planId}/{item.ItemId}";
    }

    public ExecutionResult Restore(string planId, string itemId) => ExecutionResult.Ok;
    public void Prune(int keepLastPlans = 10) => PruneKeep = keepLastPlans;
}

/// <summary>Scriptable action, used to drive failure paths the real actions cannot easily reach.</summary>
public sealed class ScriptedAction : IRepairAction
{
    private readonly FakeFs _fs;
    public ScriptedAction(FakeFs fs, string id = "scripted") { _fs = fs; ActionId = id; }

    public string ActionId { get; }
    public ChangeKind Kind { get; set; } = ChangeKind.FileAttribute;
    public bool IsReversibleByNature { get; set; } = true;
    public bool StillAppliesResult { get; set; } = true;
    public int PlanCalls { get; private set; }

    public ValidationResult ValidateParameters(IReadOnlyDictionary<string, string> p)
        => p.ContainsKey("bad") ? ValidationResult.Fail("bad parameter") : ValidationResult.Ok;

    public IReadOnlyList<PlannedChange> Plan(RepairContext ctx, IReadOnlyDictionary<string, string> p)
    {
        PlanCalls++;
        var t = ctx.Finding.FilePath!;
        return new[]
        {
            new PlannedChange
            {
                ActionId = ActionId, Kind = Kind, Target = t,
                Before = _fs.Files.TryGetValue(t, out var b) ? System.Text.Encoding.UTF8.GetString(b) : "<absent>",
                After = "fixed", Reversible = IsReversibleByNature,
            }
        };
    }

    public bool StillApplies(RepairContext ctx) => StillAppliesResult;

    public ExecutionResult Execute(PlannedChange c)
    {
        try { _fs.WriteAllBytes(c.Target, System.Text.Encoding.UTF8.GetBytes(c.After)); return ExecutionResult.Ok; }
        catch (Exception e) { return ExecutionResult.Fail(e.Message); }
    }

    public ExecutionResult Revert(PlannedChange c)
    {
        if (_fs.FailRevertOn.Contains(c.Target)) return ExecutionResult.Fail($"cannot undo: {c.Target}");
        _fs.Files[c.Target] = System.Text.Encoding.UTF8.GetBytes(c.Before);
        return ExecutionResult.Ok;
    }
}

// ───────────────────────────── builders ─────────────────────────────

public static class Build
{
    public static readonly string[] Roots = { @"C:\vpx", @"C:\popper" };

    public static Finding Finding(string code, string path, string category = "bitness") => new()
    {
        Code = code,
        Severity = Severity.Critical,
        Category = category,
        Subject = path,
        FilePath = path,
        EnglishText = code,
    };

    public static RepairRule Rule(string id, string code, string actionId,
                                  int confidence = 98, bool reversible = true) => new()
    {
        Id = id, TargetCode = code, ActionId = actionId,
        RepairConfidence = confidence, Reversible = reversible,
    };

    public static RepairPlan Select(RepairPlan p)
        => p with { Items = p.Items.Select(i => i with { Selected = true }).ToList() };
}
