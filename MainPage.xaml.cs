using Microsoft.UI.Xaml.Controls;
using SoundPort.Models;
using SoundPort.Services;
using Windows.System;

namespace SoundPort;

public sealed partial class MainPage : Page
{
    public AudioReceiverService Receiver { get; }

    public MainPage(AudioReceiverService receiver)
    {
        Receiver = receiver;
        InitializeComponent();
        Receiver.StatusChanged += Receiver_StatusChanged;
        Unloaded += MainPage_Unloaded;
        UpdateStatus();
    }

    private async void ConnectButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (DeviceList.SelectedItem is RemoteAudioDevice device)
        {
            await Receiver.ConnectAsync(device);
        }
    }

    private async void DisconnectButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        await Receiver.DisconnectAsync();
    }

    private void RefreshButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        Receiver.RestartWatching();
    }

    private async void OpenBluetoothSettings_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        await Launcher.LaunchUriAsync(new Uri("ms-settings:bluetooth"));
    }

    private void DeviceList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DeviceList.SelectedItem is RemoteAudioDevice device)
        {
            Receiver.SetPreferredDevice(device.Id);
        }

        UpdateButtons();
    }

    private void Receiver_StatusChanged(object? sender, EventArgs e)
    {
        UpdateStatus();
    }

    private void UpdateStatus()
    {
        EmptyState.Visibility = Receiver.Devices.Count == 0
            ? Microsoft.UI.Xaml.Visibility.Visible
            : Microsoft.UI.Xaml.Visibility.Collapsed;

        StatusBar.Title = Receiver.State switch
        {
            ReceiverState.Searching => "正在搜索",
            ReceiverState.Enabling => "正在启用蓝牙音频",
            ReceiverState.Connecting => "正在连接",
            ReceiverState.Connected => "已连接",
            ReceiverState.Faulted => "连接失败",
            _ => "等待连接"
        };

        StatusBar.Message = Receiver.StatusMessage;
        StatusBar.Severity = Receiver.State switch
        {
            ReceiverState.Connected => InfoBarSeverity.Success,
            ReceiverState.Faulted => InfoBarSeverity.Error,
            ReceiverState.Enabling or ReceiverState.Connecting or ReceiverState.Searching => InfoBarSeverity.Informational,
            _ => InfoBarSeverity.Warning
        };

        if (DeviceList.SelectedItem is null && Receiver.PreferredDevice is not null)
        {
            DeviceList.SelectedItem = Receiver.PreferredDevice;
        }

        UpdateButtons();
    }

    private void UpdateButtons()
    {
        var isBusy = Receiver.State is ReceiverState.Enabling or ReceiverState.Connecting;
        ConnectButton.IsEnabled =
            DeviceList.SelectedItem is RemoteAudioDevice &&
            Receiver.State is not ReceiverState.Connected &&
            !isBusy;
        DisconnectButton.IsEnabled =
            Receiver.State is ReceiverState.Connected or ReceiverState.Enabling or ReceiverState.Connecting;
    }

    private void MainPage_Unloaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        Receiver.StatusChanged -= Receiver_StatusChanged;
    }
}
