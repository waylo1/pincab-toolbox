> # ⚫ DOCUMENT MORT — NE PAS UTILISER
>
> Archivé le 25/07/2026. Copie figée du code source à une date donnée, faite pour une revue externe. Le code a évolué depuis : ce fichier ment. Lire le vrai code.
>
> La source de vérité est `docs/PROJECT-BRAIN.md`. Ce fichier est conservé
> uniquement pour retrouver le raisonnement d'origine.

---

# Pincab Toolbox — Review Pack

> À coller dans une IA pour obtenir une revue ciblée. Joins aussi 2-3 captures d'écran (résultats de scan, ligne sélectionnée avec panneau de détail, onglet À propos). Le document complet de vision est `docs/ARCHITECTURE-KnowledgeEngine.md` — donne-le en plus si tu veux une revue stratégique.

---

## 0. Brief de contexte (à lire avant de critiquer)

**Ce que c'est** : Pincab Toolbox, un scanner de diagnostic **gratuit et en lecture seule** pour cabinets de flipper virtuel (Visual Pinball X / PinUP Popper), sous Windows 10/11. .NET 8 / WPF. Micro-entreprise solo (MC Automation), stade **alpha 0.1**.

**La vision** : devenir *le moteur de diagnostic de référence* de l'écosystème Virtual Pinball. Le réflexe communautaire visé : « Lance Pincab Toolbox → Health Check → Poste le rapport ». Un module **Repair** (payant) suivra ; le scanner détecte, Repair corrige.

**Philosophie non négociable** (ne pas proposer d'y déroger) :
- **Lecture seule** : ne modifie jamais un fichier, ne télécharge rien, zéro télémétrie côté app.
- **Justesse > quantité** : mieux vaut 30 checks fiables que 200 artificiels. Zéro faux positif est prioritaire sur le nombre de fonctionnalités.
- **Le moat est la base de connaissance**, pas l'UI. Architecture data-driven (« Knowledge Pack » mis à jour indépendamment de l'app).
- Pipeline cible : **Checks → Findings → Scénarios → Repair Rules**, chacun avec un **niveau de confiance mesuré**.

**Hors périmètre (ne PAS suggérer)** : mesure des FPS / performance en temps réel, détection matérielle live (tout ce qui exige de lancer les tables) ; refonte MVVM/DI (prématuré à ce stade) ; dépendances lourdes (QuestPDF, LiveCharts) ; reCAPTCHA/analytics intrusifs.

**Déjà fait** : 7 modules de scan (ROM Validator, Bitness Doctor, Install Auditor, Compatibility Linter, Blocked-file check, Dependency Check, Update Watcher) + Script Diff. Findings récents : mismatch bitness inversé (32-bit VPX + VPinMAME 64-bit), ROM dézippée, backglass orphelin/mal nommé, média Popper (wheel) manquant, serveur B2S absent, FlexDMD absent. Chaque finding porte une explication **Impact / Cause / Correctif** (bilingue) et les findings corrélés remontent en **diagnostic principal** avec un niveau de fiabilité (Knowledge.cs / Scenarios.cs). Score de santé 0-100 (base 100, −15/critique, −5/avertissement). Recherche, tri, panneau de détail avec bouton d'action, export HTML/Markdown/BBCode/Texte + « Copier pour le forum ». Bilingue FR/EN. Mémorisation dossier/langue/fenêtre. 42 tests (moteur compilé et testé en TDD).

**Prochains checks envisagés** (tous lecture seule) : cohérence `dmddevice.ini`, sanity écrans via `VPinballX.ini`, config DOF, structure PUP-Pack, colorisation (pin2dmd/serum).

**Gaps connus** : matching de l'Update Watcher heuristique (nom de fichier) ; version VPX installée pas encore lue depuis l'exe.

---

## 1. Arborescence du projet

