using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Input;
using Microsoft.Win32;
using PincabToolbox.App.Localization;
using PincabToolbox.Core.Diagnostics;
using PincabToolbox.Core.Models;
using PincabToolbox.Core.Profiles;
using PincabToolbox.Core.Reporting;
using PincabToolbox.Core.Scanning;
using PincabToolbox.Core.Services;
using PincabToolbox.Repair;
using PincabToolbox.Repair.Licensing;

namespace PincabToolbox.App;

public sealed class FindingRow
{
    public required string SevLabel { get; init; }
    public required Brush SevBrush { get; init; }
    public required Brush RowBg { get; init; }
    public required string Category { get; init; }
    public required string Subject { get; init; }
    public required string Message { get; init; }
    public string? FilePath { get; init; }
    public string? FixHint { get; init; }
    public string? Code { get; init; }
    public required string ActionLabel { get; init; }
    public required string ActionKind { get; init; }
    public required string ActionArg { get; init; }
    public required Severity Severity { get; init; }
}

/// <summary>
/// Une carte de cause racine de l'onglet « Causes racines » (maquette 11/08) : badge de gravité,
/// titre, puce de confiance, phrase joueur, phrase d'impact, chaîne causale et pied de carte.
/// Construite par RefreshCauseCards depuis Scenarios.DetectAll — chaque valeur affichée vient
/// d'un résultat de scan réel, jamais d'un gabarit rempli pour faire joli.
/// </summary>
public sealed class CauseCardRow
{
    public required string BadgeText { get; init; }
    public required Brush AccentBrush { get; init; }
    public required Brush BadgeBg { get; init; }
    public required Brush BadgeBorder { get; init; }
    public required string Title { get; init; }
    public required string ConfText { get; init; }
    public required string Player { get; init; }
    public required string Impact { get; init; }
    public required Visibility ImpactVis { get; init; }
    public required IReadOnlyList<CauseChainRow> Chain { get; init; }
    public required Visibility ChainVis { get; init; }
    public required string FootComponents { get; init; }
    public required Visibility FootComponentsVis { get; init; }
    public required string FootTables { get; init; }
    public required Visibility FootTablesVis { get; init; }
    public required string FootCodes { get; init; }
    public required string ManualText { get; init; }
    public required Visibility ManualVis { get; init; }
    public required string StepsLabel { get; init; }
    /// <summary>Codes déclencheurs — « Voir les étapes » ouvre le premier de ces résultats.</summary>
    public required IReadOnlyList<string> Codes { get; init; }
}

/// <summary>
/// Une case de la chaîne causale d'une carte (maquette 11/08). Chaque case est un résultat
/// RÉELLEMENT mesuré qui a déclenché la corrélation (Scenarios filtre par RequiresCode) — jamais
/// une étape décorative, sinon la chaîne raconterait une histoire que le scan n'a pas vérifiée.
/// </summary>
public sealed class CauseChainRow
{
    /// <summary>Vide pour la première case ; « ✕→ » rouge sur la première rupture bon→cassé.</summary>
    public required string Arrow { get; init; }
    public required Brush ArrowBrush { get; init; }
    public required string Label { get; init; }
    public required string Status { get; init; }
    public required Brush StatusBrush { get; init; }
    public required Brush NodeBg { get; init; }
    public required Brush NodeBorder { get; init; }
}

/// <summary>Une ligne des encadrés « Résultats critiques » / « Remarques » — clic = renvoi
/// vers la ligne correspondante de l'onglet Tous les résultats (Code + Subject).</summary>
public sealed class SideRow
{
    public required Brush Dot { get; init; }
    public required string Text { get; init; }
    public required string Sub { get; init; }
    public required string Code { get; init; }
    public required string Subject { get; init; }

    /// <summary>Lot 3 — masque la ligne de sous-texte quand elle est vide (onglet Mises à jour :
    /// VPS_MATCH_SUMMARY/VPS_UNAVAILABLE n'ont pas de Subject). Visible par défaut pour ne rien
    /// changer aux encadrés existants (Critiques/Remarques), qui ont toujours un sous-texte.</summary>
    public Visibility SubVis { get; init; } = Visibility.Visible;
}

/// <summary>Une ligne de « Santé des composants » — uniquement des composants portés par un
/// résultat réel du scan (inventaire bitness, composant manquant, base Popper lue).</summary>
public sealed class CompRow
{
    public required string Name { get; init; }
    public required string Meta { get; init; }
    public required string StatusText { get; init; }
    public required Brush StatusBrush { get; init; }
}

/// <summary>Une ligne du tableau « Tables analysées » — chaque cellule vient d'un résultat de
/// scan ou d'une lecture positive (base Popper) ; « — » = non vérifié sur ce scan.</summary>
public sealed class TableRowVm
{
    public required string Name { get; init; }
    public required string Rom { get; init; }
    public required Brush RomBrush { get; init; }
    public required string B2s { get; init; }
    public required Brush B2sBrush { get; init; }
    public required string Frontend { get; init; }
    public required Brush FeBrush { get; init; }
}

public sealed class DiffRow
{
    public string OldNum { get; init; } = "";
    public string OldText { get; init; } = "";
    public Brush OldBrush { get; init; } = Brushes.Transparent;
    public string NewNum { get; init; } = "";
    public string NewText { get; init; } = "";
    public Brush NewBrush { get; init; } = Brushes.Transparent;
}

/// <summary>
/// LOT H (spec 10/08) — one row of the Repair tab's checklist. Plain/mutable on purpose: WPF's
/// default TwoWay binding on <c>CheckBox.IsChecked</c> writes straight back to
/// <see cref="IsSelected"/>, and the row is only ever read at Apply-click time (not observed live),
/// so <c>INotifyPropertyChanged</c> would be pure ceremony here — same posture as <see cref="FindingRow"/>.
/// </summary>
public sealed class RepairItemRow
{
    public required string ItemId { get; init; }
    public required string Description { get; init; }
    public bool IsSelected { get; set; }

    /// <summary>
    /// False for ManualOnly (no automatable change, e.g. ROM_MISSING) and Locked (a fix exists but
    /// needs a licence) rows — added 13/08 alongside showing them at all (ADR-006 gap). The
    /// checkbox stays visible but disabled for these so the row's presence and its explanation are
    /// never mistaken for "nothing Repair can do here", while still making clear there is nothing
    /// to select and apply.
    /// </summary>
    public bool CanApply { get; init; }
}

/// <summary>
/// 13/08 — one row of the "Réparé" or "Annulé" list, replacing a single raw-plan-ID ListBox. Built
/// from <see cref="PlanSummary"/>, never re-derives facts the engine already computed.
/// </summary>
public sealed class RepairPlanSummaryRow
{
    public required string PlanId { get; init; }
    public required string Description { get; init; }

    /// <summary>Button text — empty for an "Annulé" row, which carries no button at all (see the DataTemplate).</summary>
    public required string UndoLabel { get; init; }

    /// <summary>True only for a "Réparé" row (Applied/PartiallyUndone) — an "Annulé" row has nothing left to undo.</summary>
    public bool CanUndo { get; init; }
}

public partial class MainWindow : Window
{
    private ScanReport? _report;
    // Écran 1 only (free "Repair available" offer) — see RepairOfferBuilder. Always recomputed
    // together with _report so the two never point at different scans.
    private RepairOfferBuilder.Result? _repairResult;
    // Set only when the last scan was a whole-drive scan (TRANSMISSION #14, 10/08) — the real
    // per-install roots found, used to confine Repair correctly (ADR-005/ADR-011) instead of the
    // merged report's synthesized drive-wide RootPath.
    private List<string>? _lastDriveScanRoots;
    private CancellationTokenSource? _cts;
    private readonly Settings _settings = Settings.Load();

    // ───────── LOT H (spec 10/08) — Écran 2, the write path. All decision logic lives in
    // RepairSession (PincabToolbox.Repair, fully unit-tested); this window only calls into it and
    // renders what it returns. See RepairSession's own header comment for why it lives there. ─────────
    private RepairSession? _repairSession;
    private RepairPlan? _repairPlan;
    private PreflightResult? _repairPreflight;
    private List<RepairItemRow> _repairItemRows = new();
    // Re-verified against the embedded key every time the user clicks "Verify" (BtnRepairVerifyLicense_Click)
    // — never trusted just because it was true on a previous click (H.4: never assumed).
    private bool _licensed;

    // Keep in sync with AssemblyInfo/csproj version — same literal ApplyTexts already displayed
    // in the About tab before this change, just named now so BtnCheckUpdate_Click can compare
    // against it too.
    private const string CurrentVersion = "0.1.2";
    private readonly IUpdateChecker _updateChecker = new GitHubUpdateChecker();

    private bool _showCritical = true, _showWarning = true, _showNote = true, _showInfo = true, _showOk = false;
    private string? _sortKey;
    private bool _sortAsc = true;
    private string? _demoRoot;                       // real demo path while the box shows a friendly label
    private System.Windows.Threading.DispatcherTimer? _flashTimer;

    // ── Écran Scanner porté sur la maquette 11/08 — contexte du dernier scan ──
    // Nombre de contrôles réellement configurés sur ce scan (ScanEngine.Scanners.Count, jamais
    // une constante : un scanner ajouté demain doit se compter tout seul).
    private int _scanChecks;
    private bool _scanWasDemo;
    // Lecture POSITIVE de la base PinUP Popper (même requête que CompletenessScanner) pour la
    // colonne Frontend du tableau des tables : null = base absente ou illisible, la colonne
    // affiche alors « — » plutôt qu'une valeur déduite du silence (ADR-010).
    private HashSet<string>? _popperRegistered;

    // Lot 4 : réconciliation du rouge — les deux sources de vérité avaient divergé (App.xaml Critical
    // #E5484D, déjà utilisé par les pastilles et les teintes de ligne, vs ce brosse-miroir #FF6B6E).
    // App.xaml est la source canonique ; c'est donc elle qui gagne (une seule couleur, pas cinq).
    private static readonly Brush BrushCritical = new SolidColorBrush(Color.FromRgb(0xE5, 0x48, 0x4D));
    private static readonly Brush BrushWarning = new SolidColorBrush(Color.FromRgb(0xF5, 0xA5, 0x24));
    // Severity.Note — distinct from Info on purpose (Doctrine Note, HANDOFF §"rendu App"): a heuristic
    // fact is neither a neutral confirmation (Info, blue) nor actionable (Warning, orange). Same violet
    // as App.xaml's NoteSev StaticResource — kept in sync manually, same pattern as the other 4 pairs.
    private static readonly Brush BrushNote = new SolidColorBrush(Color.FromRgb(0xB5, 0x8D, 0xF5));
    private static readonly Brush BrushInfo = new SolidColorBrush(Color.FromRgb(0x3E, 0x9C, 0xF3));
    private static readonly Brush BrushOk = new SolidColorBrush(Color.FromRgb(0x46, 0xC0, 0x6E));

    private static readonly Brush RowCritical = new SolidColorBrush(Color.FromArgb(0x1E, 0xE5, 0x48, 0x4D));
    private static readonly Brush RowWarning = new SolidColorBrush(Color.FromArgb(0x12, 0xF5, 0xA5, 0x24));
    private static readonly Brush RowNote = new SolidColorBrush(Color.FromArgb(0x14, 0xB5, 0x8D, 0xF5));
    private static readonly Brush RowInfo = new SolidColorBrush(Color.FromArgb(0x14, 0x3E, 0x9C, 0xF3));
    private static readonly Brush RowOk = new SolidColorBrush(Color.FromArgb(0x14, 0x46, 0xC0, 0x6E));

    // Cartes de causes racines (maquette 11/08) — teintes des cases de chaîne causale.
    private static readonly Brush BrushDim = new SolidColorBrush(Color.FromRgb(0x9C, 0x9C, 0xAC));
    private static readonly Brush NodeGoodBg = new SolidColorBrush(Color.FromRgb(0x26, 0x26, 0x30));
    private static readonly Brush NodeGoodBorder = new SolidColorBrush(Color.FromArgb(0x52, 0x46, 0xC0, 0x6E));
    private static readonly Brush NodeBadBg = new SolidColorBrush(Color.FromArgb(0x14, 0xE5, 0x48, 0x4D));
    private static readonly Brush NodeBadBorder = new SolidColorBrush(Color.FromArgb(0x66, 0xE5, 0x48, 0x4D));
    private static readonly Brush NodeWarnBg = new SolidColorBrush(Color.FromArgb(0x10, 0xF5, 0xA5, 0x24));
    private static readonly Brush NodeWarnBorder = new SolidColorBrush(Color.FromArgb(0x5C, 0xF5, 0xA5, 0x24));

    private static string SevGlyph(Severity s) => s switch
    {
        Severity.Critical => "✕",
        Severity.Warning => "▲",
        Severity.Note => "✎",
        Severity.Info => "ⓘ",
        _ => "✓",
    };

    private static Brush SevBrushOf(Severity s) => s switch
    {
        Severity.Critical => BrushCritical,
        Severity.Warning => BrushWarning,
        Severity.Note => BrushNote,
        Severity.Info => BrushInfo,
        _ => BrushOk,
    };

    /// <summary>Lot 4 — même mapping que <see cref="SevBrushOf"/> mais pour les teintes de fond de
    /// ligne (RowCritical/RowWarning/…), consolidées ici pour que la liste des résultats et les
    /// badges de carte ne puissent plus diverger sur une seule et même couleur de sévérité.</summary>
    private static Brush RowBgOf(Severity s) => s switch
    {
        Severity.Critical => RowCritical,
        Severity.Warning => RowWarning,
        Severity.Note => RowNote,
        Severity.Info => RowInfo,
        _ => RowOk,
    };

    /// <summary>La couleur de gravité en version voilée (fond / bordure des badges de carte).</summary>
    private static Brush SevTint(Severity s, byte alpha)
    {
        var c = ((SolidColorBrush)SevBrushOf(s)).Color;
        var br = new SolidColorBrush(Color.FromArgb(alpha, c.R, c.G, c.B));
        br.Freeze();
        return br;
    }

    private static readonly Regex UrlRx = new(@"https?://[^\s""'<>)\]]+", RegexOptions.Compiled);

    // Lot 6 — reconnaît "Reading table i/n: ..." (ScanEngine.cs:45, jamais touché ici) pour en tirer
    // un ratio de progression déterminé. Les autres étapes ("Running {scanner}…", ScanEngine.cs:52)
    // n'ont pas de total connu côté App et laissent donc la barre indéterminée.
    private static readonly Regex ScanStepRx = new(@"^Reading table (\d+)/(\d+):", RegexOptions.Compiled);

    private static readonly Brush DiffDel = new SolidColorBrush(Color.FromArgb(0x38, 0xE5, 0x48, 0x4D));
    private static readonly Brush DiffIns = new SolidColorBrush(Color.FromArgb(0x38, 0x46, 0xC0, 0x6E));
    private static readonly Brush DiffMod = new SolidColorBrush(Color.FromArgb(0x38, 0xF5, 0xA5, 0x24));

