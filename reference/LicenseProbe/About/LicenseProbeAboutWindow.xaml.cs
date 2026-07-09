using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace LicensingSystem.Revit.LicenseProbe.About;

public partial class LicenseProbeAboutWindow : Window
{
    public LicenseProbeAboutWindow(LicenseProbeAboutModel model)
    {
        InitializeComponent();

        // Theme selection: keep structure identical; for now default to Light to match agent default.
        // Can be extended later to respect Revit/OS theme.
        Resources.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri("AgentUiStyle/Controls.xaml", UriKind.Relative)
        });
        Resources.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri("AgentUiStyle/Theme.Light.xaml", UriKind.Relative)
        });

        DataContext = new ViewModel(model);
    }

    private void OnCloseClick(object sender, RoutedEventArgs e) => Close();

    internal sealed class ViewModel : INotifyPropertyChanged
    {
        public string BrandTitle { get; }
        public string ProductDisplayName { get; }
        public string Description { get; }
        public string RevitVersion { get; }
        public string ProductCode { get; }
        public string AddinAssemblyPath { get; }
        public string AddinBuild { get; }
        public string CorrelationId { get; }

        public string AgentStatusSummary { get; }
        public string AgentStatusDetail { get; }

        public ViewModel(LicenseProbeAboutModel model)
        {
            BrandTitle = model.BrandTitle;
            ProductDisplayName = model.ProductDisplayName;
            Description = model.Description;
            RevitVersion = model.RevitVersion;
            ProductCode = model.ProductCode;
            AddinAssemblyPath = model.AddinAssemblyPath;
            AddinBuild = model.AddinBuild;
            CorrelationId = model.CorrelationId;

            if (model.AgentStatus is null)
            {
                AgentStatusSummary = "Agent: unavailable";
                AgentStatusDetail =
                    $"GetStatus duration: {model.AgentStatusDurationMs} ms\n" +
                    $"Error: {model.AgentStatusError ?? "Unknown"}";
            }
            else
            {
                AgentStatusSummary = BuildSummary(model.AgentStatus);
                AgentStatusDetail = BuildDetail(model.AgentStatus, model.AgentStatusDurationMs);
            }
        }

        private static string BuildSummary(LicenseProbeAgentStatusDto s)
        {
            var online = s.IsOnline ? "online" : "offline";
            return $"Agent: {s.State ?? "(unknown)"} ({online})";
        }

        private static string BuildDetail(LicenseProbeAgentStatusDto s, int durationMs)
        {
            var lastOnline = s.LastOnlineAt?.ToString("O") ?? "(null)";
            var lastAttempt = s.LastApiAttemptUtc?.ToString("O") ?? "(null)";
            var http = s.LastApiHttpStatus?.ToString() ?? "(null)";
            var err = string.IsNullOrWhiteSpace(s.LastApiErrorCode) ? "(null)" : s.LastApiErrorCode;
            var summary = string.IsNullOrWhiteSpace(s.ApiConnectivitySummary) ? "(null)" : s.ApiConnectivitySummary;

            return
                $"GetStatus duration: {durationMs} ms\n" +
                $"LastOnlineAt: {lastOnline}\n" +
                $"OfflineRemainingMinutes: {s.OfflineRemainingMinutes}\n" +
                $"LastApiAttemptUtc: {lastAttempt}\n" +
                $"LastApiHttpStatus: {http}\n" +
                $"LastApiErrorCode: {err}\n" +
                $"ApiConnectivitySummary: {summary}";
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

