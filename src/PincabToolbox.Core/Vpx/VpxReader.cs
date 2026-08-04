using System.Text;

namespace PincabToolbox.Core.Vpx;

/// <summary>Metadata extracted from a .vpx table file.</summary>
public sealed class VpxTableData
{
    public required string FilePath { get; init; }
    public string? Script { get; set; }
    public string? TableName { get; set; }
    public string? TableVersion { get; set; }
    public string? AuthorName { get; set; }
    public string? Error { get; set; }
}

/// <summary>
/// Reads Visual Pinball X (.vpx) files.
/// A .vpx is an OLE Compound File: storage "GameStg" holds the BIFF stream "GameData"
/// (whose special CODE record embeds the VBScript), and storage "TableInfo" holds
/// metadata streams (TableName, TableVersion, AuthorName…).
/// </summary>
public static class VpxReader
{
    public static VpxTableData Read(string path)
    {
        var data = new VpxTableData { FilePath = path };
        try
        {
            var cf = CompoundFileReader.Open(path);

            // --- script ---
            var gameStg = cf.FindStorage(cf.Root, "GameStg");
            var gameData = gameStg is null ? null : cf.FindStream(gameStg, "GameData");
            if (gameData is not null)
                data.Script = ExtractScript(cf.ReadStream(gameData));

            // --- table info ---
            var info = cf.FindStorage(cf.Root, "TableInfo");
            if (info is not null)
            {
                data.TableName = ReadInfoString(cf, info, "TableName");
                data.TableVersion = ReadInfoString(cf, info, "TableVersion");
                data.AuthorName = ReadInfoString(cf, info, "AuthorName");
            }
        }
        catch (Exception ex)
        {
            data.Error = ex.Message;
        }
        return data;
    }

    /// <summary>
    /// Walks the BIFF records of GameData and extracts the script from the CODE record.
    /// Record layout: [int32 size][4-char tag][payload size-4 bytes].
    /// CODE is special: size==4 (tag only), followed by [int32 scriptLen][script bytes].
    /// </summary>
    internal static string? ExtractScript(byte[] biff)
    {
        int pos = 0;
        while (pos + 8 <= biff.Length)
        {
            int size = BitConverter.ToInt32(biff, pos);
            if (size < 4) break;
            string tag = Encoding.ASCII.GetString(biff, pos + 4, 4);
            pos += 8; // size + tag consumed

            if (tag == "CODE")
            {
                if (pos + 4 > biff.Length) return null;
                int scriptLen = BitConverter.ToInt32(biff, pos);
                pos += 4;
                if (scriptLen < 0 || pos + scriptLen > biff.Length)
                    scriptLen = Math.Max(0, biff.Length - pos);
                // VPX scripts are ANSI (Windows-1252-ish); Latin-1 is a lossless byte map.
                return Encoding.Latin1.GetString(biff, pos, scriptLen);
            }

            if (tag == "ENDB") break;

            long next = (long)pos + size - 4; // regular record: remaining payload is size-4 bytes
            if (next < pos || next > biff.Length) break;
            pos = (int)next;
        }

        // Fallback: locate "CODE" marker anywhere (robust against exotic writers)
        return FallbackScanForCode(biff);
    }

    private static string? FallbackScanForCode(byte[] biff)
    {
        for (int i = 0; i + 8 < biff.Length; i++)
        {
            if (biff[i] == (byte)'C' && biff[i + 1] == (byte)'O' && biff[i + 2] == (byte)'D' && biff[i + 3] == (byte)'E')
            {
                int len = BitConverter.ToInt32(biff, i + 4);
                int start = i + 8;
                if (len > 16 && start + len <= biff.Length)
                {
                    var candidate = Encoding.Latin1.GetString(biff, start, len);
                    if (candidate.Contains("Sub", StringComparison.OrdinalIgnoreCase) ||
                        candidate.Contains("Dim", StringComparison.OrdinalIgnoreCase))
                        return candidate;
                }
            }
        }
        return null;
    }

    private static string? ReadInfoString(CompoundFileReader cf, CompoundFileReader.DirEntry info, string streamName)
    {
        var s = cf.FindStream(info, streamName);
        if (s is null) return null;
        byte[] bytes;
        try { bytes = cf.ReadStream(s); }
        catch { return null; }
        if (bytes.Length == 0) return "";
        return DecodeInfoBytes(bytes);
    }

    /// <summary>TableInfo strings are UTF-16LE in modern VPX; older writers used ANSI. Heuristic decode.</summary>
    internal static string DecodeInfoBytes(byte[] bytes)
    {
        if (bytes.Length >= 2 && bytes.Length % 2 == 0)
        {
            int zeroOdd = 0;
            for (int i = 1; i < bytes.Length; i += 2)
                if (bytes[i] == 0) zeroOdd++;
            if (zeroOdd >= (bytes.Length / 2) * 0.6)
                return Encoding.Unicode.GetString(bytes).TrimEnd('\0');
        }
        return Encoding.Latin1.GetString(bytes).TrimEnd('\0');
    }
}
