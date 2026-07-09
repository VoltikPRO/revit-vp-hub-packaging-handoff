namespace LicensingSystem.Agent.Ipc;

/// <summary>
/// Length-prefixed UTF-8 frames (netstandard2.0 / .NET Framework–friendly stream APIs).
/// </summary>
internal static class PipeJsonFraming
{
    public const int MaxFrameBytes = 4 * 1024 * 1024;

    public static async Task WriteFrameAsync(Stream stream, byte[] payload, CancellationToken ct)
    {
        if (payload == null || payload.Length == 0 || payload.Length > MaxFrameBytes)
            throw new ArgumentOutOfRangeException(nameof(payload), "IPC frame length must be in (0, MaxFrameBytes].");

        var header = new byte[4];
        var len = (uint)payload.Length;
        header[0] = (byte)len;
        header[1] = (byte)(len >> 8);
        header[2] = (byte)(len >> 16);
        header[3] = (byte)(len >> 24);
        await stream.WriteAsync(header, 0, 4, ct).ConfigureAwait(false);
        await stream.WriteAsync(payload, 0, payload.Length, ct).ConfigureAwait(false);
        await stream.FlushAsync(ct).ConfigureAwait(false);
    }

    public static async Task<byte[]> ReadFrameAsync(Stream stream, CancellationToken ct)
    {
        var lenBuf = new byte[4];
        await ReadExactlyAsync(stream, lenBuf, 0, 4, ct).ConfigureAwait(false);
        var len = (uint)lenBuf[0] | ((uint)lenBuf[1] << 8) | ((uint)lenBuf[2] << 16) | ((uint)lenBuf[3] << 24);
        if (len == 0 || len > MaxFrameBytes)
            throw new InvalidOperationException($"Invalid IPC frame length: {len}.");

        var body = new byte[len];
        await ReadExactlyAsync(stream, body, 0, body.Length, ct).ConfigureAwait(false);
        return body;
    }

    private static async Task ReadExactlyAsync(Stream stream, byte[] buffer, int offset, int count, CancellationToken ct)
    {
        var readTotal = 0;
        while (readTotal < count)
        {
            var n = await stream.ReadAsync(buffer, offset + readTotal, count - readTotal, ct).ConfigureAwait(false);
            if (n == 0)
                throw new EndOfStreamException("Unexpected end of stream while reading IPC frame.");
            readTotal += n;
        }
    }
}
