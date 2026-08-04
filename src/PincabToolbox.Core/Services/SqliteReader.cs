using System.Text;
using System.Text.RegularExpressions;

namespace PincabToolbox.Core.Services;

/// <summary>
/// Minimal, dependency-free, strictly read-only SQLite database file reader.
/// Understands just enough of the on-disk format (b-tree table pages, record
/// serial types, overflow pages) to full-scan one table — which is all the
/// scanner needs for PUPDatabase.db. Never opens the file for writing.
/// Note: a pending -wal file is ignored; data read may be slightly stale, which
/// is acceptable for a diagnostic scan.
/// </summary>
public static partial class SqliteReader
{
    /// <summary>Reads the requested columns of every row of a table. Returns null when the file/table is unreadable.</summary>
    public static List<string?[]>? TryReadTable(string dbPath, string tableName, params string[] columns)
    {
        try
        {
            var db = File.ReadAllBytes(dbPath);
            return ReadTable(db, tableName, columns);
        }
        catch
        {
            return null;
        }
    }

    internal static List<string?[]> ReadTable(byte[] db, string tableName, string[] columns)
    {
        if (db.Length < 100 || Encoding.ASCII.GetString(db, 0, 16) != "SQLite format 3\0")
            throw new InvalidDataException("Not a SQLite 3 database.");

        int pageSize = db[16] << 8 | db[17];
        if (pageSize == 1) pageSize = 65536;
        int reserved = db[20];
        int usable = pageSize - reserved;

        // ---- find the table's root page + CREATE statement in sqlite_schema (root = page 1) ----
        int rootPage = -1;
        string? createSql = null;
        foreach (var record in WalkTable(db, pageSize, usable, 1))
        {
            // sqlite_schema: type, name, tbl_name, rootpage, sql
            if (record.Length >= 5 &&
                record[0].AsText() == "table" &&
                string.Equals(record[1].AsText(), tableName, StringComparison.OrdinalIgnoreCase))
            {
                rootPage = (int)(record[3].AsInteger() ?? -1);
                createSql = record[4].AsText();
                break;
            }
        }
        if (rootPage <= 0 || createSql is null)
            throw new InvalidDataException($"Table '{tableName}' not found.");

        var columnOrder = ParseColumns(createSql);
        var wanted = columns
            .Select(c => columnOrder.FindIndex(n => string.Equals(n, c, StringComparison.OrdinalIgnoreCase)))
            .ToArray();

        // INTEGER PRIMARY KEY columns are stored as null in the record; the rowid carries the value.
        // For our use (text columns) reading null there is fine.
        var rows = new List<string?[]>();
        foreach (var record in WalkTable(db, pageSize, usable, rootPage))
        {
            var row = new string?[wanted.Length];
            for (int i = 0; i < wanted.Length; i++)
                row[i] = wanted[i] >= 0 && wanted[i] < record.Length ? record[wanted[i]].AsText() : null;
            rows.Add(row);
        }
        return rows;
    }

    // ---------- b-tree walking ----------

    private static IEnumerable<Value[]> WalkTable(byte[] db, int pageSize, int usable, int page)
    {
        var stack = new Stack<int>();
        stack.Push(page);
        var visited = new HashSet<int>();

        while (stack.Count > 0)
        {
            int p = stack.Pop();
            if (!visited.Add(p) || p <= 0) continue;
            long baseOff = (long)(p - 1) * pageSize;
            if (baseOff + pageSize > db.Length) continue;
            int hdr = (int)baseOff + (p == 1 ? 100 : 0);

            byte type = db[hdr];
            int cellCount = db[hdr + 3] << 8 | db[hdr + 4];
            int headerSize = type is 0x05 or 0x02 ? 12 : 8;
            int cellPtrArray = hdr + headerSize;

            if (type == 0x05) // interior table page
            {
                for (int i = 0; i < cellCount; i++)
                {
                    int cellOff = (int)baseOff + (db[cellPtrArray + i * 2] << 8 | db[cellPtrArray + i * 2 + 1]);
                    int child = ReadBE32(db, cellOff);
                    stack.Push(child);
                }
                stack.Push(ReadBE32(db, hdr + 8)); // right-most pointer
            }
            else if (type == 0x0D) // leaf table page
            {
                for (int i = 0; i < cellCount; i++)
                {
                    int cellOff = (int)baseOff + (db[cellPtrArray + i * 2] << 8 | db[cellPtrArray + i * 2 + 1]);
                    var payload = ReadCellPayload(db, pageSize, usable, cellOff);
                    if (payload is not null)
                        yield return ParseRecord(payload);
                }
            }
            // other page types (index/overflow/freelist) are irrelevant here
        }
    }

