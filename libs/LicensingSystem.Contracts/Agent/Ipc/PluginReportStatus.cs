namespace LicensingSystem.Contracts.Agent.Ipc;

/// <summary>
/// Where a problem report sent from a plugin has got to. <see cref="Ok"/> is false only when
/// the report failed or the agent has no record of it; while it is on its way
/// <see cref="State"/> is <see cref="PluginReportStates.Sending"/>.
/// </summary>
public sealed record PluginReportStatus(
    bool Ok,
    string? RequestId,
    string? State,
    string? ReportId,
    string? ErrorCode,
    string? Message);

public static class PluginReportStates
{
    public const string Sending = "sending";
    public const string Sent = "sent";
    public const string Failed = "failed";
}
