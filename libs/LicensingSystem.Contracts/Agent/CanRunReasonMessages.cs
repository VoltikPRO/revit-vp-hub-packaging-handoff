using LicensingSystem.Contracts.Agent.Grants;

namespace LicensingSystem.Contracts.Agent;

/// <summary>English explanations for agent <c>canRun</c> reason codes (Revit / support UI).</summary>
public static class CanRunReasonMessages
{
    public static string Describe(string? reason)
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
            _ =>
                "Unexpected or unknown reason from the agent.",
        };
    }

    /// <summary>Returns a line like <c>OK — …</c> for logs and UI.</summary>
    public static string FormatWithCode(string? reason) =>
        $"{ReasonCodes.Normalize(reason)} — {Describe(reason)}";
}
