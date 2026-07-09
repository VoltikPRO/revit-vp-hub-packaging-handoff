using System.Reflection;
using System.Text;
using LicensingSystem.Revit.Licensing;

namespace LicensingSystem.Revit.LicenseProbe;

/// <summary>Central licensing gate for all commands (canRun + proof verification).</summary>
internal static class LicenseProbeEnsureLicensed
{
    internal const string VerifiedOkMarker = "License: OK";

    internal static Task<string> BuildStatusReportAsync(
        string? revitVersion,
        CancellationToken ct = default,
        string? correlationId = null)
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "(unknown)";
        return RevitLicenseCanRunReport.BuildAsync(
            LicenseProbePinningFactory.Create(),
            version,
            revitVersion,
            verifiedOkCacheTtl: RevitLicenseCanRunReport.DefaultVerifiedOkCacheTtl,
            ipcTimeout: TimeSpan.FromSeconds(4),
            trace: msg => LicenseProbeFileLog.Write(msg, correlationId),
            correlationId: correlationId,
            cancellationToken: ct);
    }

    internal static bool TryAllow(
        string? revitVersion,
        string? correlationId,
        out string userMessage)
    {
        userMessage = "";
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var report = BuildStatusReportAsync(revitVersion, cts.Token, correlationId)
                .GetAwaiter()
                .GetResult();

            if (report.Contains(VerifiedOkMarker, StringComparison.Ordinal))
                return true;

            userMessage = FormatDeniedMessage(report);
            return false;
        }
        catch (Exception ex)
        {
            LicenseProbeFileLog.Write(
                $"EnsureLicensed: exception {ex.GetType().Name}: {ex.Message}",
                correlationId);
            userMessage =
                "Licensing check failed. Start VP-Hub / LicensingSystem agent, sign in, and sync entitlements.";
            return false;
        }
    }

    private static string FormatDeniedMessage(string report)
    {
        var lines = report
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.Trim())
            .Where(l => l.Length > 0)
            .Take(3)
            .ToList();

        if (lines.Count == 0)
            return "This product is not licensed for this machine.";

        var sb = new StringBuilder();
        foreach (var line in lines)
            sb.AppendLine(line);
        return sb.ToString().TrimEnd();
    }
}
