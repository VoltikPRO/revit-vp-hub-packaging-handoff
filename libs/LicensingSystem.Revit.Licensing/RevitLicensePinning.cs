using System.Security.Cryptography;

namespace LicensingSystem.Revit.Licensing;

/// <summary>
/// Publisher and product pinning for grant proof verification. Typically built from add-in constants
/// (see admin <c>plugin-pinning</c> page) plus P-256 public <see cref="ECParameters"/> for Revit 2024 / net48 signature checks.
///
/// Optionally pins a *second*, independently-trusted key — the trial key (see
/// docs/architecture/canrun-contract-versioning.md §1b). Registered the same way as any other
/// publisher key (a new <c>publisher_keys</c> row with its own <c>kid</c>); the Worker has no
/// notion of "trial" at all. What makes it a trial key is purely that an add-in chooses to pin
/// it as one here — and the verifier then enforces hard caps (seat count, validity window) on
/// anything it signs, regardless of what the payload itself claims, so a compromised trial key
/// costs bounded free trials rather than arbitrary licenses. Add-ins that never pin a trial key
/// simply cannot verify anything signed by one (deny-by-default, unchanged from before this
/// existed).
/// </summary>
public sealed class RevitLicensePinning
{
    private readonly ECParameters _p256PublicKeyOnly;
    private readonly ECParameters? _trialP256PublicKeyOnly;

    public RevitLicensePinning(
        string productCode,
        string expectedPublisherId,
        string expectedKid,
        string expectedPublisherPublicKeyPem,
        string? expectedAudienceServerId,
        ECParameters p256PublicKeyOnly,
        string? expectedTrialKid = null,
        string? expectedTrialPublisherPublicKeyPem = null,
        ECParameters? trialP256PublicKeyOnly = null)
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

        var trialConfigured = !string.IsNullOrWhiteSpace(expectedTrialKid)
            || !string.IsNullOrWhiteSpace(expectedTrialPublisherPublicKeyPem)
            || trialP256PublicKeyOnly is not null;
        if (trialConfigured)
        {
            if (string.IsNullOrWhiteSpace(expectedTrialKid))
                throw new ArgumentException("expectedTrialKid is required when pinning a trial key.", nameof(expectedTrialKid));
            if (string.IsNullOrWhiteSpace(expectedTrialPublisherPublicKeyPem))
                throw new ArgumentException("expectedTrialPublisherPublicKeyPem is required when pinning a trial key.", nameof(expectedTrialPublisherPublicKeyPem));
            if (trialP256PublicKeyOnly is null)
                throw new ArgumentException("trialP256PublicKeyOnly is required when pinning a trial key.", nameof(trialP256PublicKeyOnly));
            if (string.Equals(expectedTrialKid, expectedKid, StringComparison.Ordinal))
                throw new ArgumentException("Trial kid must differ from the production kid.", nameof(expectedTrialKid));

            var trialParams = trialP256PublicKeyOnly.Value;
            trialParams.Curve = ECCurve.NamedCurves.nistP256;
            if (trialParams.Q.X is null || trialParams.Q.Y is null)
                throw new ArgumentException("ECParameters must include public point Q (P-256).", nameof(trialP256PublicKeyOnly));

            ExpectedTrialKid = expectedTrialKid;
            ExpectedTrialPublisherPublicKeyPem = expectedTrialPublisherPublicKeyPem;
            _trialP256PublicKeyOnly = trialParams;
        }
    }

    public string ProductCode { get; }

    public string ExpectedPublisherId { get; }

    public string ExpectedKid { get; }

    public string ExpectedPublisherPublicKeyPem { get; }

    /// <summary>Optional tenant binding; empty unless grants pin <c>audienceServerId</c>.</summary>
    public string ExpectedAudienceServerId { get; }

    /// <summary>Kid of the pinned trial key, if this add-in trusts one. Null means: no trial key pinned.</summary>
    public string? ExpectedTrialKid { get; }

    public string? ExpectedTrialPublisherPublicKeyPem { get; }

    internal ECParameters P256PublicKeyParameters => _p256PublicKeyOnly;

    internal ECParameters? TrialP256PublicKeyParameters => _trialP256PublicKeyOnly;
}
