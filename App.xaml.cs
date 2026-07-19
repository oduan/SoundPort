using Microsoft.UI.Xaml;
using SoundPort.Services;

namespace SoundPort;

public partial class App : Application
{
    private MainWindow? _window;
    private TrayIconService? _trayIcon;
    private AudioReceiverService? _receiver;
    private bool _isShuttingDown;

    public App()
    {
        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var settings = new SettingsService();
        var dispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
        _receiver = new AudioReceiverService(dispatcherQueue, settings);
        _window = new MainWindow(_receiver, settings);

        _trayIcon = new TrayIconService(
            _receiver,
            showWindow: _window.ShowFromTray,
            connect: () => _receiver.ConnectPreferredAsync(),
            disconnect: () => _receiver.DisconnectAsync(),
            exit: ShutdownAsync);

        _window.AttachTrayIcon(_trayIcon);
        _window.Activate();
        _receiver.StartWatching();
    }

    private async Task ShutdownAsync()
    {
        if (_isShuttingDown)
        {
            return;
        }

        _isShuttingDown = true;

        if (_receiver is not null)
        {
            await _receiver.DisposeAsync();
        }

        _trayIcon?.Dispose();

        if (_window is not null)
        {
            _window.AllowClose();
            _window.Close();
        }

        Exit();
    }
}
