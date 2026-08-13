using System.Text.Json;
using System.Text.Json.Serialization;

namespace PincabToolbox.Repair;

public sealed record PackStep
{
    public required int Step { get; init; }
    public required string RuleId { get; init; }
    public bool ManualOnly { get; init; }
    public string? ReasonFr { get; init; }
    public string? ReasonEn { get; init; }
    public string? ReasonEs { get; init; }
}

public sealed record PackScenario
{
    public required string Id { get; init; }
    public required string TitleFr { get; init; }
    public required string TitleEn { get; init; }
    public string? TitleEs { get; init; }
    public IReadOnlyList<string> Requires { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> Supports { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> Excludes { get; init; } = Array.Empty<string>();
    public int BaseConfidence { get; init; } = 100;
    public string? ExplanationFr { get; init; }
    public string? ExplanationEn { get; init; }
    public string? ExplanationEs { get; init; }
    public IReadOnlyList<PackStep> Playbook { get; init; } = Array.Empty<PackStep>();
}

/// <summary>
/// Plain-language, per-code editorial content for the Écran 1 finding detail panel — deliberately
/// separate from <c>Knowledge.cs</c>'s hardcoded Impact/Cause table (that one covers all 51 known
/// codes and stays the single source of truth for those two fields; duplicating it here from the
/// pack would create two sources of truth for the same text). What's here is genuinely new and
/// pack-only: <see cref="Player"/> restates the problem the way the cabinet owner would describe
/// it, <see cref="Explanation"/> gives the plain-language mental model, and <see cref="Verification"/>
/// gives the diagnostic check that confirms the finding for real. Only present for the subset of
/// codes the pack author has actually written this content for (7 as of pack-2026.08) — a missing
/// entry degrades to nothing shown, same tolerance as the rest of the pack (ADR-005).
/// </summary>
public sealed record PackEntry
{
    public required string Code { get; init; }
    public string? PlayerFr { get; init; }
    public string? PlayerEn { get; init; }
    public string? PlayerEs { get; init; }
    public string? ExplanationFr { get; init; }
    public string? ExplanationEn { get; init; }
    public string? ExplanationEs { get; init; }
    public string? VerificationFr { get; init; }
    public string? VerificationEn { get; init; }
    public string? VerificationEs { get; init; }
}

/// <summary>
/// The knowledge, as DATA. Rules may only compose capabilities from the compiled
/// registry — they can never define one (ADR-005).
/// </summary>
public interface IKnowledgePack
{
    string PackVersion { get; }
    RepairRule? RuleFor(string code);
    RepairRule? RuleById(string ruleId);
    IReadOnlyList<PackScenario> Scenarios { get; }
    PackEntry? EntryFor(string code);
}

public sealed class KnowledgePack : IKnowledgePack
{
    private readonly Dictionary<string, RepairRule> _byCode = new(StringComparer.Ordinal);
    private readonly Dictionary<string, RepairRule> _byId = new(StringComparer.Ordinal);
    private readonly Dictionary<string, PackEntry> _entriesByCode = new(StringComparer.Ordinal);

    public string PackVersion { get; }
    public IReadOnlyList<PackScenario> Scenarios { get; }

    public KnowledgePack(string packVersion, IEnumerable<RepairRule> rules,
                         IEnumerable<PackScenario>? scenarios = null,
                         IEnumerable<PackEntry>? entries = null)
    {
        PackVersion = packVersion;
        foreach (var r in rules)
        {
            _byId[r.Id] = r;
            // First rule wins for a given code — packs are ordered by intent.
            if (!_byCode.ContainsKey(r.TargetCode)) _byCode[r.TargetCode] = r;
        }
        Scenarios = (scenarios ?? Array.Empty<PackScenario>()).ToList();
        foreach (var e in entries ?? Array.Empty<PackEntry>())
            _entriesByCode[e.Code] = e;
    }

    public RepairRule? RuleFor(string code) => _byCode.TryGetValue(code, out var r) ? r : null;
    public RepairRule? RuleById(string ruleId) => _byId.TryGetValue(ruleId, out var r) ? r : null;
    public PackEntry? EntryFor(string code) => _entriesByCode.TryGetValue(code, out var e) ? e : null;

    /// <summary>Empty pack: everything falls back to ManualOnly. Used before a pack is shipped.</summary>
    public static KnowledgePack Empty { get; } = new("0000.00", Array.Empty<RepairRule>());

    // ───────────────────────────── JSON loading ─────────────────────────────

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    /// <summary>
    /// Loads a pack. Tolerant by design: a malformed entry is skipped rather than
    /// crashing the app — a bad pack must never prevent the free scanner from running.
    /// </summary>
    public static KnowledgePack Load(string json, IList<string>? warnings = null)
    {
        var dto = JsonSerializer.Deserialize<PackDto>(json, Options)
                  ?? throw new InvalidDataException("empty knowledge pack");

        var rules = new List<RepairRule>();
        var entries = new List<PackEntry>();
        foreach (var e in dto.Entries ?? new List<EntryDto>())
        {
            if (string.IsNullOrWhiteSpace(e.Code)) { warnings?.Add("entry without code, skipped"); continue; }

            if (!string.IsNullOrWhiteSpace(e.PlayerEn) || !string.IsNullOrWhiteSpace(e.ExplanationEn) ||
                !string.IsNullOrWhiteSpace(e.VerificationEn))
            {
                entries.Add(new PackEntry
                {
                    Code = e.Code!,
                    PlayerFr = e.PlayerFr,
                    PlayerEn = e.PlayerEn,
                    PlayerEs = e.PlayerEs,
                    ExplanationFr = e.ExplanationFr,
                    ExplanationEn = e.ExplanationEn,
                    ExplanationEs = e.ExplanationEs,
                    VerificationFr = e.VerificationFr,
                    VerificationEn = e.VerificationEn,
                    VerificationEs = e.VerificationEs,
                });
            }

            foreach (var r in e.RepairRules ?? new List<RuleDto>())
            {
                if (string.IsNullOrWhiteSpace(r.Id) || string.IsNullOrWhiteSpace(r.ActionId))
                {
                    warnings?.Add($"{e.Code}: rule without id or actionId, skipped");
                    continue;
                }
                if (r.RepairConfidence is < 0 or > 100)
                {
                    warnings?.Add($"{r.Id}: confidence out of range, skipped");
                    continue;
                }

                rules.Add(new RepairRule
                {
                    Id = r.Id!,
                    TargetCode = e.Code!,
                    ActionId = r.ActionId!,
                    Parameters = r.Parameters ?? new Dictionary<string, string>(),
                    RepairConfidence = r.RepairConfidence,
                    Reversible = r.Reversible,
                    BackupRequired = r.BackupRequired ?? true,
                    ManualProcedureFr = r.ManualProcedureFr,
                    ManualProcedureEn = r.ManualProcedureEn,
                });
            }
        }

        var scenarios = new List<PackScenario>();
        foreach (var s in dto.Scenarios ?? new List<ScenarioDto>())
        {
            if (string.IsNullOrWhiteSpace(s.Id)) { warnings?.Add("scenario without id, skipped"); continue; }
            if ((s.Requires?.Count ?? 0) < 2)
            {
                warnings?.Add($"{s.Id}: fewer than 2 required codes, skipped (anti false positive)");
                continue;
            }
            scenarios.Add(new PackScenario
            {
                Id = s.Id!,
                TitleFr = s.TitleFr ?? s.Id!,
                TitleEn = s.TitleEn ?? s.Id!,
                TitleEs = s.TitleEs,
                Requires = s.Requires!,
                Supports = s.Supports ?? new List<string>(),
                Excludes = s.Excludes ?? new List<string>(),
                BaseConfidence = s.BaseConfidence ?? 100,
                ExplanationFr = s.ExplanationFr,
                ExplanationEn = s.ExplanationEn,
                ExplanationEs = s.ExplanationEs,
                Playbook = (s.RepairPlaybook ?? new List<StepDto>())
                    .Where(x => !string.IsNullOrWhiteSpace(x.RuleId))
                    .Select(x => new PackStep
                    {
                        Step = x.Step,
                        RuleId = x.RuleId!,
                        ManualOnly = x.ManualOnly,
                        ReasonFr = x.ReasonFr,
                        ReasonEn = x.ReasonEn,
                        ReasonEs = x.ReasonEs,
                    }).ToList(),
            });
        }

        return new KnowledgePack(dto.PackVersion ?? "0000.00", rules, scenarios, entries);
    }

    // DTOs — deliberately permissive; validation lives in the CI validator.
    private sealed class PackDto
    {
        public string? PackVersion { get; set; }
        public List<EntryDto>? Entries { get; set; }
        public List<ScenarioDto>? Scenarios { get; set; }
    }

    private sealed class EntryDto
    {
        public string? Code { get; set; }
        public List<RuleDto>? RepairRules { get; set; }
        public string? PlayerFr { get; set; }
        public string? PlayerEn { get; set; }
        public string? PlayerEs { get; set; }
        public string? ExplanationFr { get; set; }
        public string? ExplanationEn { get; set; }
        public string? ExplanationEs { get; set; }
        public string? VerificationFr { get; set; }
        public string? VerificationEn { get; set; }
        public string? VerificationEs { get; set; }
    }

    private sealed class RuleDto
    {
        public string? Id { get; set; }
        public string? ActionId { get; set; }
        public Dictionary<string, string>? Parameters { get; set; }
        public int RepairConfidence { get; set; }
        public bool Reversible { get; set; }
        public bool? BackupRequired { get; set; }
        public string? ManualProcedureFr { get; set; }
        public string? ManualProcedureEn { get; set; }
    }

    private sealed class ScenarioDto
    {
        public string? Id { get; set; }
        public string? TitleFr { get; set; }
        public string? TitleEn { get; set; }
        public string? TitleEs { get; set; }
        public List<string>? Requires { get; set; }
        public List<string>? Supports { get; set; }
        public List<string>? Excludes { get; set; }
        public int? BaseConfidence { get; set; }
        public string? ExplanationFr { get; set; }
        public string? ExplanationEn { get; set; }
        public string? ExplanationEs { get; set; }
        public List<StepDto>? RepairPlaybook { get; set; }
    }

    private sealed class StepDto
    {
        public int Step { get; set; }
        public string? RuleId { get; set; }
        public bool ManualOnly { get; set; }
        public string? ReasonFr { get; set; }
        public string? ReasonEn { get; set; }
        public string? ReasonEs { get; set; }
    }
}
