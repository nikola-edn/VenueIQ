using VenueIQ.App.Helpers;
using VenueIQ.App.ViewModels;
using VenueIQ.App.Controls;
using VenueIQ.App.Services;
using VenueIQ.App.Utils;
using VenueIQ.Core.Models;

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
                        Map.HeatmapRendered += (_, ____) => MainThread.BeginInvokeOnMainThread(() => SemanticScreenReader.Announce("Heatmap updated"));
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

            AnalyzeButton.Clicked += async (_, __) => await RunAnalysisAsync();
            ResultsList_SelectionSetup();
        }

        private CancellationTokenSource? _renderCts;
        private async Task RunAnalysisAsync()
        {
            try
            {
                _renderCts?.Cancel();
                _renderCts = new CancellationTokenSource();
                MapLoadingOverlay.IsVisible = true;
                var vm = (MainViewModel)BindingContext;
                var (apiKey, lang) = await vm.GetMapInitAsync();
                // BusinessType selection TBD; default to Coffee for MVP
                var input = new AnalysisInput(BusinessType.Coffee, 44.787, 20.449, vm.RadiusKm, lang);
                var weights = new Weights(vm.WComplements, vm.WAccessibility, vm.WDemand, vm.WCompetition);
                var analysis = ServiceHost.GetRequiredService<MapAnalysisService>();
                var res = await analysis.AnalyzeAsync(input, weights, _renderCts.Token);
                if (res.CellDetails is { Count: > 0 })
                {
                    var geo = GeoJsonHelper.BuildFeatureCollection(res.CellDetails);
                    await Map.UpdateHeatmapAsync(geo, _renderCts.Token);
                    vm.SetResults(res.CellDetails);
                }
                else
                {
                    await Map.ClearHeatmapAsync(_renderCts.Token);
                    vm.Results.Clear();
                }
            }
            catch (OperationCanceledException)
            {
                // ignore
            }
            catch
            {
                // TODO: telemetry
            }
            finally
            {
                MapLoadingOverlay.IsVisible = false;
            }
        }

        private void ResultsList_SelectionSetup()
        {
            // Hook selection events to center the map
            // Handle via BindingContext changes
            this.PropertyChanged += async (s, e) =>
            {
                if (e.PropertyName == nameof(MainViewModel.Selected))
                {
                    var vm = (MainViewModel)BindingContext;
                    var sel = vm.Selected;
                    if (sel is not null)
                    {
                        await Map.CenterOnAsync(sel.Lat, sel.Lng, sel.Score);
                    }
                }
            };
        }
    }
}
