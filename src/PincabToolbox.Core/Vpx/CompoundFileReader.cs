using System.Text;

namespace PincabToolbox.Core.Vpx;

/// <summary>
/// Minimal, dependency-free, read-only parser for the Microsoft Compound File
/// Binary Format (MS-CFB) — the container used by .vpx files.
/// Supports v3 (512-byte sectors) and v4 (4096-byte sectors), FAT + miniFAT streams.
/// Defensive: throws <see cref="InvalidDataException"/> on structural corruption,
/// never reads past buffers, and bounds every chain walk against cycles.
/// </summary>
public sealed class CompoundFileReader
{
    private const uint EndOfChain = 0xFFFFFFFE;
    private const uint FreeSect = 0xFFFFFFFF;
    private const uint FatSect = 0xFFFFFFFD;
    private const uint DifSect = 0xFFFFFFFC;
    private const uint NoStream = 0xFFFFFFFF;

    private readonly byte[] _data;
    private readonly int _sectorSize;
    private readonly int _miniSectorSize = 64;
    private readonly uint _miniStreamCutoff;
    private readonly uint[] _fat;
    private readonly uint[] _miniFat;
    private readonly List<DirEntry> _entries = new();
    private readonly byte[] _miniStream;

    public sealed class DirEntry
    {
        public required int Id { get; init; }
        public required string Name { get; init; }
        public required byte Type { get; init; }   // 1=storage 2=stream 5=root
        public required uint Left { get; init; }
        public required uint Right { get; init; }
        public required uint Child { get; init; }
        public required uint StartSector { get; init; }
        public required long Size { get; init; }
        public bool IsStorage => Type is 1 or 5;
        public bool IsStream => Type == 2;
    }

    public DirEntry Root => _entries[0];

    public static CompoundFileReader Open(string path) => new(File.ReadAllBytes(path));

    public CompoundFileReader(byte[] data)
    {
        _data = data;
        if (data.Length < 512) throw new InvalidDataException("File too small to be a compound file.");
        if (ReadU64(0) != 0xE11AB1A1E011CFD0UL) throw new InvalidDataException("Not a compound file (bad signature).");

        ushort sectorShift = ReadU16(30);
        if (sectorShift is not (9 or 12)) throw new InvalidDataException($"Unsupported sector shift {sectorShift}.");
        _sectorSize = 1 << sectorShift;

        uint numFatSectors = ReadU32(44);
        uint firstDirSector = ReadU32(48);
        _miniStreamCutoff = ReadU32(56);
        if (_miniStreamCutoff == 0) _miniStreamCutoff = 4096;
        uint firstMiniFat = ReadU32(60);
        uint numMiniFat = ReadU32(64);
        uint firstDifat = ReadU32(68);
        uint numDifat = ReadU32(72);

        // ---- DIFAT: locations of FAT sectors ----
        var fatSectors = new List<uint>();
        for (int i = 0; i < 109; i++)
        {
            uint s = ReadU32(76 + i * 4);
            if (s is not (FreeSect or EndOfChain)) fatSectors.Add(s);
        }
        uint difat = firstDifat;
        int difatGuard = 0;
        int idsPerDifat = _sectorSize / 4 - 1;
        while (difat != EndOfChain && difat != FreeSect && difatGuard++ <= numDifat + 4)
        {
            long off = SectorOffset(difat);
            for (int i = 0; i < idsPerDifat; i++)
            {
                uint s = ReadU32(off + i * 4);
                if (s is not (FreeSect or EndOfChain)) fatSectors.Add(s);
            }
            difat = ReadU32(off + idsPerDifat * 4);
        }

        // ---- FAT ----
        int entriesPerSector = _sectorSize / 4;
        _fat = new uint[fatSectors.Count * entriesPerSector];
        for (int i = 0; i < fatSectors.Count; i++)
        {
            long off = SectorOffset(fatSectors[i]);
            for (int j = 0; j < entriesPerSector; j++)
                _fat[i * entriesPerSector + j] = ReadU32(off + j * 4);
        }

        // ---- Directory ----
        foreach (var sector in WalkChain(firstDirSector))
        {
            long off = SectorOffset(sector);
            for (int e = 0; e < _sectorSize / 128; e++)
                ParseDirEntry(off + e * 128);
        }
        if (_entries.Count == 0 || _entries[0].Type != 5)
            throw new InvalidDataException("Missing root directory entry.");

        // ---- Mini FAT ----
        var miniFatList = new List<uint>();
        foreach (var sector in WalkChain(firstMiniFat, (int)numMiniFat + 4))
        {
            long off = SectorOffset(sector);
            for (int j = 0; j < entriesPerSector; j++)
                miniFatList.Add(ReadU32(off + j * 4));
        }
        _miniFat = miniFatList.ToArray();

        // ---- Mini stream (root entry's stream, holds all mini sectors) ----
        _miniStream = ReadRegularStream(Root.StartSector, Root.Size);
    }

