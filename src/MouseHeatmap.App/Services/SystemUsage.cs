using System.Diagnostics;

namespace MouseHeatmap.App.Services;

public sealed class SystemUsage
{
    private readonly Process _process = Process.GetCurrentProcess();
    private TimeSpan _lastCpuTime;
    private DateTime _lastCheckUtc = DateTime.UtcNow;

    public (double CpuPercent, double RamMb) Sample()
    {
        _process.Refresh();

        var now = DateTime.UtcNow;
        var cpuNow = _process.TotalProcessorTime;

        var wallMs = (now - _lastCheckUtc).TotalMilliseconds;
        var cpuMs = (cpuNow - _lastCpuTime).TotalMilliseconds;

        var cpuPercent = wallMs > 0
            ? cpuMs / (wallMs * Environment.ProcessorCount) * 100
            : 0;

        _lastCpuTime = cpuNow;
        _lastCheckUtc = now;

        var ramMb = _process.WorkingSet64 / (1024.0 * 1024.0);
        return (Math.Clamp(cpuPercent, 0, 100), ramMb);
    }
}
