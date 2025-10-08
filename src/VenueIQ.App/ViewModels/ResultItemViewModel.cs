using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using VenueIQ.Core.Models;
using VenueIQ.Core.Utils;
using System.Windows.Input;

namespace VenueIQ.App.ViewModels;

public class ResultItemViewModel : INotifyPropertyChanged
{
    public int Rank { get; set; }
    public double Score { get; set; }
    public double Lat { get; set; }
    public double Lng { get; set; }
    public double CI { get; set; }
    public double CoI { get; set; }
    public double AI { get; set; }
    public double DI { get; set; }
    public string? PrimaryBadgeKey { get; set; }
    public ObservableCollection<string> SupportingBadgeKeys { get; } = new();
    public ObservableCollection<string> RationaleKeys { get; } = new();

    // Factor badges descriptors
    public BadgeDescriptor? CompetitionBadge { get; set; }
    public BadgeDescriptor? ComplementsBadge { get; set; }
    public BadgeDescriptor? AccessibilityBadge { get; set; }
    public BadgeDescriptor? DemandBadge { get; set; }

    // Tooltip / details
    private bool _isTooltipOpen;
    public bool IsTooltipOpen
    {
        get => _isTooltipOpen;
        set
        {
            if (_isTooltipOpen != value)
            {
                _isTooltipOpen = value;
                OnPropertyChanged();
                if (value)
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                        Microsoft.Maui.Accessibility.SemanticScreenReader.Announce(VenueIQ.App.Helpers.LocalizationResourceManager.Instance["Tooltip_Opened"]))
                    ;
                }
            }
        }
    }
    public ICommand ToggleTooltipCommand { get; }

    public ResultItemViewModel()
    {
        ToggleTooltipCommand = new Command(() => IsTooltipOpen = !IsTooltipOpen);
    }

    public double? NearestAccessMeters { get; set; }
    public ObservableCollection<NearbyPoiViewModel> NearestComplements { get; } = new();
    public ObservableCollection<NearbyPoiViewModel> NearestCompetitors { get; } = new();

    public string AutomationId => $"Results.Item.{Rank}";

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? n = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
}

public class NearbyPoiViewModel
{
    public string? Name { get; set; }
    public string? Category { get; set; }
    public double DistanceMeters { get; set; }
    public string DistanceText => FormatDistance(DistanceMeters);

    private static string FormatDistance(double m)
    {
        if (m < 950) return string.Format(System.Globalization.CultureInfo.CurrentUICulture, "{0:0} m", m);
        return string.Format(System.Globalization.CultureInfo.CurrentUICulture, "{0:0.0} km", m / 1000.0);
    }
}
