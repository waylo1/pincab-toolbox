namespace PincabToolbox.Repair.Licensing;

/// <summary>
/// What a license key actually asserts, once its signature checks out. ADR-002: the purchase is
/// PERPETUAL — <see cref="UpdatesUntilUtc"/> gates Knowledge Pack updates only, never whether
/// Repair itself stays unlocked. A license with an expired update window is still a fully valid,
/// fully unlocked license; it just stops being offered new packs.
/// </summary>
public sealed record LicensePayload
{
    /// <summary>The email the license was issued to (ADR-002: "signature hors ligne liée à l'email").</summary>
    public required string Email { get; init; }

    public required DateTimeOffset IssuedUtc { get; init; }

    /// <summary>Knowledge Pack updates are offered up to this date. Does not affect Repair unlock.</summary>
    public required DateTimeOffset UpdatesUntilUtc { get; init; }
}

/// <summary>Result of verifying a license key. Never throws — a malformed or forged key is just Invalid.</summary>
public sealed record LicenseCheckResult(bool IsValid, LicensePayload? Payload, string? Error)
{
    public static LicenseCheckResult Valid(LicensePayload payload) => new(true, payload, null);
    public static LicenseCheckResult Invalid(string error) => new(false, null, error);
}
