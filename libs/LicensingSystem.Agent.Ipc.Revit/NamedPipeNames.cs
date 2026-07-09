using System.Security.Principal;

namespace LicensingSystem.Agent.Ipc;

public static class NamedPipeNames
{
    public const string DefaultBaseName = "LicensingSystem.Agent";

    /// <summary>
    /// Optional override for debugging / service hosting / backward compatibility.
    /// If set, its value is used verbatim as the pipe name.
    /// </summary>
    public const string PipeNameOverrideEnvVar = "LICENSING_AGENT_PIPE_NAME";

    public static string ForCurrentUser()
    {
        var overrideName = Environment.GetEnvironmentVariable(PipeNameOverrideEnvVar);
        if (!string.IsNullOrWhiteSpace(overrideName))
            return overrideName.Trim();

        var sid = WindowsIdentity.GetCurrent().User?.Value;
        if (string.IsNullOrWhiteSpace(sid))
            return DefaultBaseName;

        return $"{DefaultBaseName}.{sid}";
    }
}
