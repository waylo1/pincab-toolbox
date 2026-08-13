using PincabToolbox.Core.Models;

namespace PincabToolbox.Repair;

public interface IRepairEngine
{
    RepairPlan Plan(string scanReportId, IReadOnlyList<Finding> findings, bool licensed);
    PreflightResult Preflight(RepairPlan plan);
    ApplyResult Apply(RepairPlan plan);
    ExecutionResult Undo(string planId, string? itemId = null);
    IReadOnlyDictionary<string, bool> Verify(RepairPlan plan);
}

/// <summary>
/// The Repair engine.
///
/// Flow: Plan (pure) → Preflight → Backup → Apply (compensating) → Verify → Undo.
/// Apply always runs Preflight itself: it must be impossible to write without it.
/// </summary>
public sealed class RepairEngine : IRepairEngine
{
    private readonly IRepairActionRegistry _registry;
    private readonly IKnowledgePack _pack;
    private readonly IRepairJournal _journal;
    private readonly IBackupService _backup;
    private readonly IEnvironmentProbe _probe;
    private readonly ISystemClock _clock;
    private readonly IReadOnlyList<string> _installRoots;
    private readonly InstallLayout? _layout;

    /// <summary>Minimum free space required before any write, in bytes.</summary>
    public long MinimumFreeSpaceBytes { get; init; } = 50L * 1024 * 1024;

    public RepairEngine(
        IRepairActionRegistry registry,
        IKnowledgePack pack,
        IRepairJournal journal,
        IBackupService backup,
        IEnvironmentProbe probe,
        ISystemClock clock,
        IReadOnlyList<string> installRoots,
        InstallLayout? layout = null)
    {
        _registry = registry;
        _pack = pack;
        _journal = journal;
        _backup = backup;
        _probe = probe;
        _clock = clock;
        _installRoots = installRoots;
        _layout = layout;
    }

    // ───────────────────────────── PLAN (pure) ─────────────────────────────

    public RepairPlan Plan(string scanReportId, IReadOnlyList<Finding> findings, bool licensed)
    {
        var planId = $"plan-{_clock.UtcNow:yyyyMMdd-HHmmss}-{Math.Abs(scanReportId.GetHashCode()) % 10000:D4}";
        var items = new List<RepairPlanItem>();
        var consumed = new HashSet<Finding>();
        var present = findings.Select(f => f.Code).ToHashSet(StringComparer.Ordinal);

        // 1. Scenarios first — a playbook is ONE item, so it compensates as a whole.
        foreach (var sc in _pack.Scenarios)
        {
            if (!sc.Requires.All(present.Contains)) continue;
            if (sc.Excludes.Any(present.Contains)) continue;

            var item = BuildScenarioItem(planId, sc, findings, licensed, consumed);
            if (item is not null) items.Add(item);
        }

        // 2. One item per remaining finding.
        var index = 0;
        foreach (var f in findings)
        {
            if (consumed.Contains(f)) continue;
            index++;
            items.Add(BuildFindingItem($"{planId}-i{index:D3}", f, licensed));
        }

        _journal.Write(Entry(JournalEvent.PlanCreated, planId, detail: $"{items.Count} items"));

        return new RepairPlan
        {
            PlanId = planId,
            CreatedAtUtc = _clock.UtcNow,
            ScanReportId = scanReportId,
            Items = items,
        };
    }

