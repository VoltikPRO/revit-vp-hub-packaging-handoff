namespace LicensingSystem.Contracts.Org;

public sealed record JoinOrgByKeyRequest(string OrgKey);

public sealed record JoinOrgByKeyResponse(OrgDto Org, MembershipDto Membership);

public sealed record OrgDto(Guid Id, string Name, string Status);

public sealed record MembershipDto(string Role);

