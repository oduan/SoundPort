using Microsoft.UI.Xaml;
using SoundPort.Services;

namespace SoundPort;

public sealed partial class MainWindow : Window
{
    private readonly SettingsService _settings;
    private TrayIconService? _trayIcon;
    private bool _allowClose;

    public MainWindow(AudioReceiverService receiver, SettingsService settings)
    {
        _settings = settings;
        InitializeComponent();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        AppWindow.SetIcon("Assets/AppIcon.ico");
        AppWindow.Resize(new Windows.Graphics.SizeInt32(860, 680));
        AppWindow.Closing += OnAppWindowClosing;
        RootFrame.Content = new MainPage(receiver);
    }

    public void AttachTrayIcon(TrayIconService trayIcon)
    {
        _trayIcon = trayIcon;
    }

    public void AllowClose()
    {
        _allowClose = true;
    }

    public void ShowFromTray()
    {
        AppWindow.Show();
        Activate();
    }

    private void OnAppWindowClosing(
        Microsoft.UI.Windowing.AppWindow sender,
        Microsoft.UI.Windowing.AppWindowClosingEventArgs args)
    {
        if (_allowClose)
        {
            return;
        }

        args.Cancel = true;
        AppWindow.Hide();

        if (!_settings.HasShownTrayHint)
        {
            _settings.HasShownTrayHint = true;
            _trayIcon?.ShowInfo("SoundPort 仍在运行", "可通过系统托盘连接、断开或退出。");
        }
    }
}
