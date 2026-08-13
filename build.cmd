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

echo [2/5] Running Core tests...
rem Scenarios.DetectAll and the Build*-supporting row planners (point 3/5, 13/08) live in
rem PincabToolbox.Core.Diagnostics now and are covered here — PincabToolbox.App.Tests (the
rem temporary WPF-free bridge project) was retired at point 5, its job done.
dotnet run --project tests\PincabToolbox.Core.Tests -c Release
if errorlevel 1 goto :fail

echo [3/5] Running Repair tests...
dotnet run --project tests\PincabToolbox.Repair.Tests -c Release
if errorlevel 1 goto :fail

echo [4/5] Publishing single-file win-x64 exe...
rem NuGet.Config clears all package sources by design (zero third-party deps in the app).
rem Self-contained publish still needs to fetch the official Microsoft .NET/WPF runtime
rem packs (not app dependencies) at least once, so nuget.org is added just for this
rem restore, on the command line only — NuGet.Config itself is untouched.
dotnet publish src\PincabToolbox.App -c Release -r win-x64 --self-contained ^
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true ^
  -p:RestoreSources=https://api.nuget.org/v3/index.json ^
  -o publish
if errorlevel 1 goto :fail

echo [5/5] Done.
echo.
echo   publish\PincabToolbox.exe   ^<-- double-click to run
echo.
goto :eof

:fixturesfail
echo BUILD FAILED — could not generate test fixtures. Is Python 3 installed and on PATH?
exit /b 1

:fail
echo BUILD FAILED
exit /b 1