    public MainWindow()
    {
        InitializeComponent();

        // restore saved language before the first text pass so the UI opens in the right language
        if (!string.IsNullOrEmpty(_settings.Lang)) Loc.SetLang(_settings.Lang!);

        Loc.LanguageChanged += ApplyTexts;
        ApplyTexts();

        // restore last scanned folder, or auto-detect a common VPX install on first run
        if (!string.IsNullOrEmpty(_settings.RepairLicenseKey))
            TxtRepairLicense.Text = _settings.RepairLicenseKey!;   // re-verified on click, never assumed valid just because it was saved

        if (!string.IsNullOrEmpty(_settings.LastRoot) && Directory.Exists(_settings.LastRoot))
            TxtRoot.Text = _settings.LastRoot!;
        else if (string.IsNullOrWhiteSpace(TxtRoot.Text))
        {
            var detected = TryAutoDetectRoot();
            if (detected is not null) TxtRoot.Text = detected;
        }

        // restore window size/position (FitToWorkArea below still guarantees it stays on-screen)
        if (_settings.WindowWidth >= MinWidth && _settings.WindowHeight >= MinHeight)
        {
            Width = _settings.WindowWidth;
            Height = _settings.WindowHeight;
        }
        if (!double.IsNaN(_settings.WindowLeft) && !double.IsNaN(_settings.WindowTop))
        {
            WindowStartupLocation = WindowStartupLocation.Manual;
            Left = _settings.WindowLeft;
            Top = _settings.WindowTop;
        }

        AllowDrop = true;
        DragOver += Window_DragOver;
        Drop += Window_Drop;
        Closing += Window_Closing;
        Loaded += FitToWorkArea;

        if (!_settings.OnboardingSeen)
            OnboardingOverlay.Visibility = Visibility.Visible;

        // H.2 rule 5 — the Undo history must be visible even before any scan/plan this session
        // (it is read from the on-disk journal, which survives closing the app). Best-effort: a
        // fresh install has no journal yet, and any disk issue here must never block startup.
        try { RefreshRepairUndoList(); } catch { /* the Repair tab will just show an empty list */ }
    }

    private void OnbStart_Click(object sender, RoutedEventArgs e)
    {
        OnboardingOverlay.Visibility = Visibility.Collapsed;
        _settings.OnboardingSeen = true;
        _settings.Save();
    }

    private void OnbTutorial_Click(object sender, RoutedEventArgs e)
    {
        OnboardingOverlay.Visibility = Visibility.Collapsed;
        _settings.OnboardingSeen = true;
        _settings.Save();
        MainTabs.SelectedItem = TabTutorial;
    }

