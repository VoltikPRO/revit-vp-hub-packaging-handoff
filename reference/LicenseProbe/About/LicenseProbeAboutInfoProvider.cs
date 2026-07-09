using LicensingSystem.Agent.Ipc;
using System.Diagnostics;
using System.Reflection;

namespace LicensingSystem.Revit.LicenseProbe.About;

internal static class LicenseProbeAboutInfoProvider
{
    internal static LicenseProbeAboutModel Build(
        string revitVersion,
        string correlationId,
        TimeSpan agentStatusTimeout)
    {
        var addinAsm = typeof(LicenseProbeAboutInfoProvider).Assembly;
        var addinBuild = LicenseProbeBuildInfo.DescribeAssembly(addinAsm);

        LicenseProbeAgentStatusDto? status = null;
        string? agentStatusError = null;
        var sw = Stopwatch.StartNew();
        try
        {
            using var cts = new CancellationTokenSource(agentStatusTimeout);
            var ipc = new NamedPipeAgentClient();
            status = ipc
                .CallAsync<LicenseProbeAgentStatusDto>(new { type = "status" }, cts.Token)
                .GetAwaiter()
                .GetResult();
        }
        catch (Exception ex)
        {
            agentStatusError = $"{ex.GetType().Name}: {ex.Message}";
        }
        finally
        {
            sw.Stop();
        }

        return new LicenseProbeAboutModel(
            brandTitle: "VP-Hub / LicensingSystem",
            productDisplayName: "License Probe",
            description:
                "Reference Revit add-in template.\n" +
                "Talks to the local licensing agent via named pipe and verifies publisher-signed grants.\n" +
                "Does not store secrets and does not call the cloud API directly.",
            revitVersion: revitVersion,
            productCode: LicenseProbeConstants.ProductCode,
            addinAssemblyPath: addinAsm.Location,
            addinBuild: addinBuild,
            correlationId: correlationId,
            agentStatus: status,
            agentStatusDurationMs: (int)sw.ElapsedMilliseconds,
            agentStatusError: agentStatusError);
    }
}

