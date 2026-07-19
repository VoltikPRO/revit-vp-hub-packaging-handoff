using Autodesk.Revit.UI;
using LicensingSystem.Revit.Licensing;

namespace LicensingSystem.Revit.LicenseProbe;

/// <summary>Registers the ribbon entry for the licensing diagnostics window.</summary>
public sealed class LicenseProbeApplication : IExternalApplication
{
    private const string TabName = "VP-Hub";
    private const string PanelName = "Licensing";

    public Result OnStartup(UIControlledApplication application)
    {
        try
        {
            var revitVersion = application.ControlledApplication.VersionNumber;
            var asmPath = typeof(LicenseProbeApplication).Assembly.Location;
            LicenseProbeFileLog.Write(
                $"ExternalApplication.OnStartup: Revit={revitVersion} add-in={asmPath}");

            try
            {
                application.CreateRibbonTab(TabName);
            }
            catch
            {
                // Tab already exists (another add-in or reload).
                LicenseProbeFileLog.Write($"ExternalApplication.OnStartup: ribbon tab '{TabName}' already exists; reusing.");
            }

            var panel = application.CreateRibbonPanel(TabName, PanelName);
            var asm = typeof(LicenseProbeApplication).Assembly.Location;
            var cmd = typeof(LicenseProbeCommand).FullName ?? nameof(LicenseProbeCommand);
            var data = new PushButtonData(
                nameof(LicenseProbeCommand),
                LicenseProbeUi.LicenseStatusButtonLabel,
                asm,
                cmd)
            {
                ToolTip = LicenseProbeUi.LicenseStatusToolTip,
                LongDescription = LicenseProbeUi.LicenseStatusLongDescription,
            };
            panel.AddItem(data);

            var aboutCmd = typeof(LicenseProbeAboutCommand).FullName ?? nameof(LicenseProbeAboutCommand);
            var aboutData = new PushButtonData(
                nameof(LicenseProbeAboutCommand),
                LicenseProbeUi.AboutButtonLabel,
                asm,
                aboutCmd)
            {
                ToolTip = LicenseProbeUi.AboutToolTip,
                LongDescription = LicenseProbeUi.AboutLongDescription,
            };
            panel.AddItem(aboutData);

            LicenseProbeFileLog.Write(
                $"ExternalApplication.OnStartup: push buttons registered tab={TabName} panel={PanelName} commands={cmd}, {aboutCmd}");
        }
        catch (Exception ex)
        {
            LicenseProbeFileLog.Write(
                $"ExternalApplication.OnStartup: FAILED {ex.GetType().Name}: {ex.Message}");
            TaskDialog.Show(LicenseProbeUi.DialogTitle, LicenseProbeUi.RibbonRegistrationFailed(ex.Message));
            return Result.Failed;
        }

        return Result.Succeeded;
    }

    public Result OnShutdown(UIControlledApplication application) => Result.Succeeded;
}
