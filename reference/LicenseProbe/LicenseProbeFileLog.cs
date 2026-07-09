using System.Diagnostics;
using System.IO;
using System.Text;

namespace LicensingSystem.Revit.LicenseProbe;

/// <summary>Local file trace for diagnosing IPC and verification (no secrets: no tokens, no grant JWT bodies).</summary>
internal static class LicenseProbeFileLog
{
    private static readonly object Gate = new();
    private static string? _path;

    private static string LogPath
    {
        get
        {
            if (_path is not null)
                return _path;

            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "VP-Hub",
                "logs");
            _path = Path.Combine(dir, "licencing.probe.log");
            return _path;
        }
    }

    internal static void Write(string message, string? correlationId = null)
    {
        try
        {
            var dir = Path.GetDirectoryName(LogPath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            var pid = Process.GetCurrentProcess().Id;
            var cid = string.IsNullOrEmpty(correlationId) ? "" : $" cid={correlationId}";
            var line =
                $"{DateTimeOffset.UtcNow:yyyy-MM-ddTHH:mm:ss.fffZ} pid={pid}{cid} {message}";

            lock (Gate)
            {
                File.AppendAllText(LogPath, line + Environment.NewLine, Encoding.UTF8);
            }
        }
        catch
        {
            // Never break Revit because of diagnostics.
        }
    }
}