```
pincab-suite/
├── PincabToolbox.sln
├── docs/
│   ├── ARCHITECTURE-KnowledgeEngine.md
│   └── REVIEW-PACK.md
├── src/
│   ├── PincabToolbox.Core/              # moteur, agnostique UI
│   │   ├── Models/
│   │   │   ├── Finding.cs               # une observation de scan (+ Code, Severity, FixHint…)
│   │   │   ├── InstallLayout.cs         # emplacements résolus de l'install
│   │   │   └── ScanReport.cs            # rapport agrégé (+ Score, Grade)
│   │   ├── Profiles/Profile.cs          # config JSON (rôles binaires, chemins…)
│   │   ├── Scanning/
│   │   │   ├── IScanner.cs              # interface + ScanContext
│   │   │   ├── ScanEngine.cs            # orchestrateur
│   │   │   ├── LayoutDetector.cs
│   │   │   ├── RomValidatorScanner.cs
│   │   │   ├── BitnessScanner.cs
│   │   │   ├── CompletenessScanner.cs
│   │   │   ├── CompatibilityScanner.cs
│   │   │   ├── UpdateWatcherScanner.cs
│   │   │   ├── BlockedFileScanner.cs    # DLL bloquées par Windows (Mark of the Web)
│   │   │   └── DependencyScanner.cs     # serveur B2S / FlexDMD requis mais absent
│   │   ├── Services/                    # VpsDatabase, PeInspector, DiffService, SqliteReader…
│   │   └── Vpx/                         # lecture des .vpx (compound file)
│   └── PincabToolbox.App/               # UI WPF
│       ├── App.xaml(.cs)                # palette / styles
│       ├── MainWindow.xaml(.cs)         # 3 onglets : Scanner, Diff, À propos
│       ├── Knowledge.cs / Scenarios.cs  # Knowledge Engine : impact/cause + corrélation
│       └── Localization/Loc.cs          # dictionnaire FR/EN (findings + fix hints)
└── tests/PincabToolbox.Core.Tests/
```

---

## 2. Code source du cœur (le plus intéressant à revoir)

### 2.1 `Models/Finding.cs`
```csharp
namespace PincabToolbox.Core.Models;

public enum Severity { Ok = 0, Info = 1, Warning = 2, Critical = 3 }

/// <summary>A single structured finding produced by a scanner.</summary>
public sealed record Finding
{
    public required string Code { get; init; }          // stable message code (ROM_MISSING…)
    public required Severity Severity { get; init; }
    public required string Category { get; init; }      // scanner id
    public string Subject { get; init; } = "";
    public string? FilePath { get; init; }
    public IReadOnlyList<string> Args { get; init; } = Array.Empty<string>();
    public required string EnglishText { get; init; }   // fallback rendering
    public string? FixHint { get; init; }               // English fallback; UI may localize by code
}
```

### 2.2 `Models/ScanReport.cs`
```csharp
namespace PincabToolbox.Core.Models;

public sealed class ScanReport
{
    public required InstallLayout Layout { get; init; }
    public List<Finding> Findings { get; } = new();
    public DateTimeOffset StartedAt { get; init; }
    public DateTimeOffset FinishedAt { get; set; }

    public int Count(Severity s) => Findings.Count(f => f.Severity == s);

    // Health score: base 100, −15 per Critical, −5 per Warning, Info neutral, floored at 0.
    public int Score => Math.Max(0, 100 - Count(Severity.Critical) * 15 - Count(Severity.Warning) * 5);
    public string Grade => Score switch { >= 100 => "A+", >= 90 => "A", >= 70 => "B", >= 40 => "C", _ => "F" };

    public IEnumerable<Finding> Ordered() =>
        Findings.OrderByDescending(f => f.Severity).ThenBy(f => f.Category).ThenBy(f => f.Subject);
}
```

### 2.3 `Models/InstallLayout.cs`
```csharp
namespace PincabToolbox.Core.Models;

/// <summary>Resolved locations of a virtual pinball installation. Null when not found; scanners degrade gracefully.</summary>
public sealed class InstallLayout
{
    public required string RootPath { get; init; }
    public string? TablesDir { get; set; }
    public List<string> VpxExecutables { get; } = new();
    public string? VPinMameDir { get; set; }
    public string? RomsDir { get; set; }
    public string? AliasFilePath { get; set; }
    public string? PupDatabasePath { get; set; }
    public string? PopMediaDir { get; set; }
    public string? PupVideosDir { get; set; }
    public List<string> VpxTables { get; } = new();
}
```

