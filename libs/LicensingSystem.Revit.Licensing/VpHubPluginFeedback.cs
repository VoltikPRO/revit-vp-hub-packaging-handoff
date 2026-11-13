using System.IO;
using LicensingSystem.Agent.Ipc;

namespace LicensingSystem.Revit.Licensing;

/// <summary>
/// In-product feedback (thumbs up/down + optional comment), sent to the agent over the same
/// named-pipe IPC as <c>logPluginEvent</c> — never a direct network call from the add-in
/// (see docs/architecture/revit-licensing.md "Forbidden patterns").
///
/// Two rules an add-in integrating this must not violate:
///
/// 1. <see cref="SubmitAsync"/> must never be awaited on the Revit UI thread in a way that
///    blocks command completion. Call it as a fire-and-forget task (or await it only after
///    the licensed command's own work is done and the user has already seen a result) — a
///    feedback prompt failing or timing out must never be visible as the command failing.
/// 2. Ask sparingly. <see cref="ShouldPrompt"/> tracks successful-run counts per product in a
///    local file (mirrors <see cref="VpHubPluginFileLog"/>'s storage convention) so an add-in
///    does not need to invent its own throttling; the default is "at most once every 20
///    successful runs, and never again after a comment has been left".
/// </summary>
public static class VpHubPluginFeedback
{
    /// <summary>Hard timeout for the whole submit — this is a courtesy signal, not a critical path.</summary>
    private const int SubmitTimeoutMs = 3_000;

    private const int DefaultMinRunsBetweenPrompts = 20;

    private static readonly object StateGate = new();

    /// <summary>
    /// Sends feedback for a command. Safe to call without awaiting; never throws.
    /// Rating: positive = thumbs up, negative = thumbs down (any nonzero value works — the
    /// server normalizes to +1/-1).
    /// </summary>
    public static async Task<bool> SubmitAsync(
        string productCode,
        string commandId,
        int rating,
        string? pluginVersion = null,
        string? comment = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(productCode) || string.IsNullOrWhiteSpace(commandId) || rating == 0)
        {
            return false;
        }

        try
        {
            using var timeoutCts = new CancellationTokenSource(SubmitTimeoutMs);
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);

            var client = new NamedPipeAgentClient();
            var result = await client
                .SubmitPluginFeedbackAsync(productCode, commandId, rating, pluginVersion, comment, linked.Token)
                .ConfigureAwait(false);

            if (result.Ok)
            {
                RecordCommentGiven(productCode, comment);
            }
            return result.Ok;
        }
        catch
        {
            // Never let a feedback prompt affect the licensed command it is attached to.
            return false;
        }
    }

    /// <summary>
    /// Whether an add-in should show a feedback prompt now, given the number of successful
    /// runs recorded so far via <see cref="RecordSuccessfulRun"/>. Call this before deciding
    /// to show any UI — it does no IPC or network work.
    /// </summary>
    public static bool ShouldPrompt(string productCode, int minRunsBetweenPrompts = DefaultMinRunsBetweenPrompts)
    {
        try
        {
            var state = ReadState(productCode);
            if (state.CommentGiven)
            {
                return false; // Already have qualitative feedback; stop interrupting this user.
            }
            if (minRunsBetweenPrompts <= 0)
            {
                return true;
            }
            return state.SuccessfulRunsSincePrompt >= minRunsBetweenPrompts;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>Call once per successful licensed command, regardless of whether a prompt is shown.</summary>
    public static void RecordSuccessfulRun(string productCode)
    {
        try
        {
            lock (StateGate)
            {
                var state = ReadState(productCode);
                state.SuccessfulRunsSincePrompt += 1;
                WriteState(productCode, state);
            }
        }
        catch
        {
            // Never break Revit because of local throttling state.
        }
    }

    /// <summary>Call after a prompt was shown and the user answered (even "no thanks"), to reset the counter.</summary>
    public static void RecordPromptShown(string productCode)
    {
        try
        {
            lock (StateGate)
            {
                var state = ReadState(productCode);
                state.SuccessfulRunsSincePrompt = 0;
                WriteState(productCode, state);
            }
        }
        catch
        {
            // Never break Revit because of local throttling state.
        }
    }

    private static void RecordCommentGiven(string productCode, string? comment)
    {
        if (string.IsNullOrWhiteSpace(comment))
        {
            return;
        }
        try
        {
            lock (StateGate)
            {
                var state = ReadState(productCode);
                state.CommentGiven = true;
                WriteState(productCode, state);
            }
        }
        catch
        {
            // Never break Revit because of local throttling state.
        }
    }

    private static string GetStateFilePath(string productCode)
    {
        var safe = VpHubPluginFileLog.SanitizeProductCodeForFileName(productCode);
        return Path.Combine(VpHubPluginFileLog.GetLogsDirectory(), safe + ".feedback-state.json");
    }

    private static FeedbackState ReadState(string productCode)
    {
        var path = GetStateFilePath(productCode);
        if (!File.Exists(path))
        {
            return new FeedbackState();
        }
        try
        {
            var json = File.ReadAllText(path);
            // IpcJsonCodec: STJ first, Newtonsoft fallback on AppDomain STJ/Encodings conflicts
            // (Revit 2023-2024 shared AppDomain — another add-in's incompatible STJ can otherwise
            // throw MissingMethodException even for a purely-local file read like this one).
            return IpcJsonCodec.Deserialize<FeedbackState>(json) ?? new FeedbackState();
        }
        catch
        {
            return new FeedbackState();
        }
    }

    private static void WriteState(string productCode, FeedbackState state)
    {
        var path = GetStateFilePath(productCode);
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }
        File.WriteAllText(path, IpcJsonCodec.Serialize(state));
    }

    private sealed class FeedbackState
    {
        public int SuccessfulRunsSincePrompt { get; set; }
        public bool CommentGiven { get; set; }
    }
}
