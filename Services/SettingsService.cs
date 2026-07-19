using Windows.Storage;

namespace SoundPort.Services;

public sealed class SettingsService
{
    private const string PreferredDeviceKey = "PreferredDeviceId";
    private const string TrayHintKey = "HasShownTrayHint";
    private readonly ApplicationDataContainer _settings = ApplicationData.Current.LocalSettings;

    public string? PreferredDeviceId
    {
        get => _settings.Values[PreferredDeviceKey] as string;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                _settings.Values.Remove(PreferredDeviceKey);
            }
            else
            {
                _settings.Values[PreferredDeviceKey] = value;
            }
        }
    }

    public bool HasShownTrayHint
    {
        get => _settings.Values[TrayHintKey] as bool? ?? false;
        set => _settings.Values[TrayHintKey] = value;
    }
}
