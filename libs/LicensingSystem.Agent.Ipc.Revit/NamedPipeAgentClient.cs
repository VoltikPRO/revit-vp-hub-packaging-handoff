using System.IO.Pipes;
using System.Text;
using LicensingSystem.Contracts.Agent.Grants;
using LicensingSystem.Contracts.Agent.Ipc;

namespace LicensingSystem.Agent.Ipc;

/// <summary>
/// Revit 2024 (.NET Framework 4.8) build of the named-pipe client. Same protocol as <c>LicensingSystem.Agent.Ipc</c> on .NET 8.
/// </summary>
public sealed class NamedPipeAgentClient
{
    /// <summary>
    /// Default connect timeout for calls where the caller has no specific latency requirement
    /// (used by the real agent's own UI ↔ service communication, where both processes are part
    /// of the same installation and a generous margin for cold-start is appropriate).
    /// </summary>
    public const int ConnectTimeoutMs = 60_000;

    /// <summary>
    /// Recommended connect timeout for a Revit add-in's own license check (see
    /// docs/architecture/canrun-contract-versioning.md §1f). A running agent accepts a pipe
    /// connection in milliseconds — Windows named-pipe connect semantics only take *this long* to
    /// fail when the agent isn't running at all, since <c>WaitNamedPipe</c> polls for the pipe to
    /// appear rather than failing fast. Using <see cref="ConnectTimeoutMs"/> (60s) here means every
    /// license check on a machine with no agent installed blocks Revit's UI thread for up to a
    /// minute (measured; this is not theoretical) — the exact experience that costs App Store
    /// reviews and blocks a genuine standalone-trial flow. This value still comfortably covers a
    /// just-launched agent process initializing its pipe server.
    /// </summary>
    public const int FastFailConnectTimeoutMs = 3_000;

    public const int ResponseTimeoutMs = 20_000;

    private static readonly SemaphoreSlim IpcGate = new(1, 1);

    private readonly string _pipeName;

    public NamedPipeAgentClient(string? pipeName = null)
    {
        _pipeName = string.IsNullOrWhiteSpace(pipeName)
            ? NamedPipeNames.ForCurrentUser()
            : pipeName!;
    }

    public Task<TResponse> CallAsync<TResponse>(object request, CancellationToken ct = default) =>
        CallAsync<TResponse>(request, ConnectTimeoutMs, ct);

    public async Task<TResponse> CallAsync<TResponse>(object request, int connectTimeoutMs, CancellationToken ct = default)
    {
        await IpcGate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            using var client = new NamedPipeClientStream(".", _pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
            await ConnectWithTimeoutAsync(client, connectTimeoutMs, ct).ConfigureAwait(false);

            // IpcJsonCodec: STJ first; Newtonsoft on AppDomain STJ/Encodings conflicts (Revit 2023–2024).
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

    private static Task ConnectWithTimeoutAsync(NamedPipeClientStream client, int timeoutMs, CancellationToken ct)
    {
        return Task.Run(
            () =>
            {
                ct.ThrowIfCancellationRequested();
                client.Connect(timeoutMs);
            },
            ct);
    }

    public Task<CanRunProofDto> CanRunAsync(
        string productCode,
        string? pluginVersion,
        string nonce,
        DateTimeOffset timestampUtc,
        CancellationToken ct = default) =>
        CanRunAsync(productCode, pluginVersion, nonce, timestampUtc, FastFailConnectTimeoutMs, ct);

    /// <param name="connectTimeoutMs">
    /// Defaults to <see cref="FastFailConnectTimeoutMs"/> — see its doc comment for why a license
    /// check should not use the 60s default. Pass <see cref="ConnectTimeoutMs"/> explicitly only
    /// for parity testing against the old behavior.
    /// </param>
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

    /// <summary>
    /// Submits lightweight in-product feedback (thumbs up/down + optional comment) for a command.
    /// Never call this synchronously from a Revit command handler — it is a network round trip;
    /// callers must fire-and-forget with their own timeout (see VpHubPluginFeedback in
    /// LicensingSystem.Revit.Licensing, which wraps exactly that).
    /// </summary>
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

    /// <summary>Starts a problem report from a plugin; see VpHubPluginProblemReport in LicensingSystem.Revit.Licensing.</summary>
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
