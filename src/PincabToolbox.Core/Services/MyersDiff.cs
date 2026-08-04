namespace PincabToolbox.Core.Services;

public enum DiffOp { Equal, Delete, Insert }

public readonly record struct DiffChunk(DiffOp Op, int OldIndex, int NewIndex, int Length);

/// <summary>
/// Dependency-free line diff — Myers O(ND) greedy algorithm with linear-space refinement
/// skipped in favour of a common-prefix/suffix trim + histogram fallback for huge inputs.
/// Produces ordered chunks over line indexes; UI layers build side-by-side views from them.
/// </summary>
public static class MyersDiff
{
    public static List<DiffChunk> DiffLines(string oldText, string newText)
    {
        var a = SplitLines(oldText);
        var b = SplitLines(newText);
        return Diff(a, b);
    }

    public static string[] SplitLines(string text) =>
        text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');

    public static List<DiffChunk> Diff(string[] a, string[] b)
    {
        // trim common prefix/suffix — typical table edits touch a tiny fraction of lines
        int prefix = 0;
        while (prefix < a.Length && prefix < b.Length && a[prefix] == b[prefix]) prefix++;
        int suffix = 0;
        while (suffix < a.Length - prefix && suffix < b.Length - prefix &&
               a[a.Length - 1 - suffix] == b[b.Length - 1 - suffix]) suffix++;

        var core = MyersCore(
            a, prefix, a.Length - prefix - suffix,
            b, prefix, b.Length - prefix - suffix);

        var chunks = new List<DiffChunk>();
        if (prefix > 0) chunks.Add(new DiffChunk(DiffOp.Equal, 0, 0, prefix));
        chunks.AddRange(core);
        if (suffix > 0) chunks.Add(new DiffChunk(DiffOp.Equal, a.Length - suffix, b.Length - suffix, suffix));
        return Coalesce(chunks);
    }

    private static List<DiffChunk> MyersCore(string[] a, int aStart, int n, string[] b, int bStart, int m)
    {
        var result = new List<DiffChunk>();
        if (n == 0 && m == 0) return result;
        if (n == 0) { result.Add(new DiffChunk(DiffOp.Insert, aStart, bStart, m)); return result; }
        if (m == 0) { result.Add(new DiffChunk(DiffOp.Delete, aStart, bStart, n)); return result; }

        int max = n + m;
        // Guard: for pathological sizes fall back to whole-block replace (keeps UI honest & fast)
        if ((long)n * m > 40_000_000)
        {
            result.Add(new DiffChunk(DiffOp.Delete, aStart, bStart, n));
            result.Add(new DiffChunk(DiffOp.Insert, aStart + n, bStart, m));
            return result;
        }

        var v = new int[2 * max + 1];
        var trace = new List<int[]>();
        int offset = max;
        bool found = false;

        for (int d = 0; d <= max && !found; d++)
        {
            trace.Add((int[])v.Clone());
            for (int k = -d; k <= d; k += 2)
            {
                int x = k == -d || k != d && v[offset + k - 1] < v[offset + k + 1]
                    ? v[offset + k + 1]
                    : v[offset + k - 1] + 1;
                int y = x - k;
                while (x < n && y < m && a[aStart + x] == b[bStart + y]) { x++; y++; }
                v[offset + k] = x;
                if (x >= n && y >= m) { found = true; break; }
            }
        }

        // backtrack
        var ops = new List<(DiffOp op, int ai, int bi)>();
        {
            int x = n, y = m;
            for (int d = trace.Count - 1; d > 0; d--)
            {
                var vPrev = trace[d];
                int k = x - y;
                int prevK = k == -d || k != d && vPrev[offset + k - 1] < vPrev[offset + k + 1]
                    ? k + 1
                    : k - 1;
                int prevX = vPrev[offset + prevK];
                int prevY = prevX - prevK;
                while (x > prevX && y > prevY) { ops.Add((DiffOp.Equal, --x, --y)); }
                if (d > 0)
                {
                    if (x == prevX) ops.Add((DiffOp.Insert, x, --y));
                    else ops.Add((DiffOp.Delete, --x, y));
                }
            }
            while (x > 0 && y > 0) { ops.Add((DiffOp.Equal, --x, --y)); }
            while (x > 0) ops.Add((DiffOp.Delete, --x, y));
            while (y > 0) ops.Add((DiffOp.Insert, x, --y));
        }
        ops.Reverse();

        // fold single ops into chunks
        foreach (var (op, ai, bi) in ops)
        {
            if (result.Count > 0)
            {
                var last = result[^1];
                bool contiguous = op == last.Op && op switch
                {
                    DiffOp.Equal => ai == last.OldIndex - aStart + last.Length && bi == last.NewIndex - bStart + last.Length,
                    DiffOp.Delete => ai == last.OldIndex - aStart + last.Length,
                    _ => bi == last.NewIndex - bStart + last.Length,
                };
                if (contiguous)
                {
                    result[^1] = last with { Length = last.Length + 1 };
                    continue;
                }
            }
            result.Add(new DiffChunk(op, aStart + ai, bStart + bi, 1));
        }
        return result;
    }

    private static List<DiffChunk> Coalesce(List<DiffChunk> chunks)
    {
        var result = new List<DiffChunk>();
        foreach (var c in chunks)
        {
            if (c.Length == 0) continue;
            if (result.Count > 0)
            {
                var last = result[^1];
                if (last.Op == c.Op &&
                    last.OldIndex + (c.Op != DiffOp.Insert ? last.Length : 0) == c.OldIndex &&
                    last.NewIndex + (c.Op != DiffOp.Delete ? last.Length : 0) == c.NewIndex)
                {
                    result[^1] = last with { Length = last.Length + c.Length };
                    continue;
                }
            }
            result.Add(c);
        }
        return result;
    }
}