### 2.4 `Scanning/IScanner.cs`
```csharp
using PincabToolbox.Core.Models;
using PincabToolbox.Core.Profiles;
using PincabToolbox.Core.Vpx;

namespace PincabToolbox.Core.Scanning;

/// <summary>Shared context passed to every scanner (tables are parsed once).</summary>
public sealed class ScanContext
{
    public required InstallLayout Layout { get; init; }
    public required Profile Profile { get; init; }
    public Dictionary<string, VpxTableData> Tables { get; } = new();
    public HashSet<string> RomSets { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, string> Aliases { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public CancellationToken Cancellation { get; init; }
}

public interface IScanner
{
    string Id { get; }      // used as Finding.Category
    string Name { get; }    // English display name
    IEnumerable<Finding> Scan(ScanContext context);
}
```

### 2.5 `Scanning/ScanEngine.cs`
```csharp
using PincabToolbox.Core.Models;
using PincabToolbox.Core.Profiles;
using PincabToolbox.Core.Services;
using PincabToolbox.Core.Vpx;

namespace PincabToolbox.Core.Scanning;

/// <summary>Orchestrates layout detection, shared parsing, and all scanners.</summary>
public sealed class ScanEngine
{
    private readonly List<IScanner> _scanners = new();
    public ScanEngine Add(IScanner scanner) { _scanners.Add(scanner); return this; }
    public IReadOnlyList<IScanner> Scanners => _scanners;

    public ScanReport Run(string rootPath, Profile profile,
        IProgress<string>? progress = null, CancellationToken ct = default)
    {
        var layout = LayoutDetector.Detect(rootPath, profile);
        var report = new ScanReport { Layout = layout, StartedAt = DateTimeOffset.Now };
        var ctx = new ScanContext { Layout = layout, Profile = profile, Cancellation = ct };

        if (layout.RomsDir is not null)
            foreach (var zip in Directory.EnumerateFiles(layout.RomsDir, "*.zip", SearchOption.TopDirectoryOnly))
                ctx.RomSets.Add(Path.GetFileNameWithoutExtension(zip));

        if (layout.AliasFilePath is not null)
            ctx.Aliases = AliasFile.Parse(layout.AliasFilePath);

        int i = 0;
        foreach (var table in layout.VpxTables)
        {
            ct.ThrowIfCancellationRequested();
            progress?.Report($"Reading table {++i}/{layout.VpxTables.Count}: {Path.GetFileName(table)}");
            ctx.Tables[table] = VpxReader.Read(table);
        }

        foreach (var scanner in _scanners)
        {
            ct.ThrowIfCancellationRequested();
            progress?.Report($"Running {scanner.Name}…");
            try { report.Findings.AddRange(scanner.Scan(ctx)); }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                report.Findings.Add(new Finding {
                    Code = "SCANNER_ERROR", Severity = Severity.Warning, Category = scanner.Id,
                    Subject = scanner.Name, Args = new[] { scanner.Name, ex.Message },
                    EnglishText = $"Scanner '{scanner.Name}' failed: {ex.Message}",
                });
            }
        }
        report.FinishedAt = DateTimeOffset.Now;
        return report;
    }
}
```