    // ---------- public API ----------

    public DirEntry? FindStorage(DirEntry parent, string name) =>
        Children(parent).FirstOrDefault(c => c.IsStorage && NameEquals(c.Name, name));

    public DirEntry? FindStream(DirEntry parent, string name) =>
        Children(parent).FirstOrDefault(c => c.IsStream && NameEquals(c.Name, name));

    public IEnumerable<DirEntry> Children(DirEntry parent)
    {
        var result = new List<DirEntry>();
        var visited = new HashSet<uint>();
        void Walk(uint id)
        {
            if (id == NoStream || id >= _entries.Count || !visited.Add(id)) return;
            var e = _entries[(int)id];
            Walk(e.Left);
            result.Add(e);
            Walk(e.Right);
        }
        Walk(parent.Child);
        return result;
    }

    public byte[] ReadStream(DirEntry entry)
    {
        if (!entry.IsStream) throw new InvalidOperationException($"'{entry.Name}' is not a stream.");
        if (entry.Size == 0) return Array.Empty<byte>();
        return entry.Size < _miniStreamCutoff
            ? ReadMiniStream(entry.StartSector, entry.Size)
            : ReadRegularStream(entry.StartSector, entry.Size);
    }

    // ---------- internals ----------

    private static bool NameEquals(string a, string b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

    private void ParseDirEntry(long off)
    {
        if (off + 128 > _data.Length) return;
        ushort nameLen = ReadU16(off + 64);
        byte type = _data[off + 66];
        if (type == 0 || nameLen < 2 || nameLen > 64) return;
        string name = Encoding.Unicode.GetString(_data, (int)off, nameLen - 2);
        long size = _sectorSize == 512
            ? ReadU32(off + 120)          // v3: only low 4 bytes are valid
            : (long)ReadU64(off + 120);
        _entries.Add(new DirEntry
        {
            Id = _entries.Count,
            Name = name,
            Type = type,
            Left = ReadU32(off + 68),
            Right = ReadU32(off + 72),
            Child = ReadU32(off + 76),
            StartSector = ReadU32(off + 116),
            Size = size,
        });
    }

    private long SectorOffset(uint sector) => (long)(sector + 1) * _sectorSize;

    private IEnumerable<uint> WalkChain(uint start, int maxSectors = 1 << 20)
    {
        uint s = start;
        var visited = new HashSet<uint>();
        while (s != EndOfChain && s != FreeSect && s != FatSect && s != DifSect)
        {
            if (!visited.Add(s) || visited.Count > maxSectors)
                throw new InvalidDataException("Cyclic or oversized sector chain.");
            if (SectorOffset(s) + _sectorSize > _data.Length)
                throw new InvalidDataException("Sector chain points past end of file.");
            yield return s;
            if (s >= _fat.Length) throw new InvalidDataException("Sector index outside FAT.");
            s = _fat[s];
        }
    }

    private byte[] ReadRegularStream(uint startSector, long size)
    {
        var result = new byte[size];
        long written = 0;
        foreach (var sector in WalkChain(startSector))
        {
            if (written >= size) break;
            long off = SectorOffset(sector);
            int take = (int)Math.Min(_sectorSize, size - written);
            Array.Copy(_data, off, result, written, take);
            written += take;
        }
        if (written < size) throw new InvalidDataException("Stream chain shorter than declared size.");
        return result;
    }

    private byte[] ReadMiniStream(uint startMiniSector, long size)
    {
        var result = new byte[size];
        long written = 0;
        uint s = startMiniSector;
        var visited = new HashSet<uint>();
        while (written < size)
        {
            if (s == EndOfChain || s == FreeSect)
                throw new InvalidDataException("Mini stream chain shorter than declared size.");
            if (!visited.Add(s)) throw new InvalidDataException("Cyclic mini stream chain.");
            long off = (long)s * _miniSectorSize;
            if (off + _miniSectorSize > _miniStream.Length)
                throw new InvalidDataException("Mini sector outside mini stream.");
            int take = (int)Math.Min(_miniSectorSize, size - written);
            Array.Copy(_miniStream, off, result, written, take);
            written += take;
            if (s >= _miniFat.Length) throw new InvalidDataException("Mini sector index outside miniFAT.");
            s = _miniFat[s];
        }
        return result;
    }

    private ushort ReadU16(long off) => (ushort)(_data[off] | _data[off + 1] << 8);
    private uint ReadU32(long off) => (uint)(_data[off] | _data[off + 1] << 8 | _data[off + 2] << 16 | _data[off + 3] << 24);
    private ulong ReadU64(long off) => ReadU32(off) | (ulong)ReadU32(off + 4) << 32;
}
