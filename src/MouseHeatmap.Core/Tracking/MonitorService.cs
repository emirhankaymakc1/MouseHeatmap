using System.Runtime.InteropServices;
using MouseHeatmap.Core.Models;

namespace MouseHeatmap.Core.Tracking;

public static class MonitorService
{
    [StructLayout(LayoutKind.Sequential)]
    private struct Rect
    {
        public int Left, Top, Right, Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MonitorInfoNative
    {
        public uint Size;
        public Rect Monitor;
        public Rect WorkArea;
        public uint Flags;
    }

    private delegate bool MonitorEnumProc(
        IntPtr hMonitor, IntPtr hdc, ref Rect rect, IntPtr data);

    [DllImport("user32.dll")]
    private static extern bool EnumDisplayMonitors(
        IntPtr hdc, IntPtr clip, MonitorEnumProc callback, IntPtr data);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern bool GetMonitorInfo(
        IntPtr hMonitor, ref MonitorInfoNative info);

    public static List<MonitorInfo> DetectMonitors()
    {
        var monitors = new List<MonitorInfo>();

        EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero,
            (IntPtr hMonitor, IntPtr _, ref Rect _, IntPtr _) =>
            {
                var info = new MonitorInfoNative
                {
                    Size = (uint)Marshal.SizeOf<MonitorInfoNative>()
                };
                if (GetMonitorInfo(hMonitor, ref info))
                {
                    monitors.Add(new MonitorInfo(
                        Index: 0,
                        Left: info.Monitor.Left,
                        Top: info.Monitor.Top,
                        Width: info.Monitor.Right - info.Monitor.Left,
                        Height: info.Monitor.Bottom - info.Monitor.Top,
                        IsPrimary: (info.Flags & 1) != 0));
                }
                return true;
            }, IntPtr.Zero);

        return monitors
            .OrderByDescending(m => m.IsPrimary)
            .ThenBy(m => m.Left)
            .Select((m, i) => m with { Index = i })
            .ToList();
    }

    public static int FindMonitorIndex(IReadOnlyList<MonitorInfo> monitors, int x, int y)
    {
        for (var i = 0; i < monitors.Count; i++)
        {
            if (monitors[i].Contains(x, y))
                return monitors[i].Index;
        }
        return 0;
    }
}