### 2.6 `Scanning/BitnessScanner.cs` (un scanner d'exemple)
```csharp
using PincabToolbox.Core.Models;
using PincabToolbox.Core.Profiles;
using PincabToolbox.Core.Services;

namespace PincabToolbox.Core.Scanning;

public sealed class BitnessScanner : IScanner
{
    public string Id => "bitness";
    public string Name => "Bitness Doctor";

    public IEnumerable<Finding> Scan(ScanContext ctx)
    {
        var found = new List<(BinaryRole role, string path, Bitness bits)>();
        foreach (var role in ctx.Profile.BinaryRoles)
            foreach (var root in ResolveScope(ctx.Layout, role.Scope))
                foreach (var file in LayoutDetector.FindFilesByPattern(root, role.Pattern, 4))
                    found.Add((role, file, PeInspector.GetBitness(file)));

        var unique = found.GroupBy(f => f.path, StringComparer.OrdinalIgnoreCase).Select(g => g.First()).ToList();
        if (unique.Count == 0)
        {
            yield return new Finding { Code = "BITNESS_NOTHING_FOUND", Severity = Severity.Info, Category = Id,
                EnglishText = "No known binaries found to analyse." };
            yield break;
        }

        foreach (var (role, path, bits) in unique)
            yield return new Finding { Code = "BITNESS_INVENTORY", Severity = Severity.Info, Category = Id,
                Subject = Path.GetFileName(path), FilePath = path,
                Args = new[] { Path.GetFileName(path), Render(bits), role.Role },
                EnglishText = $"{Path.GetFileName(path)} — {Render(bits)} ({role.Role})." };

        var mains = unique.Where(u => u.role.Role == "main-exe" && u.bits != Bitness.Unknown).ToList();
        bool has64Main = mains.Any(m => m.bits == Bitness.X64);
        bool hasVpm32 = unique.Any(u => u.role.Role is "vpinmame" && u.bits == Bitness.X86);
        bool hasVpm64 = unique.Any(u => u.role.Role is "vpinmame64" || (u.role.Role is "vpinmame" && u.bits == Bitness.X64));

        if (has64Main && !hasVpm64 && hasVpm32)
            yield return new Finding { Code = "BITNESS_MISMATCH_VPM", Severity = Severity.Critical, Category = Id,
                Subject = "VPinMAME",
                EnglishText = "A 64-bit Visual Pinball executable is installed but only a 32-bit VPinMAME.dll was found. " +
                              "64-bit VPX cannot use the 32-bit COM server — ROM tables will fail.",
                FixHint = "Install and register the 64-bit VPinMAME (VPinMAME64.dll) for the 64-bit VPX, or launch the 32-bit VPX for these tables." };
        // (+ hybrid-install and dmddevice64-missing cross-checks)
    }

    private static string Render(Bitness b) => b switch { Bitness.X86 => "32-bit", Bitness.X64 => "64-bit", Bitness.Arm64 => "ARM64", _ => "unknown" };
    private static IEnumerable<string> ResolveScope(InstallLayout layout, string scope) => scope switch {
        "root" => new[] { layout.RootPath },
        "vpinmame" => layout.VPinMameDir is null ? Array.Empty<string>() : new[] { layout.VPinMameDir },
        "tables" => layout.TablesDir is null ? Array.Empty<string>() : new[] { layout.TablesDir },
        _ => new[] { layout.RootPath } };
}
```

---

## 3. Les 3 prompts de rôle (un par IA, pour des points de vue distincts)

### Prompt A — Architecte logiciel senior
> Tu es un architecte logiciel .NET senior. Voici le brief, l'arborescence et le cœur d'un scanner de diagnostic pincab (lecture seule, alpha, solo). Reste strictement dans la philosophie décrite (lecture seule, justesse > quantité, pas de refonte MVVM/DI, pas de dépendances lourdes). Revois : la solidité de l'architecture Check→Finding→Scenario→Repair, la testabilité du cœur, les pièges du futur « Knowledge Pack » data-driven, la gestion des erreurs et de l'annulation, et la robustesse des scanners face à des installs incomplètes. Donne 5 à 8 améliorations concrètes classées par impact, avec le pourquoi. Pas de liste générique — appuie-toi sur le code fourni.

### Prompt B — Designer UX produit
> Tu es un designer UX produit. Voici des captures d'écran et le XAML d'un scanner de diagnostic pincab. Le rapport doit être clair, rassurant, et *partageable* (l'objectif est que les gens postent leur rapport sur les forums). Revois : la hiérarchie visuelle du rapport, la lisibilité des gravités, l'onboarding au premier lancement, la clarté du score de santé, et la friction éventuelle. Reste dans une app desktop dark WPF, une seule couleur d'accent (orange), pas de refonte complète. Donne 5 à 8 améliorations concrètes et priorisées.

### Prompt C — Chef de produit / go-to-market
> Tu es un chef de produit orienté go-to-market pour un logiciel de niche communautaire (flipper virtuel). Voici le brief et la vision (moteur de diagnostic de référence, freemium : scanner gratuit → module Repair payant). Le marché a des installeurs (Baller), des gestionnaires (VPin Studio), des outils de config (DOF Config Tool) — mais personne sur le diagnostic de l'existant. Revois : la crédibilité du positionnement, la roadmap, le modèle freemium, la stratégie d'adoption communautaire (Pincab Passion / VPUniverse), et les risques. Donne un avis franc et 5 recommandations priorisées.
