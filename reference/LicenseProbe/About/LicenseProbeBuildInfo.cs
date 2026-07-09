using System.Reflection;

namespace LicensingSystem.Revit.LicenseProbe.About;

internal static class LicenseProbeBuildInfo
{
    internal static string DescribeAssembly(Assembly asm)
    {
        var info = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        var version = string.IsNullOrWhiteSpace(info)
            ? (asm.GetName().Version?.ToString() ?? "—")
            : StripCommitSuffix(info!).Trim();

        var commit = asm.GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(a => string.Equals(a.Key, "GitCommit", StringComparison.OrdinalIgnoreCase))?.Value;
        if (string.IsNullOrWhiteSpace(commit) && !string.IsNullOrWhiteSpace(info))
        {
            var p = info!.IndexOf('+');
            if (p >= 0 && p + 1 < info.Length)
                commit = info.Substring(p + 1).Trim();
        }

        var date = asm.GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(a => string.Equals(a.Key, "BuildDateUtc", StringComparison.OrdinalIgnoreCase))?.Value;

        var parts = new List<string> { version };
        if (!string.IsNullOrWhiteSpace(commit))
            parts.Add(commit!.Trim());
        if (!string.IsNullOrWhiteSpace(date))
            parts.Add(date!.Trim());
        return string.Join(" · ", parts);
    }

    private static string StripCommitSuffix(string informationalVersion)
    {
        var plusIndex = informationalVersion.IndexOf('+');
        return plusIndex >= 0 ? informationalVersion.Substring(0, plusIndex) : informationalVersion;
    }
}

