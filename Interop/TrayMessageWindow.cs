using System.Runtime.InteropServices;

namespace SoundPort.Interop;

internal sealed class TrayMessageWindow : IDisposable
{
    private readonly string _className = $"SoundPort.Tray.{Guid.NewGuid():N}";
    private readonly NativeMethods.WindowProc _windowProc;
    private readonly Func<IntPtr, uint, IntPtr, IntPtr, IntPtr> _messageHandler;
    private readonly IntPtr _instance;
    private bool _disposed;

    internal TrayMessageWindow(Func<IntPtr, uint, IntPtr, IntPtr, IntPtr> messageHandler)
    {
        _messageHandler = messageHandler;
        _windowProc = WindowProcedure;
        _instance = NativeMethods.GetModuleHandle(null);
        var windowClass = new NativeMethods.WindowClass
        {
            Size = (uint)Marshal.SizeOf<NativeMethods.WindowClass>(),
            WindowProcedure = _windowProc,
            Instance = _instance,
            ClassName = _className
        };

        if (NativeMethods.RegisterClass(ref windowClass) == 0)
        {
            throw new InvalidOperationException($"无法注册托盘消息窗口：{Marshal.GetLastWin32Error()}");
        }

        Handle = NativeMethods.CreateWindowEx(
            0,
            _className,
            "SoundPort Tray",
            0,
            0,
            0,
            0,
            0,
            IntPtr.Zero,
            IntPtr.Zero,
            _instance,
            IntPtr.Zero);

        if (Handle == IntPtr.Zero)
        {
            var error = Marshal.GetLastWin32Error();
            NativeMethods.UnregisterClass(_className, _instance);
            throw new InvalidOperationException($"无法创建托盘消息窗口：{error}");
        }
    }

    internal IntPtr Handle { get; }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        NativeMethods.DestroyWindow(Handle);
        NativeMethods.UnregisterClass(_className, _instance);
    }

    private IntPtr WindowProcedure(IntPtr hwnd, uint message, IntPtr wParam, IntPtr lParam)
    {
        if (!_disposed)
        {
            return _messageHandler(hwnd, message, wParam, lParam);
        }

        return NativeMethods.DefWindowProc(hwnd, message, wParam, lParam);
    }
}
