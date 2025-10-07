using System.ComponentModel;
using System.Runtime.CompilerServices;
using VenueIQ.App.Services;

namespace VenueIQ.App.ViewModels;

public class SettingsViewModel : INotifyPropertyChanged
{
    private readonly SettingsService _settings;
    private readonly LocalizationService _localization;

    public SettingsViewModel(SettingsService settings, LocalizationService localization)
    {
        _settings = settings;
        _localization = localization;
    }

    private string _language = "sr-Latn";
    public string Language
    {
        get => _language;
        set
        {
            if (_language == value) return;
            _language = value; OnPropertyChanged();
            _ = _settings.SetLanguageAsync(value);
            _localization.SetCulture(value);
        }
    }

    public async Task LoadAsync()
    {
        Language = await _settings.GetLanguageAsync().ConfigureAwait(false);
    }

    public string[] Languages { get; } = new[] { "sr-Latn", "en" };

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
