using System.Globalization;
using System.Text;

namespace LicensingSystem.Contracts.Agent.Grants;

/// <summary>
/// Canonical UTF-8 JSON for license grant payloads — mirrors <c>grantCanonical.ts</c> in worker-api
/// and <c>licensing-grant-webcrypto.js</c> in frontend/admin (browser signing tool; the private key
/// never reaches this Worker/backend). All three must produce byte-identical output for a given
/// payload, since that output is exactly what the ES256 signature covers.
/// </summary>
public static class LicenseGrantPayloadCanonical
{
    /// <summary>
    /// Fixed forever: the byte format signed for every grant issued before SchemaVersion existed.
    /// Do not change this value — it is what "absent schemaVersion" means, not "oldest supported".
    /// </summary>
    public const int LegacySchemaVersion = 1;

    /// <summary>
    /// Version new grants should be minted at going forward. Equal to <see cref="LegacySchemaVersion"/>
    /// until a future step actually needs new signed fields (see canrun-contract-versioning.md) —
    /// bump this only together with a new branch in <see cref="BuildJson"/> and the TS/JS mirrors.
    /// </summary>
    public const int CurrentSchemaVersion = 1;

    /// <summary>Schema versions this build knows how to canonicalize. Anything else is rejected.</summary>
    public static readonly IReadOnlyCollection<int> KnownSchemaVersions = new[] { 1 };

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
            ? JsonString(p.AudienceServerId!.Trim())
            : "null";
        var issued = JsonString(FormatDotNetOFromUtc(p.IssuedAtUtc));
        var kid = JsonString(p.Kid);
        var product = JsonString(p.ProductCode);
        var channel = JsonString(p.Channel);
        var seat = p.SeatLimit.ToString(CultureInfo.InvariantCulture);

        // Version 1 reproduces the exact field set/order used before SchemaVersion existed — no
        // "schemaVersion" key in the byte output, so every already-issued v1 signature still
        // verifies. A future version 2+ branch goes here alongside new fields; it must emit a
        // different byte sequence (that's the whole point) and is only reachable for grants
        // explicitly minted at that version.
        if (p.SchemaVersion == LegacySchemaVersion)
        {
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

        throw new NotSupportedException(
            $"LicenseGrantPayloadCanonical does not know schema version {p.SchemaVersion}. " +
            "Add a branch here (and in grantCanonical.ts / licensing-grant-webcrypto.js) before minting grants at this version.");
    }

    /// <summary>
    /// JSON string literal matching System.Text.Json's <c>UnsafeRelaxedJsonEscaping</c> for grant
    /// fields (escape quotes/backslash/controls; leave everything else, including HTML-sensitive
    /// characters, unescaped). Hand-rolled rather than calling into <c>System.Text.Json</c>: on
    /// Revit 2024's shared AppDomain, another add-in can load an older, incompatible STJ first,
    /// and any call into STJ here — even just string escaping — then throws
    /// <see cref="MissingMethodException"/>. This canonical builder must work regardless of what
    /// else is loaded in the process, since it runs inside the add-in's own verification path.
    /// </summary>
    private static string JsonString(string s)
    {
        var sb = new StringBuilder(s.Length + 2);
        sb.Append('"');
        foreach (var c in s)
        {
            switch (c)
            {
                case '"':
                    sb.Append("\\\"");
                    break;
                case '\\':
                    sb.Append("\\\\");
                    break;
                case '\b':
                    sb.Append("\\b");
                    break;
                case '\f':
                    sb.Append("\\f");
                    break;
                case '\n':
                    sb.Append("\\n");
                    break;
                case '\r':
                    sb.Append("\\r");
                    break;
                case '\t':
                    sb.Append("\\t");
                    break;
                default:
                    if (c < 0x20)
                        sb.AppendFormat(CultureInfo.InvariantCulture, "\\u{0:x4}", (int)c);
                    else
                        sb.Append(c);
                    break;
            }
        }

        sb.Append('"');
        return sb.ToString();
    }

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
