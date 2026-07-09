namespace LicensingSystem.Contracts.Org;

public sealed record LeaveOrgRequest();

public sealed record LeaveOrgResponse(bool Ok, OrgDto? PreviousOrg);

