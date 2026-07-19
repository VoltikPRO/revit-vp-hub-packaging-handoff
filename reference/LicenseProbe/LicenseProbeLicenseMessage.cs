using System.Reflection;
using LicensingSystem.Revit.Licensing;

namespace LicensingSystem.Revit.LicenseProbe;

internal static class LicenseProbeLicenseMessage
{
    internal static Task<string> BuildAsync(
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
}
