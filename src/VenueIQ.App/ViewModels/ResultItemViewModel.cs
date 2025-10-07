using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using VenueIQ.Core.Models;

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

    public string AutomationId => $"Results.Item.{Rank}";

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? n = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
}