    private static byte[]? ReadCellPayload(byte[] db, int pageSize, int usable, int off)
    {
        long payloadLen = ReadVarint(db, ref off);
        _ = ReadVarint(db, ref off); // rowid

        int maxLocal = usable - 35;
        if (payloadLen <= maxLocal)
        {
            if (off + payloadLen > db.Length) return null;
            return db.AsSpan(off, (int)payloadLen).ToArray();
        }

        // overflow
        int minLocal = (usable - 12) * 32 / 255 - 23;
        int local = minLocal + (int)((payloadLen - minLocal) % (usable - 4));
        if (local > maxLocal) local = minLocal;

        var result = new byte[payloadLen];
        if (off + local > db.Length) return null;
        Array.Copy(db, off, result, 0, local);
        long written = local;
        int overflowPage = ReadBE32(db, off + local);
        var guard = new HashSet<int>();
        while (overflowPage > 0 && written < payloadLen && guard.Add(overflowPage))
        {
            long pOff = (long)(overflowPage - 1) * pageSize;
            if (pOff + pageSize > db.Length) return null;
            overflowPage = ReadBE32(db, (int)pOff);
            int take = (int)Math.Min(usable - 4, payloadLen - written);
            Array.Copy(db, pOff + 4, result, written, take);
            written += take;
        }
        return written == payloadLen ? result : null;
    }

    // ---------- record decoding ----------

    private readonly record struct Value(long? Int, double? Real, string? Text)
    {
        public string? AsText() => Text ?? Int?.ToString() ?? Real?.ToString();
        public long? AsInteger() => Int;
    }

    private static Value[] ParseRecord(byte[] payload)
    {
        int pos = 0;
        long headerSize = ReadVarint(payload, ref pos);
        var serialTypes = new List<long>();
        while (pos < headerSize)
            serialTypes.Add(ReadVarint(payload, ref pos));

        var values = new Value[serialTypes.Count];
        int body = (int)headerSize;
        for (int i = 0; i < serialTypes.Count; i++)
        {
            long st = serialTypes[i];
            switch (st)
            {
                case 0: values[i] = default; break;
                case >= 1 and <= 6:
                    int len = st switch { 1 => 1, 2 => 2, 3 => 3, 4 => 4, 5 => 6, _ => 8 };
                    long v = 0;
                    for (int b = 0; b < len; b++) v = v << 8 | payload[body + b];
                    // sign-extend
                    int bits = len * 8;
                    if (bits < 64 && (v & 1L << bits - 1) != 0) v -= 1L << bits;
                    values[i] = new Value(v, null, null);
                    body += len;
                    break;
                case 7:
                    long raw = 0;
                    for (int b = 0; b < 8; b++) raw = raw << 8 | payload[body + b];
                    values[i] = new Value(null, BitConverter.Int64BitsToDouble(raw), null);
                    body += 8;
                    break;
                case 8: values[i] = new Value(0, null, null); break;
                case 9: values[i] = new Value(1, null, null); break;
                case >= 12 when st % 2 == 0: // blob
                    int blobLen = (int)((st - 12) / 2);
                    values[i] = default;
                    body += blobLen;
                    break;
                case >= 13: // text
                    int textLen = (int)((st - 13) / 2);
                    values[i] = new Value(null, null, Encoding.UTF8.GetString(payload, body, textLen));
                    body += textLen;
                    break;
                default:
                    values[i] = default;
                    break;
            }
        }
        return values;
    }

    // ---------- helpers ----------

    private static long ReadVarint(byte[] data, ref int pos)
    {
        long result = 0;
        for (int i = 0; i < 8; i++)
        {
            byte b = data[pos++];
            result = result << 7 | (long)(b & 0x7F);
            if ((b & 0x80) == 0) return result;
        }
        result = result << 8 | data[pos++];
        return result;
    }

    private static int ReadBE32(byte[] d, int off) => d[off] << 24 | d[off + 1] << 16 | d[off + 2] << 8 | d[off + 3];

    [GeneratedRegex(@"[\[\]`""']")]
    private static partial Regex QuoteChars();

    /// <summary>Extracts ordered column names from a CREATE TABLE statement (defensive, handles quoting/constraints).</summary>
    internal static List<string> ParseColumns(string createSql)
    {
        var cols = new List<string>();
        int open = createSql.IndexOf('(');
        if (open < 0) return cols;
        int depth = 0, start = open + 1;
        var parts = new List<string>();
        for (int i = open; i < createSql.Length; i++)
        {
            char c = createSql[i];
            if (c == '(') depth++;
            else if (c == ')')
            {
                depth--;
                if (depth == 0) { parts.Add(createSql[start..i]); break; }
            }
            else if (c == ',' && depth == 1)
            {
                parts.Add(createSql[start..i]);
                start = i + 1;
            }
        }
        foreach (var partRaw in parts)
        {
            var part = partRaw.Trim();
            if (part.Length == 0) continue;
            var firstToken = part.Split(' ', '\t', '\r', '\n')[0];
            var cleaned = QuoteChars().Replace(firstToken, "");
            int paren = cleaned.IndexOf('(');
            if (paren >= 0) cleaned = cleaned[..paren];
            // table-level constraints are not columns
            if (cleaned.Length == 0) continue;
            var upper = cleaned.ToUpperInvariant();
            if (upper is "PRIMARY" or "UNIQUE" or "CHECK" or "FOREIGN" or "CONSTRAINT") continue;
            cols.Add(cleaned);
        }
        return cols;
    }
}
