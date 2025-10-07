using VenueIQ.App.Helpers;
using VenueIQ.App.ViewModels;
using VenueIQ.App.Controls;

namespace VenueIQ.App.Views
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
            var vm = ServiceHost.GetRequiredService<MainViewModel>();
            BindingContext = vm;
            _ = vm.LoadAsync();
            Appearing += async (_, __) =>
            {
                var (apiKey, lang) = await vm.GetMapInitAsync();
                if (!string.IsNullOrWhiteSpace(apiKey))
                {
                    try
                    {
                        Map.MapReady += (_, ____) => MapLoadingOverlay.IsVisible = false;
                        Map.MapError += (_, reason) =>
                        {
                            MapLoadingOverlay.IsVisible = false;
                            // TODO: Hook telemetry/logging here
                            MainThread.BeginInvokeOnMainThread(() =>
                                SemanticScreenReader.Announce($"Map error: {reason}"));
                        };
                        await Map.InitializeAsync(apiKey, lang);
                    }
                    catch
                    {
                        MapLoadingOverlay.IsVisible = false;
                    }
                }
            };
        }
    }
}
