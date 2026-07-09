using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using LicensingSystem.Agent.Ipc;
using System.Diagnostics;

namespace LicensingSystem.Revit.LicenseProbe;

[Transaction(TransactionMode.Manual)]
[Regeneration(RegenerationOption.Manual)]
public sealed class LicenseProbeCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        if (commandData is null)
            throw new ArgumentNullException(nameof(commandData));
        var correlationId = Guid.NewGuid().ToString("N");
        var sw = Stopwatch.StartNew();
        LicenseProbeFileLog.Write("Command.Execute: begin (License status).", correlationId);
        // Avoid pre-command TryLog: it shares IpcGate with BuildAsync; fire-and-forget + GetResult() can deadlock Revit.

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var revitVersion = commandData.Application?.Application?.VersionNumber ?? "(unknown)";
            var text = LicenseProbeEnsureLicensed
                .BuildStatusReportAsync(revitVersion, ct: cts.Token, correlationId: correlationId)
                .GetAwaiter()
                .GetResult();
            LicenseProbeFileLog.Write("Command.Execute: showing TaskDialog.", correlationId);
            TaskDialog.Show("License Probe — Licensing", text);
            sw.Stop();
            LicenseProbeFileLog.Write(
                $"Command.Execute: dialog closed; total {sw.ElapsedMilliseconds}ms.",
                correlationId);
            _ = TryLogAsync(
                stage: "success",
                correlationId,
                msg: "License probe command completed.",
                durationMs: (int)sw.ElapsedMilliseconds,
                errorCode: null,
                errorMessage: null);
            return Result.Succeeded;
        }
        catch (Exception ex)
        {
            sw.Stop();
            LicenseProbeFileLog.Write(
                $"Command.Execute: exception after {sw.ElapsedMilliseconds}ms {ex.GetType().Name}: {ex.Message}",
                correlationId);
            _ = TryLogAsync(
                stage: "fail",
                correlationId,
                msg: "License probe command failed.",
                durationMs: (int)sw.ElapsedMilliseconds,
                errorCode: "EXCEPTION",
                errorMessage: ex.Message);
            message = ex.Message;
            return Result.Failed;
        }
    }

    private static Task TryLogAsync(
        string stage,
        string correlationId,
        string msg,
        int? durationMs,
        string? errorCode,
        string? errorMessage)
    {
        // Best-effort: never block Revit UI on IPC / agent availability.
        return Task.Run(async () =>
        {
            try
            {
                using var cts = new CancellationTokenSource(millisecondsDelay: 1500);
                var ipc = new NamedPipeAgentClient();
                await ipc.LogPluginEventAsync(
                        LicenseProbeConstants.ProductCode,
                        pluginVersion: typeof(LicenseProbeCommand).Assembly.GetName().Version?.ToString() ?? "(unknown)",
                        commandId: nameof(LicenseProbeCommand),
                        stage,
                        correlationId: correlationId,
                        message: msg,
                        durationMs: durationMs,
                        errorCode: errorCode,
                        errorMessage: errorMessage,
                        extra: null,
                        ct: cts.Token)
                    .ConfigureAwait(false);
            }
            catch
            {
                // ignore
            }
        });
    }
}