    private RepairPlanItem BuildFindingItem(string itemId, Finding f, bool licensed)
    {
        var rule = _pack.RuleFor(f.Code);
        var hasAction = rule is not null && _registry.TryGet(rule.ActionId, out _);

        if (rule is null || !hasAction)
        {
            // Unknown ActionId is NOT an error — clean degradation (ADR-005). A code with no pack
            // entry at all (rule is null) is not a degraded case of "should have been automatic" —
            // for a code like ROM_MISSING it never will be (Repair cannot invent a ROM dump), so the
            // pack simply never defines a rule for it. ADR-006 still applies to it: the finding's
            // own FixHint (the same text Scanner already shows) is what Repair must surface here —
            // an empty Missing[] would make the item look like it has no guidance at all, which is
            // the gap found 13/08 (real ROM_MISSING findings invisible/unexplained in Repair).
            return new RepairPlanItem
            {
                ItemId = itemId,
                TargetCode = f.Code,
                Mode = RepairMode.ManualOnly,
                Changes = Array.Empty<PlannedChange>(),
                SourceFinding = f,
                RuleId = rule?.Id,
                Missing = rule is null
                    ? (string.IsNullOrWhiteSpace(f.FixHint)
                        ? Array.Empty<RepairLimitation>()
                        : new[] { new RepairLimitation { Code = f.Code, MessageEn = f.FixHint } })
                    : new[] { new RepairLimitation { Code = f.Code, MessageEn = $"action '{rule.ActionId}' not available in this version" } },
            };
        }

        _registry.TryGet(rule!.ActionId, out var action);
        var ctx = Context(f);

        if (!action.ValidateParameters(rule.Parameters).IsValid)
        {
            return new RepairPlanItem
            {
                ItemId = itemId, TargetCode = f.Code, Mode = RepairMode.ManualOnly,
                Changes = Array.Empty<PlannedChange>(), SourceFinding = f, RuleId = rule.Id,
            };
        }

        var changes = action.Plan(ctx, rule.Parameters);
        var reversible = rule.Reversible && action.IsReversibleByNature;

        // An action that planned nothing has nothing to sell. Actions fail closed by design
        // (KillZombiePinUpDisplayAction with no resolvable exe path, SetDefaultAudioDeviceAction
        // when the previous default is unknown, QuarantineOrphanedMediaAction when nothing is
        // actually orphaned…). Without this, such an item still surfaced as Locked — "unlock
        // Repair to fix this" for a fix that does not exist, and after payment the item would
        // simply do nothing. Same overselling failure the gate ordering in RepairModeResolver
        // fixes, one level up. (FIELD-LOG 2026-08-03.)
        var mode = changes.Count == 0
            ? RepairMode.ManualOnly
            : RepairModeResolver.Resolve(true, licensed, rule.RepairConfidence, reversible);

        return new RepairPlanItem
        {
            ItemId = itemId,
            TargetCode = f.Code,
            Mode = mode,
            // ADR-006: the detailed plan is what Repair sells. Redacted at the engine boundary
            // so no UI bug can leak it.
            Changes = licensed ? changes : Array.Empty<PlannedChange>(),
            Summary = changes.Count > 0 ? RepairSummary.From(changes, rule.BackupRequired) : null,
            SourceFinding = f,
            RuleId = rule.Id,
            Completeness = changes.Count == 0 ? Completeness.Partial : Completeness.Full,
            Missing = changes.Count == 0
                ? new[] { new RepairLimitation { Code = f.Code, MessageEn = $"action '{rule.ActionId}' found nothing it could safely change on this install" } }
                : Array.Empty<RepairLimitation>(),
        };
    }

    private RepairPlanItem? BuildScenarioItem(string planId, PackScenario sc,
                                              IReadOnlyList<Finding> findings, bool licensed,
                                              HashSet<Finding> consumed)
    {
        var changes = new List<PlannedChange>();
        var missing = new List<RepairLimitation>();
        var used = new List<Finding>();
        var minConfidence = 100;
        var allReversible = true;

        foreach (var step in sc.Playbook.OrderBy(s => s.Step))
        {
            if (step.ManualOnly)
            {
                missing.Add(new RepairLimitation
                {
                    Code = step.RuleId,
                    MessageEn = step.ReasonEn ?? step.RuleId,
                    MessageFr = step.ReasonFr,
                    MessageEs = step.ReasonEs,
                });
                continue;
            }

            var rule = _pack.RuleById(step.RuleId);
            if (rule is null || !_registry.TryGet(rule.ActionId, out var action))
            {
                missing.Add(new RepairLimitation { Code = step.RuleId, MessageEn = $"step {step.Step}: no action available" });
                continue;
            }

            var f = findings.FirstOrDefault(x => x.Code == rule.TargetCode);
            if (f is null) { missing.Add(new RepairLimitation { Code = rule.TargetCode, MessageEn = $"step {step.Step}: finding {rule.TargetCode} absent" }); continue; }

            if (!action.ValidateParameters(rule.Parameters).IsValid)
            {
                missing.Add(new RepairLimitation { Code = rule.TargetCode, MessageEn = $"step {step.Step}: invalid parameters" }); continue;
            }

            changes.AddRange(action.Plan(Context(f), rule.Parameters));
            used.Add(f);
            minConfidence = Math.Min(minConfidence, rule.RepairConfidence);
            allReversible &= rule.Reversible && action.IsReversibleByNature;
        }

        if (changes.Count == 0) return null;   // nothing automatable: leave the findings on their own

        foreach (var f in used) consumed.Add(f);

        return new RepairPlanItem
        {
            ItemId = $"{planId}-{sc.Id}",
            TargetCode = sc.Id,
            Mode = RepairModeResolver.Resolve(true, licensed, Math.Min(minConfidence, sc.BaseConfidence), allReversible),
            // ADR-006 — same redaction for playbooks. The ORDER is part of what we sell.
            Changes = licensed ? changes : Array.Empty<PlannedChange>(),
            Summary = RepairSummary.From(changes, backupPlanned: true),
            SourceFinding = used.FirstOrDefault(),
            // Missing[] stays visible without a licence: hiding a limitation is not protecting
            // value, it is overselling. And those steps are ones Repair will never automate.
            Completeness = missing.Count > 0 ? Completeness.Partial : Completeness.Full,
            Missing = missing,
        };
    }

