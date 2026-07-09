namespace LicensingSystem.Contracts.Agent.Ipc;

/// <summary>Generic success/failure envelope returned by several agent IPC operations.</summary>
public sealed record IpcOperationResult(bool Ok, string? ErrorCode, string? Message);
