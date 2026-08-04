using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Input;
using Microsoft.Win32;
using PincabToolbox.App.Localization;
using PincabToolbox.Core.Models;
using PincabToolbox.Core.Profiles;
using PincabToolbox.Core.Scanning;
using PincabToolbox.Core.Services;

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

public sealed class DiffRow
{
    public string OldNum { get; init; } = "";
    public string OldText { get; init; } = "";
    public Brush OldBrush { get; init; } = Brushes.Transparent;
    public string NewNum { get; init; } = "";
    public string NewText { get; init; } = "";
    public Brush NewBrush { get; init; } = Brushes.Transparent;
}

public partial class MainWindow : Window
{
    private ScanReport? _report;
    private CancellationTokenSource? _cts;
    private readonly Settings _settings = Settings.Load();

    private bool _showCritical = true, _showWarning = true, _showInfo = true, _showOk = false;
    private string? _sortKey;
    private bool _sortAsc = true;
    private string? _demoRoot;                       // real demo path while the box shows a friendly label
    private System.Windows.Threading.DispatcherTimer? _flashTimer;

    private static readonly Brush BrushCritical = new SolidColorBrush(Color.FromRgb(0xFF, 0x6B, 0x6E));
    private static readonly Brush BrushWarning = new SolidColorBrush(Color.FromRgb(0xF5, 0xA5, 0x24));
    private static readonly Brush BrushInfo = new SolidColorBrush(Color.FromRgb(0x3E, 0x9C, 0xF3));
    private static readonly Brush BrushOk = new SolidColorBrush(Color.FromRgb(0x46, 0xC0, 0x6E));

    private static readonly Brush RowCritical = new SolidColorBrush(Color.FromArgb(0x1E, 0xE5, 0x48, 0x4D));
    private static readonly Brush RowWarning = new SolidColorBrush(Color.FromArgb(0x12, 0xF5, 0xA5, 0x24));
    private static readonly Brush RowInfo = new SolidColorBrush(Color.FromArgb(0x14, 0x3E, 0x9C, 0xF3));
    private static readonly Brush RowOk = new SolidColorBrush(Color.FromArgb(0x14, 0x46, 0xC0, 0x6E));

