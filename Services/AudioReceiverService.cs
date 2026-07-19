using System.Collections.ObjectModel;
using Microsoft.UI.Dispatching;
using SoundPort.Models;
using Windows.Devices.Enumeration;
using Windows.Media.Audio;

namespace SoundPort.Services;

public sealed class AudioReceiverService : IAsyncDisposable
{
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly SettingsService _settings;
    private readonly SemaphoreSlim _connectionLock = new(1, 1);
    private DeviceWatcher? _watcher;
    private AudioPlaybackConnection? _connection;
    private string? _connectedDeviceId;
    private bool _isDisposed;

    public AudioReceiverService(DispatcherQueue dispatcherQueue, SettingsService settings)
    {
        _dispatcherQueue = dispatcherQueue;
        _settings = settings;
    }

    public ObservableCollection<RemoteAudioDevice> Devices { get; } = [];

    public ReceiverState State { get; private set; } = ReceiverState.Idle;

    public string StatusMessage { get; private set; } = "正在准备蓝牙音频接收器。";

    public RemoteAudioDevice? PreferredDevice =>
        Devices.FirstOrDefault(device => device.Id == _settings.PreferredDeviceId)
        ?? Devices.FirstOrDefault();

    public string? ConnectedDeviceName =>
        Devices.FirstOrDefault(device => device.Id == _connectedDeviceId)?.Name;

    public event EventHandler? StatusChanged;

    public void StartWatching()
    {
        if (_watcher is not null || _isDisposed)
        {
            return;
        }

        SetStatus(ReceiverState.Searching, "正在搜索支持蓝牙音频播放的已配对设备。");
        _watcher = DeviceInformation.CreateWatcher(AudioPlaybackConnection.GetDeviceSelector());
        _watcher.Added += Watcher_Added;
        _watcher.Updated += Watcher_Updated;
        _watcher.Removed += Watcher_Removed;
        _watcher.EnumerationCompleted += Watcher_EnumerationCompleted;
        _watcher.Stopped += Watcher_Stopped;
        _watcher.Start();
    }

    public void RestartWatching()
    {
        StopWatching();
        Devices.Clear();
        StartWatching();
    }

    public void SetPreferredDevice(string deviceId)
    {
        _settings.PreferredDeviceId = deviceId;
        StatusChanged?.Invoke(this, EventArgs.Empty);
    }

    public Task ConnectPreferredAsync()
    {
        var device = PreferredDevice;
        if (device is null)
        {
            SetStatus(ReceiverState.Faulted, "没有可连接的设备。请先在 Windows 设置中配对手机。");
            return Task.CompletedTask;
        }

        return ConnectAsync(device);
    }

