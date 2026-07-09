namespace LicensingSystem.Contracts.Agent.Seats;

public sealed record SeatLeaseRequest(Guid ProductId, Guid DeviceId);

public sealed record SeatLeaseResponse(Guid LeaseId, string LeaseToken, DateTimeOffset ExpiresAt);

public sealed record SeatRenewRequest(Guid LeaseId, string LeaseToken);

public sealed record SeatRenewResponse(DateTimeOffset ExpiresAt);

public sealed record SeatReleaseRequest(Guid LeaseId, string LeaseToken);