    // ───────────────────────────── PREFLIGHT ─────────────────────────────

    public PreflightResult Preflight(RepairPlan plan)
    {
        var blockers = new List<Blocker>();

        // 1. Nothing may be written while the cab software is running. Refusal, not a warning.
        //
        // Exception: a process this very plan intends to terminate (ChangeKind.ProcessTermination,
        // e.g. KillZombiePinUpDisplayAction) does not count as a blocker — otherwise the zombie's
        // mere presence would forever prevent the one action that exists to end it. Anything ELSE
        // running (VPinballX itself, an unrelated frontend component) still blocks normally.
        var killTargets = plan.Items
            .SelectMany(i => i.Changes)
            .Where(c => c.Kind == ChangeKind.ProcessTermination)
            .Select(c => ProcessNameFromPath(c.Target))
            .Where(n => !string.IsNullOrEmpty(n))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var running = _probe.RunningBlockingProcesses().Where(p => !killTargets.Contains(p)).ToList();
        if (running.Count > 0)
        {
            blockers.Add(new Blocker
            {
                Code = "VPX_RUNNING",
                MessageFr = $"Ferme d'abord : {string.Join(", ", running)}. Rien n'a été touché.",
                MessageEn = $"Close these first: {string.Join(", ", running)}. Nothing has been touched.",
                MessageEs = $"Cierra esto primero: {string.Join(", ", running)}. No se tocó nada.",
            });
        }

        // 2. A truncated backup is worse than no backup.
        if (_probe.FreeBackupSpaceBytes() < MinimumFreeSpaceBytes)
        {
            blockers.Add(new Blocker
            {
                Code = "NO_DISK_SPACE",
                MessageFr = "Pas assez de place pour la sauvegarde. Repair ne modifie rien tant qu'il ne peut pas sauvegarder d'abord.",
                MessageEn = "Not enough space for the backup. Repair changes nothing until it can back up first.",
                MessageEs = "No hay suficiente espacio para la copia de seguridad. Repair no cambia nada hasta poder respaldar antes.",
            });
        }

        if (blockers.Count > 0)
        {
            _journal.Write(Entry(JournalEvent.PreflightFailed, plan.PlanId,
                detail: string.Join(" | ", blockers.Select(b => b.Code))));
            return new PreflightResult
            {
                Passed = false, Blockers = blockers, RetainedItems = Array.Empty<RepairPlanItem>(),
            };
        }

        var retained = new List<RepairPlanItem>();
        foreach (var item in plan.Items)
        {
            // 3. Containment — the net under the closed registry (ADR-005).
            //
            // Exception: ChangeKind.AudioDeviceDefault (e.g. SetDefaultAudioDeviceAction). Its
            // Target is a Windows audio endpoint GUID, not a filesystem path — IsContained compares
            // normalized path SEGMENTS, so a device id would be rejected on every single run, no
            // matter what install root is passed in (revue qualité pré-v1.0, 2026-08-04 : ce n'est
            // pas un bug de path traversal, c'est que ce contrôle ne s'applique tout simplement pas
            // à ce type de changement). Same reasoning as the ProcessTermination exemption above —
            // narrow to the one ChangeKind that structurally cannot be a path, everything else
            // (file writes, registry, sqlite) still goes through the full segment check.
            var outside = item.Changes.FirstOrDefault(c =>
                c.Kind != ChangeKind.AudioDeviceDefault && !IsContained(c.Target));
            if (outside is not null)
            {
                _journal.Write(Entry(JournalEvent.RuleRejected, plan.PlanId, item.ItemId,
                    $"target outside install: {PathAnonymizer.Anonymize(outside.Target)}"));
                continue;
            }

            // 4. Write access, checked before writing rather than mid-playbook.
            var unwritable = item.Changes.FirstOrDefault(c => !_probe.CanWriteTo(c.Target));
            if (unwritable is not null)
            {
                _journal.Write(Entry(JournalEvent.RuleRejected, plan.PlanId, item.ItemId,
                    $"no write access: {PathAnonymizer.Anonymize(unwritable.Target)}"));
                continue;
            }

            // 5. A scan is a snapshot — is the finding still true?
            if (!StillApplies(item))
            {
                _journal.Write(Entry(JournalEvent.StaleDropped, plan.PlanId, item.ItemId,
                    "the problem no longer exists"));
                continue;
            }

            retained.Add(item);
        }

        _journal.Write(Entry(JournalEvent.PreflightPassed, plan.PlanId,
            detail: $"{retained.Count}/{plan.Items.Count} items retained"));

        return new PreflightResult { Passed = true, Blockers = blockers, RetainedItems = retained };
    }

