using System.Globalization;
using System.Runtime.InteropServices;
using LicensingSystem.Agent.Ipc;
using LicensingSystem.Contracts.Agent.Ipc;

namespace LicensingSystem.Revit.Licensing;

public sealed class VpHubPluginProblemReportResult
{
    public VpHubPluginProblemReportResult(bool sent, string? reportId, string? errorCode)
    {
        Sent = sent;
        ReportId = reportId;
        ErrorCode = errorCode;
    }

    public bool Sent { get; }

    /// <summary>The number on the platform, for the person to quote to support.</summary>
    public string? ReportId { get; }

    public string? ErrorCode { get; }
}

/// <summary>
/// One click, and a problem report with the logs goes to the platform. The add-in only asks: the
/// agent packs the logs and sends them, so this makes no network call of its own (see
/// docs/architecture/revit-licensing.md "Forbidden patterns").
///
/// This is not a licensed entry point and must not be gated: somebody whose license check is
/// failing is who it is for.
///
/// From a command, call <see cref="SendInBackground"/> and return. It does not touch the Revit API
/// and does not hold the Revit thread.
/// </summary>
public static class VpHubPluginProblemReport
{
    public const string AgentUnavailable = "AGENT_UNAVAILABLE";
    public const string AgentOutdated = "AGENT_OUTDATED";
    public const string StillSending = "STILL_SENDING";

    private static readonly TimeSpan DefaultMaxWait = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan DefaultPollInterval = TimeSpan.FromSeconds(1);

    private const uint MbOk = 0x0;
    private const uint MbIconWarning = 0x30;
    private const uint MbIconInformation = 0x40;
    private const uint MbSetForeground = 0x10000;
    private const uint MbTopmost = 0x40000;

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int MessageBoxW(IntPtr hWnd, string text, string caption, uint type);

    /// <summary>
    /// Sends the report on a background thread and tells the person how it went in a plain Windows
    /// message box, because a TaskDialog can only be shown on the Revit thread. Never throws.
    /// </summary>
    /// <param name="hostVersion">For example <c>"Revit 2026"</c>.</param>
    public static void SendInBackground(
        string productCode,
        string? pluginVersion,
        string? hostVersion = null,
        string? description = null)
    {
        var culture = CultureInfo.CurrentUICulture;
        _ = Task.Run(async () =>
        {
            try
            {
                var result = await SendAsync(productCode, pluginVersion, hostVersion, description).ConfigureAwait(false);
                VpHubPluginFileLog.Write(
                    productCode,
                    $"Problem report: sent={result.Sent} reportId={result.ReportId ?? "-"} error={result.ErrorCode ?? "-"}");
                MessageBoxW(
                    IntPtr.Zero,
                    DescribeResult(result, culture),
                    Ui(culture, "Report a problem", "Повідомити про проблему"),
                    MbOk | MbSetForeground | MbTopmost | (result.Sent ? MbIconInformation : MbIconWarning));
            }
            catch
            {
                // Never break Revit because of a problem report.
            }
        });
    }

    /// <summary>Never throws. Takes as long as the upload does, so do not await it on the Revit thread.</summary>
    public static Task<VpHubPluginProblemReportResult> SendAsync(
        string productCode,
        string? pluginVersion,
        string? hostVersion = null,
        string? description = null,
        CancellationToken ct = default) =>
        SendAsync(new NamedPipeAgentClient(), productCode, pluginVersion, hostVersion, description, DefaultMaxWait, DefaultPollInterval, ct);

    internal static async Task<VpHubPluginProblemReportResult> SendAsync(
        NamedPipeAgentClient client,
        string productCode,
        string? pluginVersion,
        string? hostVersion,
        string? description,
        TimeSpan maxWait,
        TimeSpan pollInterval,
        CancellationToken ct)
    {
        PluginReportStatus status;
        try
        {
            status = await client.SubmitPluginReportAsync(productCode, pluginVersion, hostVersion, description, ct).ConfigureAwait(false);
        }
        catch (Exception)
        {
            return new VpHubPluginProblemReportResult(false, null, AgentUnavailable);
        }

        // An agent from before this existed answers { error: "UNKNOWN_TYPE" }, which reads as all empty.
        if (!status.Ok && status.ErrorCode is null)
        {
            return new VpHubPluginProblemReportResult(false, null, AgentOutdated);
        }

        var deadline = DateTimeOffset.UtcNow + maxWait;
        while (status.Ok && status.State == PluginReportStates.Sending)
        {
            if (DateTimeOffset.UtcNow >= deadline)
            {
                return new VpHubPluginProblemReportResult(false, null, StillSending);
            }

            try
            {
                await Task.Delay(pollInterval, ct).ConfigureAwait(false);
                status = await client.GetPluginReportStatusAsync(status.RequestId!, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                return new VpHubPluginProblemReportResult(false, null, StillSending);
            }
            catch (Exception)
            {
                // The pipe answers one request at a time, so a missed answer says nothing about the
                // report. Keep asking until the deadline.
            }
        }

        return status.Ok
            ? new VpHubPluginProblemReportResult(true, status.ReportId, null)
            : new VpHubPluginProblemReportResult(false, null, status.ErrorCode);
    }

    /// <summary>What to tell the person, in their language.</summary>
    public static string DescribeResult(VpHubPluginProblemReportResult result, CultureInfo? culture = null)
    {
        if (result.Sent)
        {
            var sent = Ui(
                culture,
                "Your report and logs were sent to VP-Hub support.",
                "Звіт і журнали надіслано до підтримки VP-Hub.");
            return string.IsNullOrEmpty(result.ReportId)
                ? sent
                : sent + Environment.NewLine + Ui(culture, "Report number: ", "Номер звіту: ") + result.ReportId;
        }

        return result.ErrorCode switch
        {
            AgentUnavailable => Ui(
                culture,
                "The report was not sent: the VP-Hub agent is not running. Start VP-Hub and try again.",
                "Звіт не надіслано: агент VP-Hub не запущено. Запустіть VP-Hub і спробуйте ще раз."),
            AgentOutdated => Ui(
                culture,
                "The report was not sent: this version of the VP-Hub agent cannot send it. Update VP-Hub in Products and try again.",
                "Звіт не надіслано: ця версія агента VP-Hub не вміє його надсилати. Оновіть VP-Hub у розділі Products і спробуйте ще раз."),
            StillSending => Ui(
                culture,
                "The report is still being sent. There is no need to send it again.",
                "Звіт ще надсилається. Надсилати його повторно не потрібно."),
            "RATE_LIMITED" => Ui(
                culture,
                "The report was not sent: too many reports in the last hour. Try again later.",
                "Звіт не надіслано: забагато звітів за останню годину. Спробуйте пізніше."),
            "TOO_LARGE" => Ui(
                culture,
                "The report was not sent: the logs are too large. Use Report a problem in VP-Hub → About.",
                "Звіт не надіслано: журнали завеликі. Скористайтеся «Повідомити про проблему» у VP-Hub → About."),
            _ => Ui(
                culture,
                "The report was not sent. Check your internet connection and try again.",
                "Звіт не надіслано. Перевірте підключення до інтернету і спробуйте ще раз."),
        };
    }

    private static string Ui(CultureInfo? culture, string en, string uk) =>
        RevitLicenseCanRunReport.IsUkUiCulture(culture) ? uk : en;
}
