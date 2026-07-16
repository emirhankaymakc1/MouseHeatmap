using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
    private readonly ConcurrentQueue<MouseEvent> _queue = new();
    private readonly DispatcherTimer _timer;

    [ObservableProperty] private string _cursorPosition = "Konum: -";
    [ObservableProperty] private bool _isPaused;

    public ObservableCollection<LiveRow> Rows { get; } = new();

    public LiveViewModel(AppServices services)
    {
        _monitors = services.Tracker.Monitors.ToArray();
        services.Tracker.EventRecorded += OnEventRecorded;

        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
        _timer.Tick += OnTimerTick;
        _timer.Start();
    }

    private void OnEventRecorded(MouseEvent e)
    {
        _queue.Enqueue(e);
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        if (_queue.IsEmpty) return;

        if (IsPaused)
        {
            while (_queue.TryDequeue(out var latest))
            {
                CursorPosition = $"Konum: {latest.X}, {latest.Y}  (Monitör {latest.MonitorIndex + 1})";
            }
            return;
        }

        var newRows = new List<LiveRow>();
        MouseEvent? lastEvent = null;
        var processed = 0;

        while (processed < 100 && _queue.TryDequeue(out var ev))
        {
            lastEvent = ev;
            var time = TimeUtil.ToLocal(ev.Timestamp).ToString("HH:mm:ss");
            newRows.Add(new LiveRow(time, TypeLabel(ev.Type), $"{ev.X}, {ev.Y}", Detail(ev)));
            processed++;
        }

        if (lastEvent is not null)
        {
            CursorPosition = $"Konum: {lastEvent.X}, {lastEvent.Y}  (Monitör {lastEvent.MonitorIndex + 1})";
        }

        if (newRows.Count > 0)
        {
            for (var i = newRows.Count - 1; i >= 0; i--)
            {
                Rows.Insert(0, newRows[i]);
            }

            while (Rows.Count > MaxRows)
            {
                Rows.RemoveAt(Rows.Count - 1);
            }
        }
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
