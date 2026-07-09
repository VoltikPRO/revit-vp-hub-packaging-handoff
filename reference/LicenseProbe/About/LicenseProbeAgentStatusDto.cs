namespace LicensingSystem.Revit.LicenseProbe.About;

// Local DTO for agent IPC status response. Kept in the plugin project so it works for both net48 and net8 builds.
// Must stay compatible with the JSON shape produced by LicensingSystem.Agent.Core.AgentStatus (camelCase).
public sealed class LicenseProbeAgentStatusDto
{
    public string? State { get; set; }
    public bool IsOnline { get; set; }
    public DateTimeOffset? LastOnlineAt { get; set; }
    public int OfflineRemainingMinutes { get; set; }
    public DateTimeOffset? LastApiAttemptUtc { get; set; }
    public int? LastApiHttpStatus { get; set; }
    public string? LastApiErrorCode { get; set; }
    public string? ApiConnectivitySummary { get; set; }
    public bool ApiRespondedRecently { get; set; }
}

