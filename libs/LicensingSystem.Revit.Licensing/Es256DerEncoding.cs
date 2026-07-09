namespace LicensingSystem.Revit.Licensing;

/// <summary>ES256 raw R||S (64 bytes) → ASN.1 DER SEQUENCE for <see cref="System.Security.Cryptography.ECDsa.VerifyHash"/> on .NET Framework.</summary>
internal static class Es256DerEncoding
{
    internal static byte[] IeeeP1363ToDer(byte[] ieeeP1363)
    {
        if (ieeeP1363.Length != 64)
            throw new ArgumentException("Expected 64-byte ES256 signature.", nameof(ieeeP1363));

        var r = EncodeInteger(ieeeP1363, 0, 32);
        var s = EncodeInteger(ieeeP1363, 32, 32);
        return WrapSequence(r, s);
    }

    private static byte[] EncodeInteger(byte[] value, int offset, int length)
    {
        var start = offset;
        var end = offset + length;
        while (start < end - 1 && value[start] == 0)
            start++;

        var needsPad0 = (value[start] & 0x80) != 0;
        var len = end - start + (needsPad0 ? 1 : 0);
        var buf = new byte[2 + len];
        buf[0] = 0x02;
        buf[1] = (byte)len;
        var o = 2;
        if (needsPad0)
            buf[o++] = 0;
        Buffer.BlockCopy(value, start, buf, o, end - start);
        return buf;
    }

    private static byte[] WrapSequence(byte[] a, byte[] b)
    {
        var len = a.Length + b.Length;
        var buf = new byte[2 + len];
        buf[0] = 0x30;
        buf[1] = (byte)len;
        Buffer.BlockCopy(a, 0, buf, 2, a.Length);
        Buffer.BlockCopy(b, 0, buf, 2 + a.Length, b.Length);
        return buf;
    }
}
