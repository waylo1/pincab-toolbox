@echo off
rem ============================================================
rem  test-sac.cmd — experience decisive sur le blocage
rem  "Controle intelligent des applications" (Smart App Control)
rem
rem  Contexte : le build de 13h44 (commit 20ba4b3, AVANT les 4 lots)
rem  se lancait normalement ; le build de 14h04 (APRES merge des 4
rem  lots) est bloque en dur par Smart App Control.
rem
rem  Deux hypotheses possibles :
rem    H1 - un changement de mon code fait basculer le classifieur
rem    H2 - c'est la reputation cloud : chaque nouveau build = un
rem         hash inedit, non signe, donc inconnu du service Microsoft
rem
rem  Ce script fabrique deux exe de test dans des dossiers separes,
rem  SANS toucher a ton build normal (publish\ reste intact).
rem ============================================================
setlocal
cd /d "%~dp0"

echo.
echo ============================================================
echo  VARIANTE A - rebuild du commit connu-bon 20ba4b3
echo  Meme source qu'au build de 13h44 qui se lancait bien,
echo  mais recompilee maintenant, donc hash tout neuf.
echo ============================================================
echo.

if exist "..\_sac-test-A" (
  echo Nettoyage du worktree precedent...
  git worktree remove "..\_sac-test-A" --force >nul 2>nul
  if exist "..\_sac-test-A" rd /s /q "..\_sac-test-A"
)

git worktree add "..\_sac-test-A" 20ba4b3
if errorlevel 1 goto :fail

dotnet publish "..\_sac-test-A\src\PincabToolbox.App" -c Release -r win-x64 --self-contained ^
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true ^
  -p:RestoreSources=https://api.nuget.org/v3/index.json ^
  -o publish-test-A
if errorlevel 1 goto :fail

echo.
echo ============================================================
echo  VARIANTE B - code ACTUEL, mais sans auto-extraction native
echo  On retire IncludeNativeLibrariesForSelfExtract : les DLL
echo  natives se posent A COTE de l'exe au lieu d'etre extraites
echo  dans un dossier temporaire au lancement. Ce comportement
echo  d'auto-extraction est un motif classique de "dropper" pour
echo  les classifieurs heuristiques.
echo ============================================================
echo.

dotnet publish src\PincabToolbox.App -c Release -r win-x64 --self-contained ^
  -p:PublishSingleFile=true ^
  -p:RestoreSources=https://api.nuget.org/v3/index.json ^
  -o publish-test-B
if errorlevel 1 goto :fail

echo.
echo ============================================================
echo  TERMINE. Lance maintenant CHACUN des deux, dans cet ordre :
echo.
echo    1^)  publish-test-A\PincabToolbox.exe
echo        ^(ancien code connu-bon, hash neuf^)
echo.
echo    2^)  publish-test-B\PincabToolbox.exe
echo        ^(code actuel, sans auto-extraction^)
echo.
echo  Puis dis-moi lequel se lance et lequel est bloque.
echo  C'est ce resultat qui tranche entre H1 et H2.
echo ============================================================
echo.
goto :eof

:fail
echo.
echo ECHEC du script de test.
exit /b 1
