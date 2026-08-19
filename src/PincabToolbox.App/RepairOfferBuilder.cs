using System;
using System.IO;
using System.Linq;
using PincabToolbox.Core.Models;
using PincabToolbox.Repair;
using PincabToolbox.Repair.Actions;

namespace PincabToolbox.App;

/// <summary>
/// Écran 1 ONLY — "Repair available", the free/pre-purchase view (DESIGN-Repair-v1.md §10,
/// UX-COPY-Repair.md). Builds a <see cref="RepairOffer"/> from a completed scan by calling
/// <see cref="IRepairEngine.Plan"/> with <c>licensed: false</c> — the same pure, read-only path
/// <see cref="RepairOffer"/> itself requires (ADR-006: it throws if handed a licensed plan).
///
/// <para>
/// Deliberately stops here. <see cref="IRepairEngine.Preflight"/>, <c>Apply</c> and <c>Undo</c> —
/// the write path — are never called from the App. Wiring them is its own decision Maxime asked
/// to be re-asked about before it happens (HANDOFF 27/07); this class only turns on the free
/// summary the scanner already computes internally, same as <c>RepairOffer</c> was designed for.
/// </para>
/// </summary>
public static class RepairOfferBuilder
{
    private static IKnowledgePack? _pack;

    /// <summary>
    /// Made <c>public</c> for LOT H (spec 10/08): <c>RepairSession</c>'s App-side caller needs the
    /// exact same knowledge pack Écran 1 already loads — duplicating this cache/degrade logic in
    /// MainWindow would risk the two screens silently disagreeing about which pack version is in
    /// effect. Behavior unchanged: still caches after the first call, still degrades to
    /// <see cref="KnowledgePack.Empty"/> on any failure (ADR-005).
    /// </summary>
    public static IKnowledgePack LoadPack()
    {
        if (_pack is not null) return _pack;
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "knowledge", "pack-2026.08.json");
            _pack = File.Exists(path) ? KnowledgePack.Load(File.ReadAllText(path)) : KnowledgePack.Empty;
        }
        catch
        {
            // A malformed or missing pack must never break the free scanner — same tolerance
            // KnowledgePack.Load already applies per-entry (ADR-005: unknown degrades cleanly).
            _pack = KnowledgePack.Empty;
        }
        return _pack;
    }

    /// <summary>
    /// Per-code facts for the checkmarks shown in the finding detail panel — aggregated by hand
    /// here rather than via the engine's internal <c>RepairSummary.From</c> (that factory is
    /// intentionally <c>internal</c> to PincabToolbox.Repair); AND across items sharing a code so
    /// one non-reversible item never lets the others claim "reversible" — same rule RepairOffer
    /// itself applies across the whole scan (ADR-006).
    /// </summary>
    public sealed record CodeSummary(bool FullyReversible, bool BackupPlanned, DurationBucket EstimatedDuration);

    public sealed record Result(RepairOffer Offer, IReadOnlyDictionary<string, CodeSummary> ByCode);

    /// <summary>
    /// Returns null on any failure — Repair is a bonus surface on top of the free scan, and a
    /// probe/COM/IO error while building the offer must never take the scan report down with it.
    /// Confines every planned action to <c>report.Layout.RootPath</c> (ADR-005) — the normal,
    /// single-install case.
    /// </summary>
    public static Result? Build(ScanReport report) => Build(report, new[] { report.Layout.RootPath });

    /// <summary>
    /// Same as <see cref="Build(ScanReport)"/>, but with an explicit confinement root list
    /// (ADR-005/ADR-011, 10/08). <b>Required</b> when <paramref name="report"/> was produced by
    /// <see cref="Scanning.DriveScanReport.ToMergedScanReport"/> — its synthesized
    /// <c>Layout.RootPath</c> is the whole drive (e.g. "C:\"), and passing that as the sole
    /// confinement root would let Repair validate a write target anywhere on the entire drive,
    /// defeating ADR-005's purpose. Callers scanning a whole drive must pass the REAL per-install
    /// roots (each <c>ScanReport.Layout.RootPath</c> from <c>DriveScanReport.Reports</c>) instead.
    /// </summary>
    public static Result? Build(ScanReport report, IEnumerable<string> confinementRoots)
    {
        try
        {
            var fs = new RealFileSystem();
            var registry = new RepairActionRegistry(
                new UnblockFileAction(fs),
                new RestoreRomArchiveAction(fs),
                new QuarantineOrphanedMediaAction(fs),
                new KillZombiePinUpDisplayAction(new RealProcessControl()),
                new RegisterComComponentAction(new RealProcessLauncher(), new RealElevatedProcessLauncher()));
            // Registered (19/08) but still inert in production: the pack has no repairRules entry
            // for COM_NOT_REGISTERED/VPINMAME_NOT_REGISTERED/COM_BITNESS_GAP yet, so RepairEngine.Plan
            // never actually offers it — see RegisterComComponentAction's own header for why.
            // SetDefaultAudioDeviceAction is intentionally excluded — not wired to any Finding yet
            // (see its own header comment); the pack has no rule referencing it either.

            var backupRoot = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "PincabToolbox", "repair-backups");

            var engine = new RepairEngine(
                registry,
                LoadPack(),
                new InMemoryRepairJournal(),        // Plan() only — nothing here ever touches disk
                new FileBackupService(fs, backupRoot),
                new RealEnvironmentProbe(backupRoot),
                new SystemClock(),
                confinementRoots.ToArray(),
                report.Layout);

            var scanReportId = $"scan-{report.StartedAt:yyyyMMdd-HHmmss}";
            var plan = engine.Plan(scanReportId, report.Findings, licensed: false);
            var offer = RepairOffer.From(plan, report.Findings.Count);

            var byCode = plan.Items
                .Where(i => i.Mode == RepairMode.Locked && i.Summary is { ChangeCount: > 0 })
                .GroupBy(i => i.TargetCode, StringComparer.Ordinal)
                .ToDictionary(
                    g => g.Key,
                    g => new CodeSummary(
                        g.All(i => i.Summary!.FullyReversible),
                        g.All(i => i.Summary!.BackupPlanned),
                        g.Max(i => i.Summary!.EstimatedDuration)),
                    StringComparer.Ordinal);

            return new Result(offer, byCode);
        }
        catch
        {
            return null;
        }
    }
}