    private static readonly Regex UrlRx = new(@"https?://[^\s""'<>)\]]+", RegexOptions.Compiled);

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
    }

    private void OnbStart_Click(object sender, RoutedEventArgs e)
    {
        OnboardingOverlay.Visibility = Visibility.Collapsed;
        _settings.OnboardingSeen = true;
        _settings.Save();
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
        Title = Loc.Get("app.title");
        HeaderTagline.Text = Loc.Get("about.tagline");
        TabScannerHeader.Text = Loc.Get("tab.scanner");
        TabDiffHeader.Text = Loc.Get("tab.diff");
        TabAboutHeader.Text = Loc.Get("tab.about");
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
        AboutRoadmap.Text = Loc.Get("about.roadmap");
        AboutVersion.Text = Loc.Get("about.version") + " 0.1.1";
        OnbTitle.Text = Loc.Get("onb.title");
        OnbLead.Text = Loc.Get("onb.lead");
        OnbP1.Text = Loc.Get("onb.p1");
        OnbP2.Text = Loc.Get("onb.p2");
        OnbP3.Text = Loc.Get("onb.p3");
        OnbStart.Content = Loc.Get("onb.start");
        if (string.IsNullOrEmpty(LblStatus.Text) || LblStatus.Text == "Prêt." || LblStatus.Text == "Ready.")
            LblStatus.Text = Loc.Get("status.ready");
        if (_report is not null) RefreshList();
    }

    private void BtnLang_Click(object sender, RoutedEventArgs e) => Loc.Toggle();

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

    private async void BtnScan_Click(object sender, RoutedEventArgs e)
    {
        if (_cts is not null) return;
        var root = (_demoRoot is not null && TxtRoot.Text == Loc.Get("scan.demolabel"))
            ? _demoRoot : TxtRoot.Text.Trim();
        if (root.Length == 0 || !Directory.Exists(root))
        {
            LblStatus.Text = Loc.Get("scan.placeholder");
            return;
        }

        Profile profile;
        try
        {
            profile = Profile.Load(Path.Combine(AppContext.BaseDirectory, "profiles", "vpx-popper.json"));
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "profiles/vpx-popper.json: " + ex.Message, "Pincab Toolbox");
            return;
        }

        _cts = new CancellationTokenSource();
        BtnScan.IsEnabled = false;
        BtnScan.Content = Loc.Get("scan.running");
        BtnCancel.Visibility = Visibility.Visible;
        ScanProgress.Visibility = Visibility.Visible;
        BtnExport.IsEnabled = false;
        BtnCopyForum.IsEnabled = false;
        LblPlaceholder.Text = "";

        var progress = new Progress<string>(msg => LblStatus.Text = msg);
        try
        {
            var ct = _cts.Token;
            var report = await Task.Run(async () =>
            {
                var vps = await new VpsDatabase(profile.UpdateSource).LoadAsync(ct).ConfigureAwait(false);
                var engine = new ScanEngine()
                    .Add(new RomValidatorScanner())
                    .Add(new BitnessScanner())
                    .Add(new CompletenessScanner())
                    .Add(new CompatibilityScanner())
                    .Add(new BlockedFileScanner())
                    .Add(new DependencyScanner())
                    .Add(new DiskSpaceScanner())
                    .Add(new LegacyTableScanner())
                    .Add(new PinupDisplayZombieScanner())
                    .Add(new DisplaySetupScanner())
                    .Add(new OrphanedMediaScanner())
                    .Add(new UpdateWatcherScanner(vps));
                return engine.Run(root, profile, progress, ct);
            }, ct);

            _report = report;
            RefreshList();
            LblStatus.Text = string.Format(Loc.Get("status.done"), report.Findings.Count,
                report.Count(Severity.Critical), report.Count(Severity.Warning), report.Count(Severity.Info));
            if (report.Layout.VpxTables.Count == 0)
                LblPlaceholder.Text = Loc.Get("scan.hint.notables");
            BtnExport.IsEnabled = true;
            BtnCopyForum.IsEnabled = true;
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
        ChipInfo.Text = $"{_report.Count(Severity.Info)} {Loc.Get("filter.info")}";
        ChipOk.Text = $"{_report.Count(Severity.Ok)} {Loc.Get("filter.ok")}";

        // health score chip
        ScoreChip.Visibility = Visibility.Visible;
        ScoreValue.Text = _report.Score.ToString();
        ScoreGrade.Text = _report.Grade;
        ScoreStatus.Text = Loc.Get(_report.Score >= 90 ? "score.a" : _report.Score >= 70 ? "score.b" : _report.Score >= 40 ? "score.c" : "score.f");
        var scoreBrush = _report.Score >= 90 ? BrushOk : _report.Score >= 70 ? BrushWarning : BrushCritical;
        ScoreValue.Foreground = scoreBrush;
        ScoreGrade.Foreground = scoreBrush;

        // primary insight — a correlated scenario if detectable, else the single most severe issue
        var present = new HashSet<string>(_report.Findings.Select(f => f.Code));
        var scenario = Scenarios.Detect(present);
        if (scenario is not null)
        {
            PriorityLabel.Text = $"{Loc.Get("diagnosis.label")} · {Loc.Get("diagnosis.confidence")} {scenario.Confidence}%";
            PriorityText.Text = $"{scenario.Title} — {scenario.Explanation}";
            PriorityFix.Visibility = Visibility.Collapsed;
            PriorityAccent.Background = BrushCritical;
            // transparency — show which findings triggered this correlation (never a black box)
            var triggers = _report.Findings
                .Where(f => scenario.TriggeredBy.Contains(f.Code) && !string.IsNullOrEmpty(f.Subject))
                .Select(f => f.Subject).Distinct().ToList();
            PriorityTriggers.Text = triggers.Count > 0 ? $"{Loc.Get("priority.basedon")} {string.Join(", ", triggers)}" : "";
            PriorityTriggers.Visibility = triggers.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
            PriorityBanner.Visibility = Visibility.Visible;
        }
        else
        {
            PriorityTriggers.Visibility = Visibility.Collapsed;
            var top = _report.Ordered().FirstOrDefault(f => f.Severity == Severity.Critical)
                      ?? _report.Ordered().FirstOrDefault(f => f.Severity == Severity.Warning);
            if (top is not null)
            {
                // "Fix this first" is reserved for genuine breakage (Critical). A lone Warning
                // is worth a look, not an emergency — a softer label + warning accent, so a
                // healthy install with only minor notes never gets an alarming red "FIX THIS
                // FIRST" on a non-issue (FIELD-LOG 2026-07-30 / FD's compat note surfaced there).
                var isCritical = top.Severity == Severity.Critical;
                PriorityLabel.Text = Loc.Get(isCritical ? "priority.label" : "priority.watch");
                PriorityText.Text = Loc.FindingText(top);
                var pfix = Loc.FixHintText(top);
                PriorityFix.Text = string.IsNullOrEmpty(pfix) ? "" : "→ " + pfix;
                PriorityFix.Visibility = string.IsNullOrEmpty(pfix) ? Visibility.Collapsed : Visibility.Visible;
                PriorityAccent.Background = isCritical ? BrushCritical : BrushWarning;
                PriorityBanner.Visibility = Visibility.Visible;
            }
            else PriorityBanner.Visibility = Visibility.Collapsed;
        }

        bool Show(Severity s) => s switch
        {
            Severity.Critical => _showCritical,
            Severity.Warning => _showWarning,
            Severity.Info => _showInfo,
            _ => _showOk,
        };

        // Rolled(), not Ordered(): repetitive per-table findings collapse to one counted row
        // so the handful that matter stay visible. Criticals are never collapsed.
        // The full text export keeps every line. (FIELD-LOG 2026-08-03.)
        var rows = _report.Rolled()
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
                    SevBrush = f.Severity switch
                    {
                        Severity.Critical => BrushCritical,
                        Severity.Warning => BrushWarning,
                        Severity.Info => BrushInfo,
                        _ => BrushOk,
                    },
                    RowBg = f.Severity switch
                    {
                        Severity.Critical => RowCritical,
                        Severity.Warning => RowWarning,
                        Severity.Info => RowInfo,
                        _ => RowOk,
                    },
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

        DetailImpactLabel.Text = Loc.Get("detail.impact");
        SetSection(DetailImpactLabel, DetailImpact, Knowledge.Impact(row.Code));

        DetailCauseLabel.Text = Loc.Get("detail.cause");
        SetSection(DetailCauseLabel, DetailCause, Knowledge.Cause(row.Code));

        DetailFixLabel.Text = Loc.Get("detail.fix");
        SetSection(DetailFixLabel, DetailFix, row.FixHint);

        DetailRepairTagText.Text = Loc.Get("repair.tag");
        DetailRepairTag.Visibility = Knowledge.IsAutoFixable(row.Code) ? Visibility.Visible : Visibility.Collapsed;

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

    private void BtnCloseDetail_Click(object sender, RoutedEventArgs e)
    {
        ListFindings.SelectedItem = null; // clears selection → SelectionChanged hides the panel
    }

    private void BtnExport_Click(object sender, RoutedEventArgs e)
    {
        if (_report is null) return;
        var dlg = new SaveFileDialog
        {
            FileName = $"pincab-toolbox-report-{DateTime.Now:yyyyMMdd-HHmm}",
            DefaultExt = ".html",
            Filter = "HTML report (*.html)|*.html|Text report (*.txt)|*.txt|Markdown for forums (*.md)|*.md|BBCode for forums (*.bbcode)|*.bbcode|JSON report (*.json)|*.json",
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
        sb.AppendLine($"{r.Count(Severity.Critical)} critical, {r.Count(Severity.Warning)} warnings, {r.Count(Severity.Info)} info, {r.Count(Severity.Ok)} ok");
        return sb.ToString();
    }

    private string BuildForumMarkdown()
    {
        var r = _report!;
        var sb = new StringBuilder();
        sb.AppendLine($"**Pincab Toolbox — scan report** · {DateTime.Now:yyyy-MM-dd HH:mm}");
        sb.AppendLine();
        sb.AppendLine($"**Health score: {r.Score}/100 ({r.Grade})** — {r.Count(Severity.Critical)} critical · {r.Count(Severity.Warning)} warnings · {r.Count(Severity.Info)} info");
        sb.AppendLine();
        foreach (var sev in new[] { Severity.Critical, Severity.Warning, Severity.Info })
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
            Severity.Info => "royalblue",
            _ => "green",
        };
        var scoreColor = r.Score >= 90 ? "green" : r.Score >= 70 ? "orange" : "red";
        var sb = new StringBuilder();
        sb.AppendLine($"[b]Pincab Toolbox — scan report[/b] ({DateTime.Now:yyyy-MM-dd HH:mm})");
        sb.AppendLine($"[b]Health score: [color={scoreColor}]{r.Score}/100 ({r.Grade})[/color][/b] — {r.Count(Severity.Critical)} critical, {r.Count(Severity.Warning)} warnings, {r.Count(Severity.Info)} info");
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
        sb.AppendLine("h1{font-size:24px;margin:0}h1 span{color:#FF9F1C}");
        sb.AppendLine(".meta{color:#9C9CAC;font-size:13px;margin:4px 0 22px}");
        sb.AppendLine(".score{display:inline-flex;align-items:baseline;gap:8px;border:1px solid #34343F;background:#1E1E26;border-radius:12px;padding:12px 18px;margin-bottom:18px}");
        sb.AppendLine(".score b{font-size:30px;font-weight:900}.score .g{font-weight:700}");
        sb.AppendLine(".sum{color:#9C9CAC;font-size:14px;margin-bottom:22px}");
        sb.AppendLine("table{width:100%;border-collapse:collapse;font-size:14px}");
        sb.AppendLine("th,td{text-align:left;padding:10px 12px;border-bottom:1px solid #26262F;vertical-align:top}");
        sb.AppendLine("th{color:#9C9CAC;font-size:12px;text-transform:uppercase;letter-spacing:.5px}");
        sb.AppendLine(".sev{font-weight:800;white-space:nowrap}.c{color:#E5484D}.w{color:#F5A524}.i{color:#3E9CF3}.o{color:#46C06E}");
        sb.AppendLine(".fix{color:#46C06E;font-size:12.5px}.path{color:#6b6b78;font-family:Consolas,monospace;font-size:11.5px}");
        sb.AppendLine("footer{color:#6b6b78;font-size:12px;margin-top:26px}");
        sb.AppendLine("</style></head><body><div class=\"wrap\">");
        sb.AppendLine("<h1>PINCAB <span>TOOLBOX</span></h1>");
        sb.AppendLine($"<div class=\"meta\">Scan report · {DateTime.Now:yyyy-MM-dd HH:mm}</div>");
        sb.AppendLine($"<div class=\"score\"><b style=\"color:{scoreColor}\">{r.Score}</b><span style=\"color:#9C9CAC\">/100</span><span class=\"g\" style=\"color:{scoreColor}\">{Esc(r.Grade)}</span></div>");
        sb.AppendLine($"<div class=\"sum\">{r.Count(Severity.Critical)} critical · {r.Count(Severity.Warning)} warnings · {r.Count(Severity.Info)} info · {r.Count(Severity.Ok)} ok</div>");
        sb.AppendLine("<table><tr><th>Severity</th><th>Module</th><th>Subject</th><th>Details</th></tr>");
        // Rolled: the shared HTML report must stay readable on a 2000-table collection.
        foreach (var f in r.Rolled())
        {
            var cls = f.Severity switch { Severity.Critical => "c", Severity.Warning => "w", Severity.Info => "i", _ => "o" };
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
