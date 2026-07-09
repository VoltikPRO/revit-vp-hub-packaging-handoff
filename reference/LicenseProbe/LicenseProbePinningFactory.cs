using LicensingSystem.Revit.Licensing;

namespace LicensingSystem.Revit.LicenseProbe;

internal static class LicenseProbePinningFactory
{
    internal static RevitLicensePinning Create() => new(
        LicenseProbeConstants.ProductCode,
        LicenseProbeConstants.ExpectedPublisherId,
        LicenseProbeConstants.ExpectedKid,
        LicenseProbeConstants.ExpectedPublisherPublicKeyPem,
        string.IsNullOrEmpty(LicenseProbeConstants.ExpectedAudienceServerId)
            ? null
            : LicenseProbeConstants.ExpectedAudienceServerId,
        LicenseProbePinnedEcKey.DevSeedPublicParameters);
}
