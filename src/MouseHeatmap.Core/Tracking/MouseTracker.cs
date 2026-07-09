using MouseHeatmap.Core.Data;
using MouseHeatmap.Core.Models;

namespace MouseHeatmap.Core.Tracking;

public sealed class MouseTracker : IDisposable
{
    private readonly WindowsMouseHook _hook = new();
    private readonly SmartFilter _filter;
    private readonly EventWriter _writer;
    private IReadOnlyList<MonitorInfo> _monitors;
    private long _sessionEventCount;

    public event Action<MouseEvent>? EventRecorded;

    public bool IsRecording { get; private set; }

    public long SessionEventCount => Interlocked.Read(ref _sessionEventCount);

    public IReadOnlyList<MonitorInfo> Monitors => _monitors;

    public SmartFilter Filter => _filter;

    public MouseTracker(EventWriter writer, SmartFilter filter, EventRepository repository)
    {
        _writer = writer;
        _filter = filter;
        _monitors = MonitorService.DetectMonitors();
        repository.SaveMonitors(_monitors);
        _hook.RawEvent += OnRawEvent;
    }

    public void Start()
    {
        if (IsRecording) return;
        _monitors = MonitorService.DetectMonitors();
        _hook.Start();
        IsRecording = true;
    }

    public void Stop()
    {
        if (!IsRecording) return;
        _hook.Stop();
        IsRecording = false;
    }

    private void OnRawEvent(
        int x, int y, EventType type, MouseButton button, int scrollX, int scrollY)
    {
        if (type == EventType.Move && !_filter.ShouldRecordMove(x, y))
            return;

        var mouseEvent = new MouseEvent
        {
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0,
            X = x,
            Y = y,
            Type = type,
            Button = button,
            ScrollX = scrollX,
            ScrollY = scrollY,
            MonitorIndex = MonitorService.FindMonitorIndex(_monitors, x, y)
        };

        _writer.Enqueue(mouseEvent);
        Interlocked.Increment(ref _sessionEventCount);
        EventRecorded?.Invoke(mouseEvent);
    }

    public void Dispose()
    {
        Stop();
        _hook.Dispose();
    }
}
