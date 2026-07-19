using LicensingSystem.Contracts.Agent.Grants;

namespace LicensingSystem.Contracts.Agent;

/// <summary>English and Ukrainian explanations for agent <c>canRun</c> reason codes (Revit / support UI).</summary>
public static class CanRunReasonMessages
{
    public static string Describe(string? reason, string? locale = null) =>
        IsUk(locale) ? DescribeUk(reason) : DescribeEn(reason);

    public static string NextStep(string? reason, string? locale = null) =>
        IsUk(locale) ? NextStepUk(reason) : NextStepEn(reason);

    public static string FormatWithCode(string? reason, string? locale = null) =>
        $"{ReasonCodes.Normalize(reason)} — {Describe(reason, locale)}";

    public static string FormatWithCodeAndNextStep(string? reason, string? locale = null)
    {
        var code = ReasonCodes.Normalize(reason);
        var nextLabel = IsUk(locale) ? "Далі:" : "Next:";
        return $"{code} — {Describe(reason, locale)}{Environment.NewLine}{nextLabel} {NextStep(reason, locale)}";
    }

    /// <summary>Glossary entries for diagnostics export and support tooling (no secrets).</summary>
    public static IReadOnlyList<ReasonCodeGlossaryEntry> ExportGlossary(string? locale = null)
    {
        var codes = ReasonCodes.AllCanonical
            .Concat([ReasonCodes.OfflineExpiredAlias, ReasonCodes.SeatLimitReachedAlias])
            .Distinct(StringComparer.Ordinal);

        return codes
            .Select(c => new ReasonCodeGlossaryEntry(
                Code: c,
                CanonicalCode: ReasonCodes.Normalize(c),
                Describe: Describe(c, locale),
                NextStep: NextStep(c, locale)))
            .ToList();
    }

    private static bool IsUk(string? locale) =>
        string.Equals(locale, "uk", StringComparison.OrdinalIgnoreCase)
        || string.Equals(locale, "uk-UA", StringComparison.OrdinalIgnoreCase);

    private static string DescribeEn(string? reason)
    {
        var r = ReasonCodes.Normalize(reason);
        return r switch
        {
            ReasonCodes.Ok =>
                "The product is entitled and this machine may run it. If online, a seat lease was obtained or offline grace applies.",
            ReasonCodes.NotEntitled =>
                "No active entitlement for this product. Ask your org admin to assign the product or check subscription dates.",
            ReasonCodes.OfflineGraceExpired =>
                "This machine has been offline too long since the last successful server sync. Connect to the network and refresh entitlements in the agent.",
            ReasonCodes.LeaseExhausted =>
                "Your organization has reached the concurrent seat limit for this product. Close the app on another device or ask an admin to increase seats.",
            ReasonCodes.AgentUnavailable =>
                "The licensing agent is unavailable or you are not signed in on this PC.",
            ReasonCodes.SignatureInvalid =>
                "License verification failed (invalid publisher signature). Do not treat this session as licensed.",
            ReasonCodes.ProofMissing =>
                "Required license proof is missing from the agent response.",
            ReasonCodes.DeviceCertInvalid =>
                "The device certificate used for licensing is invalid.",
            ReasonCodes.RunProofInvalid =>
                "The run proof from the agent could not be validated.",
            ReasonCodes.ReplayDetected =>
                "A licensing request replay was detected (nonce reuse).",
            ReasonCodes.TimestampOutOfRange =>
                "The license proof timestamp is outside the allowed range.",
            _ =>
                string.IsNullOrEmpty(r)
                    ? "No reason code was provided."
                    : "Unexpected or unknown reason from the agent.",
        };
    }

