namespace PincabToolbox.Core.Models;

/// <summary>Severity of a scan finding.</summary>
public enum Severity
{
    /// <summary>Everything is fine — informational confirmation.</summary>
    Ok = 0,
    /// <summary>Informational — nothing broken, worth knowing.</summary>
    Info = 1,
    /// <summary>Likely to cause degraded behaviour.</summary>
    Warning = 2,
    /// <summary>Will break a table or the cab.</summary>
    Critical = 3,
}

/// <summary>
/// A single structured finding produced by a scanner.
/// Text is code+args based so the UI can localize; <see cref="EnglishText"/> is the fallback rendering.
/// </summary>
public sealed record Finding
{
    /// <summary>Stable message code (e.g. ROM_MISSING). UI maps codes to localized templates.</summary>
    public required string Code { get; init; }

    public required Severity Severity { get; init; }

    /// <summary>Scanner id that produced this finding (rom, bitness, completeness, compat, updates).</summary>
    public required string Category { get; init; }

    /// <summary>Subject of the finding — usually a table or file name (display only).</summary>
    public string Subject { get; init; } = "";

    /// <summary>Full path of the file concerned, when applicable.</summary>
    public string? FilePath { get; init; }

    /// <summary>Ordered arguments for the localized template.</summary>
    public IReadOnlyList<string> Args { get; init; } = Array.Empty<string>();

    /// <summary>English fallback text, always populated.</summary>
    public required string EnglishText { get; init; }

    /// <summary>Optional hint about how to fix (English fallback; UI may localize by code).</summary>
    public string? FixHint { get; init; }
}
