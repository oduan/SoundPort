using System.Runtime.InteropServices;
using System.ComponentModel;
using SoundPort.Interop;
using SoundPort.Models;

namespace SoundPort.Services;

public sealed class TrayIconService : IDisposable
{
    private const uint IconId = 1;
    private const uint CallbackMessage = NativeMethods.WmApp + 1;
    private const uint MenuOpen = 1001;
    private const uint MenuConnect = 1002;
    private const uint MenuDisconnect = 1003;
    private const uint MenuExit = 1004;

    private readonly AudioReceiverService _receiver;
    private readonly Action _showWindow;
    private readonly Func<Task> _connect;
    private readonly Func<Task> _disconnect;
    private readonly Func<Task> _exit;
    private readonly TrayMessageWindow _messageWindow;
    private readonly uint _taskbarCreatedMessage;
    private readonly string _iconPath;
    private IntPtr _iconHandle;
    private bool _disposed;

    public TrayIconService(
        AudioReceiverService receiver,
        Action showWindow,
        Func<Task> connect,
        Func<Task> disconnect,
        Func<Task> exit)
    {
        _receiver = receiver;
        _showWindow = showWindow;
        _connect = connect;
        _disconnect = disconnect;
        _exit = exit;
        _iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "AppIcon.ico");

        _taskbarCreatedMessage = NativeMethods.RegisterWindowMessage("TaskbarCreated");
        _messageWindow = new TrayMessageWindow(HandleWindowMessage);
        _receiver.StatusChanged += Receiver_StatusChanged;
        AddIcon();
    }

    public void ShowInfo(string title, string message)
    {
        if (_disposed)
        {
            return;
        }

        var data = CreateNotifyIconData();
        data.Flags = NativeMethods.NifInfo;
        data.InfoTitle = title;
        data.Info = message;
        data.InfoFlags = NativeMethods.NiifInfo;
        NativeMethods.ShellNotifyIcon(NativeMethods.NimModify, ref data);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _receiver.StatusChanged -= Receiver_StatusChanged;

        var data = CreateNotifyIconData();
        NativeMethods.ShellNotifyIcon(NativeMethods.NimDelete, ref data);
        _messageWindow.Dispose();

        if (_iconHandle != IntPtr.Zero)
        {
            NativeMethods.DestroyIcon(_iconHandle);
            _iconHandle = IntPtr.Zero;
        }
    }

    private void AddIcon()
    {
        if (_iconHandle == IntPtr.Zero)
        {
            _iconHandle = NativeMethods.LoadImage(
                IntPtr.Zero,
                _iconPath,
                NativeMethods.ImageIcon,
                0,
                0,
                NativeMethods.LrLoadFromFile | NativeMethods.LrDefaultSize);

            if (_iconHandle == IntPtr.Zero)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "无法加载 SoundPort 托盘图标。");
            }
        }

        var data = CreateNotifyIconData();
        data.Flags = NativeMethods.NifMessage | NativeMethods.NifIcon | NativeMethods.NifTip;
        data.CallbackMessage = CallbackMessage;
        data.Icon = _iconHandle;
        data.Tip = BuildToolTip();
        if (!NativeMethods.ShellNotifyIcon(NativeMethods.NimAdd, ref data))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "无法创建 SoundPort 托盘图标。");
        }
    }

    private void UpdateIcon()
    {
        if (_disposed)
        {
            return;
        }

        var data = CreateNotifyIconData();
        data.Flags = NativeMethods.NifTip;
        data.Tip = BuildToolTip();
        NativeMethods.ShellNotifyIcon(NativeMethods.NimModify, ref data);
    }

    private string BuildToolTip()
    {
        return _receiver.State switch
        {
            ReceiverState.Connected => $"SoundPort - 已连接 {_receiver.ConnectedDeviceName ?? "手机"}",
            ReceiverState.Connecting or ReceiverState.Enabling => "SoundPort - 正在连接",
            ReceiverState.Faulted => "SoundPort - 连接失败",
            _ => "SoundPort - 未连接"
        };
    }

    private IntPtr HandleWindowMessage(IntPtr hwnd, uint message, IntPtr wParam, IntPtr lParam)
    {
        if (message == CallbackMessage)
        {
            var notification = unchecked((uint)lParam.ToInt64());
            if (notification == NativeMethods.WmLButtonDblClk)
            {
                _showWindow();
            }
            else if (notification is NativeMethods.WmRButtonUp or NativeMethods.WmContextMenu)
            {
                ShowContextMenu();
            }

            return IntPtr.Zero;
        }

        if (message == _taskbarCreatedMessage)
        {
            AddIcon();
            return IntPtr.Zero;
        }

        return NativeMethods.DefWindowProc(hwnd, message, wParam, lParam);
    }

    private void ShowContextMenu()
    {
        var menu = NativeMethods.CreatePopupMenu();
        if (menu == IntPtr.Zero)
        {
            return;
        }

        try
        {
            var status = _receiver.State == ReceiverState.Connected
                ? $"已连接：{_receiver.ConnectedDeviceName ?? "手机"}"
                : "状态：未连接";

            NativeMethods.AppendMenu(menu, NativeMethods.MfString | NativeMethods.MfDisabled, 0, status);
            NativeMethods.AppendMenu(menu, NativeMethods.MfSeparator, 0, null);
            NativeMethods.AppendMenu(menu, NativeMethods.MfString, MenuOpen, "打开 SoundPort");

            var canConnect = _receiver.PreferredDevice is not null &&
                             _receiver.State is not ReceiverState.Connected and
                                 not ReceiverState.Connecting and
                                 not ReceiverState.Enabling;
            NativeMethods.AppendMenu(
                menu,
                NativeMethods.MfString | (canConnect ? 0u : NativeMethods.MfDisabled),
                MenuConnect,
                "连接");

            var canDisconnect = _receiver.State is ReceiverState.Connected or
                ReceiverState.Connecting or ReceiverState.Enabling;
            NativeMethods.AppendMenu(
                menu,
                NativeMethods.MfString | (canDisconnect ? 0u : NativeMethods.MfDisabled),
                MenuDisconnect,
                "断开连接");

            NativeMethods.AppendMenu(menu, NativeMethods.MfSeparator, 0, null);
            NativeMethods.AppendMenu(menu, NativeMethods.MfString, MenuExit, "退出");

            NativeMethods.GetCursorPos(out var point);
            NativeMethods.SetForegroundWindow(_messageWindow.Handle);
            var command = NativeMethods.TrackPopupMenu(
                menu,
                NativeMethods.TpmRightButton | NativeMethods.TpmReturnCmd,
                point.X,
                point.Y,
                0,
                _messageWindow.Handle,
                IntPtr.Zero);

            _ = ExecuteCommandAsync(command);
            NativeMethods.PostMessage(_messageWindow.Handle, NativeMethods.WmNull, IntPtr.Zero, IntPtr.Zero);
        }
        finally
        {
            NativeMethods.DestroyMenu(menu);
        }
    }

    private async Task ExecuteCommandAsync(uint command)
    {
        switch (command)
        {
            case MenuOpen:
                _showWindow();
                break;
            case MenuConnect:
                await _connect();
                break;
            case MenuDisconnect:
                await _disconnect();
                break;
            case MenuExit:
                await _exit();
                break;
        }
    }

    private void Receiver_StatusChanged(object? sender, EventArgs e)
    {
        UpdateIcon();
    }

    private NotifyIconData CreateNotifyIconData()
    {
        return new NotifyIconData
        {
            Size = (uint)Marshal.SizeOf<NotifyIconData>(),
            WindowHandle = _messageWindow.Handle,
            Id = IconId,
            Tip = string.Empty,
            Info = string.Empty,
            InfoTitle = string.Empty
        };
    }
}
