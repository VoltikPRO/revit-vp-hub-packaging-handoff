using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using LicensingSystem.Agent.Ipc;
using LicensingSystem.Revit.LicenseProbe.About;
using System.Reflection;

namespace LicensingSystem.Revit.LicenseProbe;

[Transaction(TransactionMode.Manual)]
[Regeneration(RegenerationOption.Manual)]
public sealed class LicenseProbeAboutCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        if (commandData is null)
            throw new ArgumentNullException(nameof(commandData));

        var correlationId = Guid.NewGuid().ToString("N");
        LicenseProbeFileLog.Write("About.Execute: begin.", correlationId);

        try
        {
            var revitVersion = commandData.Application?.Application?.VersionNumber ?? "(unknown)";

            var model = LicenseProbeAboutInfoProvider.Build(
                revitVersion: revitVersion,
                correlationId: correlationId,
                agentStatusTimeout: TimeSpan.FromSeconds(2));

            var window = new About.LicenseProbeAboutWindow(model);
            window.ShowDialog();
            LicenseProbeFileLog.Write("About.Execute: closed.", correlationId);
            return Result.Succeeded;
        }
        catch (Exception ex)
        {
            LicenseProbeFileLog.Write($"About.Execute: exception {ex.GetType().Name}: {ex.Message}", correlationId);
            message = ex.Message;
            return Result.Failed;
        }
    }
}

