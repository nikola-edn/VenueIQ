using System.Globalization;
using Microsoft.Maui.Controls;
using VenueIQ.App.Helpers;

namespace VenueIQ.App.Utils;

public class ResKeyToTextConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string key && !string.IsNullOrWhiteSpace(key))
        {
            var norm = key.Replace('.', '_');
            return LocalizationResourceManager.Instance[norm];
        }
        return null;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
}
