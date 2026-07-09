using System.Security.Cryptography;

namespace LicensingSystem.Revit.Licensing;

/// <summary>
/// Publisher and product pinning for grant proof verification. Typically built from add-in constants
/// (see admin <c>plugin-pinning</c> page) plus P-256 public <see cref="ECParameters"/> for Revit 2024 / net48 signature checks.
/// </summary>
public sealed class RevitLicensePinning
{
    private readonly ECParameters _p256PublicKeyOnly;

    public RevitLicensePinning(
        string productCode,
        string expectedPublisherId,
        string expectedKid,
        string expectedPublisherPublicKeyPem,
        string? expectedAudienceServerId,
        ECParameters p256PublicKeyOnly)
    {
        ProductCode = productCode ?? throw new ArgumentNullException(nameof(productCode));
        ExpectedPublisherId = expectedPublisherId ?? throw new ArgumentNullException(nameof(expectedPublisherId));
        ExpectedKid = expectedKid ?? throw new ArgumentNullException(nameof(expectedKid));
        ExpectedPublisherPublicKeyPem = expectedPublisherPublicKeyPem
            ?? throw new ArgumentNullException(nameof(expectedPublisherPublicKeyPem));
        ExpectedAudienceServerId = expectedAudienceServerId ?? "";
        _p256PublicKeyOnly = p256PublicKeyOnly;
        _p256PublicKeyOnly.Curve = ECCurve.NamedCurves.nistP256;
        if (_p256PublicKeyOnly.Q.X is null || _p256PublicKeyOnly.Q.Y is null)
            throw new ArgumentException("ECParameters must include public point Q (P-256).", nameof(p256PublicKeyOnly));
    }

    public string ProductCode { get; }

    public string ExpectedPublisherId { get; }

    public string ExpectedKid { get; }

    public string ExpectedPublisherPublicKeyPem { get; }

    /// <summary>Optional tenant binding; empty unless grants pin <c>audienceServerId</c>.</summary>
    public string ExpectedAudienceServerId { get; }

    internal ECParameters P256PublicKeyParameters => _p256PublicKeyOnly;
}
