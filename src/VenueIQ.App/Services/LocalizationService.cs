using System.Globalization;
using VenueIQ.App.Helpers;

namespace VenueIQ.App.Services;

public class LocalizationService
{
    public void SetCulture(string cultureName)
    {
        var culture = new CultureInfo(cultureName);
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        LocalizationResourceManager.Instance.SetCulture(cultureName);
    }
}
