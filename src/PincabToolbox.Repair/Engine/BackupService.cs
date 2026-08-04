namespace PincabToolbox.Repair;

/// <summary>
/// Targeted backup: only what is about to be touched, NEVER the whole install
/// (a full backup is a Repair feature, not its safety net).
/// Stored outside the installation — a broken install must not take its backup with it.
/// </summary>
public interface IBackupService
{
    /// <summary>Backs up an item's targets and returns the backup folder path.</summary>
    string Backup(string planId, RepairPlanItem item);

    ExecutionResult Restore(string planId, string itemId);

    /// <summary>
    /// Prunes old plans. NEVER removes the most recent plan, nor one flagged RecoveryRequired.
    /// </summary>
    void Prune(int keepLastPlans = 10);
}

/// <summary>
/// File-based backup. Copies each existing target into
/// &lt;root&gt;/&lt;planId&gt;/&lt;itemId&gt;/ alongside a manifest.
/// </summary>
public sealed class FileBackupService : IBackupService
{
    private readonly IFileSystem _fs;
    private readonly string _root;
    private readonly HashSet<string> _protectedPlans = new(StringComparer.Ordinal);

    public FileBackupService(IFileSystem fs, string root)
    {
        _fs = fs;
        _root = root;
    }

    /// <summary>Marks a plan as never-prunable (used when recovery is required).</summary>
    public void Protect(string planId) => _protectedPlans.Add(planId);

    public string Backup(string planId, RepairPlanItem item)
    {
        var dir = Combine(_root, planId, item.ItemId);
        _fs.CreateDirectory(dir);

        var manifest = new List<string>();
        var index = 0;
        foreach (var c in item.Changes)
        {
            index++;
            if (c.Kind == ChangeKind.FileAttribute || c.Kind == ChangeKind.FileMove)
            {
                if (_fs.FileExists(c.Target))
                {
                    var dest = Combine(dir, $"{index:D3}_{SafeName(c.Target)}");
                    _fs.WriteAllBytes(dest, _fs.ReadAllBytes(c.Target));
                    manifest.Add($"{index:D3}\tfile\t{c.Target}");
                }
                else
                {
                    // Absent target is legitimate (e.g. a file about to be created).
                    manifest.Add($"{index:D3}\tabsent\t{c.Target}");
                }
            }
            else
            {
                // Registry / ini / sqlite: the Before value in the journal is the restore data.
                manifest.Add($"{index:D3}\tvalue\t{c.Target}\t{c.Before}");
            }
        }

        _fs.WriteAllBytes(Combine(dir, "manifest.tsv"),
            System.Text.Encoding.UTF8.GetBytes(string.Join("\n", manifest)));

        return dir;
    }

    public ExecutionResult Restore(string planId, string itemId)
    {
        var dir = Combine(_root, planId, itemId);
        var manifestPath = Combine(dir, "manifest.tsv");
        if (!_fs.FileExists(manifestPath))
            return ExecutionResult.Fail($"no backup found at {dir}");

        var lines = System.Text.Encoding.UTF8.GetString(_fs.ReadAllBytes(manifestPath))
                        .Split('\n', StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            var parts = line.Split('\t');
            if (parts.Length < 3) continue;
            var (idx, kind, target) = (parts[0], parts[1], parts[2]);
            if (kind != "file") continue;

            var stored = Combine(dir, $"{idx}_{SafeName(target)}");
            if (!_fs.FileExists(stored))
                return ExecutionResult.Fail($"backup file missing: {stored}");
            _fs.WriteAllBytes(target, _fs.ReadAllBytes(stored));
        }
        return ExecutionResult.Ok;
    }

    public void Prune(int keepLastPlans = 10)
    {
        if (!_fs.DirectoryExists(_root)) return;

        var plans = _fs.GetDirectories(_root).OrderBy(p => p, StringComparer.Ordinal).ToList();
        var excess = plans.Count - keepLastPlans;
        if (excess <= 0) return;

        foreach (var plan in plans.Take(excess))
        {
            var name = LastSegment(plan);
            if (_protectedPlans.Contains(name)) continue;   // never drop a recovery case
            if (plan == plans[^1]) continue;                 // never drop the most recent
            DeleteRecursive(plan);
        }
    }

    private void DeleteRecursive(string dir)
    {
        foreach (var f in _fs.GetFiles(dir)) _fs.DeleteFile(f);
        foreach (var d in _fs.GetDirectories(dir)) DeleteRecursive(d);
    }

    /// <summary>
    /// Joins and NORMALISES. The result is shown to the user on the recovery screen —
    /// a path containing ".." is the last thing someone needs when their install is broken.
    /// </summary>
    private static string Combine(params string[] parts)
    {
        var joined = string.Join("/", parts.Where(p => !string.IsNullOrEmpty(p))).Replace("\\", "/");
        while (joined.Contains("//")) joined = joined.Replace("//", "/");

        var stack = new List<string>();
        foreach (var seg in joined.Split('/'))
        {
            if (seg == ".") continue;
            if (seg == ".." && stack.Count > 0 && stack[^1] != "..") { stack.RemoveAt(stack.Count - 1); continue; }
            stack.Add(seg);
        }
        return string.Join("/", stack);
    }

    private static string LastSegment(string path)
    {
        var i = path.LastIndexOfAny(new[] { '/', '\\' });
        return i < 0 ? path : path[(i + 1)..];
    }

    private static string SafeName(string target)
    {
        var name = LastSegment(target);
        foreach (var c in System.IO.Path.GetInvalidFileNameChars()) name = name.Replace(c, '_');
        return name;
    }
}
