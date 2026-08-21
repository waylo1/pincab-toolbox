@echo off
rem ============================================================
rem  Pincab Toolbox — build script (run on Windows)
rem  Requires the .NET 8 SDK:  winget install Microsoft.DotNet.SDK.8
rem  Requires Python 3 (for test fixtures): winget install Python.Python.3.12
rem ============================================================
setlocal
cd /d "%~dp0"

echo [1/5] Generating test fixtures...
where python >nul 2>nul
if not errorlevel 1 (
  python tests\fixtures\make_fixtures.py
) else (
  py tests\fixtures\make_fixtures.py
)
if errorlevel 1 goto :fixturesfail

echo [2/5] Publishing single-file win-x64 exe...
rem NuGet.Config clears all package sources by design (zero third-party deps in the app).
rem Self-contained publish still needs to fetch the official Microsoft .NET/WPF runtime
rem packs (not app dependencies) at least once, so nuget.org is added just for this
rem restore, on the command line only — NuGet.Config itself is untouched.
rem Published BEFORE the local test runs (moved 21/08) — on a machine where Windows Smart
rem App Control blocks launching a freshly-compiled, unsigned test .exe (confirmed cause,
rem see knowledge/FIELD-LOG.md 2026-08-04, irreversible short of reinstalling Windows), the
rem old order meant that OS policy, unrelated to code health, silently prevented ever
rem reaching this step — no exe, even when the code was correct. GitHub CI (Linux, immune to
rem this) already is the trusted test gate per that same decision; this script just stops
rem also gating the one artifact a Windows machine can actually produce on it.
dotnet publish src\PincabToolbox.App -c Release -r win-x64 --self-contained ^
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true ^
  -p:RestoreSources=https://api.nuget.org/v3/index.json ^
  -o publish
if errorlevel 1 goto :fail

echo [3/5] Running Core tests...
rem Scenarios.DetectAll and the Build*-supporting row planners (point 3/5, 13/08) live in
rem PincabToolbox.Core.Diagnostics now and are covered here — PincabToolbox.App.Tests (the
rem temporary WPF-free bridge project) was retired at point 5, its job done.
rem Best-effort from here on: does NOT abort the script anymore (see note above [2/5]) — a
rem failure here on THIS machine may just be Smart App Control blocking the .exe launch,
rem not a real regression. Read the actual error before assuming the worst.
dotnet run --project tests\PincabToolbox.Core.Tests -c Release

echo [4/5] Running Repair tests...
dotnet run --project tests\PincabToolbox.Repair.Tests -c Release

echo [5/5] Done.
echo.
echo   publish\PincabToolbox.exe   ^<-- double-click to run
echo.
echo   If [3/5] or [4/5] above showed "Une strategie de controle d'application a bloque ce
echo   fichier", that's Windows Smart App Control blocking the LOCAL test run only, not the
echo   app above — the exe was already published successfully in step 2/5.
goto :eof

:fixturesfail
echo BUILD FAILED — could not generate test fixtures. Is Python 3 installed and on PATH?
exit /b 1

:fail
echo BUILD FAILED
exit /b 1
