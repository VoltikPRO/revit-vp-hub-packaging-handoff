using System.Diagnostics;
using System.IO;
using System.Text;

namespace LicensingSystem.Revit.Licensing;

/// <summary>
/// Append-only local file log for Revit add-ins.
/// Path: <c>%LocalAppData%\VP-Hub\logs\{productCode}.log</c>.
/// VP-Hub Agent packs this folder into Export diagnostics / Report a problem.
/// Never throws; never log secrets (see <c>docs/architecture/logging-redaction-policy.md</c>).
/// Cloud telemetry remains a separate IPC channel (<c>logPluginEvent</c>).
/// </summary>
public static class VpHubPluginFileLog
{
    private static readonly object Gate = new();

    public static string GetLogsDirectory()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "VP-Hub",
            "logs");
    }

    public static string GetLogFilePath(string productCode)
    {
        var safe = SanitizeProductCodeForFileName(productCode);
        return Path.Combine(GetLogsDirectory(), safe + ".log");
    }

    /// <summary>
    /// Maps a product code to a safe file name stem (keeps dots; replaces invalid path chars with <c>_</c>).
    /// </summary>
    public static string SanitizeProductCodeForFileName(string productCode)
    {
        if (string.IsNullOrWhiteSpace(productCode))
        {
            return "unknown.product";
        }

        var trimmed = productCode.Trim();
        var invalid = Path.GetInvalidFileNameChars();
        var chars = trimmed.Select(c => invalid.Contains(c) ? '_' : c).ToArray();
        var safe = new string(chars);
        return string.IsNullOrWhiteSpace(safe) ? "unknown.product" : safe;
    }

    public static void Write(string productCode, string message, string? correlationId = null)
    {
        try
        {
            var path = GetLogFilePath(productCode);
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var pid = Process.GetCurrentProcess().Id;
            var cid = string.IsNullOrEmpty(correlationId) ? "" : $" cid={correlationId}";
            var line =
                $"{DateTimeOffset.UtcNow:yyyy-MM-ddTHH:mm:ss.fffZ} pid={pid}{cid} {message}";

            lock (Gate)
            {
                File.AppendAllText(path, line + Environment.NewLine, Encoding.UTF8);
            }
        }
        catch
        {
            // Never break Revit because of diagnostics.
        }
    }
}
