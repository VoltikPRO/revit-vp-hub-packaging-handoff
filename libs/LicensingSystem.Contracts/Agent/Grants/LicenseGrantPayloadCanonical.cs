using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace LicensingSystem.Contracts.Agent.Grants;

/// <summary>
/// Canonical UTF-8 JSON for license grant payloads — mirrors <c>grantCanonical.ts</c> in worker-api.
/// </summary>
public static class LicenseGrantPayloadCanonical
{
    private static readonly JsonSerializerOptions JsonEscaping = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };
    /// <summary>UTF-8 bytes of the canonical JSON object (no surrounding whitespace).</summary>
    public static byte[] ToUtf8Bytes(LicenseGrantPayloadDto p) => Encoding.UTF8.GetBytes(BuildJson(p));

    /// <summary>Canonical JSON string (UTF-16); useful for tests and tooling.</summary>
    public static string BuildJson(LicenseGrantPayloadDto p)
    {
        var gid = JsonString(p.GrantId.ToString("d"));
        var pid = JsonString(p.PublisherId.ToString("d"));
        var cid = JsonString(p.CustomerOrgId.ToString("d"));
        var vf = JsonString(FormatDotNetOFromUtc(p.ValidFromUtc));
        var vu = p.ValidUntilUtc is { } vUntil ? JsonString(FormatDotNetOFromUtc(vUntil)) : "null";
        var aud = !string.IsNullOrWhiteSpace(p.AudienceServerId)
            ? JsonString(p.AudienceServerId.Trim())
            : "null";
        var issued = JsonString(FormatDotNetOFromUtc(p.IssuedAtUtc));
        var kid = JsonString(p.Kid);
        var product = JsonString(p.ProductCode);
        var channel = JsonString(p.Channel);
        var seat = p.SeatLimit.ToString(CultureInfo.InvariantCulture);
        return "{" +
               $"\"grantId\":{gid}," +
               $"\"publisherId\":{pid}," +
               $"\"customerOrgId\":{cid}," +
               $"\"productCode\":{product}," +
               $"\"validFromUtc\":{vf}," +
               $"\"validUntilUtc\":{vu}," +
               $"\"seatLimit\":{seat}," +
               $"\"channel\":{channel}," +
               $"\"audienceServerId\":{aud}," +
               $"\"issuedAtUtc\":{issued}," +
               $"\"kid\":{kid}" +
               "}";
    }

    private static string JsonString(string s) => JsonSerializer.Serialize(s, JsonEscaping);

    /// <summary>Format instant as UTC with 7-digit fractional seconds (matches worker <c>formatDotNetOFromIso</c>).</summary>
    public static string FormatDotNetOFromUtc(DateTimeOffset d)
    {
        d = d.ToUniversalTime();
        var y = d.Year;
        var mo = d.Month.ToString("D2", CultureInfo.InvariantCulture);
        var day = d.Day.ToString("D2", CultureInfo.InvariantCulture);
        var h = d.Hour.ToString("D2", CultureInfo.InvariantCulture);
        var mi = d.Minute.ToString("D2", CultureInfo.InvariantCulture);
        var s = d.Second.ToString("D2", CultureInfo.InvariantCulture);
        var frac7 = (d.Millisecond * 10_000L).ToString("D7", CultureInfo.InvariantCulture);
        return $"{y:D4}-{mo}-{day}T{h}:{mi}:{s}.{frac7}+00:00";
    }
}
