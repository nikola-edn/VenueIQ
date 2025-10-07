using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using VenueIQ.App.Resources.Strings;

namespace VenueIQ.App.Helpers;

public class LocalizationResourceManager : INotifyPropertyChanged
{
    public static LocalizationResourceManager Instance { get; } = new();

    public event PropertyChangedEventHandler? PropertyChanged;

    public string this[string text]
        => AppResources.ResourceManager.GetString(text, AppResources.Culture) ?? text;

    public void SetCulture(string cultureName)
    {
        var culture = new CultureInfo(cultureName);
        AppResources.Culture = culture;
        OnPropertyChanged("Item[]");
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