    /// <summary>Persist window bounds, last folder and language on close (best-effort).</summary>
    private void Window_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        _settings.Lang = Loc.Lang;
        _settings.LastRoot = TxtRoot.Text.Trim();
        var b = WindowState == WindowState.Normal
            ? new Rect(Left, Top, Width, Height)
            : RestoreBounds;
        _settings.WindowLeft = b.Left;
        _settings.WindowTop = b.Top;
        _settings.WindowWidth = b.Width;
        _settings.WindowHeight = b.Height;
        _settings.Save();
    }

    /// <summary>Accept a folder dragged onto the window as the scan root.</summary>
    private void Window_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private void Window_Drop(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;
        if (e.Data.GetData(DataFormats.FileDrop) is not string[] paths || paths.Length == 0) return;
        var first = paths[0];
        var dir = Directory.Exists(first) ? first : Path.GetDirectoryName(first);
        if (!string.IsNullOrEmpty(dir)) TxtRoot.Text = dir!;
    }

    /// <summary>Best-effort scan for a Visual Pinball install in the usual locations (first run only).</summary>
    private static string? TryAutoDetectRoot()
    {
        string[] candidates =
        {
            @"C:\Visual Pinball",
            @"C:\vPinball\VisualPinball",
            @"C:\Games\Visual Pinball",
            @"C:\VPX",
            @"C:\VisualPinball",
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Visual Pinball"),
        };
        foreach (var c in candidates)
        {
            try
            {
                if (Directory.Exists(c) && Directory.EnumerateFiles(c, "VPinballX*.exe").Any())
                    return c;
            }
            catch { /* skip unreadable candidate */ }
        }
        return null;
    }

    /// <summary>
    /// Keeps the window fully inside the screen work area. On small or high-DPI laptop
    /// displays (e.g. 1080p at 150% scale, ~720px usable height) an 780px window centered
    /// vertically would open with its title bar above the top of the screen — this shrinks
    /// and repositions the window so the title bar is always reachable.
    /// </summary>
    private void FitToWorkArea(object sender, RoutedEventArgs e)
    {
        var wa = SystemParameters.WorkArea;
        if (Width > wa.Width) Width = wa.Width;
        if (Height > wa.Height) Height = wa.Height;
        if (Left < wa.Left) Left = wa.Left;
        if (Top < wa.Top) Top = wa.Top;
        if (Left + Width > wa.Right) Left = System.Math.Max(wa.Left, wa.Right - Width);
        if (Top + Height > wa.Bottom) Top = System.Math.Max(wa.Top, wa.Bottom - Height);
    }

    private void ApplyTexts()
    {
        // 14/08/2026: was one "FR / EN" toggle button, then a single button showing the active
        // language and cycling on click — Maxime's feedback: not discoverable, a user who didn't
        // already know had no way to find the other languages. Now 3 buttons, always visible,
        // side by side; UpdateLangButtons highlights whichever one is active.
        UpdateLangButtons();
        Title = Loc.Get("app.title");
        HeaderTagline.Text = Loc.Get("about.tagline");
        TabScannerHeader.Text = Loc.Get("tab.scanner");
        TabDiffHeader.Text = Loc.Get("tab.diff");
        TabAboutHeader.Text = Loc.Get("tab.about");
        TabRepairHeader.Text = Loc.Get("tab.repair");
        TabTutorialHeader.Text = Loc.Get("tab.tutorial");
        LblRoot.Text = Loc.Get("scan.root");
        BtnBrowse.Content = Loc.Get("scan.browse");
        BtnDemo.Content = Loc.Get("scan.demo");
        BtnScan.Content = _cts is null ? Loc.Get("scan.start") : Loc.Get("scan.running");
        BtnCancel.Content = Loc.Get("scan.cancel");
        BtnExport.Content = Loc.Get("scan.export");
        BtnCopyForum.Content = Loc.Get("scan.copyforum");
        LblPlaceholder.Text = _report is null ? Loc.Get("scan.placeholder") : "";
        if (_report is null)
        {
            ChipCritical.Text = "0 " + Loc.Get("filter.critical");
            ChipWarning.Text = "0 " + Loc.Get("filter.warning");
            ChipNote.Text = "0 " + Loc.Get("filter.note");
            ChipInfo.Text = "0 " + Loc.Get("filter.info");
            ChipOk.Text = "0 " + Loc.Get("filter.ok");
        }
        ColSeverity.Header = Loc.Get("col.severity");
        ColCategory.Header = Loc.Get("col.category");
        ColSubject.Header = Loc.Get("col.subject");
        ColMessage.Header = Loc.Get("col.message");
        ColAction.Header = Loc.Get("col.action");
        SearchHint.Text = Loc.Get("search.hint");
        LblDiffOld.Text = Loc.Get("diff.old");
        LblDiffNew.Text = Loc.Get("diff.new");
        BtnCompare.Content = Loc.Get("diff.compare");
        DiffEmpty.Text = Loc.Get("diff.empty");
        AboutTagline.Text = Loc.Get("about.tagline");
        AboutBody.Text = Loc.Get("about.body");
        AboutVersion.Text = Loc.Get("about.version") + " " + CurrentVersion;
        BtnCheckUpdate.Content = Loc.Get("about.checkupdate");
        BtnGotoRepair.Content = Loc.Get("repair.goto");

        TutorialTitle.Text = Loc.Get("tutorial.title");
        TutorialIntro.Text = Loc.Get("tutorial.intro");
        Tutorial1Title.Text = Loc.Get("tutorial.step1.title");
        Tutorial1Body.Text = Loc.Get("tutorial.step1.body");
        Tutorial2Title.Text = Loc.Get("tutorial.step2.title");
        Tutorial2Body.Text = Loc.Get("tutorial.step2.body");
        Tutorial3Title.Text = Loc.Get("tutorial.step3.title");
        Tutorial3Body.Text = Loc.Get("tutorial.step3.body");
        Tutorial4Title.Text = Loc.Get("tutorial.step4.title");
        Tutorial4Body.Text = Loc.Get("tutorial.step4.body");
        Tutorial5Title.Text = Loc.Get("tutorial.step5.title");
        Tutorial5Body.Text = Loc.Get("tutorial.step5.body");

        // Écran Scanner porté sur la maquette 11/08 — libellés statiques ; les compteurs et
        // contenus dépendants du scan sont repris par RefreshList() en fin de méthode.
        StabCausesHeader.Text = Loc.Get("stab.causes");
        StabResultsHeader.Text = Loc.Get("stab.results");
        StabComponentsHeader.Text = Loc.Get("stab.components");
        StabTablesHeader.Text = Loc.Get("stab.tables");
        StabSystemHeader.Text = Loc.Get("stab.system");
        StabUpdatesHeader.Text = Loc.Get("stab.updates");
        BtnRepairCardOpen.Content = Loc.Get("repaircard.open");
        SideCompNote.Text = CompTabNote.Text = Loc.Get("side.comp.note");
        TblHTable.Text = TblHTable2.Text = Loc.Get("tbl.h.table");
        TblHRom.Text = TblHRom2.Text = Loc.Get("tbl.h.rom");
        TblHB2s.Text = TblHB2s2.Text = Loc.Get("tbl.h.b2s");
        TblHFe.Text = TblHFe2.Text = Loc.Get("tbl.h.frontend");
        if (_report is null)
        {
            CompTabEmpty.Text = Loc.Get("sys.needscan");
            TablesTabEmpty.Text = Loc.Get("sys.needscan");
            UpdatesTabEmpty.Text = Loc.Get("sys.needscan");
        }
        RefreshSystemTab();

        RepairIntro.Text = Loc.Get("repair.intro");
        LblRepairLicense.Text = Loc.Get("repair.license.label");
        TxtRepairLicense.ToolTip = Loc.Get("repair.license.hint");
        BtnRepairVerifyLicense.Content = Loc.Get("repair.license.verify");
        BtnRepairBuildPlan.Content = Loc.Get("repair.plan.build");
        ChkRepairSelectAll.Content = Loc.Get("repair.selectall");
        BtnRepairApply.Content = Loc.Get("repair.apply.button");
        BtnRescore.Content = Loc.Get("repair.rescore.button");
        LblRepairDone.Text = Loc.Get("repair.done.label");
        RepairDoneEmpty.Text = Loc.Get("repair.done.empty");
        LblRepairUndone.Text = Loc.Get("repair.undone.label");
        RepairUndoneEmpty.Text = Loc.Get("repair.undone.empty");
        if (_repairSession is not null) RefreshRepairUndoList();   // re-render row text (button label included) in the new language
        if (string.IsNullOrEmpty(RepairPlanStatus.Text)) RepairPlanStatus.Text = Loc.Get("repair.needscan");
        OnbTitle.Text = Loc.Get("onb.title");
        OnbLead.Text = Loc.Get("onb.lead");
        OnbP1.Text = Loc.Get("onb.p1");
        OnbP2.Text = Loc.Get("onb.p2");
        OnbP3.Text = Loc.Get("onb.p3");
        OnbStart.Content = Loc.Get("onb.start");
        OnbTutorial.Content = Loc.Get("onb.tutorial");
        if (string.IsNullOrEmpty(LblStatus.Text) || LblStatus.Text == "Prêt." || LblStatus.Text == "Ready.")
            LblStatus.Text = Loc.Get("status.ready");
        if (_report is not null) RefreshList();
    }

    private void BtnLangEn_Click(object sender, RoutedEventArgs e) => Loc.SetLang("en");
    private void BtnLangFr_Click(object sender, RoutedEventArgs e) => Loc.SetLang("fr");
    private void BtnLangEs_Click(object sender, RoutedEventArgs e) => Loc.SetLang("es");

    /// <summary>Highlights whichever of the 3 language buttons is active (AccentButton style,
    /// same visual language used elsewhere for a selected/primary action) and leaves the other
    /// two in the plain default Button style.</summary>
    private void UpdateLangButtons()
    {
        var active = (Style)FindResource("AccentButton");
        // Fix build (18/08) : "Button" n'était pas qualifié — CS0246 chez Maxime (pas d'implicit
        // using WPF actif sur sa machine), alors que le reste du fichier qualifie déjà
        // System.Windows.Controls.Button partout ailleurs (RowAction_Click, etc.). Bug préexistant
        // du 13/08, sans rapport avec les lots 1-6 de la refonte.
        var inactive = (Style)FindResource(typeof(System.Windows.Controls.Button));
        BtnLangEn.Style = Loc.Lang == "en" ? active : inactive;
        BtnLangFr.Style = Loc.Lang == "fr" ? active : inactive;
        BtnLangEs.Style = Loc.Lang == "es" ? active : inactive;
    }

    /// <summary>
    /// The one and only network call in the app (see <see cref="GitHubUpdateChecker"/>) — fires
    /// exclusively on this click, never automatically. Disables the button for the duration so a
    /// slow/offline check can't be fired twice, and always ends in a result text (never a
    /// crash/hang), including when the cab PC has no internet at all.
    /// </summary>
    private async void BtnCheckUpdate_Click(object sender, RoutedEventArgs e)
    {
        BtnCheckUpdate.IsEnabled = false;
        UpdateResultText.Inlines.Clear();
        UpdateResultText.Text = Loc.Get("about.update.checking");

        var result = await _updateChecker.CheckAsync(CancellationToken.None);

        UpdateResultText.Text = "";
        UpdateResultText.Inlines.Clear();

        if (!result.Success || result.LatestVersion is null || result.ReleaseUrl is null)
        {
            UpdateResultText.Text = Loc.Get("about.update.error");
        }
        else if (AppVersionCompare.IsNewer(result.LatestVersion, CurrentVersion))
        {
            var link = new System.Windows.Documents.Hyperlink(new System.Windows.Documents.Run(
                string.Format(Loc.Get("about.update.available"), result.LatestVersion)))
            {
                NavigateUri = new Uri(result.ReleaseUrl),
            };
            link.Click += (_, _) =>
            {
                try { Process.Start(new ProcessStartInfo(result.ReleaseUrl) { UseShellExecute = true }); }
                catch { /* best-effort — never let a browser-launch failure crash the app */ }
            };
            UpdateResultText.Inlines.Add(link);
        }
        else
        {
            UpdateResultText.Text = string.Format(Loc.Get("about.update.uptodate"), CurrentVersion);
        }

        BtnCheckUpdate.IsEnabled = true;
    }

    // ---------------- scanner ----------------

    private void BtnBrowse_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFolderDialog { Title = Loc.Get("scan.root") };
        if (dlg.ShowDialog(this) == true)
            TxtRoot.Text = dlg.FolderName;
    }

    // Keeps the demo state and the demo-button prominence in sync with the folder box.
    private void TxtRoot_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        if (TxtRoot.Text != Loc.Get("scan.demolabel")) _demoRoot = null;
        if (BtnDemo is not null)
            BtnDemo.Opacity = Directory.Exists(TxtRoot.Text) ? 0.5 : 1.0;
    }

    // Brief inline confirmation on a button (e.g. "✓ Copied"), then restores its label.
    private void FlashButton(System.Windows.Controls.Button btn, string doneText, string restoreText)
    {
        btn.Content = doneText;
        _flashTimer?.Stop();
        _flashTimer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1600) };
        _flashTimer.Tick += (s, ev) => { btn.Content = restoreText; _flashTimer!.Stop(); };
        _flashTimer.Start();
    }

    private void BtnDemo_Click(object sender, RoutedEventArgs e)
    {
        var demo = Path.Combine(AppContext.BaseDirectory, "DemoData", "install");
        if (!Directory.Exists(demo))
        {
            MessageBox.Show(this, "DemoData folder missing next to the exe.", "Pincab Toolbox");
            return;
        }
        _demoRoot = demo;
        TxtRoot.Text = Loc.Get("scan.demolabel");   // friendly label; real path kept in _demoRoot
        BtnScan_Click(sender, e);
    }

    /// <summary>True for a bare drive root ("C:\", "D:\"), false for any folder under it — the
    /// same test Windows itself uses (a drive root is the only DirectoryInfo with no Parent).</summary>
    private static bool IsDriveRoot(string path)
    {
        try { return new DirectoryInfo(path).Parent is null; }
        catch { return false; }
    }

    /// <summary>
    /// Lot 6 — surface les étapes nommées que ScanEngine rapporte déjà via IProgress&lt;string&gt;
    /// (jamais touché ici, ni sa signature ni ScanEngine lui-même) : le libellé d'étape remplace le
    /// texte de statut avec un court fondu (180 ms, jamais bloquant — une BeginAnimation ne suspend
    /// jamais le thread UI ni ne peut se trouver sur le chemin d'un MessageBox/dialogue). La barre
    /// devient déterminée quand le message porte un ratio "i/n" réel ("Reading table i/n: …"),
    /// repasse indéterminée pour les étapes sans total connu ("Running {scanner}…", recherche
    /// d'installs sur un scan disque entier).
    /// </summary>
    private void ReportScanProgress(string message)
    {
        LblStatus.Text = message;
        LblStatus.FontWeight = FontWeights.SemiBold;   // reset to Normal in BtnScan_Click's finally

        var m = ScanStepRx.Match(message);
        if (m.Success && int.TryParse(m.Groups[1].Value, out var i) && int.TryParse(m.Groups[2].Value, out var n) && n > 0)
        {
            ScanProgress.IsIndeterminate = false;
            ScanProgress.Value = 100.0 * Math.Min(i, n) / n;
        }
        else
        {
            ScanProgress.IsIndeterminate = true;
        }

        var fade = new System.Windows.Media.Animation.DoubleAnimation(0.35, 1.0, TimeSpan.FromMilliseconds(180));
        LblStatus.BeginAnimation(OpacityProperty, fade);
    }

    private async void BtnScan_Click(object sender, RoutedEventArgs e) => await RunScanAsync(sender, e);

    /// <summary>
    /// LOT Rescore (18/08): extracted so "Revoir mon score" (<see cref="BtnRescore_Click"/>) can
    /// await the exact same scan path BtnScan_Click already used — same TxtRoot/_demoRoot, same
    /// profile, same scanner list, nothing duplicated — instead of writing a second, divergent copy.
    /// BtnScan_Click itself is now a thin fire-and-forget wrapper around this method, unchanged in
    /// every other respect. Returns whether the scan actually completed and <see cref="_report"/> was
    /// updated (false on cancel or failure), so a caller that needs the fresh score afterwards knows
    /// not to trust a stale report.
    /// </summary>
    private async Task<bool> RunScanAsync(object sender, RoutedEventArgs e)
    {
        if (_cts is not null) return false;
        var root = (_demoRoot is not null && TxtRoot.Text == Loc.Get("scan.demolabel"))
            ? _demoRoot : TxtRoot.Text.Trim();
        if (root.Length == 0 || !Directory.Exists(root))
        {
            LblStatus.Text = Loc.Get("scan.placeholder");
            return false;
        }

        Profile profile;
        try
        {
            profile = Profile.Load(Path.Combine(AppContext.BaseDirectory, "profiles", "vpx-popper.json"));
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "profiles/vpx-popper.json: " + ex.Message, "Pincab Toolbox");
            return false;
        }

        _cts = new CancellationTokenSource();
        BtnScan.IsEnabled = false;
        BtnScan.Content = Loc.Get("scan.running");
        BtnCancel.Visibility = Visibility.Visible;
        ScanProgress.Visibility = Visibility.Visible;
        // Lot 6 : repart déterminée=false à chaque scan — sinon un scan qui enchaîne directement sur
        // la phase "Running {scanner}…" hériterait de la barre déterminée laissée par le scan précédent.
        ScanProgress.IsIndeterminate = true;
        ScanProgress.Value = 0;
        BtnExport.IsEnabled = false;
        BtnCopyForum.IsEnabled = false;
        LblPlaceholder.Text = "";

        // TRANSMISSION #14 (10/08) — "le scanner doit lire tout le disque, pas fichier par
        // fichier". If the user pointed the picker at a bare drive root (e.g. "C:\") instead of a
        // specific pincab folder, that means every install on that drive, not one. No new UI: the
        // existing root textbox is the only entry point, this just recognizes a drive root the
        // same way Windows does (DirectoryInfo.Parent is null only for a drive root).
        var isWholeDrive = IsDriveRoot(root);

        var progress = new Progress<string>(ReportScanProgress);
        var completed = false;
        try
        {
            var ct = _cts.Token;
            var (report, checksConfigured) = await Task.Run(async () =>
            {
                var vps = await new VpsDatabase(profile.UpdateSource).LoadAsync(ct).ConfigureAwait(false);
                var engine = new ScanEngine()
                    .Add(new RomValidatorScanner())
                    .Add(new BitnessScanner())
                    .Add(new CompletenessScanner())
                    .Add(new CompatibilityScanner())
                    .Add(new VpxVersionScanner())
                    .Add(new BlockedFileScanner())
                    .Add(new DependencyScanner())
                    .Add(new DiskSpaceScanner())
                    .Add(new LegacyTableScanner())
                    .Add(new PinupDisplayZombieScanner())
                    .Add(new DisplaySetupScanner())
                    .Add(new OrphanedMediaScanner())
                    .Add(new UpdateWatcherScanner(vps))
                    .Add(new AliasLoopScanner())
                    .Add(new NvramScanner())
                    .Add(new AltColorScanner())
                    .Add(new AltSoundScanner())
                    .Add(new ScreenTopologyScanner())
                    .Add(new JunctionScanner())
                    .Add(new DirectB2sScanner())
                    .Add(new PopperPlaylistScanner())
                    // Tier B (handoff Sonnet 5, 06/08) — tous Severity.Note (ADR-010 Doctrine).
                    .Add(new AudioStateScanner())
                    .Add(new DpiScalingScanner())
                    .Add(new DmdComPortScanner())
                    .Add(new LocaleSeparatorScanner())
                    .Add(new ConfigPhantomScanner())
                    // Lot communauté 10/08 (LOT A→G) — voir docs/SPEC-lot-communaute-2026-08-10.md.
                    .Add(new ComHealthScanner())
                    .Add(new ChainBitnessScanner())
                    .Add(new DmdConfigScanner())
                    .Add(new FeatureEnabledScanner())
                    .Add(new ScreenResUnparsedScanner())
                    .Add(new NvramWritabilityScanner())
                    // Lot scanner 18/08 — les 4 derniers détecteurs de l'audit du 05/08
                    // (docs/PROMPT-session-lot-complet-2026-08-18.md). Une ligne = un scanner,
                    // comme tous les précédents, pour que Maxime puisse en désactiver un seul.
                    .Add(new GlobalConfigB2SScanner())
                    .Add(new FontDependencyScanner())
                    .Add(new HardcodedPathScanner())
                    .Add(new ScriptDoctorScanner());
                // Le nombre de contrôles remonte avec le rapport (ligne méta « Contrôles N / N ») —
                // compté sur le moteur réel, jamais une constante à maintenir à la main.
                if (!isWholeDrive) return (engine.Run(root, profile, progress, ct), engine.Scanners.Count);

                var driveReport = engine.RunAcrossDrive(root, profile, progress, ct);
                _lastDriveScanRoots = driveReport.Reports.Select(r => r.Layout.RootPath).ToList();
                return (driveReport.ToMergedScanReport(), engine.Scanners.Count);
            }, ct);

            _report = report;
            _scanChecks = checksConfigured;
            _scanWasDemo = _demoRoot is not null && root == _demoRoot;
            // Lecture positive de la base Popper pour la colonne Frontend du tableau des tables
            // (même requête que CompletenessScanner) — null si base absente ou illisible.
            _popperRegistered = await Task.Run(() => LoadPopperRegistered(report.Layout));
            // Bonus surface on top of the free scan — RepairOfferBuilder.Build never throws
            // (returns null on any failure), so it can never take the scan report down with it.
            // Whole-drive scans MUST pass the real per-install roots (ADR-005/ADR-011, 10/08) —
            // report.Layout.RootPath there is the synthesized drive root ("C:\"), and confining
            // Repair to that would let it validate a write target anywhere on the whole drive.
            _repairResult = isWholeDrive
                ? await Task.Run(() => RepairOfferBuilder.Build(report, _lastDriveScanRoots ?? Enumerable.Empty<string>()))
                : await Task.Run(() => RepairOfferBuilder.Build(report));
            RefreshList();
            LblStatus.Text = string.Format(Loc.Get("status.done"), report.Findings.Count,
                report.Count(Severity.Critical), report.Count(Severity.Warning), report.Count(Severity.Info),
                report.Count(Severity.Note));
            if (report.Layout.VpxTables.Count == 0)
            {
                LblPlaceholder.Text = Loc.Get("scan.hint.notables");
                LblPlaceholder.Visibility = Visibility.Visible;   // RefreshList vient de le replier au profit de la ligne méta
            }
            BtnExport.IsEnabled = true;
            BtnCopyForum.IsEnabled = true;
            completed = true;
        }
        catch (OperationCanceledException)
        {
            LblStatus.Text = Loc.Get("status.ready");
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Pincab Toolbox");
        }
        finally
        {
            _cts = null;
            BtnScan.IsEnabled = true;
            BtnScan.Content = Loc.Get("scan.start");
            BtnCancel.Visibility = Visibility.Collapsed;
            ScanProgress.Visibility = Visibility.Collapsed;
            // Lot 6 : le renfort visuel des étapes (SemiBold) ne vaut que pendant le scan.
            LblStatus.FontWeight = FontWeights.Normal;
        }
        return completed;
    }

    /// <summary>
    /// LOT Rescore (18/08) — offered on <see cref="RepairApplyStatus"/> after a successful or
    /// partially successful Apply (see <see cref="BtnRepairApply_Click"/>): re-runs the SAME scan
    /// (same root/profile, via <see cref="RunScanAsync"/>) and shows old→new score, honestly, even
    /// when the score did not improve. Never invents or estimates a delta — both numbers come
    /// straight from <see cref="ScanReport.Score"/>, before and after the real rescan.
    /// </summary>
    private async void BtnRescore_Click(object sender, RoutedEventArgs e)
    {
        if (_report is null) return;
        var oldScore = _report.Score;

        BtnRescore.IsEnabled = false;
        try
        {
            var completed = await RunScanAsync(sender, e);
            if (!completed || _report is null) return;   // cancelled/failed: nothing honest to show

            var newScore = _report.Score;
            RescoreStatus.Text = string.Format(Loc.Get("repair.rescore.result"), oldScore, newScore);
            RescoreStatus.Visibility = Visibility.Visible;
        }
        finally
        {
            BtnRescore.IsEnabled = true;
        }
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e) => _cts?.Cancel();

    private void PillCritical_Click(object sender, MouseButtonEventArgs e)
    {
        _showCritical = !_showCritical;
        PillCritical.Opacity = _showCritical ? 1.0 : 0.4;
        if (_report is not null) RefreshList();
    }

    private void PillWarning_Click(object sender, MouseButtonEventArgs e)
    {
        _showWarning = !_showWarning;
        PillWarning.Opacity = _showWarning ? 1.0 : 0.4;
        if (_report is not null) RefreshList();
    }

    private void PillNote_Click(object sender, MouseButtonEventArgs e)
    {
        _showNote = !_showNote;
        PillNote.Opacity = _showNote ? 1.0 : 0.4;
        if (_report is not null) RefreshList();
    }

    private void PillInfo_Click(object sender, MouseButtonEventArgs e)
    {
        _showInfo = !_showInfo;
        PillInfo.Opacity = _showInfo ? 1.0 : 0.4;
        if (_report is not null) RefreshList();
    }

    private void PillOk_Click(object sender, MouseButtonEventArgs e)
    {
        _showOk = !_showOk;
        PillOk.Opacity = _showOk ? 1.0 : 0.4;
        if (_report is not null) RefreshList();
    }

    private void RowAction_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as System.Windows.Controls.Button)?.DataContext is FindingRow row) DoRowAction(row);
    }

    // Same action as the grid button, driven from the detail panel's own button.
    private void DetailAction_Click(object sender, RoutedEventArgs e)
    {
        if (ListFindings.SelectedItem is FindingRow row) DoRowAction(row);
    }

    private void DoRowAction(FindingRow row)
    {
        try
        {
            switch (row.ActionKind)
            {
                case "url":
                    Process.Start(new ProcessStartInfo(row.ActionArg) { UseShellExecute = true });
                    break;
                case "folder":
                    var p = row.ActionArg;
                    if (File.Exists(p))
                    {
                        Process.Start(new ProcessStartInfo("explorer.exe", "/select,\"" + p + "\"") { UseShellExecute = true });
                    }
                    else
                    {
                        var dir = Directory.Exists(p) ? p : Path.GetDirectoryName(p);
                        if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
                            Process.Start(new ProcessStartInfo("explorer.exe", "\"" + dir + "\"") { UseShellExecute = true });
                    }
                    break;
                default:
                    Clipboard.SetText(Public(row.ActionArg));   // scrub avant presse-papiers, comme les exports (ADR-003)
                    LblStatus.Text = Loc.Get("action.copied");
                    break;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Pincab Toolbox");
        }
    }

    private void RefreshList()
    {
        if (_report is null) return;

        ChipCritical.Text = $"{_report.Count(Severity.Critical)} {Loc.Get("filter.critical")}";
        ChipWarning.Text = $"{_report.Count(Severity.Warning)} {Loc.Get("filter.warning")}";
        ChipNote.Text = $"{_report.Count(Severity.Note)} {Loc.Get("filter.note")}";
        // Lot 3 : la pastille Info ne compte plus les findings "updates" (nouvel onglet dédié) —
        // sinon elle promettrait des lignes qui ne sont plus dans "Tous les résultats".
        var infoCountExUpdates = _report.Findings.Count(f => f.Severity == Severity.Info && f.Category != "updates");
        ChipInfo.Text = $"{infoCountExUpdates} {Loc.Get("filter.info")}";
        ChipOk.Text = $"{_report.Count(Severity.Ok)} {Loc.Get("filter.ok")}";

        // health score chip
        ScoreChip.Visibility = Visibility.Visible;
        ScoreValue.Text = _report.Score.ToString();
        ScoreGrade.Text = _report.Grade;
        ScoreStatus.Text = Loc.Get(_report.Score >= 90 ? "score.a" : _report.Score >= 70 ? "score.b" : _report.Score >= 40 ? "score.c" : "score.f");
        var scoreBrush = _report.Score >= 90 ? BrushOk : _report.Score >= 70 ? BrushWarning : BrushCritical;
        ScoreValue.Foreground = scoreBrush;
        ScoreGrade.Foreground = scoreBrush;

        // Arc de la jauge (bandeau, maquette 11/08). StrokeDashArray se compte en MULTIPLES de
        // l'épaisseur du trait, pas en pixels : circonférence (2·π·45) ÷ épaisseur (8) = 35.34
        // unités pour un tour complet. Le second segment, volontairement énorme, garantit qu'il
        // n'y a jamais de second tiret visible. Si le diamètre ou l'épaisseur de l'Ellipse
        // changent dans MainWindow.xaml, cette constante doit changer avec eux.
        const double fullTurn = 35.34;
        ScoreArc.Stroke = scoreBrush;
        ScoreArc.StrokeDashArray = new DoubleCollection { fullTurn * Math.Max(0, Math.Min(100, _report.Score)) / 100.0, 1000 };

        // Accroche : ce que l'utilisateur doit retenir en une ligne. Le nombre de bloquants informe,
        // là où la seule note "F" juge — voir docs/REVUE-maquettes-scanner-2026-08-11.md.
        var blocking = _report.Count(Severity.Critical);
        HeroHeadline.Text = blocking switch
        {
            0 => Loc.Get("hero.ok"),
            1 => Loc.Get("hero.blocking.one"),
            _ => string.Format(Loc.Get("hero.blocking.many"), blocking),
        };
        HeroHeadline.Foreground = blocking > 0 ? BrushCritical : BrushOk;

        // ── Écran porté sur la maquette 11/08 : ligne méta, cartes de causes racines, carte
        // réparation, colonne de droite, tableau des tables, onglet Système, compteurs d'onglets.
        // Tout est reconstruit ensemble à chaque scan (et à chaque bascule de langue) pour que
        // les vues ne divergent jamais du même ScanReport.
        RefreshMetaRow();

        var present = new HashSet<string>(_report.Findings.Select(f => f.Code));
        var scenarios = Scenarios.DetectAll(present, Loc.Lang);
        // Sous-titre du bandeau : le nombre de causes de fond réellement détectées ; sinon la
        // phrase de score existante. Pas de « tout se règle en cascade » — on n'affirme que la
        // relation cause → symptômes, pas la disparition de résultats non liés (ADR-010).
        if (scenarios.Count > 0)
            ScoreStatus.Text = scenarios.Count == 1
                ? Loc.Get("hero.causes.one")
                : string.Format(Loc.Get("hero.causes.many"), scenarios.Count);

        var causeCount = RefreshCauseCards(scenarios);
        RefreshRepairCard();
        var compRows = BuildComponentRows();
        var tableRows = BuildTableRows();
        RefreshSideBoxes(compRows);
        RefreshTablesViews(tableRows);
        RefreshSystemTab();
        var updatesCount = RefreshUpdatesTab();
        RefreshInnerTabHeaders(causeCount, compRows.Count, tableRows.Count, updatesCount);

        // Exhaustive over all 5 Severity values on purpose (not "_ => _showOk"): a wildcard here once
        // silently routed Note through the Ok toggle (hidden by default, wrong bucket) — the exact
        // "switch App non exhaustif" risk the handoff warned about. Kept explicit so a future Severity
        // value fails loudly (falls to _showOk, same as today) rather than silently misclassifying.
        bool Show(Severity s) => s switch
        {
            Severity.Critical => _showCritical,
            Severity.Warning => _showWarning,
            Severity.Note => _showNote,
            Severity.Info => _showInfo,
            Severity.Ok => _showOk,
            _ => _showOk,
        };

        // Rolled(), not Ordered(): repetitive per-table findings collapse to one counted row
        // so the handful that matter stay visible. Criticals are never collapsed.
        // The full text export keeps every line. (FIELD-LOG 2026-08-03.)
        // Lot 3 : Category=="updates" en est exclu — ces findings vivent désormais uniquement dans
        // l'onglet dédié Mises à jour (RefreshUpdatesTab), pour désencombrer cette liste sans rien
        // masquer (retour Joey Mahon, FIELD-LOG). Ni le scanner ni les codes/sévérités ne changent.
        var rows = _report.Rolled()
            .Where(f => f.Category != "updates")
            .Where(f => Show(f.Severity))
            .Select(f =>
            {
                var msg = Loc.FindingText(f);
                var um = UrlRx.Match(msg);
                string actKind, actArg, actLabel;
                if (um.Success)
                {
                    actKind = "url";
                    actArg = um.Value.TrimEnd('.', ',', ')', ']', '»');
                    actLabel = Loc.Get("action.update");
                }
                else if (!string.IsNullOrEmpty(f.FilePath))
                {
                    actKind = "folder";
                    actArg = f.FilePath!;
                    actLabel = Loc.Get("action.folder");
                }
                else
                {
                    actKind = "copy";
                    actArg = msg;
                    actLabel = Loc.Get("action.copy");
                }
                return new FindingRow
                {
                    Severity = f.Severity,
                    SevLabel = Loc.SeverityLabel(f.Severity),
                    // Lot 4 : appelle désormais SevBrushOf/RowBgOf au lieu de dupliquer le switch —
                    // même mapping exhaustif qu'avant (tout ce qui n'est pas explicitement Critical/
                    // Warning/Note/Info retombe sur Ok, jamais l'inverse), une seule définition.
                    SevBrush = SevBrushOf(f.Severity),
                    RowBg = RowBgOf(f.Severity),
                    Category = Loc.Get("cat." + f.Category),
                    Subject = f.Subject,
                    Message = msg,
                    FilePath = f.FilePath,
                    FixHint = Loc.FixHintText(f),
                    Code = f.Code,
                    ActionLabel = actLabel,
                    ActionKind = actKind,
                    ActionArg = actArg,
                };
            })
            .ToList();

        // text search across subject / details / module
        var q = TxtSearch.Text?.Trim();
        if (!string.IsNullOrEmpty(q))
        {
            rows = rows.Where(r =>
                r.Subject.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                r.Message.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                r.Category.Contains(q, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        // optional column sort (null keeps the default severity order)
        if (_sortKey is not null)
        {
            IEnumerable<FindingRow> s = _sortKey switch
            {
                "sev"  => rows.OrderBy(r => (int)r.Severity),
                "cat"  => rows.OrderBy(r => r.Category, StringComparer.OrdinalIgnoreCase),
                "subj" => rows.OrderBy(r => r.Subject, StringComparer.OrdinalIgnoreCase),
                "msg"  => rows.OrderBy(r => r.Message, StringComparer.OrdinalIgnoreCase),
                _      => rows,
            };
            if (!_sortAsc) s = s.Reverse();
            rows = s.ToList();
        }

        ListFindings.ItemsSource = rows;
        DetailPanel.Visibility = Visibility.Collapsed;
    }

    // ═════════════ Écran Scanner porté sur la maquette 11/08 (docs/maquette-scanner-2026-08-11.html) ═════════════
    // Règle unique de toutes ces méthodes : chaque valeur affichée vient d'un résultat de scan
    // réel (ou d'une lecture positive vérifiée) — une case sans mesure affiche « — », jamais une
    // valeur plausible (ADR-010, docs/REVUE-maquettes-scanner-2026-08-11.md).

    /// <summary>Ligne méta sous le bandeau : mode, horodatage, durée, contrôles, tables.</summary>
    private void RefreshMetaRow()
    {
        MetaMode.Text = $"{Loc.Get("meta.mode")} {Loc.Get(_scanWasDemo ? "meta.mode.demo" : "meta.mode.folder")}";
        MetaStarted.Text = $"{Loc.Get("meta.started")} {_report!.StartedAt.ToLocalTime():dd/MM/yyyy HH:mm}";
        var dur = _report.FinishedAt - _report.StartedAt;
        var hasDur = _report.FinishedAt != default && dur > TimeSpan.Zero;
        MetaDuration.Text = hasDur ? $"{Loc.Get("meta.duration")} {dur:hh\\:mm\\:ss}" : "";
        MetaDuration.Visibility = hasDur ? Visibility.Visible : Visibility.Collapsed;
        MetaChecks.Text = _scanChecks > 0 ? $"{Loc.Get("meta.checks")} {_scanChecks} / {_scanChecks}" : "";
        MetaChecks.Visibility = _scanChecks > 0 ? Visibility.Visible : Visibility.Collapsed;
        MetaTables.Text = $"{Loc.Get("meta.tablecount")} {_report.Layout.VpxTables.Count}";
        MetaRow.Visibility = Visibility.Visible;
        LblPlaceholder.Visibility = Visibility.Collapsed;
    }

    /// <summary>
    /// Cartes de causes racines. Une carte par scénario réellement détecté (Scenarios.DetectAll),
    /// triées gravité réelle puis confiance ; en l'absence de scénario, une carte de repli est
    /// construite depuis le résultat le plus grave — même comportement que l'ancien bandeau
    /// priorité, au format carte. Retourne le nombre de cartes (compteur d'onglet).
    /// </summary>
    private int RefreshCauseCards(IReadOnlyList<ScenarioMatch> scenarios)
    {
        var tableNames = new HashSet<string>(
            _report!.Layout.VpxTables.Select(p => Path.GetFileNameWithoutExtension(p)),
            StringComparer.OrdinalIgnoreCase);

        var cards = new List<(Severity Sev, int Conf, CauseCardRow Row)>();
        foreach (var sc in scenarios)
        {
            var trig = _report.Findings.Where(f => sc.TriggeredBy.Contains(f.Code)).ToList();
            // Gravité du badge = la gravité MAX réellement mesurée parmi les déclencheurs — jamais
            // une gravité déclarée par le scénario (sur le démo, « Intégration frontend » sort en
            // À noter/Info, pas en Avertissement : le badge doit le dire tel quel).
            var sev = trig.Count > 0 ? trig.Max(f => f.Severity) : Severity.Note;
            var subjects = trig.Select(f => f.Subject).Where(s => !string.IsNullOrEmpty(s))
                .Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            var tables = subjects.Count(s => tableNames.Contains(s));
            var comps = subjects.Count - tables;
            // Confiance en mots, pas en pourcentage : le score sort d'une formule volontairement
            // simple (une base + un bonus par code trouvé), l'afficher au point près prêterait à
            // ce calcul une précision qu'il n'a pas — doctrine ADR-010.
            var confKey = sc.Confidence >= 85 ? "diagnosis.conf.high"
                        : sc.Confidence >= 65 ? "diagnosis.conf.mid" : "diagnosis.conf.low";
            var fixable = _repairResult is not null && sc.TriggeredBy.Any(c => _repairResult.ByCode.ContainsKey(c));
            cards.Add((sev, sc.Confidence, BuildCauseCard(
                sev, sc.Title,
                $"{Loc.Get("card.conf")} {Loc.Get(confKey)}",
                sc.Player, sc.Explanation,
                BuildChainRows(sc.Chain),
                comps, tables, tableNames.Count,
                sc.TriggeredBy, manual: !fixable)));
        }

        var rows = cards.OrderByDescending(c => c.Sev).ThenByDescending(c => c.Conf).Select(c => c.Row).ToList();

        if (rows.Count == 0)
        {
            var top = _report.Ordered().FirstOrDefault(f => f.Severity == Severity.Critical)
                   ?? _report.Ordered().FirstOrDefault(f => f.Severity == Severity.Warning);
            if (top is not null)
            {
                var fixable = _repairResult is not null && _repairResult.ByCode.ContainsKey(top.Code);
                var pfix = Loc.FixHintText(top);
                rows.Add(BuildCauseCard(
                    top.Severity,
                    string.IsNullOrEmpty(top.Subject) ? Loc.Get("cat." + top.Category) : top.Subject,
                    Loc.Get(top.Severity == Severity.Critical ? "priority.label" : "priority.watch"),
                    Loc.FindingText(top),
                    string.IsNullOrEmpty(pfix) ? "" : "→ " + pfix,
                    new List<CauseChainRow>(),
                    comps: 0, tables: 0, tableTotal: 0,
                    codes: new[] { top.Code }, manual: !fixable));
            }
        }

        CauseCards.ItemsSource = rows;
        return rows.Count;
    }

    private CauseCardRow BuildCauseCard(Severity sev, string title, string confText, string player,
        string impact, List<CauseChainRow> chain, int comps, int tables, int tableTotal,
        IReadOnlyList<string> codes, bool manual)
    {
        return new CauseCardRow
        {
            BadgeText = $"{SevGlyph(sev)} {Loc.SeverityLabel(sev).ToUpperInvariant()}",
            AccentBrush = SevBrushOf(sev),
            BadgeBg = SevTint(sev, 0x24),
            BadgeBorder = SevTint(sev, 0x4D),
            Title = title,
            ConfText = confText,
            Player = player,
            Impact = impact,
            ImpactVis = string.IsNullOrEmpty(impact) ? Visibility.Collapsed : Visibility.Visible,
            Chain = chain,
            ChainVis = chain.Count > 1 ? Visibility.Visible : Visibility.Collapsed,
            FootComponents = comps > 0 ? "🧩 " + string.Format(Loc.Get(comps == 1 ? "card.comp.one" : "card.comp.many"), comps) : "",
            FootComponentsVis = comps > 0 ? Visibility.Visible : Visibility.Collapsed,
            FootTables = tables > 0 ? "🎰 " + string.Format(Loc.Get(tables == 1 ? "card.tbl.one" : "card.tbl.many"), tables, tableTotal) : "",
            FootTablesVis = tables > 0 ? Visibility.Visible : Visibility.Collapsed,
            FootCodes = "🔎 " + string.Join(" · ", codes),
            ManualText = Loc.Get("card.manual"),
            ManualVis = manual ? Visibility.Visible : Visibility.Collapsed,
            StepsLabel = Loc.Get("card.steps"),
            Codes = codes,
        };
    }

    /// <summary>« ✕→ » rouge sur la première rupture bon→cassé, « → » discret ailleurs — même
    /// convention que la maquette. The cut-point/arrow decision itself lives in
    /// <see cref="ChainRowPlanner"/> (PincabToolbox.Core.Diagnostics — point 3, moved to Core at
    /// point 5, 13/08) — real, tested, WPF-free; this method only maps the plan to Brushes.</summary>
    private static List<CauseChainRow> BuildChainRows(IReadOnlyList<ChainStepMatch> steps) =>
        ChainRowPlanner.Plan(steps).Select(p => new CauseChainRow
        {
            Arrow = p.Arrow,
            ArrowBrush = p.IsCutPoint ? BrushCritical : BrushDim,
            Label = p.Label,
            Status = p.Status,
            StatusBrush = p.Tone switch { ChainTone.Good => BrushOk, ChainTone.Bad => BrushCritical, _ => BrushWarning },
            NodeBg = p.Tone switch { ChainTone.Bad => NodeBadBg, ChainTone.Warn => NodeWarnBg, _ => NodeGoodBg },
            NodeBorder = p.Tone switch { ChainTone.Bad => NodeBadBorder, ChainTone.Warn => NodeWarnBorder, _ => NodeGoodBorder },
        }).ToList();

    /// <summary>
    /// Carte réparation : l'offre réelle de RepairOfferBuilder quand elle existe, sinon la carte
    /// « aucune réparation automatique disponible » (ADR-006 — l'état honnête est affiché, jamais
    /// caché). Les étapes qui resteront TOUJOURS manuelles restent visibles AVANT l'achat.
    /// </summary>
    private void RefreshRepairCard()
    {
        BtnRepairCardOpen.Content = Loc.Get("repaircard.open");
        if (_repairResult is { } rr && !rr.Offer.IsEmpty)
        {
            RepairCardTitle.Text = Loc.Get("repaircard.some.title");
            RepairCardBody.Visibility = Visibility.Collapsed;
            RepairSummaryLine.Text = string.Format(Loc.Get("repair.summary"), rr.Offer.FixableCount, rr.Offer.FindingsConsidered);
            RepairSummaryLine.Visibility = Visibility.Visible;
        }
        else
        {
            RepairCardTitle.Text = Loc.Get("repaircard.none.title");
            RepairCardBody.Text = Loc.Get("repaircard.none.body");
            RepairCardBody.Visibility = Visibility.Visible;
            RepairSummaryLine.Visibility = Visibility.Collapsed;
        }

        // ADR-006 (décision Maxime, revue qualité 04/08) : un scénario partiellement automatisable
        // (ex. migration 32→64) compte quand même dans FixableCount ci-dessus — c'est mérité, il y a
        // une vraie valeur à vendre. Mais les étapes qui resteront TOUJOURS manuelles doivent être
        // visibles ici, avant l'achat, pas découvertes après (RepairOffer.NotAutomatable).
        if (_repairResult is { } rrNa && rrNa.Offer.NotAutomatable.Count > 0)
        {
            const int maxShown = 4;
            var items = rrNa.Offer.NotAutomatable;
            var shown = items.Take(maxShown).ToList();
            var suffix = items.Count > maxShown ? $" (+{items.Count - maxShown})" : "";
            RepairNotAutomatableLine.Text = Loc.Get("repair.notautomatable") + " " +
                string.Join(" · ", shown.Select(Loc.MissingReasonText)) + suffix;
            RepairNotAutomatableLine.Visibility = Visibility.Visible;
        }
        else
        {
            RepairNotAutomatableLine.Visibility = Visibility.Collapsed;
        }
        RepairCard.Visibility = Visibility.Visible;
    }

    /// <summary>Rôle du profil → clé de composant regroupée (32 et 64-bit sur la même ligne).</summary>
    private static string RoleKey(string role) => role switch
    {
        "vpinmame" or "vpinmame64" => "vpinmame",
        "dmddevice" or "dmddevice64" => "dmddevice",
        _ => role,
    };

    /// <summary>À quel composant un résultat défavorable se rattache — mapping explicite par code,
    /// jamais une heuristique floue sur le texte.</summary>
    private static string? CompKeyForFinding(Finding f) => f.Code switch
    {
        "BITNESS_MISMATCH_VPM" or "VPINMAME_NOT_REGISTERED" => "vpinmame",
        "BITNESS_DMD64_MISSING" => "dmddevice",
        "B2S_SERVER_MISSING" => "b2s",
        "FLEXDMD_MISSING" => "flexdmd",
        "COM_NOT_REGISTERED" or "COM_STALE_PATH" or "COM_PATH_OUTSIDE_INSTALL" or "COM_BITNESS_GAP" =>
            f.Subject.Contains("VPinMAME", StringComparison.OrdinalIgnoreCase) ? "vpinmame"
            : f.Subject.Contains("B2S", StringComparison.OrdinalIgnoreCase) ? "b2s"
            : f.Subject.Contains("FlexDMD", StringComparison.OrdinalIgnoreCase) ? "flexdmd"
            : null,
        _ => null,
    };

    /// <summary>
    /// « Santé des composants » : uniquement des lignes portées par un résultat réel du scan
    /// (BITNESS_INVENTORY, composant manquant, base Popper) — jamais une ligne déduite du silence
    /// d'un scanner. C'est pour ça que la ligne « FlexDMD — non requis » de la maquette n'existe
    /// pas ici : « aucun résultat » n'est pas une mesure (doctrine affichée dans l'encadré même).
    /// </summary>
    private List<CompRow> BuildComponentRows()
    {
        var meta = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        foreach (var f in _report!.Findings.Where(f => f.Code == "BITNESS_INVENTORY" && f.Args.Count >= 3))
        {
            var key = RoleKey(f.Args[2]);
            if (!meta.TryGetValue(key, out var list)) meta[key] = list = new List<string>();
            if (!list.Contains(f.Args[1])) list.Add(f.Args[1]);
        }

        var absent = new HashSet<string>(StringComparer.Ordinal);
        foreach (var f in _report.Findings)
        {
            if (f.Code == "B2S_SERVER_MISSING") absent.Add("b2s");
            if (f.Code == "FLEXDMD_MISSING") absent.Add("flexdmd");
        }

        var worst = new Dictionary<string, Finding>(StringComparer.Ordinal);
        foreach (var f in _report.Findings)
        {
            var key = CompKeyForFinding(f);
            if (key is null) continue;
            if (!worst.TryGetValue(key, out var cur) || f.Severity > cur.Severity) worst[key] = f;
        }

        var rows = new List<CompRow>();
        foreach (var key in new[] { "main-exe", "vpinmame", "b2s", "dmddevice", "flexdmd" })
        {
            var hasMeta = meta.TryGetValue(key, out var bits);
            var isAbsent = absent.Contains(key);
            if (!hasMeta && !isAbsent) continue;   // aucun résultat sur ce composant → pas de ligne
            worst.TryGetValue(key, out var w);
            rows.Add(new CompRow
            {
                Name = Loc.Get("comp.name." + key),
                Meta = hasMeta ? string.Join(" + ", bits!) : Loc.Get("comp.meta.absent"),
                StatusText = CompStatusText(w),
                StatusBrush = w is null ? BrushOk : SevBrushOf(w.Severity),
            });
        }

        if (_report.Layout.PupDatabasePath is not null)
        {
            // « base lue » seulement quand la lecture positive a réussi ; illisible → le fait
            // mesuré est dit tel quel et la colonne statut se tait (rien n'est affirmé).
            var read = _popperRegistered is not null;
            rows.Add(new CompRow
            {
                Name = Loc.Get("comp.name.popper"),
                Meta = Loc.Get(read ? "comp.meta.dbread" : "comp.meta.dbunreadable"),
                StatusText = read ? Loc.Get("comp.st.ok") : "",
                StatusBrush = BrushOk,
            });
        }
        return rows;
    }

    private static string CompStatusText(Finding? worst)
    {
        if (worst is null) return Loc.Get("comp.st.ok");
        var perCode = Loc.Get("comp.code." + worst.Code);
        if (perCode != "comp.code." + worst.Code) return perCode;   // libellé court spécifique au code
        return worst.Severity switch
        {
            Severity.Critical => Loc.Get("comp.st.critical"),
            Severity.Warning => Loc.Get("comp.st.warn"),
            _ => Loc.Get("comp.st.note"),
        };
    }

    /// <summary>
    /// Lignes du tableau « Tables analysées ». ROM : résultats du RomValidator uniquement (« — »
    /// quand le dossier roms est introuvable — le résultat ROMS_DIR_NOT_FOUND l'explique dans la
    /// liste). Backglass : B2S_MISSING sinon « présent » — inférence sûre, le contrôle est
    /// inconditionnel par table (CompletenessScanner) et un plantage du scanner produirait un
    /// SCANNER_ERROR qui la désactive ici. Frontend : lecture positive de la base Popper.
    /// </summary>
    private List<TableRowVm> BuildTableRows()
    {
        var byTable = _report!.Findings
            .Where(f => !string.IsNullOrEmpty(f.Subject))
            .GroupBy(f => f.Subject, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);
        var completenessFailed = _report.Findings.Any(f => f.Code == "SCANNER_ERROR" && f.Category == "completeness");

        var rows = new List<TableRowVm>();
        foreach (var path in _report.Layout.VpxTables)
        {
            var name = Path.GetFileNameWithoutExtension(path);
            var fs = byTable.TryGetValue(name, out var found) ? found : new List<Finding>();

            // Which finding wins per column, and at what severity, is decided by TableRowPlanner
            // (point 3, 13/08) — real, tested, WPF-free. This loop only turns that plan into the
            // localized text and Brush a WPF row needs.
            var romPlan = TableRowPlanner.PlanRom(fs);
            var (rom, romBrush) = romPlan.Status switch
            {
                RomColumnStatus.Ok => (string.Format(Loc.Get("tbl.rom.ok"), romPlan.RomName ?? ""), BrushOk),
                RomColumnStatus.Missing => (string.Format(Loc.Get("tbl.rom.missing"), romPlan.RomName ?? ""), BrushCritical),
                RomColumnStatus.NotRequired => (Loc.Get("tbl.rom.notrequired"), BrushOk),
                RomColumnStatus.Unzipped => (Loc.Get("tbl.rom.unzipped"), BrushWarning),
                _ => (Loc.Get("tbl.unknown"), BrushDim),
            };

            var b2sPlan = TableRowPlanner.PlanB2s(fs, completenessFailed);
            var (b2s, b2sBrush) = b2sPlan.Status switch
            {
                B2sColumnStatus.Present => (Loc.Get("tbl.b2s.present"), BrushOk),
                B2sColumnStatus.Missing => ($"{SevGlyph(b2sPlan.Severity)} {Loc.Get("tbl.b2s.missing")}", SevBrushOf(b2sPlan.Severity)),
                _ => (Loc.Get("tbl.unknown"), BrushDim),
            };

            // La teinte suit la gravité RÉELLE du résultat (Info sur le pack actuel) — pas l'orange
            // de la maquette, qui surjouait un résultat informatif.
            var fePlan = TableRowPlanner.PlanFrontend(name, _popperRegistered, fs);
            var (fe, feBrush) = fePlan.Status switch
            {
                FrontendColumnStatus.Registered => (Loc.Get("tbl.fe.registered"), BrushOk),
                FrontendColumnStatus.NotRegistered => ($"{SevGlyph(fePlan.Severity)} {Loc.Get("tbl.fe.notregistered")}", SevBrushOf(fePlan.Severity)),
                _ => (Loc.Get("tbl.unknown"), BrushDim),
            };

            rows.Add(new TableRowVm
            {
                Name = name,
                Rom = rom, RomBrush = romBrush,
                B2s = b2s, B2sBrush = b2sBrush,
                Frontend = fe, FeBrush = feBrush,
            });
        }
        return rows;
    }

    /// <summary>Colonne de droite : résultats critiques réels, santé des composants, remarques
    /// (Rolled — les répétitions par table restent regroupées comme dans la liste).</summary>
    private void RefreshSideBoxes(List<CompRow> compRows)
    {
        SideRow ToRow(Finding f, Brush dot) => new()
        {
            Dot = dot,
            Text = Loc.FindingText(f),
            Sub = string.IsNullOrEmpty(f.Subject)
                ? Loc.Get("cat." + f.Category)
                : $"{Loc.Get("cat." + f.Category)} · {f.Subject}",
            Code = f.Code,
            Subject = f.Subject,
        };

        // Plafond des encadrés : une install cassée peut porter des dizaines de critiques et un
        // ItemsControl ne virtualise pas. Le TOTAL réel reste dans l'en-tête, le renvoi dit
        // combien de lignes ne sont pas montrées ici — rien n'est caché, tout est dans la liste.
        const int sideCap = 8;

        var crits = _report!.Ordered().Where(f => f.Severity == Severity.Critical)
            .Select(f => ToRow(f, BrushCritical)).ToList();
        SideCritHeader.Text = "✕ " + Loc.Get("side.criticals");
        SideCritCount.Text = crits.Count.ToString();
        SideCritList.ItemsSource = crits.Count > sideCap ? crits.Take(sideCap).ToList() : crits;
        SideCritMore.Text = crits.Count > sideCap ? string.Format(Loc.Get("more.results"), crits.Count - sideCap) : "";
        SideCritMore.Visibility = crits.Count > sideCap ? Visibility.Visible : Visibility.Collapsed;
        SideCritBox.Visibility = crits.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

        SideCompHeader.Text = Loc.Get("side.components");
        SideCompCount.Text = compRows.Count.ToString();
        SideCompList.ItemsSource = compRows;
        SideCompNote.Text = Loc.Get("side.comp.note");
        SideCompBox.Visibility = compRows.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

        // Onglet Composants : les mêmes lignes, pleine largeur.
        CompList.ItemsSource = compRows;
        CompTabNote.Text = Loc.Get("side.comp.note");
        CompTabCard.Visibility = compRows.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        CompTabEmpty.Text = compRows.Count > 0 ? "" : Loc.Get("scan.empty");

        var notes = _report.Rolled().Where(f => f.Severity == Severity.Note)
            .Select(f => ToRow(f, BrushNote)).ToList();
        SideNoteHeader.Text = "✎ " + Loc.Get("side.notes");
        SideNoteCount.Text = _report.Count(Severity.Note).ToString();
        SideNoteList.ItemsSource = notes.Count > sideCap ? notes.Take(sideCap).ToList() : notes;
        SideNoteMore.Text = notes.Count > sideCap ? string.Format(Loc.Get("more.results"), notes.Count - sideCap) : "";
        SideNoteMore.Visibility = notes.Count > sideCap ? Visibility.Visible : Visibility.Collapsed;
        SideNoteBox.Visibility = notes.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    /// <summary>Le tableau des tables, aux deux endroits où la maquette le montre : la vue
    /// Causes racines et l'onglet Tables (même liste, zéro dérive possible).</summary>
    private void RefreshTablesViews(List<TableRowVm> tableRows)
    {
        TablesCardHeader.Text = "🎰 " + Loc.Get("tbl.header");
        TablesCardCount.Text = tableRows.Count.ToString();
        TblHTable.Text = TblHTable2.Text = Loc.Get("tbl.h.table");
        TblHRom.Text = TblHRom2.Text = Loc.Get("tbl.h.rom");
        TblHB2s.Text = TblHB2s2.Text = Loc.Get("tbl.h.b2s");
        TblHFe.Text = TblHFe2.Text = Loc.Get("tbl.h.frontend");
        // Résumé plafonné dans la vue Causes racines (ItemsControl non virtualisé) ; la liste
        // complète, virtualisée, vit dans l'onglet Tables. Le compteur d'en-tête reste le total réel.
        const int cardCap = 12;
        TablesList.ItemsSource = tableRows.Count > cardCap ? tableRows.Take(cardCap).ToList() : tableRows;
        TablesListMore.Text = tableRows.Count > cardCap ? string.Format(Loc.Get("more.tables"), tableRows.Count - cardCap) : "";
        TablesListMore.Visibility = tableRows.Count > cardCap ? Visibility.Visible : Visibility.Collapsed;
        TablesListFull.ItemsSource = tableRows;
        var vis = tableRows.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        TablesCard.Visibility = vis;
        TablesTabCard.Visibility = vis;
        TablesTabEmpty.Text = tableRows.Count > 0 ? "" : Loc.Get("scan.hint.notables");
    }

    /// <summary>
    /// Onglet Système — uniquement des faits mesurés : métadonnées du dernier scan (ScanReport)
    /// et machine (OS/CPU/mémoire par les API système, écrans par MonitorTopologyProbe — silence
    /// hors Windows). Pas de GPU : le lire demanderait WMI, hors contrainte zéro-dépendance.
    /// </summary>
    private void RefreshSystemTab()
    {
        SysScanHeader.Text = Loc.Get("sys.scan.header");
        SysMachineHeader.Text = Loc.Get("sys.machine.header");
        SysMachineBody.Text = MachineInfoText();

        if (_report is null)
        {
            SysScanBody.Text = Loc.Get("sys.needscan");
            return;
        }

        static string Mark(object? v) => v is null ? "—" : "✓";
        var l = _report.Layout;
        var sb = new StringBuilder();
        sb.AppendLine($"{Loc.Get("sys.root")} : {l.RootPath}");
        sb.AppendLine($"{Loc.Get("sys.dirs")} : Tables {Mark(l.TablesDir)} · VPinMAME {Mark(l.VPinMameDir)} · roms {Mark(l.RomsDir)} · PinUP Popper {Mark(l.PupDatabasePath)}");
        sb.AppendLine($"{Loc.Get("meta.started")} {_report.StartedAt.ToLocalTime():dd/MM/yyyy HH:mm}");
        var dur = _report.FinishedAt - _report.StartedAt;
        if (_report.FinishedAt != default && dur > TimeSpan.Zero)
            sb.AppendLine($"{Loc.Get("meta.duration")} {dur:hh\\:mm\\:ss}");
        if (_scanChecks > 0)
            sb.AppendLine($"{Loc.Get("meta.checks")} {_scanChecks} / {_scanChecks}");
        sb.Append($"{Loc.Get("meta.tablecount")} {l.VpxTables.Count}");
        SysScanBody.Text = sb.ToString();
    }

    private static string MachineInfoText()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"{Loc.Get("sys.os")} : {System.Runtime.InteropServices.RuntimeInformation.OSDescription} · {(Environment.Is64BitOperatingSystem ? "64-bit" : "32-bit")}");

        string? cpu = null;
        try
        {
            if (OperatingSystem.IsWindows())
                cpu = Registry.GetValue(@"HKEY_LOCAL_MACHINE\HARDWARE\DESCRIPTION\System\CentralProcessor\0",
                    "ProcessorNameString", null) as string;
        }
        catch { /* lecture registre refusée → la ligne se réduit au nombre de cœurs */ }
        var cores = string.Format(Loc.Get("sys.cores"), Environment.ProcessorCount);
        sb.AppendLine($"{Loc.Get("sys.cpu")} : {(string.IsNullOrWhiteSpace(cpu) ? cores : cpu!.Trim() + " · " + cores)}");

        var gb = Math.Round(GC.GetGCMemoryInfo().TotalAvailableMemoryBytes / 1073741824.0);
        sb.Append($"{Loc.Get("sys.ram")} : {string.Format(Loc.Get("sys.ram.fmt"), gb)}");

        // Écrans : la sonde existante du Core (Windows uniquement — null ailleurs, la ligne se tait).
        var monitors = MonitorTopologyProbe.TryGetMonitorRects();
        if (monitors is not null)
        {
            sb.AppendLine();
            sb.Append($"{Loc.Get("sys.screens")} : {string.Format(Loc.Get("sys.screens.fmt"), monitors.Count, (int)SystemParameters.VirtualScreenWidth, (int)SystemParameters.VirtualScreenHeight)}");
        }
        return sb.ToString();
    }

    /// <summary>
    /// Lot 3 — onglet « Mises à jour » : les findings Category="updates" (UpdateWatcherScanner,
    /// tous Severity.Info) désencombrés de "Tous les résultats". Le watcher produit toujours au
    /// moins une ligne après un scan (VPS_MATCH_SUMMARY ou VPS_UNAVAILABLE, jamais les deux) — cet
    /// onglet ne doit donc jamais paraître plus mûr que le scanner bêta qu'il reflète : on affiche
    /// l'état réel, pas une promesse de fonctionnalité aboutie. Retourne le nombre de lignes
    /// affichées (compteur d'onglet).
    /// </summary>
    private int RefreshUpdatesTab()
    {
        var updateRows = _report!.Rolled()
            .Where(f => f.Category == "updates")
            .Select(f => new SideRow
            {
                Dot = BrushInfo,
                Text = Loc.FindingText(f),
                Sub = string.IsNullOrEmpty(f.Subject) ? "" : f.Subject,
                SubVis = string.IsNullOrEmpty(f.Subject) ? Visibility.Collapsed : Visibility.Visible,
                Code = f.Code,
                Subject = f.Subject,
            })
            .ToList();

        UpdatesList.ItemsSource = updateRows;
        UpdatesTabCard.Visibility = updateRows.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        UpdatesTabEmpty.Text = updateRows.Count > 0 ? "" : Loc.Get("scan.empty");
        return updateRows.Count;
    }

    private void RefreshInnerTabHeaders(int causes, int comps, int tables, int updates)
    {
        StabCausesHeader.Text = $"{Loc.Get("stab.causes")} · {causes}";
        // Lot 3 : le compteur de "Tous les résultats" exclut désormais les findings "updates",
        // cohérent avec ce qui y est réellement listé (même principe que ChipInfo ci-dessus).
        var resultsCount = _report!.Findings.Count(f => f.Category != "updates");
        StabResultsHeader.Text = $"{Loc.Get("stab.results")} · {resultsCount}";
        StabComponentsHeader.Text = comps > 0 ? $"{Loc.Get("stab.components")} · {comps}" : Loc.Get("stab.components");
        StabTablesHeader.Text = tables > 0 ? $"{Loc.Get("stab.tables")} · {tables}" : Loc.Get("stab.tables");
        StabSystemHeader.Text = Loc.Get("stab.system");
        StabUpdatesHeader.Text = updates > 0 ? $"{Loc.Get("stab.updates")} · {updates}" : Loc.Get("stab.updates");
    }

    /// <summary>Même requête que CompletenessScanner.LoadPopperGames — la colonne Frontend doit
    /// dire la même chose que le scanner, jamais une réimplémentation qui dérive.</summary>
    private static HashSet<string>? LoadPopperRegistered(InstallLayout layout)
    {
        if (layout.PupDatabasePath is null || !File.Exists(layout.PupDatabasePath)) return null;
        var dbRows = SqliteReader.TryReadTable(layout.PupDatabasePath, "Games", "GameName", "GameFileName");
        if (dbRows is null) return null;
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in dbRows)
        {
            foreach (var v in row)
            {
                if (string.IsNullOrEmpty(v)) continue;
                names.Add(v);
                names.Add(Path.GetFileNameWithoutExtension(v));
            }
        }
        return names;
    }

    /// <summary>« Voir les étapes » d'une carte : ouvre Tous les résultats sur le premier résultat
    /// déclencheur — le panneau de détail (impact / cause / correction) porte les étapes.</summary>
    private void CauseSteps_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as System.Windows.Controls.Button)?.DataContext is CauseCardRow row)
            JumpToFinding(row.Codes, null);
    }

    private void SideRow_Click(object sender, MouseButtonEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is SideRow row)
            JumpToFinding(new[] { row.Code }, row.Subject);
    }

    private void JumpToFinding(IReadOnlyList<string> codes, string? subject)
    {
        ScannerTabs.SelectedItem = StabResults;
        foreach (var item in ListFindings.Items)
        {
            if (item is FindingRow r && codes.Contains(r.Code) &&
                (string.IsNullOrEmpty(subject) || string.Equals(r.Subject, subject, StringComparison.OrdinalIgnoreCase)))
            {
                ListFindings.SelectedItem = item;
                ListFindings.ScrollIntoView(item);
                return;
            }
        }
    }

    private void TxtSearch_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        SearchHint.Visibility = string.IsNullOrEmpty(TxtSearch.Text) ? Visibility.Visible : Visibility.Collapsed;
        if (_report is not null) RefreshList();
    }

    private void Header_Click(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is not System.Windows.Controls.GridViewColumnHeader h || h.Column is null) return;
        string? key =
            h.Column == ColSeverity ? "sev" :
            h.Column == ColCategory ? "cat" :
            h.Column == ColSubject ? "subj" :
            h.Column == ColMessage ? "msg" : null;
        if (key is null) return;                 // Action column / padding header — not sortable
        if (_sortKey == key) _sortAsc = !_sortAsc;
        else { _sortKey = key; _sortAsc = true; }
        if (_report is not null) RefreshList();
    }

    private void ListFindings_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (ListFindings.SelectedItem is not FindingRow row)
        {
            DetailPanel.Visibility = Visibility.Collapsed;
            return;
        }
        DetailSubject.Text = $"{row.SevLabel} · {row.Category} · {row.Subject}";
        DetailMessage.Text = row.Message;

        DetailSymptomLabel.Text = Loc.Get("detail.symptom");
        SetSection(DetailSymptomLabel, DetailSymptom, PackKnowledge.Player(row.Code));

        DetailImpactLabel.Text = Loc.Get("detail.impact");
        SetSection(DetailImpactLabel, DetailImpact, Knowledge.Impact(row.Code));

        DetailCauseLabel.Text = Loc.Get("detail.cause");
        SetSection(DetailCauseLabel, DetailCause, Knowledge.Cause(row.Code));

        DetailExplanationLabel.Text = Loc.Get("detail.explanation");
        SetSection(DetailExplanationLabel, DetailExplanation, PackKnowledge.Explanation(row.Code));

        DetailFixLabel.Text = Loc.Get("detail.fix");
        SetSection(DetailFixLabel, DetailFix, row.FixHint);

        DetailVerifyLabel.Text = Loc.Get("detail.verify");
        SetSection(DetailVerifyLabel, DetailVerify, PackKnowledge.Verification(row.Code));

        // Écran 1 — real per-code facts from the computed plan, not the static Knowledge.cs
        // approximation: only a code the engine actually resolved to Locked+fixable shows the tag.
        if (row.Code is not null && _repairResult is not null && _repairResult.ByCode.TryGetValue(row.Code, out var cs))
        {
            var checks = new List<string> { Loc.Get("repair.checks.fixable") };
            if (cs.BackupPlanned) checks.Add(Loc.Get("repair.checks.backup"));
            if (cs.FullyReversible) checks.Add(Loc.Get("repair.checks.reversible"));
            checks.Add(Loc.Get(cs.EstimatedDuration switch
            {
                DurationBucket.Seconds => "repair.checks.duration.seconds",
                DurationBucket.UnderAMinute => "repair.checks.duration.underminute",
                _ => "repair.checks.duration.minutes",
            }));
            DetailRepairTagText.Text = string.Join(" · ", checks) + "\n" + Loc.Get("repair.tag");
            DetailRepairTag.Visibility = Visibility.Visible;
        }
        else
        {
            DetailRepairTag.Visibility = Visibility.Collapsed;
        }

        ShowTableCompanionTeaser(row.Code);

        DetailActionBtn.Content = row.ActionLabel;   // same action as the grid row, reachable without closing the panel

        DetailPath.Text = row.FilePath ?? "";
        DetailPath.Visibility = string.IsNullOrEmpty(row.FilePath) ? Visibility.Collapsed : Visibility.Visible;
        DetailPanel.Visibility = Visibility.Visible;
    }

    private static void SetSection(System.Windows.Controls.TextBlock label, System.Windows.Controls.TextBlock content, string? text)
    {
        var has = !string.IsNullOrEmpty(text);
        content.Text = text ?? "";
        label.Visibility = content.Visibility = has ? Visibility.Visible : Visibility.Collapsed;
    }

    /// <summary>Discreet contact address for the not-yet-released Table Companion product, kept as
    /// a single constant so the mailto: teaser and any future mention share one source.</summary>
    private const string TableCompanionContactEmail = "flipsync.contact@gmail.com";

    /// <summary>
    /// LOT Table Companion teaser (18/08, docs/PROMPT-session-lot-complet-2026-08-18.md): a discreet
    /// opt-in for the two findings a future Table Companion product would fix (ALTCOLOR_INCOMPLETE,
    /// ALTSOUND_SAMPLE_MISSING) — never shown for any other code. Zero network calls (ADR-002): a
    /// plain <c>mailto:</c> link that opens the user's own mail client, nothing sent automatically,
    /// nothing tracked. Honest by construction — the Loc text says plainly the product is not out
    /// yet, never oversells it as available or imminent.
    /// </summary>
    private void ShowTableCompanionTeaser(string? code)
    {
        if (code is not ("ALTCOLOR_INCOMPLETE" or "ALTSOUND_SAMPLE_MISSING"))
        {
            DetailTeaser.Visibility = Visibility.Collapsed;
            return;
        }

        DetailTeaserText.Text = Loc.Get("teaser.tablecompanion.text");
        DetailTeaserLink.Inlines.Clear();

        var subject = Uri.EscapeDataString("Table Companion");
        var body = Uri.EscapeDataString($"Code: {code}");
        var mailtoUri = $"mailto:{TableCompanionContactEmail}?subject={subject}&body={body}";

        var link = new System.Windows.Documents.Hyperlink(
            new System.Windows.Documents.Run(Loc.Get("teaser.tablecompanion.link")))
        {
            NavigateUri = new Uri(mailtoUri),
        };
        link.Click += (_, _) =>
        {
            try { Process.Start(new ProcessStartInfo(mailtoUri) { UseShellExecute = true }); }
            catch { /* best-effort — never let a missing mail client crash the app */ }
        };
        DetailTeaserLink.Inlines.Add(link);

        DetailTeaser.Visibility = Visibility.Visible;
    }

    private void BtnCloseDetail_Click(object sender, RoutedEventArgs e)
    {
        ListFindings.SelectedItem = null; // clears selection → SelectionChanged hides the panel
    }

    // ═════════════════════════ LOT H (spec 10/08) — Écran 2, the write path ═════════════════════════
    // Every decision (license check, plan, preflight, apply, undo) is made by RepairSession
    // (PincabToolbox.Repair — cross-platform, fully unit-tested, 122/122 green). This window only
    // calls into it, holds the results, and renders them: no write-path decision is made here.

    private void BtnGotoRepair_Click(object sender, RoutedEventArgs e) => MainTabs.SelectedItem = TabRepair;

    /// <summary>
    /// Builds a fresh <see cref="RepairSession"/> confined to the roots of the CURRENT scan — never
    /// reused stale roots from a previous, possibly different, scan. Single-install scans confine to
    /// that one root; a whole-drive scan confines to the real per-install roots found
    /// (<see cref="_lastDriveScanRoots"/>), same rule <see cref="RepairOfferBuilder"/> already
    /// follows for Écran 1 (ADR-005/ADR-011) — Repair must never validate a write target anywhere on
    /// an entire drive.
    /// </summary>
    private RepairSession NewRepairSessionForCurrentScan()
    {
        IReadOnlyList<string> roots = _lastDriveScanRoots is { Count: > 0 }
            ? _lastDriveScanRoots
            : new[] { _report!.Layout.RootPath };
        return new RepairSession(RepairOfferBuilder.LoadPack(), roots, _report!.Layout);
    }

    /// <summary>
    /// Used only to read the on-disk Undo history before any scan has happened this session (H.2
    /// rule 5). No real containment check is ever performed with these roots — <c>Undo()</c> reverts
    /// strictly from the journal's own recorded changes and never re-validates against
    /// confinement roots (see <see cref="PincabToolbox.Repair.Engine.RepairEngine.Undo"/>) — so an
    /// empty root list here is safe.
    /// </summary>
    private RepairSession NewRepairSessionForBrowsing()
    {
        IReadOnlyList<string> roots = _report is not null
            ? (_lastDriveScanRoots is { Count: > 0 } ? _lastDriveScanRoots : new[] { _report.Layout.RootPath })
            : Array.Empty<string>();
        return new RepairSession(RepairOfferBuilder.LoadPack(), roots, _report?.Layout);
    }

    /// <summary>H.4 — always re-verified against the embedded key here, never trusted from a previous click.</summary>
    private void BtnRepairVerifyLicense_Click(object sender, RoutedEventArgs e)
    {
        var key = TxtRepairLicense.Text.Trim();
        var result = new LicenseVerifier().Verify(string.IsNullOrEmpty(key) ? null : key);
        _licensed = result.IsValid;

        _settings.RepairLicenseKey = key;
        _settings.Save();

        LblRepairLicenseStatus.Text = _licensed ? Loc.Get("repair.license.valid") : Loc.Get("repair.license.invalid");
    }

    /// <summary>H.2 steps 1-3 — Plan then Preflight, then render exactly what Preflight retained (never the raw plan).</summary>
    private void BtnRepairBuildPlan_Click(object sender, RoutedEventArgs e)
    {
        if (_report is null)
        {
            RepairPlanStatus.Text = Loc.Get("repair.needscan");
            return;
        }

        _repairSession = NewRepairSessionForCurrentScan();

        var scanReportId = $"scan-{_report.StartedAt:yyyyMMdd-HHmmss}";
        _repairPlan = _repairSession.Plan(scanReportId, _report.Findings, licensed: _licensed);
        _repairPreflight = _repairSession.Preflight(_repairPlan);

        if (_repairPreflight.Blockers.Count > 0)
        {
            RepairBlockers.Text = string.Join("\n", _repairPreflight.Blockers.Select(b => "• " + BlockerText(b)));
            RepairBlockers.Visibility = Visibility.Visible;
        }
        else
        {
            RepairBlockers.Visibility = Visibility.Collapsed;
        }

        RefreshRepairItemsList();
        RefreshRepairUndoList();

        // 13/08 — the list now also carries ManualOnly/Locked rows (ADR-006), so "réparable(s)
        // trouvé(s)" alone would misdescribe most of what's on screen for an install with real,
        // non-automatable Criticals like ROM_MISSING. Two counts instead of one.
        var fixableCount = _repairItemRows.Count(r => r.CanApply);
        var manualCount = _repairItemRows.Count - fixableCount;
        RepairPlanStatus.Text = _repairItemRows.Count > 0
            ? string.Format(Loc.Get("repair.plan.status"), fixableCount, manualCount)
            : Loc.Get("repair.plan.empty");

        // 11/08/2026, ADR-012 "Suite" — never let this run silently: if the kill switch is on,
        // say so every time a plan is built, not just once, so it can't be missed mid-session.
        if (_repairSession.ForceDryRunActive)
            RepairPlanStatus.Text = Loc.Get("repair.forceddryrun.banner") + "\n" + RepairPlanStatus.Text;
    }

    /// <summary>
    /// Every retained item is shown, not just the ones with an automatic fix — ADR-006 gap closed
    /// 13/08: before this, a ManualOnly item (e.g. ROM_MISSING — Repair cannot invent a ROM dump)
    /// or a Locked one (fix exists, needs a licence) was filtered out entirely, so a real Critical
    /// finding could be invisible in Repair even though Scanner reported it and the engine already
    /// had a step-by-step reason ready (<see cref="RepairPlanItem.Missing"/>). Text comes from
    /// <see cref="RepairSession.Describe"/>, the same pure facts the confirmation screen must show
    /// per H.2 rule 3 — never re-derived by hand here. <see cref="RepairItemRow.CanApply"/> is what
    /// now decides whether a row's checkbox can be ticked, not whether the row exists at all.
    /// </summary>
    private void RefreshRepairItemsList()
    {
        var retained = _repairPreflight?.RetainedItems ?? Array.Empty<RepairPlanItem>();
        _repairItemRows = RepairSession.Describe(retained)
            .Select(ic => new RepairItemRow
            {
                ItemId = ic.ItemId,
                Description = BuildConfirmationText(ic),
                CanApply = ic.Mode is RepairMode.Automatic or RepairMode.ConfirmationRequired,
            })
            .ToList();
        ListRepairItems.ItemsSource = null;   // force the ItemsControl to re-bind (rows are mutable POCOs)
        ListRepairItems.ItemsSource = _repairItemRows;

        // "Tout sélectionner" : visible seulement s'il y a au moins une ligne cochable (sinon rien à
        // faire avec elle), et toujours redémarrée décochée — un plan neuf ne doit jamais hériter d'un
        // état "tout coché" d'un plan précédent que l'utilisateur n'a pas revu.
        ChkRepairSelectAll.Visibility = _repairItemRows.Any(r => r.CanApply) ? Visibility.Visible : Visibility.Collapsed;
        ChkRepairSelectAll.IsChecked = false;
    }

    /// <summary>
    /// Coche/décoche toutes les lignes CanApply=true d'un coup. N'importe jamais IsSelected sur une
    /// ligne ManualOnly/Locked (CanApply=false) — sa case est déjà désactivée dans le DataTemplate,
    /// mais on ne dépend pas de ça ici : le filtre est explicite. Même pattern de re-bind forcé que
    /// RefreshRepairItemsList (RepairItemRow est un POCO mutable, sans INotifyPropertyChanged).
    /// </summary>
    private void ChkRepairSelectAll_Click(object sender, RoutedEventArgs e)
    {
        var check = ChkRepairSelectAll.IsChecked == true;
        foreach (var row in _repairItemRows.Where(r => r.CanApply))
            row.IsSelected = check;
        ListRepairItems.ItemsSource = null;
        ListRepairItems.ItemsSource = _repairItemRows;
    }

    /// <summary>14/08/2026: Blocker gained MessageEs alongside Spanish support — was a binary
    /// fr/en ternary before, now a 3-way switch matching Loc.MissingReasonText's shape.</summary>
    private static string BlockerText(Blocker b) => Loc.Lang switch
    {
        "fr" => b.MessageFr,
        "es" => b.MessageEs,
        _ => b.MessageEn,
    };

    private static string BuildConfirmationText(ItemConfirmation ic)
    {
        var head = ic.Targets.Count > 0
            ? $"{ic.TargetCode} — {string.Join(", ", ic.Targets)}"
            : ic.TargetCode;

        if (ic.Mode == RepairMode.ManualOnly)
        {
            var why = ic.Missing.Count > 0
                ? string.Join(" ", ic.Missing.Select(Loc.MissingReasonText))
                : Loc.Get("repair.manual.noreason");
            return $"{head}\n{Loc.Get("repair.manual.label")} — {why}";
        }

        if (ic.Mode == RepairMode.Locked)
            return $"{head}\n{Loc.Get("repair.locked.label")}";

        var reversible = ic.Reversible ? Loc.Get("repair.reversible.yes") : Loc.Get("repair.reversible.no");
        var backup = ic.BackupPlanned ? Loc.Get("repair.backup.yes") : Loc.Get("repair.backup.no");
        return $"{head}\n{reversible} · {backup}";
    }

    /// <summary>
    /// 13/08 — replaces a single "Historique d'annulation" list of raw plan IDs with two clear,
    /// described lists. Maxime, testing on his real cab: "les intitulés sont pas parlants, on voit
    /// pas le detail du plan donc on pourrait très bien annuler un plan qui a fonctionné [...] tu
    /// fais une colonne plan fait avec les corectifs faits, et plan annulé de l'autre, pour
    /// l'utilisateur c'est plus simple." "Plan à faire" is not a third list here — that is the
    /// live checklist <see cref="RefreshRepairItemsList"/> already renders above; nothing new to
    /// build for it. Plans where nothing was ever really applied (blocked at preflight, or only a
    /// <c>PINCAB_REPAIR_FORCE_DRYRUN</c> test run) are left out of both — neither "repaired" nor
    /// "undone" honestly describes them, and showing them would just be noise.
    /// </summary>
    private void RefreshRepairUndoList()
    {
        _repairSession ??= NewRepairSessionForBrowsing();
        var summaries = _repairSession.AllPlanSummaries();

        var done = summaries.Where(s => s.Outcome is PlanOutcome.Applied or PlanOutcome.PartiallyUndone)
            .Select(s => new RepairPlanSummaryRow
            {
                PlanId = s.PlanId,
                Description = BuildPlanSummaryText(s),
                UndoLabel = Loc.Get("repair.undo.button"),
                CanUndo = true,
            }).ToList();
        var undone = summaries.Where(s => s.Outcome == PlanOutcome.FullyUndone)
            .Select(s => new RepairPlanSummaryRow
            {
                PlanId = s.PlanId,
                Description = BuildPlanSummaryText(s),
                UndoLabel = "",
                CanUndo = false,
            }).ToList();

        ListRepairDone.ItemsSource = null;
        ListRepairDone.ItemsSource = done;
        RepairDoneEmpty.Visibility = done.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

        ListRepairUndone.ItemsSource = null;
        ListRepairUndone.ItemsSource = undone;
        RepairUndoneEmpty.Visibility = undone.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

        RepairUndoStatus.Text = _repairSession.LastJournalWriteFailed ? Loc.Get("repair.undo.journalwarning") : "";
    }

    private static string BuildPlanSummaryText(PlanSummary s)
    {
        var when = s.CreatedAtUtc?.ToLocalTime().ToString("dd/MM HH:mm") ?? "?";
        var targets = s.Targets.Count > 0 ? " — " + string.Join(", ", s.Targets) : "";
        var head = string.Format(Loc.Get("repair.done.count"), when, s.ItemsCompleted) + targets;

        if (s.Outcome == PlanOutcome.PartiallyUndone)
            return head + "\n" + string.Format(Loc.Get("repair.done.partiallyundone"), s.ItemsUndone);
        if (s.Outcome == PlanOutcome.FullyUndone)
            return head + "\n" + Loc.Get("repair.done.fullyundone");
        return head;
    }

    /// <summary>
    /// H.2 steps 4-5. Opt-in only (H.2 rule 3 — never a silent "fix everything"): nothing is applied
    /// unless the user checked its box. H.3: any selected item that is not fully reversible gets an
    /// explicit, unambiguous confirmation dialog before Apply is ever called — the wording says
    /// plainly that the operation cannot be undone.
    ///
    /// <para>
    /// Runs <see cref="RepairSession.Apply"/> on a background thread (<c>Task.Run</c>), same pattern
    /// as <see cref="BtnScan_Click"/>. Today's four wired actions are all fast, so this made no
    /// visible difference before — but LOT I's <c>RegisterComComponentAction</c> (not yet wired, see
    /// ADR-012) can legitimately wait up to its 20-second launch timeout, and freezing the whole
    /// window for that long would be a real regression the day it IS wired. Cheap to do now, before
    /// there is a real plan sitting on screen to lose if it were forgotten later.
    /// </para>
    /// </summary>
    private async void BtnRepairApply_Click(object sender, RoutedEventArgs e)
    {
        if (_repairSession is null || _repairPlan is null || _report is null) return;

        var selected = _repairItemRows.Where(r => r.IsSelected)
            .Select(r => r.ItemId).ToHashSet(StringComparer.Ordinal);
        if (selected.Count == 0)
        {
            RepairApplyStatus.Text = Loc.Get("repair.noneselected");
            return;
        }

        var retained = _repairPreflight?.RetainedItems ?? Array.Empty<RepairPlanItem>();
        var selectedFacts = RepairSession.Describe(retained.Where(i => selected.Contains(i.ItemId)).ToList());
        if (selectedFacts.Any(f => !f.Reversible))
        {
            var answer = MessageBox.Show(
                Loc.Get("repair.confirm.nonreversible"), Loc.Get("repair.confirm.title"),
                MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No);
            if (answer != MessageBoxResult.Yes) return;
        }

        // Snapshot what the background call needs — _repairSession/_repairPlan/selected must not be
        // reassigned by another click while this one is still running (BtnRepairApply is disabled
        // below for exactly that reason, but the local copies are the actual guarantee).
        var session = _repairSession;
        var plan = _repairPlan;

        BtnRepairApply.IsEnabled = false;
        RepairApplyStatus.Text = Loc.Get("repair.apply.running");
        try
        {
            var result = await Task.Run(() => session.Apply(plan, selected));

            // 19/08 — Execute() succeeding only ever means "the action ran"; for most of the 4
            // built-in actions that IS the fix (they write synchronously). It is not a universal
            // guarantee — an action that launches an external tool (e.g. a component's own
            // registration installer) can report success the instant the tool's window opens,
            // before the user has done anything inside it. Verify() re-probes the real, current
            // state (RepairEngine.StillApplies) for every item Execute() reported OK, so "applied"
            // never overclaims. Skipped for a forced dry-run: nothing was written, so there is
            // nothing real to re-probe — every dry-run item stays counted as if applied, as before.
            var verified = result.ForcedDryRun
                ? new Dictionary<string, bool>()
                : await Task.Run(() => session.Verify(plan));

            var executedOk = result.ItemOutcomes.Where(kv => kv.Value).Select(kv => kv.Key).ToList();
            var failed = result.ItemOutcomes.Count(kv => !kv.Value);
            // Missing from verified (e.g. forced dry-run, or an item Verify() could not re-probe)
            // defaults to "confirmed" — the pre-19/08 behavior for every action that does not run
            // into this gap, never a new way to under-report a real success.
            var ok = executedOk.Count(id => !verified.TryGetValue(id, out var fixedNow) || fixedNow);
            var pending = executedOk.Count(id => verified.TryGetValue(id, out var fixedNow) && !fixedNow);

            RepairApplyStatus.Text = string.Format(Loc.Get("repair.apply.status"), ok, failed);
            if (result.ForcedDryRun)
                RepairApplyStatus.Text = Loc.Get("repair.forceddryrun.applied") + "\n" + RepairApplyStatus.Text;
            if (pending > 0)
                RepairApplyStatus.Text += "\n" + string.Format(Loc.Get("repair.apply.pending"), pending);

            // LOT Repair (18/08): the per-item failure reason existed inside the engine all along —
            // now that ApplyResult carries it (ItemFailureReasons), show it instead of leaving a
            // failed item as an unexplained count. Kept deliberately simple (raw English technical
            // text, "name — reason", one line per failed item) rather than a Loc.Get key per reason,
            // per the session prompt's own instruction — this is a diagnostic detail, not a Finding
            // message that needs translating. Skips items where RecoveryRequired already covers them
            // (see ItemFailureReasons' own doc comment) to avoid a duplicate display.
            if (failed > 0 && result.ItemFailureReasons.Count > 0)
            {
                foreach (var kv in result.ItemOutcomes.Where(kv => !kv.Value))
                {
                    if (!result.ItemFailureReasons.TryGetValue(kv.Key, out var reason) || string.IsNullOrWhiteSpace(reason))
                        continue;
                    var name = _repairItemRows.FirstOrDefault(r => r.ItemId == kv.Key)?.Description ?? kv.Key;
                    RepairApplyStatus.Text += $"\n{name} — {reason}";
                }
            }

            if (result.RecoveryRequired)
            {
                RepairApplyStatus.Text += "\n" + Loc.Get("repair.apply.recovery")
                    + (string.IsNullOrEmpty(result.BackupPath) ? "" : " " + result.BackupPath);
            }

            // LOT Rescore (18/08, extended 19/08): offered after a real, successful (or partially
            // successful) Apply — at least one item confirmed fixed OR still pending confirmation
            // (a pending item is exactly the case where rescoring is the way to find out) — and
            // never for a forced dry-run, since that path performs zero disk I/O and rescanning it
            // would just silently reproduce the same score. RescoreStatus is reset here too so a
            // stale old→new line from a previous Apply never lingers under a fresh one.
            RescoreStatus.Visibility = Visibility.Collapsed;
            RescoreStatus.Text = "";
            BtnRescore.Visibility = (ok + pending > 0 && !result.ForcedDryRun) ? Visibility.Visible : Visibility.Collapsed;

            // Re-plan from the same scan so the checklist reflects what actually got fixed (a change
            // just applied must not still be offered as if it were still broken).
            _repairPlan = session.Plan(plan.ScanReportId, _report.Findings, licensed: _licensed);
            _repairPreflight = session.Preflight(_repairPlan);
            RefreshRepairItemsList();
            RefreshRepairUndoList();
        }
        finally
        {
            BtnRepairApply.IsEnabled = true;
        }
    }

    /// <summary>
    /// H.2 rule 5 — works even for a plan from a previous app session, because the journal is on
    /// disk. One button per row now (13/08) instead of a shared button + separate list selection —
    /// the plan being undone is read straight off the button that was clicked (<c>Tag</c>), so
    /// there is no separate "select a plan first" step left to forget or misuse.
    /// </summary>
    private void BtnRepairUndoRow_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: string planId }) return;
        _repairSession ??= NewRepairSessionForBrowsing();

        var result = _repairSession.Undo(planId);
        RepairUndoStatus.Text = result.Success
            ? Loc.Get("repair.undo.ok")
            : Loc.Get("repair.undo.fail") + (string.IsNullOrEmpty(result.Error) ? "" : " " + result.Error);

        // The plan just undone may be the one currently on screen — refresh the checklist too so it
        // does not keep showing items as fixed when they were just reverted.
        if (_repairPlan is not null && _repairPlan.PlanId == planId && _report is not null)
        {
            _repairPreflight = _repairSession.Preflight(_repairPlan);
            RefreshRepairItemsList();
        }
        RefreshRepairUndoList();
    }

    private void BtnExport_Click(object sender, RoutedEventArgs e)
    {
        if (_report is null) return;
        var dlg = new SaveFileDialog
        {
            FileName = $"pincab-toolbox-report-{DateTime.Now:yyyyMMdd-HHmm}",
            DefaultExt = ".html",
            Filter = "HTML report (*.html)|*.html|PDF report (*.pdf)|*.pdf|Text report (*.txt)|*.txt|Markdown for forums (*.md)|*.md|BBCode for forums (*.bbcode)|*.bbcode|JSON report (*.json)|*.json",
        };
        if (dlg.ShowDialog(this) != true) return;

        try
        {
            var file = dlg.FileName;
            if (file.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                var payload = _report.Ordered().Select(f => new
                {
                    severity = f.Severity.ToString(),
                    code = f.Code,
                    category = f.Category,
                    subject = f.Subject,
                    file = Rel(f.FilePath),
                    message = f.EnglishText,
                    fixHint = f.FixHint,
                });
                File.WriteAllText(file, Public(JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true })));
            }
            else if (file.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
            {
                File.WriteAllText(file, Public(BuildHtmlReport()), Encoding.UTF8);
            }
            else if (file.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            {
                // Binary output: nothing to run Public() on afterwards, so every line is scrubbed
                // on the way in instead — same guarantee (ADR-003) as the text-based formats above.
                var scrubbedLines = BuildPdfLines().Select(Public).ToList();
                File.WriteAllBytes(file, PdfDocumentBuilder.Build("Pincab Toolbox - Scan Report", scrubbedLines));
            }
            else if (file.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
            {
                File.WriteAllText(file, Public(BuildForumMarkdown()), Encoding.UTF8);
            }
            else if (file.EndsWith(".bbcode", StringComparison.OrdinalIgnoreCase))
            {
                File.WriteAllText(file, Public(BuildBBCode()), Encoding.UTF8);
            }
            else
            {
                File.WriteAllText(file, Public(BuildTextReport()), Encoding.UTF8);
            }
            LblStatus.Text = Loc.Get("report.saved") + file;
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Pincab Toolbox");
        }
    }

    private void BtnCopyForum_Click(object sender, RoutedEventArgs e)
    {
        if (_report is null) return;
        try
        {
            Clipboard.SetText(Public(BuildForumMarkdown()));
            LblStatus.Text = Loc.Get("report.copied");
            FlashButton(BtnCopyForum, Loc.Get("scan.copied"), Loc.Get("scan.copyforum"));
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Pincab Toolbox");
        }
    }

    /// <summary>
    /// Last gate before a report leaves the machine. Every report is a PUBLIC document —
    /// the product asks people to paste it on a forum — and an absolute Windows path carries
    /// the account name. Nothing goes to a file or to the clipboard without passing here (ADR-003).
    /// </summary>
    private static string Public(string report) => PathScrubber.Scrub(report, Environment.UserName);

    /// <summary>Path relative to the scanned root (readable reports); falls back to the absolute path.</summary>
    private string Rel(string? path)
    {
        if (string.IsNullOrEmpty(path)) return "";
        var root = _report?.Layout.RootPath;
        if (!string.IsNullOrEmpty(root))
        {
            try
            {
                var r = Path.GetRelativePath(root, path);
                if (!r.StartsWith("..", StringComparison.Ordinal)) return r;
            }
            catch { /* fall back to absolute */ }
        }
        return path;
    }

    /// <summary>
    /// Same content and the same completeness contract as <see cref="BuildTextReport"/> — full
    /// <see cref="ScanReport.Ordered"/>, nothing collapsed — but as loose lines instead of one
    /// string, since <see cref="PdfDocumentBuilder"/> word-wraps and paginates line by line. PDF
    /// deliberately does NOT use <c>Rolled()</c> like HTML/MD/BBCode: it is meant to be the
    /// "give me everything" printable format alongside TXT/JSON, not a forum-sized summary — see
    /// FIELD-LOG 2026-08-13 for why that distinction matters (a prior reply told Gregg every
    /// format was complete, which was wrong for HTML/MD/BBCode).
    /// </summary>
    private List<string> BuildPdfLines()
    {
        var r = _report!;
        var lines = new List<string>
        {
            $"Pincab Toolbox — Free Scanner 0.1.1 — {DateTime.Now:yyyy-MM-dd HH:mm}",
            $"Health score: {r.Score}/100 ({r.Grade})",
            $"Root: {r.Layout.RootPath}",
            "",
        };
        foreach (var f in r.Ordered())
        {
            lines.Add($"[{f.Severity.ToString().ToUpperInvariant()}] ({f.Category}) {Loc.FindingText(f)}");
            var rel = Rel(f.FilePath);
            if (!string.IsNullOrEmpty(rel)) lines.Add($"   .\\{rel}");
            var fix = Loc.FixHintText(f);
            if (!string.IsNullOrEmpty(fix)) lines.Add($"   fix: {fix}");
            lines.Add("");
        }
        lines.Add($"{r.Count(Severity.Critical)} critical, {r.Count(Severity.Warning)} warnings, {r.Count(Severity.Note)} notes, {r.Count(Severity.Info)} info, {r.Count(Severity.Ok)} ok");
        return lines;
    }

    private string BuildTextReport()
    {
        var r = _report!;
        var sb = new StringBuilder();
        sb.AppendLine($"Pincab Toolbox — Free Scanner 0.1.1 — {DateTime.Now:yyyy-MM-dd HH:mm}");
        sb.AppendLine($"Health score: {r.Score}/100 ({r.Grade})");
        sb.AppendLine($"Root: {r.Layout.RootPath}");
        sb.AppendLine(new string('-', 80));
        foreach (var f in r.Ordered())
        {
            sb.AppendLine($"[{f.Severity.ToString().ToUpperInvariant(),-8}] ({f.Category}) {Loc.FindingText(f)}");
            var rel = Rel(f.FilePath);
            if (!string.IsNullOrEmpty(rel)) sb.AppendLine($"           .\\{rel}");
            var fix = Loc.FixHintText(f);
            if (!string.IsNullOrEmpty(fix)) sb.AppendLine($"           fix: {fix}");
        }
        sb.AppendLine(new string('-', 80));
        sb.AppendLine($"{r.Count(Severity.Critical)} critical, {r.Count(Severity.Warning)} warnings, {r.Count(Severity.Note)} notes, {r.Count(Severity.Info)} info, {r.Count(Severity.Ok)} ok");
        return sb.ToString();
    }

    private string BuildForumMarkdown()
    {
        var r = _report!;
        var sb = new StringBuilder();
        sb.AppendLine($"**Pincab Toolbox — scan report** · {DateTime.Now:yyyy-MM-dd HH:mm}");
        sb.AppendLine();
        sb.AppendLine($"**Health score: {r.Score}/100 ({r.Grade})** — {r.Count(Severity.Critical)} critical · {r.Count(Severity.Warning)} warnings · {r.Count(Severity.Note)} notes · {r.Count(Severity.Info)} info");
        sb.AppendLine();
        // Severity.Note MUST be in this array — it's what actually puts Note findings in the forum
        // export. Without it they don't render wrong, they silently vanish (found while wiring the
        // Note doctrine's App rendering; the other 3 severities were already exhaustive by construction).
        foreach (var sev in new[] { Severity.Critical, Severity.Warning, Severity.Note, Severity.Info })
        {
            var items = r.Rolled().Where(f => f.Severity == sev).ToList();   // shareable = readable
            if (items.Count == 0) continue;
            sb.AppendLine($"### {Loc.SeverityLabel(sev)} ({items.Count})");
            foreach (var f in items)
            {
                sb.AppendLine($"- **{f.Subject}** — {Loc.FindingText(f)}");
                var fix = Loc.FixHintText(f);
                if (!string.IsNullOrEmpty(fix)) sb.AppendLine($"  - fix: {fix}");
            }
            sb.AppendLine();
        }
        sb.AppendLine("_Scanned with Pincab Toolbox — https://pincab-toolbox.vercel.app_");
        return sb.ToString();
    }

    private string BuildBBCode()
    {
        var r = _report!;
        static string SevColor(Severity s) => s switch
        {
            Severity.Critical => "red",
            Severity.Warning => "orange",
            Severity.Note => "purple",
            Severity.Info => "royalblue",
            _ => "green",
        };
        var scoreColor = r.Score >= 90 ? "green" : r.Score >= 70 ? "orange" : "red";
        var sb = new StringBuilder();
        sb.AppendLine($"[b]Pincab Toolbox — scan report[/b] ({DateTime.Now:yyyy-MM-dd HH:mm})");
        sb.AppendLine($"[b]Health score: [color={scoreColor}]{r.Score}/100 ({r.Grade})[/color][/b] — {r.Count(Severity.Critical)} critical, {r.Count(Severity.Warning)} warnings, {r.Count(Severity.Note)} notes, {r.Count(Severity.Info)} info");
        sb.AppendLine("[list]");
        foreach (var f in r.Rolled().Where(f => f.Severity != Severity.Ok))
        {
            sb.AppendLine($"[*][b][color={SevColor(f.Severity)}]{Loc.SeverityLabel(f.Severity)}[/color][/b] — {f.Subject}: {Loc.FindingText(f)}");
            var fix = Loc.FixHintText(f);
            if (!string.IsNullOrEmpty(fix)) sb.AppendLine($"→ {fix}");
        }
        sb.AppendLine("[/list]");
        sb.AppendLine("[i]Scanned with Pincab Toolbox — https://pincab-toolbox.vercel.app[/i]");
        return sb.ToString();
    }

    private string BuildHtmlReport()
    {
        var r = _report!;
        static string Esc(string? s) => System.Net.WebUtility.HtmlEncode(s ?? "");
        var scoreColor = r.Score >= 90 ? "#46C06E" : r.Score >= 70 ? "#F5A524" : "#E5484D";
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html><html lang=\"en\"><head><meta charset=\"UTF-8\">");
        sb.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">");
        sb.AppendLine("<title>Pincab Toolbox — Scan Report</title><style>");
        sb.AppendLine("body{background:#15151B;color:#ECECF2;font:15px/1.6 system-ui,'Segoe UI',Roboto,sans-serif;margin:0;padding:32px}");
        sb.AppendLine(".wrap{max-width:960px;margin:0 auto}");
        // Vert de marque (lot 1, rebranding 17/08) — remplace l'orange dans le rapport HTML exporté.
        sb.AppendLine("h1{font-size:24px;margin:0}h1 span{color:#708830}");
        sb.AppendLine(".meta{color:#9C9CAC;font-size:13px;margin:4px 0 22px}");
        sb.AppendLine(".score{display:inline-flex;align-items:baseline;gap:8px;border:1px solid #34343F;background:#1E1E26;border-radius:12px;padding:12px 18px;margin-bottom:18px}");
        sb.AppendLine(".score b{font-size:30px;font-weight:900}.score .g{font-weight:700}");
        sb.AppendLine(".sum{color:#9C9CAC;font-size:14px;margin-bottom:22px}");
        sb.AppendLine("table{width:100%;border-collapse:collapse;font-size:14px}");
        sb.AppendLine("th,td{text-align:left;padding:10px 12px;border-bottom:1px solid #26262F;vertical-align:top}");
        sb.AppendLine("th{color:#9C9CAC;font-size:12px;text-transform:uppercase;letter-spacing:.5px}");
        sb.AppendLine(".sev{font-weight:800;white-space:nowrap}.c{color:#E5484D}.w{color:#F5A524}.n{color:#B58DF5}.i{color:#3E9CF3}.o{color:#46C06E}");
        sb.AppendLine(".fix{color:#46C06E;font-size:12.5px}.path{color:#6b6b78;font-family:Consolas,monospace;font-size:11.5px}");
        sb.AppendLine("footer{color:#6b6b78;font-size:12px;margin-top:26px}");
        sb.AppendLine("</style></head><body><div class=\"wrap\">");
        sb.AppendLine("<h1>PINCAB <span>TOOLBOX</span></h1>");
        sb.AppendLine($"<div class=\"meta\">Scan report · {DateTime.Now:yyyy-MM-dd HH:mm}</div>");
        sb.AppendLine($"<div class=\"score\"><b style=\"color:{scoreColor}\">{r.Score}</b><span style=\"color:#9C9CAC\">/100</span><span class=\"g\" style=\"color:{scoreColor}\">{Esc(r.Grade)}</span></div>");
        sb.AppendLine($"<div class=\"sum\">{r.Count(Severity.Critical)} critical · {r.Count(Severity.Warning)} warnings · {r.Count(Severity.Note)} notes · {r.Count(Severity.Info)} info · {r.Count(Severity.Ok)} ok</div>");
        sb.AppendLine("<table><tr><th>Severity</th><th>Module</th><th>Subject</th><th>Details</th></tr>");
        // Rolled: the shared HTML report must stay readable on a 2000-table collection.
        foreach (var f in r.Rolled())
        {
            // Explicit Note arm on purpose: the wildcard used to send Note to "o" (Ok's green class),
            // painting a heuristic finding as a reassuring confirmation. Same bug family as the screen
            // SevBrush/RowBg switches above.
            var cls = f.Severity switch { Severity.Critical => "c", Severity.Warning => "w", Severity.Note => "n", Severity.Info => "i", Severity.Ok => "o", _ => "o" };
            var rel = Rel(f.FilePath);
            var extra = "";
            var fix = Loc.FixHintText(f);
            if (!string.IsNullOrEmpty(fix)) extra += $"<div class=\"fix\">→ {Esc(fix)}</div>";
            if (!string.IsNullOrEmpty(rel)) extra += $"<div class=\"path\">.\\{Esc(rel)}</div>";
            sb.AppendLine($"<tr><td class=\"sev {cls}\">{Esc(Loc.SeverityLabel(f.Severity))}</td><td>{Esc(f.Category)}</td><td>{Esc(f.Subject)}</td><td>{Esc(Loc.FindingText(f))}{extra}</td></tr>");
        }
        sb.AppendLine("</table>");
        sb.AppendLine("<footer>Generated by Pincab Toolbox — free, read-only diagnostic scanner · https://pincab-toolbox.vercel.app</footer>");
        sb.AppendLine("</div></body></html>");
        return sb.ToString();
    }

    // ---------------- diff ----------------

    private void PickDiffFile(System.Windows.Controls.TextBox target)
    {
        var dlg = new OpenFileDialog
        {
            Filter = "Tables & scripts (*.vpx;*.vbs;*.txt)|*.vpx;*.vbs;*.txt|All files (*.*)|*.*",
        };
        if (dlg.ShowDialog(this) == true)
            target.Text = dlg.FileName;
    }

    private void BtnDiffOldBrowse_Click(object sender, RoutedEventArgs e) => PickDiffFile(TxtDiffOld);
    private void BtnDiffNewBrowse_Click(object sender, RoutedEventArgs e) => PickDiffFile(TxtDiffNew);

    private async void BtnCompare_Click(object sender, RoutedEventArgs e)
    {
        var oldPath = TxtDiffOld.Text.Trim();
        var newPath = TxtDiffNew.Text.Trim();
        if (!File.Exists(oldPath) || !File.Exists(newPath))
        {
            LblDiffSummary.Text = Loc.Get("diff.placeholder");
            return;
        }

        BtnCompare.IsEnabled = false;
        try
        {
            var result = await Task.Run(() => DiffService.DiffFiles(oldPath, newPath));
            if (result.Error is not null)
            {
                LblDiffSummary.Text = result.Error;
                ListDiff.ItemsSource = null;
                return;
            }

            var rows = new List<DiffRow>(result.OldLines.Count);
            for (int i = 0; i < result.OldLines.Count; i++)
            {
                var o = result.OldLines[i];
                var n = result.NewLines[i];
                rows.Add(new DiffRow
                {
                    OldNum = o.Number?.ToString() ?? "",
                    OldText = o.Text,
                    OldBrush = o.Kind switch
                    {
                        Core.Services.DiffLineKind.Deleted => DiffDel,
                        Core.Services.DiffLineKind.Modified => DiffMod,
                        _ => Brushes.Transparent,
                    },
                    NewNum = n.Number?.ToString() ?? "",
                    NewText = n.Text,
                    NewBrush = n.Kind switch
                    {
                        Core.Services.DiffLineKind.Inserted => DiffIns,
                        Core.Services.DiffLineKind.Modified => DiffMod,
                        _ => Brushes.Transparent,
                    },
                });
            }
            ListDiff.ItemsSource = rows;
            DiffEmpty.Visibility = Visibility.Collapsed;
            LblDiffSummary.Text = string.Format(Loc.Get("diff.summary"),
                result.ModifiedCount, result.InsertedCount, result.DeletedCount);
        }
        finally
        {
            BtnCompare.IsEnabled = true;
        }
    }
}
