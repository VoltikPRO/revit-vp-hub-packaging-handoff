using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using LicensingSystem.Revit.Licensing;

namespace LicensingSystem.Revit.LicenseProbe;

/// <summary>One click sends a problem report with the logs. Not gated: see <see cref="VpHubPluginProblemReport"/>.</summary>
[Transaction(TransactionMode.ReadOnly)]
[Regeneration(RegenerationOption.Manual)]
public sealed class LicenseProbeReportProblemCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        var revitVersion = commandData?.Application?.Application?.VersionNumber;
        VpHubPluginProblemReport.SendInBackground(
            LicenseProbeConstants.ProductCode,
            typeof(LicenseProbeReportProblemCommand).Assembly.GetName().Version?.ToString(),
            revitVersion is null ? null : "Revit " + revitVersion);
        return Result.Succeeded;
    }
}
