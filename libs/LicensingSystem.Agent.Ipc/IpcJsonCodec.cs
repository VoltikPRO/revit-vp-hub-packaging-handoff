using System.Text.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using StjJsonSerializer = System.Text.Json.JsonSerializer;
using StjJsonSerializerOptions = System.Text.Json.JsonSerializerOptions;

namespace LicensingSystem.Agent.Ipc;

/// <summary>
/// Named-pipe JSON codec: prefer System.Text.Json; on Revit AppDomain STJ/Encodings conflicts
/// fall back to Newtonsoft.Json. Wire format remains camelCase JSON.
/// </summary>
public static class IpcJsonCodec
{
    private static readonly StjJsonSerializerOptions StjOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = false,
    };

    private static readonly JsonSerializerSettings NewtonsoftSettings = new()
    {
        ContractResolver = new CamelCasePropertyNamesContractResolver(),
        DateFormatHandling = DateFormatHandling.IsoDateFormat,
        DateParseHandling = DateParseHandling.DateTimeOffset,
        NullValueHandling = NullValueHandling.Include,
        MissingMemberHandling = MissingMemberHandling.Ignore,
    };

    private static int _fallbackLogged;

    /// <summary>True after at least one Newtonsoft fallback in this process.</summary>
    public static bool NewtonsoftFallbackUsed { get; private set; }

    public static string Serialize(object value)
    {
        ArgumentNullException.ThrowIfNull(value);

        try
        {
            return StjJsonSerializer.Serialize(value, value.GetType(), StjOptions);
        }
        catch (Exception ex) when (IsStjDependencyFailure(ex))
        {
            MarkFallback(ex);
            return SerializeNewtonsoft(value);
        }
    }

    public static T Deserialize<T>(string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        try
        {
            var stj = StjJsonSerializer.Deserialize<T>(json, StjOptions);
            if (stj is null)
                throw new InvalidOperationException("Could not deserialize agent response.");
            return stj;
        }
        catch (Exception ex) when (IsStjDependencyFailure(ex))
        {
            MarkFallback(ex);
            return DeserializeNewtonsoft<T>(json);
        }
    }

    /// <summary>Newtonsoft path used by AppDomain fallback (and unit tests).</summary>
    internal static string SerializeNewtonsoft(object value) =>
        JsonConvert.SerializeObject(value, NewtonsoftSettings);

    /// <summary>Newtonsoft path used by AppDomain fallback (and unit tests).</summary>
    internal static T DeserializeNewtonsoft<T>(string json)
    {
        var ns = JsonConvert.DeserializeObject<T>(json, NewtonsoftSettings);
        if (ns is null)
            throw new InvalidOperationException("Could not deserialize agent response.");
        return ns;
    }

    private static void MarkFallback(Exception ex)
    {
        NewtonsoftFallbackUsed = true;
        if (Interlocked.Exchange(ref _fallbackLogged, 1) != 0)
            return;

        System.Diagnostics.Trace.TraceWarning(
            "LicensingSystem.Agent.Ipc: System.Text.Json unavailable in AppDomain (" +
            ex.GetType().Name + "); using Newtonsoft.Json for named-pipe IPC.");
    }

    internal static bool IsStjDependencyFailure(Exception ex)
    {
        for (var e = ex; e != null; e = e.InnerException)
        {
            if (e is MissingMethodException or TypeLoadException or FileNotFoundException)
            {
                var msg = e.Message ?? string.Empty;
                if (msg.IndexOf("System.Text.Json", StringComparison.OrdinalIgnoreCase) >= 0
                    || msg.IndexOf("System.Text.Encodings.Web", StringComparison.OrdinalIgnoreCase) >= 0
                    || msg.IndexOf("FindFirstCharacterToEncodeUtf8", StringComparison.OrdinalIgnoreCase) >= 0
                    || msg.IndexOf("JsonSerializer", StringComparison.OrdinalIgnoreCase) >= 0
                    || e is MissingMethodException)
                {
                    return true;
                }
            }
        }

        return false;
    }
}
