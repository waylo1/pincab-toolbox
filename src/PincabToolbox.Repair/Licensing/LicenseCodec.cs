using System.Text.Json;

namespace PincabToolbox.Repair.Licensing;

/// <summary>
/// Wire format for a license key: base64url(payload JSON) + "." + base64url(signature).
/// Deliberately JWT-shaped — a well-known, easy-to-eyeball structure — but hand-rolled with
/// System.Text.Json (in-box BCL) rather than a JWT library, consistent with the project's
/// zero-third-party-dependency rule (ADR-007 applies the same reasoning to SQLite).
/// </summary>
public static class LicenseCodec
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static byte[] Serialize(LicensePayload payload)
        => JsonSerializer.SerializeToUtf8Bytes(payload, JsonOpts);

    /// <summary>Never throws — a corrupted or hand-edited payload is just null.</summary>
    public static LicensePayload? Deserialize(byte[] payloadBytes)
    {
        try { return JsonSerializer.Deserialize<LicensePayload>(payloadBytes, JsonOpts); }
        catch { return null; }
    }

    public static string Base64UrlEncode(byte[] data)
        => Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    /// <summary>Never throws — malformed base64url (bad padding, illegal characters) is just null.</summary>
    public static byte[]? Base64UrlDecode(string text)
    {
        try
        {
            var t = text.Replace('-', '+').Replace('_', '/');
            t = (t.Length % 4) switch
            {
                2 => t + "==",
                3 => t + "=",
                0 => t,
                _ => throw new FormatException("invalid base64url length"),
            };
            return Convert.FromBase64String(t);
        }
        catch { return null; }
    }
}