    public async Task ConnectAsync(RemoteAudioDevice device)
    {
        await _connectionLock.WaitAsync();
        try
        {
            if (_isDisposed)
            {
                return;
            }

            if (_connectedDeviceId == device.Id && State == ReceiverState.Connected)
            {
                return;
            }

            ReleaseConnection();
            SetPreferredDevice(device.Id);
            SetStatus(ReceiverState.Enabling, $"正在允许 {device.Name} 向电脑发送音频。");

            var connection = AudioPlaybackConnection.TryCreateFromId(device.Id);
            if (connection is null)
            {
                SetStatus(ReceiverState.Faulted, $"{device.Name} 当前不提供可用的蓝牙音频连接。");
                return;
            }

            _connection = connection;
            _connectedDeviceId = device.Id;
            connection.StateChanged += Connection_StateChanged;

            await connection.StartAsync();
            SetStatus(ReceiverState.Connecting, $"正在连接 {device.Name}。");

            var result = await connection.OpenAsync();
            if (result.Status == AudioPlaybackConnectionOpenResultStatus.Success)
            {
                SetStatus(ReceiverState.Connected, $"已连接 {device.Name}。现在可在手机上播放音乐。");
                return;
            }

            var error = result.ExtendedError?.Message;
            var details = string.IsNullOrWhiteSpace(error) ? result.Status.ToString() : error;
            ReleaseConnection();
            SetStatus(ReceiverState.Faulted, $"无法连接 {device.Name}：{details}");
        }
        catch (Exception ex)
        {
            ReleaseConnection();
            SetStatus(ReceiverState.Faulted, $"蓝牙连接失败：{ex.Message}");
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    public async Task DisconnectAsync()
    {
        await _connectionLock.WaitAsync();
        try
        {
            ReleaseConnection();
            SetStatus(ReceiverState.Idle, Devices.Count == 0
                ? "未发现设备。请确认手机已配对且蓝牙已开启。"
                : "已断开。请选择手机并重新连接。");
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        StopWatching();

        await _connectionLock.WaitAsync();
        try
        {
            ReleaseConnection();
        }
        finally
        {
            _connectionLock.Release();
            _connectionLock.Dispose();
        }
    }

    private void StopWatching()
    {
        if (_watcher is null)
        {
            return;
        }

        _watcher.Added -= Watcher_Added;
        _watcher.Updated -= Watcher_Updated;
        _watcher.Removed -= Watcher_Removed;
        _watcher.EnumerationCompleted -= Watcher_EnumerationCompleted;
        _watcher.Stopped -= Watcher_Stopped;

        if (_watcher.Status is DeviceWatcherStatus.Started or DeviceWatcherStatus.EnumerationCompleted)
        {
            _watcher.Stop();
        }

        _watcher = null;
    }

    private void ReleaseConnection()
    {
        if (_connection is not null)
        {
            _connection.StateChanged -= Connection_StateChanged;
            _connection.Dispose();
            _connection = null;
        }

        _connectedDeviceId = null;
    }

    private void Watcher_Added(DeviceWatcher sender, DeviceInformation args)
    {
        Enqueue(() =>
        {
            if (Devices.All(device => device.Id != args.Id))
            {
                Devices.Add(new RemoteAudioDevice(args));
                StatusChanged?.Invoke(this, EventArgs.Empty);
            }
        });
    }

    private void Watcher_Updated(DeviceWatcher sender, DeviceInformationUpdate args)
    {
        Enqueue(() =>
        {
            Devices.FirstOrDefault(device => device.Id == args.Id)?.Update(args);
            StatusChanged?.Invoke(this, EventArgs.Empty);
        });
    }

    private void Watcher_Removed(DeviceWatcher sender, DeviceInformationUpdate args)
    {
        Enqueue(() =>
        {
            var device = Devices.FirstOrDefault(item => item.Id == args.Id);
            if (device is not null)
            {
                Devices.Remove(device);
            }

            if (_connectedDeviceId == args.Id)
            {
                ReleaseConnection();
                SetStatus(ReceiverState.Idle, "手机已断开或离开蓝牙范围。");
            }
            else
            {
                StatusChanged?.Invoke(this, EventArgs.Empty);
            }
        });
    }

    private void Watcher_EnumerationCompleted(DeviceWatcher sender, object args)
    {
        Enqueue(() =>
        {
            if (State != ReceiverState.Searching)
            {
                return;
            }

            SetStatus(ReceiverState.Idle, Devices.Count == 0
                ? "未发现设备。请先在 Windows 蓝牙设置中配对安卓手机。"
                : $"发现 {Devices.Count} 台设备。请选择一台手机进行连接。");
        });
    }

    private void Watcher_Stopped(DeviceWatcher sender, object args)
    {
        if (!_isDisposed)
        {
            Enqueue(() => SetStatus(ReceiverState.Idle, "设备扫描已停止。"));
        }
    }

    private void Connection_StateChanged(AudioPlaybackConnection sender, object args)
    {
        Enqueue(() =>
        {
            if (sender != _connection)
            {
                return;
            }

            if (sender.State == AudioPlaybackConnectionState.Opened)
            {
                var name = ConnectedDeviceName ?? "手机";
                SetStatus(ReceiverState.Connected, $"已连接 {name}。现在可在手机上播放音乐。");
            }
            else if (State == ReceiverState.Connected)
            {
                ReleaseConnection();
                SetStatus(ReceiverState.Idle, "蓝牙音频连接已关闭。");
            }
        });
    }

    private void SetStatus(ReceiverState state, string message)
    {
        State = state;
        StatusMessage = message;
        StatusChanged?.Invoke(this, EventArgs.Empty);
    }

    private void Enqueue(Action action)
    {
        _dispatcherQueue.TryEnqueue(() => action());
    }
}