    private bool StillApplies(RepairPlanItem item)
    {
        if (item.SourceFinding is null || item.Changes.Count == 0) return true;
        var ctx = Context(item.SourceFinding);
        foreach (var actionId in item.Changes.Select(c => c.ActionId).Distinct())
            if (_registry.TryGet(actionId, out var a) && a.StillApplies(ctx)) return true;
        return false;
    }

    // ───────────────────────────── APPLY ─────────────────────────────

    public ApplyResult Apply(RepairPlan plan)
    {
        var pre = Preflight(plan);
        if (!pre.Passed)
        {
            _journal.Write(Entry(JournalEvent.PlanCompleted, plan.PlanId, detail: "blocked at preflight"));
            return new ApplyResult
            {
                PlanId = plan.PlanId,
                ItemOutcomes = new Dictionary<string, bool>(),
                RecoveryRequired = false,
                Blockers = pre.Blockers,
            };
        }

        var outcomes = new Dictionary<string, bool>();
        var recovery = false;
        string? backupPath = null;

        foreach (var item in pre.RetainedItems)
        {
            if (!item.Selected)
            {
                _journal.Write(Entry(JournalEvent.ItemSkipped, plan.PlanId, item.ItemId, "not selected (opt-in)"));
                continue;
            }
            if (item.Mode is RepairMode.ManualOnly or RepairMode.Locked)
            {
                _journal.Write(Entry(JournalEvent.ItemSkipped, plan.PlanId, item.ItemId, $"mode {item.Mode}"));
                continue;
            }

            // H.2 rule 4: a backup that cannot be completed must never be followed by a write.
            // The backup call is not otherwise guarded anywhere in this stack (FileBackupService
            // makes real disk calls with no try/catch of its own), so this is the one place that
            // stands between a failed backup and a write proceeding anyway.
            string path;
            try
            {
                path = _backup.Backup(plan.PlanId, item);
            }
            catch (Exception ex)
            {
                _journal.Write(Entry(JournalEvent.BackupFailed, plan.PlanId, item.ItemId, ex.Message));
                outcomes[item.ItemId] = false;
                continue;
            }
            backupPath ??= path;
            _journal.Write(Entry(JournalEvent.BackupCreated, plan.PlanId, item.ItemId, path));

            var (ok, needsRecovery) = ApplyItem(plan.PlanId, item);
            outcomes[item.ItemId] = ok;

            if (needsRecovery)
            {
                recovery = true;
                (_backup as FileBackupService)?.Protect(plan.PlanId);
                break;   // stop the whole plan: the install is in an in-between state
            }
        }

        _journal.Write(Entry(JournalEvent.PlanCompleted, plan.PlanId));

        return new ApplyResult
        {
            PlanId = plan.PlanId,
            ItemOutcomes = outcomes,
            RecoveryRequired = recovery,
            BackupPath = backupPath,
        };
    }

