namespace LicensingSystem.Contracts.Agent.Devices;

public sealed record RegisterDeviceRequest(
    string DeviceName,
    string OsFingerprint,
    string AgentVersion,
    string? DevicePublicKey);

public sealed record RegisterDeviceResponse(Guid DeviceId);

