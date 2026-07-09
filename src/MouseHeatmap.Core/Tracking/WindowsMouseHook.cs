using System.Runtime.InteropServices;
using MouseHeatmap.Core.Models;

namespace MouseHeatmap.Core.Tracking;

public sealed class WindowsMouseHook : IDisposable
{
    private const int WhMouseLl = 14;
    private const int WmMouseMove = 0x0200;
    private const int WmLButtonDown = 0x0201;
    private const int WmRButtonDown = 0x0204;
    private const int WmMButtonDown = 0x0207;
    private const int WmMouseWheel = 0x020A;
    private const int WmMouseHWheel = 0x020E;
    private const int WmQuit = 0x0012;

    [StructLayout(LayoutKind.Sequential)]
    private struct Point { public int X, Y; }

    [StructLayout(LayoutKind.Sequential)]
    private struct MsLlHookStruct
    {
        public Point Pt;
        public uint MouseData;
        public uint Flags;
        public uint Time;
        public UIntPtr ExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Msg
    {
        public IntPtr Hwnd;
        public uint Message;
        public UIntPtr WParam;
        public IntPtr LParam;
        public uint Time;
        public Point Pt;
    }

    private delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(
        int idHook, HookProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll")]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(
        IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern int GetMessage(out Msg msg, IntPtr hwnd, uint min, uint max);

    [DllImport("user32.dll")]
    private static extern bool PostThreadMessage(
        uint threadId, uint msg, UIntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr GetModuleHandle(string? moduleName);

    private readonly HookProc _hookProc;
    private Thread? _hookThread;
    private IntPtr _hookHandle;
    private uint _hookThreadId;
    private volatile bool _running;

    public event Action<int, int, EventType, MouseButton, int, int>? RawEvent;

    public bool IsRunning => _running;

    public WindowsMouseHook() => _hookProc = HookCallback;

    public void Start()
    {
        if (_running) return;
        _running = true;

        _hookThread = new Thread(MessagePump)
        {
            Name = "MouseHookPump",
            IsBackground = true
        };
        _hookThread.Start();
    }

    public void Stop()
    {
        if (!_running) return;
        _running = false;

        if (_hookThreadId != 0)
            PostThreadMessage(_hookThreadId, WmQuit, UIntPtr.Zero, IntPtr.Zero);
        _hookThread?.Join(TimeSpan.FromSeconds(3));
        _hookThread = null;
    }

    private void MessagePump()
    {
        _hookThreadId = GetCurrentThreadId();
        _hookHandle = SetWindowsHookEx(
            WhMouseLl, _hookProc, GetModuleHandle(null), 0);

        if (_hookHandle == IntPtr.Zero)
        {
            _running = false;
            return;
        }

        while (GetMessage(out _, IntPtr.Zero, 0, 0) > 0)
        {
        }

        UnhookWindowsHookEx(_hookHandle);
        _hookHandle = IntPtr.Zero;
        _hookThreadId = 0;
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            var data = Marshal.PtrToStructure<MsLlHookStruct>(lParam);
            var message = wParam.ToInt32();

            switch (message)
            {
                case WmMouseMove:
                    RawEvent?.Invoke(data.Pt.X, data.Pt.Y, EventType.Move,
                        MouseButton.None, 0, 0);
                    break;

                case WmLButtonDown:
                    RawEvent?.Invoke(data.Pt.X, data.Pt.Y, EventType.Click,
                        MouseButton.Left, 0, 0);
                    break;

                case WmRButtonDown:
                    RawEvent?.Invoke(data.Pt.X, data.Pt.Y, EventType.Click,
                        MouseButton.Right, 0, 0);
                    break;

                case WmMButtonDown:
                    RawEvent?.Invoke(data.Pt.X, data.Pt.Y, EventType.Click,
                        MouseButton.Middle, 0, 0);
                    break;

                case WmMouseWheel:
                {
                    var delta = (short)(data.MouseData >> 16) / 120;
                    RawEvent?.Invoke(data.Pt.X, data.Pt.Y, EventType.Scroll,
                        MouseButton.None, 0, delta);
                    break;
                }

                case WmMouseHWheel:
                {
                    var delta = (short)(data.MouseData >> 16) / 120;
                    RawEvent?.Invoke(data.Pt.X, data.Pt.Y, EventType.Scroll,
                        MouseButton.None, delta, 0);
                    break;
                }
            }
        }

        return CallNextHookEx(_hookHandle, nCode, wParam, lParam);
    }

    public void Dispose() => Stop();
}
