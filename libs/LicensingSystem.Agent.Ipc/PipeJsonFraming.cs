using System.Buffers.Binary;

namespace LicensingSystem.Agent.Ipc;

/// <summary>
/// Length-prefixed UTF-8 frames over a bidirectional byte stream (named pipe).
/// Avoids line-based framing and mixed StreamReader/StreamWriter issues.
/// </summary>
internal static class PipeJsonFraming
{
    public const int MaxFrameBytes = 4 * 1024 * 1024;

    public static async Task WriteFrameAsync(Stream stream, ReadOnlyMemory<byte> payload, CancellationToken ct)
    {
        if (payload.Length == 0 || payload.Length > MaxFrameBytes)
            throw new ArgumentOutOfRangeException(nameof(payload), "IPC frame length must be in (0, MaxFrameBytes].");

        var header = new byte[4];
        BinaryPrimitives.WriteUInt32LittleEndian(header, (uint)payload.Length);
        await stream.WriteAsync(header, ct).ConfigureAwait(false);
        await stream.WriteAsync(payload, ct).ConfigureAwait(false);
        await stream.FlushAsync(ct).ConfigureAwait(false);
    }

    public static async Task<byte[]> ReadFrameAsync(Stream stream, CancellationToken ct)
    {
        var lenBuf = new byte[4];
        await ReadExactlyAsync(stream, lenBuf, ct).ConfigureAwait(false);
        var len = BinaryPrimitives.ReadUInt32LittleEndian(lenBuf);
        if (len == 0 || len > MaxFrameBytes)
            throw new InvalidOperationException($"Invalid IPC frame length: {len}.");

        var body = new byte[len];
        await ReadExactlyAsync(stream, body, ct).ConfigureAwait(false);
        return body;
    }

    private static async Task ReadExactlyAsync(Stream stream, Memory<byte> buffer, CancellationToken ct)
    {
        var offset = 0;
        while (offset < buffer.Length)
        {
            var read = await stream.ReadAsync(buffer[offset..], ct).ConfigureAwait(false);
            if (read == 0)
                throw new EndOfStreamException("Unexpected end of stream while reading IPC frame.");
            offset += read;
        }
    }
}
