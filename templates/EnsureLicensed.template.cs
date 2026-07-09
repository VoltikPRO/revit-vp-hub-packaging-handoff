using Autodesk.Revit.UI;
using LicensingSystem.Revit.Licensing;

namespace MyProduct.Licensing;

/// <summary>Central licensing gate for all licensed commands.</summary>
public static class EnsureLicensed
{
    public static bool TryAllow(UIApplication? uiApp, out string userMessage)
    {
        userMessage = "";
        try
        {
            var revitVersion = uiApp?.Application?.VersionNumber ?? "(unknown)";
            var pluginVersion = typeof(EnsureLicensed).Assembly.GetName().Version?.ToString() ?? "(unknown)";
            var pinning = PublisherPinningFactory.Create();

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var report = RevitLicenseCanRunReport
                .BuildAsync(pinning, pluginVersion, revitVersion, cancellationToken: cts.Token)
                .GetAwaiter()
                .GetResult();

            // BuildAsync returns human-readable status; treat lines starting with "OK" or your success marker as allow.
            if (report.StartsWith("OK", StringComparison.OrdinalIgnoreCase)
                || report.Contains("License OK", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            userMessage = report;
            return false;
        }
        catch (Exception ex)
        {
            userMessage = "Licensing check failed. Start VP-Hub / LicensingSystem agent, sign in, and sync entitlements.";
            return false;
        }
    }
}

// Implement PublisherPinningFactory like reference/LicenseProbe/LicenseProbePinningFactory.cs
