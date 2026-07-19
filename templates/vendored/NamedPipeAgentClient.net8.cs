using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using LicensingSystem.Contracts.Agent.Grants;
using LicensingSystem.Contracts.Agent.Ipc;

namespace LicensingSystem.Agent.Ipc;

/// <summary>
/// Publisher kit slice: named-pipe client for Revit add-ins (.NET 8). Wire format matches the full agent IPC.
/// </summary>
public sealed class NamedPipeAgentClient
{
    public const int ConnectTimeoutMs = 60_000;
    public const int ResponseTimeoutMs = 20_000;

    private static readonly SemaphoreSlim IpcGate = new(1, 1);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = false,
    };

    private readonly string _pipeName;

    public NamedPipeAgentClient(string? pipeName = null)
    {
        _pipeName = string.IsNullOrWhiteSpace(pipeName)
            ? NamedPipeNames.ForCurrentUser()
            : pipeName;
    }

    public async Task<TResponse> CallAsync<TResponse>(object request, CancellationToken ct = default)
    {
        await IpcGate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await using var client = new NamedPipeClientStream(".", _pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
            await client.ConnectAsync(ConnectTimeoutMs, ct).ConfigureAwait(false);

            var reqJson = JsonSerializer.Serialize(request, request.GetType(), JsonOptions);
            var reqBytes = Encoding.UTF8.GetBytes(reqJson);
            await PipeJsonFraming.WriteFrameAsync(client, reqBytes, ct).ConfigureAwait(false);

            using var respCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            respCts.CancelAfter(ResponseTimeoutMs);
            byte[] responseBytes;
            try
            {
                responseBytes = await PipeJsonFraming.ReadFrameAsync(client, respCts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                throw new TimeoutException($"Agent IPC response timed out after {ResponseTimeoutMs}ms.");
            }

            // String overload (not ReadOnlySpan<byte>) — Revit 2024 STJ AppDomain conflicts; keep in sync with Agent.Ipc.Revit.
            return JsonSerializer.Deserialize<TResponse>(Encoding.UTF8.GetString(responseBytes), JsonOptions)
                   ?? throw new InvalidOperationException("Could not deserialize agent response.");
        }
        finally
        {
            IpcGate.Release();
        }
    }

    public Task<CanRunProofDto> CanRunAsync(
        string productCode,
        string? pluginVersion,
        string nonce,
        DateTimeOffset timestampUtc,
        CancellationToken ct = default) =>
        CallAsync<CanRunProofDto>(new { type = "canRun", productCode, pluginVersion, nonce, timestampUtc }, ct);

    public Task<CanRunProofDto> CanRunAsync(string productCode, string? pluginVersion, CancellationToken ct = default) =>
        CanRunAsync(productCode, pluginVersion, Guid.NewGuid().ToString("N"), DateTimeOffset.UtcNow, ct);

    public Task<IpcOperationResult> LogPluginEventAsync(
        string productCode,
        string pluginVersion,
        string commandId,
        string stage,
        string? correlationId = null,
        string? message = null,
        int? durationMs = null,
        string? errorCode = null,
        string? errorMessage = null,
        IReadOnlyDictionary<string, object?>? extra = null,
        CancellationToken ct = default) =>
        CallAsync<IpcOperationResult>(new
        {
            type = "logPluginEvent",
            productCode,
            pluginVersion,
            commandId,
            stage,
            correlationId,
            message,
            durationMs,
            errorCode,
            errorMessage,
            extra,
        }, ct);
}
