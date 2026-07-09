using System.Collections.ObjectModel;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using MouseHeatmap.App.Services;
using MouseHeatmap.Core;
using MouseHeatmap.Core.Models;

namespace MouseHeatmap.App.ViewModels;

public sealed record LiveRow(string Time, string Type, string Position, string Detail);

public sealed partial class LiveViewModel : ObservableObject
{
    private const int MaxRows = 200;

    private readonly MonitorInfo[] _monitors;

    [ObservableProperty] private string _cursorPosition = "Konum: -";
    [ObservableProperty] private bool _isPaused;

    public ObservableCollection<LiveRow> Rows { get; } = new();

    public LiveViewModel(AppServices services)
    {
        _monitors = services.Tracker.Monitors.ToArray();
        services.Tracker.EventRecorded += OnEventRecorded;
    }

    private void OnEventRecorded(MouseEvent e)
    {
        Dispatcher.UIThread.Post(() => AddEvent(e), DispatcherPriority.Background);
    }

    private void AddEvent(MouseEvent e)
    {
        CursorPosition = $"Konum: {e.X}, {e.Y}  (Monitör {e.MonitorIndex + 1})";
        if (IsPaused) return;

        if (Rows.Count >= MaxRows)
            Rows.RemoveAt(Rows.Count - 1);

        var time = TimeUtil.ToLocal(e.Timestamp).ToString("HH:mm:ss");

        Rows.Insert(0, new LiveRow(time, TypeLabel(e.Type),
            $"{e.X}, {e.Y}", Detail(e)));
    }

    private static string TypeLabel(EventType type) => type switch
    {
        EventType.Move => "Hareket",
        EventType.Click => "Tıklama",
        EventType.Scroll => "Scroll",
        _ => "?"
    };

    private static string Detail(MouseEvent e) => e.Type switch
    {
        EventType.Click => e.Button switch
        {
            MouseButton.Left => "Sol tık",
            MouseButton.Right => "Sağ tık",
            MouseButton.Middle => "Orta tık",
            _ => "-"
        },
        EventType.Scroll => e.ScrollY > 0 ? "Scroll ▲ yukarı"
            : e.ScrollY < 0 ? "Scroll ▼ aşağı" : "Scroll ↔ yatay",
        _ => "-"
    };
}
