using System.ComponentModel;
using Windows.Devices.Enumeration;

namespace SoundPort.Models;

public sealed class RemoteAudioDevice : INotifyPropertyChanged
{
    private bool _isConnected;

    internal RemoteAudioDevice(DeviceInformation information)
    {
        Information = information;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    internal DeviceInformation Information { get; }

    public string Id => Information.Id;

    public string Name => string.IsNullOrWhiteSpace(Information.Name)
        ? "未命名蓝牙设备"
        : Information.Name;

    public bool IsConnected
    {
        get => _isConnected;
        internal set
        {
            if (_isConnected == value)
            {
                return;
            }

            _isConnected = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsConnected)));
        }
    }

    internal void Update(DeviceInformationUpdate update)
    {
        Information.Update(update);
    }
}
