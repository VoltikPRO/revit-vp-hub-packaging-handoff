using LicensingSystem.Revit.Licensing;

namespace LicensingSystem.Revit.LicenseProbe;

/// <summary>Probe-local alias over <see cref="VpHubPluginFileLog"/> (product file under VP-Hub\logs).</summary>
internal static class LicenseProbeFileLog
{
    internal static void Write(string message, string? correlationId = null) =>
        VpHubPluginFileLog.Write(LicenseProbeConstants.ProductCode, message, correlationId);
}
