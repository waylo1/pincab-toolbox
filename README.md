# Pincab Toolbox — Free Scanner

**The mechanic for your pincab.** Scans a Visual Pinball X / PinUP Popper installation and reports what is broken, missing or mismatched — before you hit Start on a table.

## What the free scanner checks

| Module | What it finds |
|---|---|
| **ROM Validator** | Tables whose ROM zip is missing from `VPinMAME/roms` (with `VPMAlias.txt` resolution, multi-candidate scripts, EM/original detection); ROMs present but left **unzipped** as a folder |
| **Bitness Doctor** (read-only) | 32/64-bit inventory of every known binary; hybrid installs; 64-bit VPX + 32-bit VPinMAME **and** the reverse 32-bit VPX + 64-bit VPinMAME mismatch; missing `dmddevice64.dll` |
| **Install Auditor** | Missing `.directb2s` backglasses, **orphan/misnamed** backglass files, tables absent from PinUP Popper's database, PUP-Pack presence, registered games with **no wheel media** under POPMedia |
| **Blocked-file check** | DLLs blocked by Windows ("Mark of the Web") that silently fail to load (VPinMAME, dmddevice, B2S, FlexDMD…) |
| **Dependency Check** | Backglasses/scripts that need the **B2S Backglass Server** but no `B2SBackglassServer.dll` is installed; scripts that use **FlexDMD** with no `FlexDMD.dll` |
| **Compatibility Linter** | nFozzy/Roth physics signatures, declared minimum VPX versions, FlexDMD/B2S usage |
| **Update Watcher** (beta) | Compares your tables against the open-source [Virtual Pinball Spreadsheet](https://virtual-pinball-spreadsheet.web.app) database and links to official pages — never downloads anything |
| **Script Diff** | Side-by-side script comparison of two `.vpx` (or `.vbs`) versions |

Every finding carries an **Impact / Cause / Recommended fix** explanation (bilingual FR/EN), and correlated findings roll up into a single **main diagnosis** (e.g. an incomplete 32→64 migration) with a reliability score — see [`docs/ARCHITECTURE-KnowledgeEngine.md`](docs/ARCHITECTURE-KnowledgeEngine.md).

## Principles

- **100% local.** Nothing is uploaded. No telemetry, no account. The only network call is fetching the open-source VPS database (cached 24 h, offline-tolerant).
- **Read-only.** The free scanner never modifies a file, a registry key, or a database.
- **Never downloads content.** No tables, no ROMs, no media — ever. Update findings link to official pages only.
- **Zero dependencies.** The Core has no NuGet packages: the OLE Compound File reader (.vpx), the read-only SQLite reader (PUPDatabase.db) and the Myers diff are implemented in-repo. Small exe, no supply chain.

## Build (Windows)

```
winget install Microsoft.DotNet.SDK.8   # if needed
build.cmd
```

Produces `publish\PincabToolbox.exe` (single file, self-contained). CI builds are also produced by `.github/workflows/build.yml`.

## Tests

42 unit/integration tests, no test framework dependency:

```
python3 tests/fixtures/make_fixtures.py   # generates synthetic .vpx / SQLite / PE fixtures
dotnet run --project tests/PincabToolbox.Core.Tests -c Release
```

The suite includes an end-to-end scan of a synthetic pincab install tree, and (when present) a validation pass against the real VPS database.

## Architecture

```
src/PincabToolbox.Core     # engine — knows nothing about VPX; reads profiles/*.json
  Vpx/                     #   CompoundFileReader (MS-CFB), VpxReader (script + TableInfo)
  Scanning/                #   LayoutDetector, ScanEngine, 7 scanners
  Services/                #   SqliteReader, PeInspector, ScriptAnalyzer, VpsDatabase, MyersDiff, DiffService
src/PincabToolbox.App      # WPF UI (dark, FR/EN), .NET 8
  Knowledge.cs / Scenarios.cs   #   impact/cause per finding + root-cause correlation (the "Knowledge Engine")
profiles/vpx-popper.json   # everything VPX-specific lives here — new ecosystems are data, not code
tests/                     # zero-dependency micro test runner + python fixture generator
```

© 2026 MC Automation — freeware.
