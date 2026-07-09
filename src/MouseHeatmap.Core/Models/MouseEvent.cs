namespace MouseHeatmap.Core.Models;

public enum EventType
{
    Move,
    Click,
    Scroll
}

public enum MouseButton
{
    None,
    Left,
    Right,
    Middle
}

public sealed record MouseEvent
{
    public required double Timestamp { get; init; }

    public required int X { get; init; }

    public required int Y { get; init; }

    public required EventType Type { get; init; }

    public MouseButton Button { get; init; } = MouseButton.None;

    public int ScrollX { get; init; }

    public int ScrollY { get; init; }

    public int MonitorIndex { get; init; }
}

public sealed record MonitorInfo(
    int Index,
    int Left,
    int Top,
    int Width,
    int Height,
    bool IsPrimary)
{
    public bool Contains(int x, int y) =>
        x >= Left && x < Left + Width && y >= Top && y < Top + Height;

    public override string ToString() =>
        $"Monitör {Index + 1} ({Width}x{Height}){(IsPrimary ? " — Birincil" : "")}";
}
