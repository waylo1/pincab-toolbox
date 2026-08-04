using PincabToolbox.Core.Vpx;

namespace PincabToolbox.Core.Services;

public enum DiffLineKind { Unchanged, Inserted, Deleted, Modified, Imaginary }

public sealed record DiffLine(DiffLineKind Kind, int? Number, string Text);

public sealed class ScriptDiffResult
{
    public required string OldLabel { get; init; }
    public required string NewLabel { get; init; }
    public List<DiffLine> OldLines { get; } = new();
    public List<DiffLine> NewLines { get; } = new();
    public int InsertedCount { get; set; }
    public int DeletedCount { get; set; }
    public int ModifiedCount { get; set; }
    public string? Error { get; set; }
}

/// <summary>
/// Diff-Master: extracts the scripts of two .vpx files (or reads .vbs/.txt directly)
/// and produces a side-by-side diff model for the UI. Delete+insert runs are paired
/// line-by-line as "Modified" for a readable view.
/// </summary>
public static class DiffService
{
    public static ScriptDiffResult DiffFiles(string oldPath, string newPath)
    {
        var result = new ScriptDiffResult
        {
            OldLabel = Path.GetFileName(oldPath),
            NewLabel = Path.GetFileName(newPath),
        };

        string? oldText = ReadScript(oldPath, out var err1);
        string? newText = ReadScript(newPath, out var err2);

        if (oldText is null || newText is null)
        {
            result.Error = err1 ?? err2 ?? "Could not read scripts.";
            return result;
        }

        var a = MyersDiff.SplitLines(oldText);
        var b = MyersDiff.SplitLines(newText);
        var chunks = MyersDiff.Diff(a, b);

        // Build side-by-side rows, pairing adjacent delete/insert chunks as "modified".
        int i = 0;
        while (i < chunks.Count)
        {
            var c = chunks[i];
            if (c.Op == DiffOp.Equal)
            {
                for (int k = 0; k < c.Length; k++)
                {
                    result.OldLines.Add(new DiffLine(DiffLineKind.Unchanged, c.OldIndex + k + 1, a[c.OldIndex + k]));
                    result.NewLines.Add(new DiffLine(DiffLineKind.Unchanged, c.NewIndex + k + 1, b[c.NewIndex + k]));
                }
                i++;
                continue;
            }

            DiffChunk? del = c.Op == DiffOp.Delete ? c : null;
            DiffChunk? ins = c.Op == DiffOp.Insert ? c : null;
            if (del is not null && i + 1 < chunks.Count && chunks[i + 1].Op == DiffOp.Insert) { ins = chunks[i + 1]; i++; }
            else if (ins is not null && i + 1 < chunks.Count && chunks[i + 1].Op == DiffOp.Delete) { del = chunks[i + 1]; i++; }
            i++;

            int delLen = del?.Length ?? 0;
            int insLen = ins?.Length ?? 0;
            int paired = Math.Min(delLen, insLen);

            for (int k = 0; k < Math.Max(delLen, insLen); k++)
            {
                if (k < delLen)
                {
                    var kind = k < paired ? DiffLineKind.Modified : DiffLineKind.Deleted;
                    result.OldLines.Add(new DiffLine(kind, del!.Value.OldIndex + k + 1, a[del.Value.OldIndex + k]));
                }
                else
                {
                    result.OldLines.Add(new DiffLine(DiffLineKind.Imaginary, null, ""));
                }

                if (k < insLen)
                {
                    var kind = k < paired ? DiffLineKind.Modified : DiffLineKind.Inserted;
                    result.NewLines.Add(new DiffLine(kind, ins!.Value.NewIndex + k + 1, b[ins.Value.NewIndex + k]));
                }
                else
                {
                    result.NewLines.Add(new DiffLine(DiffLineKind.Imaginary, null, ""));
                }
            }

            result.ModifiedCount += paired;
            result.DeletedCount += Math.Max(0, delLen - paired);
            result.InsertedCount += Math.Max(0, insLen - paired);
        }

        return result;
    }

    private static string? ReadScript(string path, out string? error)
    {
        error = null;
        var ext = Path.GetExtension(path).ToLowerInvariant();
        if (ext is ".vbs" or ".txt")
        {
            try { return File.ReadAllText(path); }
            catch (Exception ex) { error = ex.Message; return null; }
        }

        var table = VpxReader.Read(path);
        if (table.Script is null)
        {
            error = table.Error ?? $"No script found in {Path.GetFileName(path)}.";
            return null;
        }
        return table.Script;
    }
}
