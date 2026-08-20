using System.Text;
using System.Text.RegularExpressions;
using PincabToolbox.Core.Services;

namespace PincabToolbox.Repair.Actions;

/// <summary>
/// 20/08 — matches DMD_POSITION_OFFSCREEN. dmddevice.ini's <c>[virtualdmd]</c> section positions
/// the virtual DMD window at a saved (left, top) that no longer overlaps any connected monitor —
/// the window still opens, just where nothing can show it (a stale position from a previous
/// monitor layout, GPU swap, or disconnected screen). This resets left/top to (0, 0) — the
/// top-left corner of the primary monitor, which Windows always places at virtual-desktop origin
/// (0, 0) — while leaving width/height untouched.
///
/// <para>
/// Deliberately does NOT try to compute a "smarter" target position (centered on a specific
/// monitor, sized to fit, etc.): the scanner's own FixHint already says "reset... or delete them
/// to fall back to the defaults", and (0, 0) is exactly that — the simplest, always-safe choice
/// that needs no monitor enumeration and cannot itself be wrong about which screen is "the"
/// playfield/backglass/DMD screen (a decision this project has repeatedly declined to guess at,
/// see DisplaySetupScanner's own history).
/// </para>
///
/// <para>
/// Genuinely reversible, unlike <see cref="RegisterComComponentAction"/>: the original left/top
/// are recorded in <see cref="PlannedChange.Before"/> in a fixed, self-produced format
/// ("left={n}, top={n}") and parsed back out in <see cref="Revert"/> — this action is the only
/// reader of its own Before text, so the format is safe to rely on. <see cref="RewriteFile"/> is
/// the single rewrite path both <see cref="Execute"/> and <see cref="Revert"/> call, so a forward
/// write and its own undo can never drift into two different rewrite behaviours.
/// </para>
/// </summary>
public sealed class RepositionDmdAction : IRepairAction
{
    private static readonly Regex BeforePosition = new(@"left=(-?\d+), top=(-?\d+)", RegexOptions.Compiled);

    private readonly IFileSystem _fs;

    public RepositionDmdAction(IFileSystem fs) => _fs = fs;

    public string ActionId => "reposition_dmd";
    public ChangeKind Kind => ChangeKind.IniWrite;
    public bool IsReversibleByNature => true;

    public ValidationResult ValidateParameters(IReadOnlyDictionary<string, string> parameters) => ValidationResult.Ok;

    public IReadOnlyList<PlannedChange> Plan(RepairContext ctx, IReadOnlyDictionary<string, string> parameters)
    {
        var path = ctx.Finding.FilePath;
        if (string.IsNullOrWhiteSpace(path) || !_fs.FileExists(path)) return Array.Empty<PlannedChange>();

        var cfg = ReadConfig(path);
        if (cfg is null || cfg.Left is null || cfg.Top is null) return Array.Empty<PlannedChange>();
        if (cfg.Left == 0 && cfg.Top == 0) return Array.Empty<PlannedChange>();   // already at the safe default

        // Fail closed exactly like RewriteVirtualDmdPosition itself: if the rewrite can't find
        // both keys already present, plan nothing rather than invent a line.
        if (RewriteFile(path, 0, 0, dryRun: true) is null) return Array.Empty<PlannedChange>();

        return new[]
        {
            new PlannedChange
            {
                ActionId = ActionId,
                Kind = Kind,
                Target = path,
                Before = $"left={cfg.Left}, top={cfg.Top} (off every connected monitor)",
                After = "left=0, top=0 (top-left corner of the primary monitor)",
                Reversible = true,
            }
        };
    }

    public bool StillApplies(RepairContext ctx)
    {
        var path = ctx.Finding.FilePath;
        if (string.IsNullOrWhiteSpace(path) || !_fs.FileExists(path)) return false;
        var cfg = ReadConfig(path);
        return cfg is not null && cfg.Left is not null && cfg.Top is not null && !(cfg.Left == 0 && cfg.Top == 0);
    }

    public ExecutionResult Execute(PlannedChange c)
    {
        var result = RewriteFile(c.Target, 0, 0, dryRun: false);
        return result is not null
            ? ExecutionResult.Ok
            : ExecutionResult.Fail("[virtualdmd] left/top no longer both present — the file changed since planning");
    }

    public ExecutionResult Revert(PlannedChange c)
    {
        var m = BeforePosition.Match(c.Before);
        if (!m.Success)
            return ExecutionResult.Fail("cannot recover the original position — unexpected journal format");

        var oldLeft = int.Parse(m.Groups[1].Value);
        var oldTop = int.Parse(m.Groups[2].Value);
        var result = RewriteFile(c.Target, oldLeft, oldTop, dryRun: false);
        return result is not null
            ? ExecutionResult.Ok
            : ExecutionResult.Fail("[virtualdmd] left/top no longer both present — cannot restore the original position");
    }

    private DmdDeviceIniParser.VirtualDmdConfig? ReadConfig(string path)
    {
        try { return DmdDeviceIniParser.TryParseVirtualDmdConfig(Encoding.UTF8.GetString(_fs.ReadAllBytes(path))); }
        catch { return null; }
    }

    /// <summary>Single rewrite path for both Execute and Revert. dryRun=true (Plan's own fail-closed check) never touches disk.</summary>
    private string? RewriteFile(string path, int left, int top, bool dryRun)
    {
        if (!_fs.FileExists(path)) return null;
        string text;
        try { text = Encoding.UTF8.GetString(_fs.ReadAllBytes(path)); }
        catch { return null; }

        var rewritten = DmdDeviceIniParser.RewriteVirtualDmdPosition(text, left, top);
        if (rewritten is null) return null;
        if (!dryRun) _fs.WriteAllBytes(path, Encoding.UTF8.GetBytes(rewritten));
        return rewritten;
    }
}
