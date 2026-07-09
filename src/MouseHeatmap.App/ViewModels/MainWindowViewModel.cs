using CommunityToolkit.Mvvm.ComponentModel;
using MouseHeatmap.App.Services;

namespace MouseHeatmap.App.ViewModels;

public sealed partial class MainWindowViewModel : ObservableObject
{
    public DashboardViewModel Dashboard { get; }
    public LiveViewModel Live { get; }
    public HeatmapViewModel Heatmap { get; }
    public StatsViewModel Stats { get; }
    public AnalysisViewModel Analysis { get; }
    public SettingsViewModel Settings { get; }

    public string Title => "Mouse Heatmap Recorder v1.0.0";

    public MainWindowViewModel(AppServices services)
    {
        Dashboard = new DashboardViewModel(services);
        Live = new LiveViewModel(services);
        Heatmap = new HeatmapViewModel(services);
        Stats = new StatsViewModel(services);
        Analysis = new AnalysisViewModel(services);
        Settings = new SettingsViewModel(services);
    }
}