    /// <summary>
    /// Atomicity by compensation, at ITEM granularity.
    /// Returns (success, recoveryRequired).
    /// </summary>
    private (bool ok, bool recovery) ApplyItem(string planId, RepairPlanItem item)
    {
        var done = new List<PlannedChange>();

        foreach (var c in item.Changes)
        {
            if (!_registry.TryGet(c.ActionId, out var action))
            {
                _journal.Write(Entry(JournalEvent.ChangeFailed, planId, item.ItemId,
                    $"unknown action {c.ActionId}", c));
                return Compensate(planId, item, done);
            }

            var res = action.Execute(c);
            if (!res.Success)
            {
                _journal.Write(Entry(JournalEvent.ChangeFailed, planId, item.ItemId, res.Error, c));
                return Compensate(planId, item, done);
            }

            done.Add(c);
            _journal.Write(Entry(JournalEvent.ChangeApplied, planId, item.ItemId, change: c));
        }

        _journal.Write(Entry(JournalEvent.ItemCompleted, planId, item.ItemId));
        return (true, false);
    }

    private (bool ok, bool recovery) Compensate(string planId, RepairPlanItem item, List<PlannedChange> done)
    {
        for (var i = done.Count - 1; i >= 0; i--)
        {
            var c = done[i];
            if (!_registry.TryGet(c.ActionId, out var action)) continue;

            var res = action.Revert(c);
            if (!res.Success)
            {
                // STOP compensating. Going further can only make it worse.
                var remaining = done.Take(i + 1).Select(x => PathAnonymizer.Anonymize(x.Target));
                _journal.Write(Entry(JournalEvent.RecoveryRequired, planId, item.ItemId,
                    $"{res.Error} — restore by hand: {string.Join(", ", remaining)}", c));
                return (false, true);
            }
            _journal.Write(Entry(JournalEvent.ChangeReverted, planId, item.ItemId, change: c));
        }

        _journal.Write(Entry(JournalEvent.ItemRolledBack, planId, item.ItemId));
        return (false, false);
    }

    // ───────────────────────────── UNDO ─────────────────────────────

    public ExecutionResult Undo(string planId, string? itemId = null)
    {
        // Same preflight rule: we do not undo while the cab software is running either.
        var running = _probe.RunningBlockingProcesses();
        if (running.Count > 0)
            return ExecutionResult.Fail($"close these first: {string.Join(", ", running)}");

        var itemIds = itemId is not null
            ? new[] { itemId }
            : _journal.Read(planId)
                      .Where(e => e.Event == JournalEvent.ChangeApplied && e.ItemId is not null)
                      .Select(e => e.ItemId!).Distinct().ToArray();

        if (itemIds.Length == 0)
        {
            _journal.Write(Entry(JournalEvent.ItemUndone, planId, itemId, "nothing to undo"));
            return ExecutionResult.Ok;   // idempotent: undoing twice is a no-op, not an error
        }

        foreach (var id in itemIds)
        {
            var applied = _journal.AppliedChanges(planId, id);
            var reverted = _journal.Read(planId)
                                   .Where(e => e.Event == JournalEvent.ChangeReverted && e.ItemId == id)
                                   .Select(e => e.Change!.Target).ToHashSet(StringComparer.Ordinal);

            var todo = applied.Where(c => !reverted.Contains(c.Target)).ToList();
            if (todo.Count == 0)
            {
                _journal.Write(Entry(JournalEvent.ItemUndone, planId, id, "nothing to undo"));
                continue;
            }

            for (var i = todo.Count - 1; i >= 0; i--)
            {
                var c = todo[i];
                if (!_registry.TryGet(c.ActionId, out var action)) continue;
                var res = action.Revert(c);
                if (!res.Success)
                {
                    _journal.Write(Entry(JournalEvent.RecoveryRequired, planId, id, res.Error, c));
                    return ExecutionResult.Fail(res.Error!);
                }
                _journal.Write(Entry(JournalEvent.ChangeReverted, planId, id, change: c));
            }
            _journal.Write(Entry(JournalEvent.ItemUndone, planId, id));
        }
        return ExecutionResult.Ok;
    }

