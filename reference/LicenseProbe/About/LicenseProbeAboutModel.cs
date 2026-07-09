namespace LicensingSystem.Revit.LicenseProbe.About;

public sealed class LicenseProbeAboutModel
{
    public string BrandTitle { get; }
    public string ProductDisplayName { get; }
    public string Description { get; }
    public string RevitVersion { get; }
    public string ProductCode { get; }
    public string AddinAssemblyPath { get; }
    public string AddinBuild { get; }
    public string CorrelationId { get; }
    public LicenseProbeAgentStatusDto? AgentStatus { get; }
    public int AgentStatusDurationMs { get; }
    public string? AgentStatusError { get; }

    public LicenseProbeAboutModel(
        string brandTitle,
        string productDisplayName,
        string description,
        string revitVersion,
        string productCode,
        string addinAssemblyPath,
        string addinBuild,
        string correlationId,
        LicenseProbeAgentStatusDto? agentStatus,
        int agentStatusDurationMs,
        string? agentStatusError)
    {
        BrandTitle = brandTitle;
        ProductDisplayName = productDisplayName;
        Description = description;
        RevitVersion = revitVersion;
        ProductCode = productCode;
        AddinAssemblyPath = addinAssemblyPath;
        AddinBuild = addinBuild;
        CorrelationId = correlationId;
        AgentStatus = agentStatus;
        AgentStatusDurationMs = agentStatusDurationMs;
        AgentStatusError = agentStatusError;
    }
}

