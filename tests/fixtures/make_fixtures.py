#!/usr/bin/env python3
"""Generates binary test fixtures for PincabToolbox.Core.Tests:
- Minimal but structurally valid OLE Compound Files (.vpx) with GameStg/GameData(CODE script) + TableInfo
- A PinUP-Popper-like SQLite database (via the stdlib sqlite3)
- Tiny PE files (x86 / x64)
- A complete fake pincab install tree for the end-to-end ScanEngine test
"""
import os, shutil, sqlite3, struct, sys

OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "out")

FREESECT   = 0xFFFFFFFF
ENDOFCHAIN = 0xFFFFFFFE
FATSECT    = 0xFFFFFFFD
NOSTREAM   = 0xFFFFFFFF


def u16(v): return struct.pack("<H", v)
def u32(v): return struct.pack("<I", v)


def dir_entry(name, etype, left=NOSTREAM, right=NOSTREAM, child=NOSTREAM, start=0, size=0):
    raw_name = name.encode("utf-16-le") + b"\x00\x00"
    assert len(raw_name) <= 64
    e = raw_name + b"\x00" * (64 - len(raw_name))
    e += u16(len(raw_name))          # name length incl. terminator
    e += bytes([etype, 1])           # type, color=black
    e += u32(left) + u32(right) + u32(child)
    e += b"\x00" * 16                # CLSID
    e += u32(0)                      # state bits
    e += b"\x00" * 16                # timestamps
    e += u32(start)
    e += u32(size) + u32(0)          # v3: low 32 bits only
    assert len(e) == 128
    return e


