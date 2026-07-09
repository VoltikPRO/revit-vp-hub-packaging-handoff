using System.Security.Cryptography;

namespace LicensingSystem.Revit.LicenseProbe;

/// <summary>
/// P-256 public point for the pinned PEM in <see cref="LicenseProbeConstants"/> (uncompressed).
/// Used after PEM string equality check so we avoid PEM parsing on .NET Framework.
/// </summary>
internal static class LicenseProbePinnedEcKey
{
    internal static ECParameters DevSeedPublicParameters { get; } = new ECParameters
    {
        Curve = ECCurve.NamedCurves.nistP256,
        Q = new ECPoint
        {
            X = FromHex("7BE0F80D08009ADD0487388F2DB8A9C7BC3C2E77020BFD135700C30225A75393"),
            Y = FromHex("671F604E53A9C5282AA07F6A491DF86966550324E0C9820BCF63A025F17E4EFC"),
        },
    };

    private static byte[] FromHex(string hex)
    {
        var bytes = new byte[hex.Length / 2];
        for (var i = 0; i < bytes.Length; i++)
            bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
        return bytes;
    }
}
