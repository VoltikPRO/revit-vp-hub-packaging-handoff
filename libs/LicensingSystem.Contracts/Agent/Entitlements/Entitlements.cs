namespace LicensingSystem.Contracts.Agent.Entitlements;

public sealed record EntitlementsResponse(
    OrgInfo Org,
    OfflinePolicy OfflinePolicy,
    IReadOnlyList<ProductEntitlementDto> Products,
    DateTimeOffset ServerTime);

public sealed record OrgInfo(Guid Id, string Name);

public sealed record OfflinePolicy(int GraceHours);

public sealed record ProductEntitlementDto(
    Guid ProductId,
    string Code,
    string Name,
    string Status,
    DateTimeOffset? ValidUntil,
    int SeatLimit,
    string Channel);

