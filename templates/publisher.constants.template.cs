// Template: replace MyProduct namespace and paste pinning values from VP-Hub publisher portal.
namespace MyProduct.Licensing;

public static class PublisherConstants
{
    /// <summary>Must match product Code in publisher portal and artifacts/manifest.json.</summary>
    public const string ProductCode = "revit.myproduct";

    public const string ExpectedPublisherId = "PASTE-FROM-PORTAL";
    public const string ExpectedKid = "key-YYYY-MM";

    public const string ExpectedPublisherPublicKeyPem =
        """
        -----BEGIN PUBLIC KEY-----
        PASTE-FROM-PORTAL
        -----END PUBLIC KEY-----
        """;

    /// <summary>Leave empty unless grants pin audienceServerId.</summary>
    public const string ExpectedAudienceServerId = "";
}
