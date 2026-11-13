using System.IO.Pipes;
using System.Text;
using LicensingSystem.Contracts.Agent.Grants;
using LicensingSystem.Contracts.Agent.Ipc;

namespace LicensingSystem.Agent.Ipc;

/// <summary>
/// Publisher kit slice: named-pipe client for Revit add-ins (.NET 8). Wire format matches the full agent IPC.
/// Plugin-facing subset of LicensingSystem agent/src/LicensingSystem.Agent.Ipc/NamedPipeAgentClient.cs — keep in sync.
/// </summary>
public sealed class NamedPipeAgentClient
{
    /// <summary>Default connect timeout (agent's own UI ↔ service calls).</summary>
    public const int ConnectTimeoutMs = 60_000;

    /// <summary>
    /// Connect timeout for a Revit add-in's license check and problem reports. Named-pipe connect polls
    /// for the pipe rather than failing fast, so 60s would freeze Revit's UI for a minute on a machine
    /// with no agent installed.
    /// </summary>
    public const int FastFailConnectTimeoutMs = 3_000;

    /// <summary>Safety timeout waiting for one response frame from the agent.</summary>
    public const int ResponseTimeoutMs = 20_000;

    private static readonly SemaphoreSlim IpcGate = new(1, 1);

    private readonly string _pipeName;

    public NamedPipeAgentClient(string? pipeName = null)
    {
        _pipeName = string.IsNullOrWhiteSpace(pipeName)
            ? NamedPipeNames.ForCurrentUser()
            : pipeName;
    }

    public Task<TResponse> CallAsync<TResponse>(object request, CancellationToken ct = default) =>
        CallAsync<TResponse>(request, ConnectTimeoutMs, ct);

    public async Task<TResponse> CallAsync<TResponse>(object request, int connectTimeoutMs, CancellationToken ct = default)
    {
        await IpcGate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await using var client = new NamedPipeClientStream(".", _pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
            await client.ConnectAsync(connectTimeoutMs, ct).ConfigureAwait(false);

            // IpcJsonCodec: STJ first; Newtonsoft on AppDomain STJ/Encodings conflicts (parity with Agent.Ipc.Revit).
            var reqJson = IpcJsonCodec.Serialize(request);
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

            return IpcJsonCodec.Deserialize<TResponse>(Encoding.UTF8.GetString(responseBytes));
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
        CanRunAsync(productCode, pluginVersion, nonce, timestampUtc, FastFailConnectTimeoutMs, ct);

    public Task<CanRunProofDto> CanRunAsync(
        string productCode,
        string? pluginVersion,
        string nonce,
        DateTimeOffset timestampUtc,
        int connectTimeoutMs,
        CancellationToken ct = default) =>
        CallAsync<CanRunProofDto>(new { type = "canRun", productCode, pluginVersion, nonce, timestampUtc }, connectTimeoutMs, ct);

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

    /// <summary>In-product feedback; fire-and-forget only (see VpHubPluginFeedback).</summary>
    public Task<IpcOperationResult> SubmitPluginFeedbackAsync(
        string productCode,
        string commandId,
        int rating,
        string? pluginVersion = null,
        string? comment = null,
        CancellationToken ct = default) =>
        CallAsync<IpcOperationResult>(new
        {
            type = "logPluginFeedback",
            productCode,
            commandId,
            rating,
            pluginVersion,
            comment,
        }, ct);

    /// <summary>Starts a problem report from a plugin; see VpHubPluginProblemReport.</summary>
    public Task<PluginReportStatus> SubmitPluginReportAsync(
        string productCode,
        string? pluginVersion,
        string? hostVersion,
        string? description,
        CancellationToken ct = default) =>
        CallAsync<PluginReportStatus>(new
        {
            type = "submitPluginReport",
            productCode,
            pluginVersion,
            hostVersion,
            description,
        }, FastFailConnectTimeoutMs, ct);

    public Task<PluginReportStatus> GetPluginReportStatusAsync(string requestId, CancellationToken ct = default) =>
        CallAsync<PluginReportStatus>(new { type = "getPluginReportStatus", requestId }, FastFailConnectTimeoutMs, ct);
}
