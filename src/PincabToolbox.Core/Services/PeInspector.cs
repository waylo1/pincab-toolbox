namespace PincabToolbox.Core.Services;

public enum Bitness { Unknown, X86, X64, Arm64 }

/// <summary>Reads the COFF header of a PE file to determine its bitness. No dependencies, ~200 bytes read.</summary>
public static class PeInspector
{
    public static Bitness GetBitness(string path)
    {
        try
        {
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var br = new BinaryReader(fs);

            if (fs.Length < 0x40) return Bitness.Unknown;
            if (br.ReadUInt16() != 0x5A4D) return Bitness.Unknown; // 'MZ'

            fs.Seek(0x3C, SeekOrigin.Begin);
            int peOffset = br.ReadInt32();
            if (peOffset <= 0 || peOffset + 6 > fs.Length) return Bitness.Unknown;

            fs.Seek(peOffset, SeekOrigin.Begin);
            if (br.ReadUInt32() != 0x00004550) return Bitness.Unknown; // 'PE\0\0'

            return br.ReadUInt16() switch
            {
                0x014C => Bitness.X86,
                0x8664 => Bitness.X64,
                0xAA64 => Bitness.Arm64,
                _ => Bitness.Unknown,
            };
        }
        catch
        {
            return Bitness.Unknown;
        }
    }
}
