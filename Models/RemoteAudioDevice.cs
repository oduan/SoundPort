using Windows.Devices.Enumeration;

namespace SoundPort.Models;

public sealed class RemoteAudioDevice
{
    internal RemoteAudioDevice(DeviceInformation information)
    {
        Information = information;
    }

    internal DeviceInformation Information { get; }

    public string Id => Information.Id;

    public string Name => string.IsNullOrWhiteSpace(Information.Name)
        ? "未命名蓝牙设备"
        : Information.Name;

    internal void Update(DeviceInformationUpdate update)
    {
        Information.Update(update);
    }
}
