namespace LicensingSystem.Contracts.Agent.Devices;

public sealed record DeviceHeartbeatRequest(Guid DeviceId, string AgentVersion);
