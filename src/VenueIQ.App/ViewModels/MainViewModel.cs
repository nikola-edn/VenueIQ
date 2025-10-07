using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using VenueIQ.App.Services;
using VenueIQ.App.Utils;
using System.Collections.ObjectModel;
using VenueIQ.Core.Models;

namespace VenueIQ.App.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private readonly SettingsService _settings;

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

    // Results
    public ObservableCollection<ResultItemViewModel> Results { get; } = new();
    private ResultItemViewModel? _selected;
    public ResultItemViewModel? Selected { get => _selected; set { if (_selected != value) { _selected = value; OnPropertyChanged(); } } }

    public void SetResults(IReadOnlyList<CellScore> cells, int topN = 10)
    {
        var sorted = cells.OrderByDescending(c => c.Score).Take(topN).ToList();
        Results.Clear();
        int rank = 1;
        foreach (var c in sorted)
        {
            var vm = new ResultItemViewModel
            {
                Rank = rank++,
                Score = Math.Round(c.Score, 3),
                Lat = c.Lat,
                Lng = c.Lng,
                CI = c.CI, CoI = c.CoI, AI = c.AI, DI = c.DI,
                PrimaryBadgeKey = c.PrimaryBadge
            };
            foreach (var b in c.SupportingBadges) vm.SupportingBadgeKeys.Add(b);
            foreach (var r in c.RationaleTokens) vm.RationaleKeys.Add(r);
            Results.Add(vm);
        }
        if (Results.Count > 0) Selected = Results[0];
    }
}