    private static string DescribeUk(string? reason)
    {
        var r = ReasonCodes.Normalize(reason);
        return r switch
        {
            ReasonCodes.Ok =>
                "Продукт має entitlement; на цьому ПК його можна запускати. Онлайн — отримано seat lease або діє offline grace.",
            ReasonCodes.NotEntitled =>
                "Немає активного entitlement для цього продукту. Зверніться до адміна org для призначення продукту або перевірки терміну підписки.",
            ReasonCodes.OfflineGraceExpired =>
                "Цей ПК занадто довго був офлайн після останньої успішної синхронізації. Підключіться до мережі та оновіть entitlements в агенті.",
            ReasonCodes.LeaseExhausted =>
                "Досягнуто ліміт одночасних місць для вашої org. Закрийте плагін на іншому ПК або зверніться до адміна org щодо місць.",
            ReasonCodes.AgentUnavailable =>
                "Agent ліцензування недоступний або ви не увійшли на цьому ПК.",
            ReasonCodes.SignatureInvalid =>
                "Перевірку ліцензії не пройдено (недійсний підпис видавця). Не вважайте цю сесію ліцензованою.",
            ReasonCodes.ProofMissing =>
                "Відсутній обовʼязковий license proof у відповіді агента.",
            ReasonCodes.DeviceCertInvalid =>
                "Сертифікат пристрою для ліцензування недійсний.",
            ReasonCodes.RunProofInvalid =>
                "Run proof від агента не вдалося перевірити.",
            ReasonCodes.ReplayDetected =>
                "Виявлено повтор запиту ліцензування (повторне використання nonce).",
            ReasonCodes.TimestampOutOfRange =>
                "Мітка часу license proof поза дозволеним діапазоном.",
            _ =>
                string.IsNullOrEmpty(r)
                    ? "Код причини не надано."
                    : "Неочікувана або невідома причина від агента.",
        };
    }

    private static string NextStepEn(string? reason)
    {
        var r = ReasonCodes.Normalize(reason);
        return r switch
        {
            ReasonCodes.Ok => "No action needed.",
            ReasonCodes.NotEntitled =>
                "Ask your org admin to assign the product or verify subscription dates in the portal.",
            ReasonCodes.OfflineGraceExpired =>
                "Connect to the network, open VP-Hub Agent, and refresh entitlements (Status tab).",
            ReasonCodes.LeaseExhausted =>
                "Close the plugin on another PC or ask an org admin to review seats under Products & seats.",
            ReasonCodes.AgentUnavailable =>
                "Start VP-Hub Agent on this PC, sign in, and wait for entitlements to sync.",
            ReasonCodes.SignatureInvalid =>
                "Export diagnostics from VP-Hub Agent and contact support.",
            ReasonCodes.ProofMissing =>
                "Restart Revit and the agent; reinstall or update the product from VP-Hub if needed.",
            ReasonCodes.DeviceCertInvalid =>
                "Sign out and sign in again on this device; contact support if the problem persists.",
            ReasonCodes.RunProofInvalid =>
                "Export diagnostics and contact support.",
            ReasonCodes.ReplayDetected =>
                "Retry the command once; contact support if it happens again.",
            ReasonCodes.TimestampOutOfRange =>
                "Verify the PC clock is correct, then retry; contact support if it persists.",
            _ => "Export diagnostics from VP-Hub Agent and contact support.",
        };
    }

    private static string NextStepUk(string? reason)
    {
        var r = ReasonCodes.Normalize(reason);
        return r switch
        {
            ReasonCodes.Ok => "Дій не потрібно.",
            ReasonCodes.NotEntitled =>
                "Зверніться до адміна org для призначення продукту або перевірки терміну підписки в порталі.",
            ReasonCodes.OfflineGraceExpired =>
                "Підключіться до мережі, відкрийте VP-Hub Agent і оновіть entitlements (вкладка Status).",
            ReasonCodes.LeaseExhausted =>
                "Закрийте плагін на іншому ПК або зверніться до адміна org щодо місць у Products & seats.",
            ReasonCodes.AgentUnavailable =>
                "Запустіть VP-Hub Agent на цьому ПК, увійдіть і дочекайтеся синхронізації entitlements.",
            ReasonCodes.SignatureInvalid =>
                "Експортуйте diagnostics з VP-Hub Agent і зверніться до підтримки.",
            ReasonCodes.ProofMissing =>
                "Перезапустіть Revit і agent; за потреби перевстановіть продукт з VP-Hub.",
            ReasonCodes.DeviceCertInvalid =>
                "Вийдіть і увійдіть знову на цьому пристрої; якщо проблема лишається — зверніться до підтримки.",
            ReasonCodes.RunProofInvalid =>
                "Експортуйте diagnostics і зверніться до підтримки.",
            ReasonCodes.ReplayDetected =>
                "Повторіть команду один раз; якщо повториться — зверніться до підтримки.",
            ReasonCodes.TimestampOutOfRange =>
                "Перевірте системний час на ПК і повторіть спробу; якщо лишається — зверніться до підтримки.",
            _ => "Експортуйте diagnostics з VP-Hub Agent і зверніться до підтримки.",
        };
    }
}

public sealed record ReasonCodeGlossaryEntry(
    string Code,
    string CanonicalCode,
    string Describe,
    string NextStep);
