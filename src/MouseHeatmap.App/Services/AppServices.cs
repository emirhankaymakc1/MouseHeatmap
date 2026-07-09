using MouseHeatmap.Core;
using MouseHeatmap.Core.Analytics;
using MouseHeatmap.Core.Data;
using MouseHeatmap.Core.Heatmap;
using MouseHeatmap.Core.Tracking;

namespace MouseHeatmap.App.Services;

public sealed class AppServices : IDisposable
{
    public AppSettings Settings { get; }
    public Database Database { get; }
    public EventRepository Repository { get; }
    public EventWriter Writer { get; }
    public SmartFilter Filter { get; }
    public MouseTracker Tracker { get; }
    public StatisticsService Statistics { get; }
    public AnalysisService Analysis { get; }
    public ReportExporter Exporter { get; }
    public HeatmapRenderer Heatmap { get; }

    public AppServices()
    {
        Settings = AppSettings.Load();

        Database = new Database(AppSettings.DatabasePath);
        Repository = new EventRepository(Database);
        Writer = new EventWriter(Repository);

        Filter = new SmartFilter(Settings.MinDistancePx, Settings.MinIntervalMs);
        Tracker = new MouseTracker(Writer, Filter, Repository);

        Statistics = new StatisticsService(Repository);
        Analysis = new AnalysisService(Statistics);
        Exporter = new ReportExporter(Statistics, AppSettings.ReportsDir);
        Heatmap = new HeatmapRenderer();
    }

    public void Start()
    {
        if (Settings.StartRecordingOnLaunch)
            Tracker.Start();
    }

    private bool _disposed;

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        Tracker.Dispose();
        Writer.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }
}
