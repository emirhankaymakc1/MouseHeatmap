using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MouseHeatmap.App.Services;
using MouseHeatmap.Core;
using MouseHeatmap.Core.Heatmap;
using MouseHeatmap.Core.Models;
using SkiaSharp;

namespace MouseHeatmap.App.ViewModels;

public sealed partial class HeatmapViewModel : ObservableObject
{
    private readonly AppServices _services;

    private readonly DispatcherTimer _liveTimer;
    private float[]? _liveGrid;
    private int _liveGridW, _liveGridH;
    private MonitorInfo _liveMonitor = null!;
    private SKBitmap? _lastBitmap;

    [ObservableProperty] private ObservableCollection<PeriodOption> _periods = new(PeriodOption.All);
    [ObservableProperty] private PeriodOption _selectedPeriod = PeriodOption.All[3];
    [ObservableProperty] private ObservableCollection<MonitorInfo> _monitors = new();
    [ObservableProperty] private MonitorInfo? _selectedMonitor;
    [ObservableProperty] private ObservableCollection<string> _dataTypes =
        new() { "Hareketler", "Tıklamalar", "Tümü" };
    [ObservableProperty] private string _selectedDataType = "Hareketler";
    [ObservableProperty] private bool _isRealtime;
    [ObservableProperty] private Bitmap? _heatmapImage;
    [ObservableProperty] private string _statusText = "Dönem ve monitör seçip 'Oluştur'a basın.";
    [ObservableProperty] private bool _isBusy;

    public HeatmapViewModel(AppServices services)
    {
        _services = services;
        foreach (var m in services.Tracker.Monitors)
            Monitors.Add(m);
        SelectedMonitor = Monitors.FirstOrDefault();

        _liveTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
        _liveTimer.Tick += (_, _) => RenderLiveFrame();
    }

    [RelayCommand]
    private async Task Generate()
    {
        if (SelectedMonitor is null || IsBusy) return;
        IsBusy = true;
        StatusText = "Heatmap oluşturuluyor...";

        var start = SelectedPeriod.StartTimestamp();
        var monitor = SelectedMonitor;
        var type = SelectedDataType switch
        {
            "Hareketler" => (EventType?)EventType.Move,
            "Tıklamalar" => EventType.Click,
            _ => null
        };

        try
        {
            var bitmap = await Task.Run(() =>
            {
                var events = _services.Statistics.LoadEvents(start, type: type,
                    monitorIndex: monitor.Index);
                return events.Count == 0
                    ? null
                    : _services.Heatmap.Render(events, monitor);
            });

            if (bitmap is null)
            {
                StatusText = "Seçilen dönem/monitör için veri bulunamadı.";
                return;
            }

            SwapBitmap(bitmap);
            StatusText = "Heatmap hazır.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void SavePng()
    {
        if (_lastBitmap is null) return;
        var path = HeatmapRenderer.SavePng(_lastBitmap, AppSettings.ReportsDir);
        StatusText = $"Kaydedildi: {path}";
    }

    partial void OnIsRealtimeChanged(bool value)
    {
        if (value)
            StartRealtime();
        else
            StopRealtime();
    }

    private void StartRealtime()
    {
        var monitor = SelectedMonitor ?? Monitors.FirstOrDefault();
        if (monitor is null)
        {
            IsRealtime = false;
            return;
        }

        _liveMonitor = monitor;
        _liveGridW = Math.Max(1, monitor.Width / 8);
        _liveGridH = Math.Max(1, monitor.Height / 8);
        _liveGrid = new float[_liveGridW * _liveGridH];

        _services.Tracker.EventRecorded += OnLiveEvent;
        _liveTimer.Start();
        StatusText = "Gerçek zamanlı mod açık — mouse'u hareket ettirin.";
    }

    private void StopRealtime()
    {
        _liveTimer.Stop();
        _services.Tracker.EventRecorded -= OnLiveEvent;
        _liveGrid = null;
        StatusText = "Gerçek zamanlı mod kapatıldı.";
    }

    private void OnLiveEvent(MouseEvent e)
    {
        var grid = _liveGrid;
        if (grid is null || !_liveMonitor.Contains(e.X, e.Y)) return;

        var gx = Math.Clamp((e.X - _liveMonitor.Left) / 8, 0, _liveGridW - 1);
        var gy = Math.Clamp((e.Y - _liveMonitor.Top) / 8, 0, _liveGridH - 1);
        grid[gy * _liveGridW + gx] += e.Type == EventType.Click ? 5f : 1f;
    }

    private void RenderLiveFrame()
    {
        var grid = _liveGrid;
        if (grid is null) return;

        for (var i = 0; i < grid.Length; i++)
            grid[i] *= 0.92f;

        var bitmap = _services.Heatmap.RenderLiveGrid(grid, _liveGridW, _liveGridH);
        SwapBitmap(bitmap);
    }

    private void SwapBitmap(SKBitmap bitmap)
    {
        var old = _lastBitmap;
        _lastBitmap = bitmap;
        HeatmapImage = BitmapBridge.ToAvalonia(bitmap);
        old?.Dispose();
    }
}
