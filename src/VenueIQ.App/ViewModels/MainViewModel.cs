using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using VenueIQ.App.Services;
using VenueIQ.App.Utils;
using System.Collections.ObjectModel;
using VenueIQ.Core.Models;
using VenueIQ.Core.Utils;

namespace VenueIQ.App.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private readonly SettingsService _settings;
    private readonly MapAnalysisService _analysis;

    public MainViewModel(SettingsService settings, MapAnalysisService analysis)
    {
        _settings = settings;
        _analysis = analysis;
        ResetDefaultsCommand = new AsyncCommand(ResetDefaultsAsync);
        AutoBalanceCommand = new AsyncCommand(AutoBalanceAsync);
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

    // Advanced weights panel state
    private bool _isDraggingWeights;
    public bool IsDraggingWeights
    {
        get => _isDraggingWeights;
        private set
        {
            if (_isDraggingWeights != value)
            {
                _isDraggingWeights = value;
                OnPropertyChanged();
                IsAnalyzeEnabled = !(value || IsUpdating);
            }
        }
    }
    private bool _isAnalyzeEnabled = true;
    public bool IsAnalyzeEnabled { get => _isAnalyzeEnabled; private set { if (_isAnalyzeEnabled != value) { _isAnalyzeEnabled = value; OnPropertyChanged(); } } }

    // Percent-facing properties for UI (0-100)
    private double _wComplementsPercent = 35, _wAccessibilityPercent = 25, _wDemandPercent = 25, _wCompetitionPercent = 35;
    public double WComplementsPercent { get => _wComplementsPercent; set { if (Math.Abs(_wComplementsPercent - value) > 0.0001) { _wComplementsPercent = value; OnPropertyChanged(); RecomputeNormalizedWeights(); ScheduleRecompute(); } } }
    public double WAccessibilityPercent { get => _wAccessibilityPercent; set { if (Math.Abs(_wAccessibilityPercent - value) > 0.0001) { _wAccessibilityPercent = value; OnPropertyChanged(); RecomputeNormalizedWeights(); ScheduleRecompute(); } } }
    public double WDemandPercent { get => _wDemandPercent; set { if (Math.Abs(_wDemandPercent - value) > 0.0001) { _wDemandPercent = value; OnPropertyChanged(); RecomputeNormalizedWeights(); ScheduleRecompute(); } } }
    public double WCompetitionPercent { get => _wCompetitionPercent; set { if (Math.Abs(_wCompetitionPercent - value) > 0.0001) { _wCompetitionPercent = value; OnPropertyChanged(); RecomputeNormalizedWeights(); ScheduleRecompute(); } } }

    private void RecomputeNormalizedWeights()
    {
        var (c, a, d, q) = WeightsHelper.FromPercentages(WComplementsPercent, WAccessibilityPercent, WDemandPercent, WCompetitionPercent);
        WComplements = c; WAccessibility = a; WDemand = d; WCompetition = q;
    }

    public void SetWeightsDragging(bool dragging) => IsDraggingWeights = dragging;

    public ICommand ResetDefaultsCommand { get; }
    public ICommand AutoBalanceCommand { get; }

    private async Task ResetDefaultsAsync()
    {
        await _settings.ResetToDefaultsAsync().ConfigureAwait(false);
        RadiusKm = 2.0;
        // Reset UI-facing percents then recompute normalized decimals
        WComplementsPercent = 35; WAccessibilityPercent = 25; WDemandPercent = 25; WCompetitionPercent = 35;
        RecomputeNormalizedWeights();
        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            SemanticScreenReader.Announce(Helpers.LocalizationResourceManager.Instance["Toast_PrefsReset"]);
        });
    }

    private async Task AutoBalanceAsync()
    {
        // Reset to default percentages and normalize
        WComplementsPercent = 35; WAccessibilityPercent = 25; WDemandPercent = 25; WCompetitionPercent = 35;
        RecomputeNormalizedWeights();
        try { Microsoft.Maui.Devices.HapticFeedback.Default.Perform(Microsoft.Maui.Devices.HapticFeedbackType.Click); } catch { /* ignore */ }
        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            SemanticScreenReader.Announce(Helpers.LocalizationResourceManager.Instance["Weights_AutoBalanced"]);
        });
    }

    public async Task LoadAsync()
    {
        RadiusKm = await _settings.GetRadiusKmAsync().ConfigureAwait(false);
        var w = await _settings.GetWeightsAsync().ConfigureAwait(false);
        WComplements = w.complements; WAccessibility = w.accessibility; WDemand = w.demand; WCompetition = w.competition;
        // Initialize percent UI from persisted decimals
        var pos = WComplements + WAccessibility + WDemand;
        if (pos <= 1e-9)
        {
            WComplementsPercent = 35; WAccessibilityPercent = 25; WDemandPercent = 25; WCompetitionPercent = WCompetition / 0.35 * 100.0;
        }
        else
        {
            WComplementsPercent = Math.Round(WComplements / pos * 100.0);
            WAccessibilityPercent = Math.Round(WAccessibility / pos * 100.0);
            WDemandPercent = Math.Round(WDemand / pos * 100.0);
            WCompetitionPercent = Math.Round(WCompetition * 100.0);
        }
    }

    // Live recompute infrastructure
    private CancellationTokenSource? _debounceCts;
    private CancellationTokenSource? _recomputeCts;
    private const int DebounceMs = 200; // within 150-250ms per AC

    private bool _isUpdating;
    public bool IsUpdating
    {
        get => _isUpdating;
        private set
        {
            if (_isUpdating != value)
            {
                _isUpdating = value;
                OnPropertyChanged();
                IsAnalyzeEnabled = !(value || IsDraggingWeights);
            }
        }
    }

    private string? _statusMessageKey;
    public string? StatusMessageKey { get => _statusMessageKey; private set { if (_statusMessageKey != value) { _statusMessageKey = value; OnPropertyChanged(); } } }

    public event EventHandler<IReadOnlyList<CellScore>>? WeightsRecomputed;

    private void ScheduleRecompute()
    {
        _debounceCts?.Cancel();
        _debounceCts = new CancellationTokenSource();
        var token = _debounceCts.Token;
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(DebounceMs, token).ConfigureAwait(false);
                await RecomputeAsync(token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) { }
        });
    }

    private async Task RecomputeAsync(CancellationToken token)
    {
        _recomputeCts?.Cancel();
        _recomputeCts = CancellationTokenSource.CreateLinkedTokenSource(token);
        var ct = _recomputeCts.Token;
        try
        {
            IsUpdating = true;
            StatusMessageKey = "Weights_Updating";
            var weights = new Weights(WComplements, WAccessibility, WDemand, WCompetition);
            if (!await _analysis.HasCachedAsync().ConfigureAwait(false))
            {
                StatusMessageKey = "Recompute_PromptRerun";
                return;
            }
            var res = await _analysis.RecomputeAsync(weights, ct).ConfigureAwait(false);
            if (res.CellDetails is { Count: > 0 })
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    SetResults(res.CellDetails);
                    WeightsRecomputed?.Invoke(this, res.CellDetails);
                });
                StatusMessageKey = null;
            }
            else
            {
                await MainThread.InvokeOnMainThreadAsync(() => Results.Clear());
                StatusMessageKey = "Recompute_NoCells";
            }
        }
        catch (OperationCanceledException)
        {
            // swallow
        }
        catch
        {
            StatusMessageKey = "Recompute_Error";
        }
        finally
        {
            IsUpdating = false;
        }
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
        // Preserve previous selection if possible
        var prev = Selected;
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
        if (Results.Count > 0)
        {
            if (prev is not null)
            {
                // Try to find the closest match by lat/lng
                var match = Results.FirstOrDefault(r => Math.Abs(r.Lat - prev.Lat) < 1e-6 && Math.Abs(r.Lng - prev.Lng) < 1e-6);
                Selected = match ?? Results[0];
            }
            else
            {
                Selected = Results[0];
            }
        }
    }
}
