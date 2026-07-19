namespace LicensingSystem.Revit.LicenseProbe;

/// <summary>Must match <c>productCode</c> in backend and update manifest.</summary>
public static class LicenseProbeConstants
{
    public const string ProductCode = "licencing.probe";

    /// <summary>Trusted publisher issuing grants for this probe product (matches <c>license_grants.PublisherId</c>).</summary>
    public const string ExpectedPublisherId = "3de34dda-7c5f-4b39-9b56-382217e1de83";

    public const string ExpectedKid = "key-2026-05";

    /// <summary>EC P-256 SPKI public key paired with the signing key registered for this publisher in <c>publisher_keys</c>.</summary>
    /// <remarks>If you change this PEM, update <see cref="LicenseProbePinnedEcKey"/> (Revit 2024 / net48 uses embedded coordinates).</remarks>
    public const string ExpectedPublisherPublicKeyPem =
        """
        -----BEGIN PUBLIC KEY-----
        MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAEe+D4DQgAmt0EhziPLbipx7w8LncC
        C/0TVwDDAiWnU5NnH2BOU6nFKCqgf2pJHfhpZlUDJODJggvPY6Al8X5O/A==
        -----END PUBLIC KEY-----
        """;

    /// <summary>Optional tenant binding; leave empty unless the Worker pins <c>audienceServerId</c> on grants.</summary>
    public const string ExpectedAudienceServerId = "";
}
