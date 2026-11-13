using LicensingSystem.Revit.Licensing;

namespace LicensingSystem.Revit.LicenseProbe;

/// <summary>Culture-aware UI strings for the License Probe template (EN / UK).</summary>
internal static class LicenseProbeUi
{
    public static string DialogTitle => Ui(
        "License Probe — Licensing",
        "License Probe — Ліцензування");

    public static string LicenseStatusButtonLabel => Ui(
        "License" + Environment.NewLine + "status",
        "Статус" + Environment.NewLine + "ліцензії");

    public static string LicenseStatusToolTip => Ui(
        "Show whether a verified license is available (local agent IPC).",
        "Показати, чи доступна перевірена ліцензія (локальний agent IPC).");

    public static string LicenseStatusLongDescription => Ui(
        "Calls the local LicensingSystem agent once and shows a short message. Template for publisher-signed canRun checks.",
        "Один раз викликає локальний LicensingSystem agent і показує коротке повідомлення. Зразок для publisher-signed canRun перевірок.");

    public static string AboutButtonLabel => Ui("About", "Про");

    public static string AboutToolTip => Ui(
        "About VP-Hub / LicensingSystem License Probe.",
        "Про VP-Hub / LicensingSystem License Probe.");

    public static string AboutLongDescription => Ui(
        "Shows brand, build/version, and agent connectivity summary (no secrets).",
        "Показує бренд, збірку/версію та зведення підключення agent (без секретів).");

    public static string ReportProblemButtonLabel => Ui(
        "Report a" + Environment.NewLine + "problem",
        "Повідомити" + Environment.NewLine + "про проблему");

    public static string ReportProblemToolTip => Ui(
        "Send a problem report with logs to VP-Hub support in one click.",
        "Надіслати звіт про проблему з журналами до підтримки VP-Hub одним кліком.");

    public static string ReportProblemLongDescription => Ui(
        "Sends this computer's VP-Hub logs and plugin logs, its name, and your account. No passwords or tokens.",
        "Надсилає журнали VP-Hub і плагінів із цього комп'ютера, його назву та ваш обліковий запис. Без паролів і токенів.");

    public static string RibbonRegistrationFailed(string message) => Ui(
        $"Ribbon registration failed: {message}",
        $"Не вдалося зареєструвати стрічку: {message}");

    private static string Ui(string en, string uk) =>
        RevitLicenseCanRunReport.IsUkUiCulture() ? uk : en;
}
