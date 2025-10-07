using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using VenueIQ.App.Services;
using VenueIQ.App.Utils;

namespace VenueIQ.App.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private readonly SettingsService _settings;
    private readonly PoiSearchService _poi; // reserved for future use

    public MainViewModel(SettingsService settings)
    {
        _settings = settings;
        ResetDefaultsCommand = new AsyncCommand(ResetDefaultsAsync);
    }

    private double _radiusKm = 2.0;
    public double RadiusKm
    {
        get => _radiusKm;
        set { if (Math.Abs(_radiusKm - value) > 0.0001) { _radiusKm = value; OnPropertyChanged(); _ = _settings.SetRadiusKmAsync(value); } }
    }

    // Future sliders (persisted now per story 1.3)
    private double _wComplements = 0.35, _wAccessibility = 0.25, _wDemand = 0.25, _wCompetition = 0.35;
    public double WComplements { get => _wComplements; set { if (_wComplements != value) { _wComplements = value; OnPropertyChanged(); PersistWeights(); } } }
    public double WAccessibility { get => _wAccessibility; set { if (_wAccessibility != value) { _wAccessibility = value; OnPropertyChanged(); PersistWeights(); } } }
    public double WDemand { get => _wDemand; set { if (_wDemand != value) { _wDemand = value; OnPropertyChanged(); PersistWeights(); } } }
    public double WCompetition { get => _wCompetition; set { if (_wCompetition != value) { _wCompetition = value; OnPropertyChanged(); PersistWeights(); } } }

    private void PersistWeights() => _ = _settings.SetWeightsAsync(WComplements, WAccessibility, WDemand, WCompetition);

    public ICommand ResetDefaultsCommand { get; }

    private async Task ResetDefaultsAsync()
    {
        await _settings.ResetToDefaultsAsync().ConfigureAwait(false);
        RadiusKm = 2.0;
        WComplements = 0.35; WAccessibility = 0.25; WDemand = 0.25; WCompetition = 0.35;
        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            SemanticScreenReader.Announce(Helpers.LocalizationResourceManager.Instance["Toast_PrefsReset"]);
        });
    }

    public async Task LoadAsync()
    {
        RadiusKm = await _settings.GetRadiusKmAsync().ConfigureAwait(false);
        var w = await _settings.GetWeightsAsync().ConfigureAwait(false);
        WComplements = w.complements; WAccessibility = w.accessibility; WDemand = w.demand; WCompetition = w.competition;
    }

    public async Task<(string apiKey, string language)> GetMapInitAsync()
    {
        var key = await _settings.GetApiKeyAsync().ConfigureAwait(false) ?? string.Empty;
        var lang = await _settings.GetLanguageAsync().ConfigureAwait(false);
        return (key, lang);
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