    // ───────────────────────────── VERIFY ─────────────────────────────

    /// <summary>
    /// Re-checks the codes involved after applying: did they go away?
    /// Feeds RepairConfidence calibration. In v1 this signal stays LOCAL and is never
    /// transmitted (zero telemetry, ADR-004).
    /// </summary>
    public IReadOnlyDictionary<string, bool> Verify(RepairPlan plan)
    {
        var result = new Dictionary<string, bool>();
        var appliedItems = _journal.Read(plan.PlanId)
                                   .Where(e => e.Event == JournalEvent.ItemCompleted && e.ItemId is not null)
                                   .Select(e => e.ItemId!).ToHashSet(StringComparer.Ordinal);

        foreach (var item in plan.Items)
        {
            if (!appliedItems.Contains(item.ItemId)) continue;
            result[item.ItemId] = !StillApplies(item);   // true = the problem is gone
        }
        return result;
    }

    // ───────────────────────────── helpers ─────────────────────────────

    private RepairContext Context(Finding f) => new()
    {
        InstallRoots = _installRoots,
        Finding = f,
        Layout = _layout,
    };

    /// <summary>
    /// The ADR-005 "containment net" — the last line of defence stopping a write from landing
    /// outside a detected install root, even if a Knowledge Pack rule or a bug upstream produced
    /// a bad target. Audit 2026-08-04: the previous implementation was a bare string
    /// <c>StartsWith</c>, which had two real gaps — (1) no path-separator boundary after the
    /// matched prefix, so a sibling folder like <c>C:/Games/VPXtra</c> passed as "contained" under
    /// root <c>C:/Games/VPX</c>, and (2) no collapsing of <c>..</c> segments, so a traversal-shaped
    /// path could pass the string check even though the OS would resolve it outside the root.
    /// Fixed by comparing normalized PATH SEGMENTS instead of raw characters. Still does not
    /// resolve symlinks/junctions (would need real disk I/O, platform-specific, and cannot be done
    /// against the <see cref="IFileSystem"/> abstraction used by tests — left as a known residual
    /// risk, see FIELD-LOG 2026-08-04 audit entry, not silently expanded in scope here).
    /// </summary>
    private bool IsContained(string target)
    {
        var t = SegmentsOf(target);
        return _installRoots.Any(r => IsPrefixOf(SegmentsOf(r), t));
    }

    private static bool IsPrefixOf(IReadOnlyList<string> root, IReadOnlyList<string> target)
    {
        if (target.Count < root.Count) return false;
        for (var i = 0; i < root.Count; i++)
            if (!string.Equals(root[i], target[i], StringComparison.OrdinalIgnoreCase)) return false;
        return true;
    }

    /// <summary>Splits into normalized segments and collapses "." / ".." — see <see cref="IsContained"/>.</summary>
    private static List<string> SegmentsOf(string path)
    {
        var segments = new List<string>();
        foreach (var seg in path.Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries))
        {
            if (seg == ".") continue;
            if (seg == "..") { if (segments.Count > 0) segments.RemoveAt(segments.Count - 1); continue; }
            segments.Add(seg);
        }
        return segments;
    }

    /// <summary>
    /// Bare process name (no directory, no extension) from a path. Deliberately splits on both
    /// separators by hand rather than System.IO.Path — Path treats '\' as a plain character on
    /// non-Windows, so a Windows-style target path would not resolve correctly when this runs
    /// (or is tested) off Windows. Matches FileBackupService's own LastSegment helper.
    /// </summary>
    private static string ProcessNameFromPath(string path)
    {
        var i = path.LastIndexOfAny(new[] { '/', '\\' });
        var name = i < 0 ? path : path[(i + 1)..];
        var dot = name.LastIndexOf('.');
        return dot > 0 ? name[..dot] : name;
    }

    private JournalEntry Entry(JournalEvent ev, string planId, string? itemId = null,
                               string? detail = null, PlannedChange? change = null) => new()
    {
        AtUtc = _clock.UtcNow,
        Event = ev,
        PlanId = planId,
        ItemId = itemId,
        Detail = detail,
        Change = change,
    };
}
