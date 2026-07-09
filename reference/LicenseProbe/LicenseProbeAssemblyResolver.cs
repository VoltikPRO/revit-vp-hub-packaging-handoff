#if NETFRAMEWORK
using System.IO;
using System.Reflection;

namespace LicensingSystem.Revit.LicenseProbe;

internal static class LicenseProbeAssemblyResolver
{
    private static bool _registered;
    private static string _pluginDirectory = "";

    internal static void Register()
    {
        if (_registered)
            return;
        _pluginDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "";
        AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;
        _registered = true;
    }

    private static Assembly? OnAssemblyResolve(object? sender, ResolveEventArgs args)
    {
        var name = new AssemblyName(args.Name).Name;
        if (string.IsNullOrEmpty(name))
            return null;
        var path = Path.Combine(_pluginDirectory, name + ".dll");
        return File.Exists(path) ? Assembly.LoadFrom(path) : null;
    }
}
#endif