def build_vpx(path, script, table_name="Fixture Table", version="1.0", author="MC Automation"):
    """Writes a minimal v3 compound file with the exact structure VpxReader expects."""
    game_data = u32(4) + b"CODE" + u32(len(script)) + script.encode("latin-1") \
              + u32(4) + b"ENDB"
    streams = [
        game_data,
        table_name.encode("utf-16-le"),
        version.encode("utf-16-le"),
        author.encode("utf-16-le"),
    ]

    # ---- mini stream + miniFAT ----
    mini = b""
    starts, minifat = [], []
    cur = 0
    for s in streams:
        n = max(1, (len(s) + 63) // 64)
        starts.append(cur)
        for i in range(n):
            chunk = s[i * 64:(i + 1) * 64]
            mini += chunk + b"\x00" * (64 - len(chunk))
            minifat.append(cur + i + 1 if i < n - 1 else ENDOFCHAIN)
        cur += n
    mini_sectors = (len(mini) + 511) // 512

    # ---- main sector plan: 0=FAT, 1..2=directory, 3=miniFAT, 4..=mini stream ----
    first_mini_main = 4
    fat = [FATSECT, 2, ENDOFCHAIN, ENDOFCHAIN]
    for i in range(mini_sectors):
        fat.append(first_mini_main + i + 1 if i < mini_sectors - 1 else ENDOFCHAIN)
    fat += [FREESECT] * (128 - len(fat))

    entries = [
        dir_entry("Root Entry", 5, child=1, start=first_mini_main, size=len(mini)),
        dir_entry("GameStg",    1, right=3, child=2),
        dir_entry("GameData",   2, start=starts[0], size=len(streams[0])),
        dir_entry("TableInfo",  1, child=5),
        dir_entry("TableName",  2, start=starts[1], size=len(streams[1])),
        dir_entry("TableVersion", 2, left=4, right=6, start=starts[2], size=len(streams[2])),
        dir_entry("AuthorName", 2, start=starts[3], size=len(streams[3])),
        b"\x00" * 128,  # unused slot
    ]
    directory = b"".join(entries)
    assert len(directory) == 1024

    header = b"\xD0\xCF\x11\xE0\xA1\xB1\x1A\xE1"
    header += b"\x00" * 16
    header += u16(0x003E) + u16(0x0003)       # minor, major (v3)
    header += b"\xFE\xFF"                     # byte order
    header += u16(9) + u16(6)                 # sector shift, mini shift
    header += b"\x00" * 6
    header += u32(0)                          # num dir sectors (v3: 0)
    header += u32(1)                          # num FAT sectors
    header += u32(1)                          # first directory sector
    header += u32(0)                          # transaction
    header += u32(4096)                       # mini cutoff
    header += u32(3)                          # first miniFAT sector
    header += u32(1)                          # num miniFAT sectors
    header += u32(ENDOFCHAIN)                 # first DIFAT sector
    header += u32(0)                          # num DIFAT sectors
    header += u32(0) + b"".join(u32(FREESECT) for _ in range(108))
    assert len(header) == 512

    minifat_sector = b"".join(u32(x) for x in minifat)
    minifat_sector += u32(FREESECT) * (128 - len(minifat))

    body = b"".join(u32(x) for x in fat) + directory + minifat_sector + mini
    body += b"\x00" * ((512 - len(body) % 512) % 512)

    with open(path, "wb") as f:
        f.write(header + body)


def build_pe(path, machine):
    data = bytearray(b"MZ" + b"\x00" * 62)
    data[0x3C:0x40] = u32(0x40)
    data += b"PE\x00\x00" + u16(machine) + b"\x00" * 58
    with open(path, "wb") as f:
        f.write(bytes(data))


def build_sqlite(path):
    if os.path.exists(path):
        os.remove(path)
    conn = sqlite3.connect(path)
    conn.execute("""CREATE TABLE Games(
        GameID INTEGER PRIMARY KEY,
        GameName TEXT,
        GameFileName TEXT,
        GameDisplay TEXT,
        Visible INTEGER,
        Notes TEXT)""")
    rows = [
        ("Attack From Mars (Bally 1995)", "Attack From Mars (Bally 1995).vpx", "Attack From Mars", 1, None),
        ("Médiéval Madness", "Medieval Madness (Williams 1997) v2.vpx", "Médiéval Madness", 1, "accented notes é à ü"),
        ("Big Overflow", "big.vpx", "Big", 0, "x" * 8000),  # forces an overflow page
    ]
    conn.executemany("INSERT INTO Games(GameName, GameFileName, GameDisplay, Visible, Notes) VALUES (?,?,?,?,?)", rows)
    conn.commit()
    conn.close()


SCRIPT_ROM_OK = '''\
Option Explicit
Const cGameName = "afm_113b"
Dim Controller
Set Controller = CreateObject("VPinMAME.Controller")
Sub Table1_Init()
End Sub
'''

SCRIPT_ROM_MISSING = '''\
Option Explicit
Const cGameName = "mm_109c"
Set Controller = CreateObject("VPinMAME.Controller")
' requires VPX 10.8
Class FlipperPolarity
End Class
'''

SCRIPT_ALIAS = '''\
Option Explicit
Const cGameName = "afm_mod"
Set Controller = CreateObject("VPinMAME.Controller")
'''

SCRIPT_EM = '''\
Option Explicit
' An original table, no ROM at all.
Sub Table1_Init()
End Sub
'''


def build_install_tree(root):
    if os.path.exists(root):
        shutil.rmtree(root)
    tables = os.path.join(root, "Tables")
    vpm = os.path.join(root, "VPinMAME")
    roms = os.path.join(vpm, "roms")
    pup = os.path.join(root, "PinUPSystem")
    os.makedirs(tables); os.makedirs(roms); os.makedirs(pup)

    build_vpx(os.path.join(tables, "Attack From Mars (Bally 1995).vpx"), SCRIPT_ROM_OK, "Attack From Mars", "1.2")
    build_vpx(os.path.join(tables, "Medieval Madness (Williams 1997).vpx"), SCRIPT_ROM_MISSING, "Medieval Madness", "0.9")
    build_vpx(os.path.join(tables, "Aliased Table (Test 2020).vpx"), SCRIPT_ALIAS, "Aliased", "1.0")
    build_vpx(os.path.join(tables, "Original Gem (Homebrew 2024).vpx"), SCRIPT_EM, "Original Gem", "3.1")

    # backglass for AFM only
    open(os.path.join(tables, "Attack From Mars (Bally 1995).directb2s"), "w").write("<b2s/>")

    # roms: afm present, mm absent, alias target present
    open(os.path.join(roms, "afm_113b.zip"), "wb").write(b"PK")
    open(os.path.join(roms, "afm_113.zip"), "wb").write(b"PK")
    with open(os.path.join(vpm, "VPMAlias.txt"), "w") as f:
        f.write("// comment line\n")
        f.write("afm_mod,afm_113\n")

    # binaries: 64-bit main exe + 32-bit VPinMAME -> must trigger BITNESS_MISMATCH_VPM
    build_pe(os.path.join(root, "VPinballX64.exe"), 0x8664)
    build_pe(os.path.join(vpm, "VPinMAME.dll"), 0x014C)
    build_pe(os.path.join(root, "dmddevice.dll"), 0x014C)

    build_sqlite(os.path.join(pup, "PUPDatabase.db"))


def main():
    os.makedirs(OUT, exist_ok=True)
    build_vpx(os.path.join(OUT, "simple.vpx"), SCRIPT_ROM_OK, "Simple Table", "1.2.3", "Tester")
    build_vpx(os.path.join(OUT, "simple_v2.vpx"), SCRIPT_ROM_OK.replace('afm_113b', 'afm_113b') + "' new line added\n", "Simple Table", "1.3.0", "Tester")
    build_pe(os.path.join(OUT, "x86.exe"), 0x014C)
    build_pe(os.path.join(OUT, "x64.exe"), 0x8664)
    build_sqlite(os.path.join(OUT, "pup.db"))
    build_install_tree(os.path.join(OUT, "install"))
    print("fixtures written to", OUT)


if __name__ == "__main__":
    sys.exit(main())
